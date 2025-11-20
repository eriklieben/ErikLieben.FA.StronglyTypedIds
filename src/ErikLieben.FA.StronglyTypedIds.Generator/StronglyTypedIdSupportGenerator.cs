using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ErikLieben.FA.StronglyTypedIds.Generator;

[Generator]
public class StronglyTypedIdSupportGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var recordDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        var compilationAndRecords = context.CompilationProvider.Combine(recordDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndRecords,
            static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is RecordDeclarationSyntax { AttributeLists.Count: > 0 };

    static RecordDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var recordDeclaration = (RecordDeclarationSyntax)context.Node;

        foreach (AttributeListSyntax attributeList in recordDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                    continue;

                string fullName = attributeSymbol.ContainingType.ToDisplayString();
                if (fullName == "ErikLieben.FA.StronglyTypedIds.GenerateStronglyTypedIdSupportAttribute" || attributeSymbol.ContainingType.Name == "GenerateStronglyTypedIdSupportAttribute")
                {
                    return recordDeclaration;
                }
            }
        }

        return null;
    }

    static void Execute(Compilation compilation, ImmutableArray<RecordDeclarationSyntax?> records, SourceProductionContext context)
    {
        if (records.IsDefaultOrEmpty)
            return;

        var distinctRecords = records.Where(static m => m is not null).Distinct();
        var attributeSymbol = compilation.GetTypeByMetadataName("ErikLieben.FA.StronglyTypedIds.GenerateStronglyTypedIdSupportAttribute");

        if (attributeSymbol == null)
        {
            // Generate the attribute first
            context.AddSource("GenerateStronglyTypedIdSupportAttribute.g.cs",
                SourceText.From(AttributeSource, Encoding.UTF8));
            return;
        }

        foreach (var recordDeclaration in distinctRecords)
        {
            if (recordDeclaration == null)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(recordDeclaration.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(recordDeclaration) is not INamedTypeSymbol recordSymbol)
                continue;

            var attribute = recordSymbol.GetAttributes()
                .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));

            if (attribute != null)
            {
                var recordInfo = ExtractRecordInfo(recordSymbol, attribute);
                if (recordInfo != null)
                {
                    // Generate the support code
                    var supportSource = GenerateSupportCode(recordInfo);
                    context.AddSource($"{recordInfo.Name}.Support.g.cs",
                        SourceText.From(supportSource, Encoding.UTF8));

                    // Generate the partial record with attributes
                    var partialSource = GeneratePartialRecord(recordInfo);
                    context.AddSource($"{recordInfo.Name}.Partial.g.cs",
                        SourceText.From(partialSource, Encoding.UTF8));
                }
            }
        }
    }

    private static RecordInfo? ExtractRecordInfo(INamedTypeSymbol recordSymbol, AttributeData attribute)
    {
        // Get the Value property to determine the underlying type
        var valueProperty = recordSymbol.GetMembers("Value").OfType<IPropertySymbol>().FirstOrDefault();

        if (valueProperty == null)
        {
            var currentType = recordSymbol.BaseType;
            while (currentType != null && valueProperty == null)
            {
                valueProperty = currentType.GetMembers("Value")
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();
                currentType = currentType.BaseType;
            }
        }

        if (valueProperty == null)
            return null;

        var underlyingType = valueProperty.Type.ToDisplayString();

        var recordInfo = new RecordInfo
        {
            Name = recordSymbol.Name,
            Namespace = recordSymbol.ContainingNamespace?.ToDisplayString(),
            UnderlyingType = underlyingType,
            GenerateJsonConverter = GetBooleanAttributeValue(attribute, "GenerateJsonConverter", true),
            GenerateTypeConverter = GetBooleanAttributeValue(attribute, "GenerateTypeConverter", true),
            GenerateParseMethod = GetBooleanAttributeValue(attribute, "GenerateParseMethod", true),
            GenerateTryParseMethod = GetBooleanAttributeValue(attribute, "GenerateTryParseMethod", true),
            GenerateComparisons = GetBooleanAttributeValue(attribute, "GenerateComparisons", true),
            GenerateNewMethod = GetBooleanAttributeValue(attribute, "GenerateNewMethod", true),
            GenerateExtensions = GetBooleanAttributeValue(attribute, "GenerateExtensions", true)
        };

        return recordInfo;
    }

    private static bool GetBooleanAttributeValue(AttributeData attribute, string propertyName, bool defaultValue)
    {
        var namedArg = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == propertyName);
        return namedArg.Value.Value as bool? ?? defaultValue;
    }

    private static string GeneratePartialRecord(RecordInfo recordInfo)
    {
        var sb = new StringBuilder();

        // Add file header
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        // Add usings conditionally
        if ((recordInfo.GenerateParseMethod || recordInfo.GenerateTryParseMethod) && SupportsFormatProvider(recordInfo.UnderlyingType))
        {
            sb.AppendLine("using System;");
        }
        sb.AppendLine("using System.Diagnostics;");
        if (recordInfo.GenerateComparisons && IsComparable(recordInfo.UnderlyingType))
        {
            sb.AppendLine("using System.Collections.Generic;");
        }
        if (recordInfo.GenerateTypeConverter)
        {
            sb.AppendLine("using System.ComponentModel;");
        }
        if (recordInfo.GenerateJsonConverter)
        {
            sb.AppendLine("using System.Text.Json.Serialization;");
        }
        sb.AppendLine();

        // Add namespace if needed
        if (!string.IsNullOrEmpty(recordInfo.Namespace) && recordInfo.Namespace != "<global namespace>")
        {
            sb.AppendLine($"namespace {recordInfo.Namespace};");
            sb.AppendLine();
        }

        // Generate partial record with attributes
        var attributes = new List<string>();

        // Always add DebuggerDisplay for better debugging experience
        attributes.Add($"[DebuggerDisplay(\"{{Value}}\")]");

        if (recordInfo.GenerateJsonConverter)
        {
            attributes.Add($"[JsonConverter(typeof({recordInfo.Name}JsonConverter))]");
        }

        if (recordInfo.GenerateTypeConverter)
        {
            attributes.Add($"[TypeConverter(typeof({recordInfo.Name}TypeConverter))]");
        }

        foreach (var attr in attributes)
        {
            sb.AppendLine(attr);
        }

        // Add XML documentation for the partial record
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Partial record extension for {recordInfo.Name} with generated support methods and attributes");
        sb.AppendLine("/// </summary>");

        // Implement IComparable<T> when comparisons are enabled
        if (recordInfo.GenerateComparisons && IsComparable(recordInfo.UnderlyingType))
        {
            sb.AppendLine($"public partial record {recordInfo.Name} : IComparable<{recordInfo.Name}>");
        }
        else
        {
            sb.AppendLine($"public partial record {recordInfo.Name}");
        }
        sb.AppendLine("{");

        // Add static methods if requested
        if (recordInfo.GenerateParseMethod)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Creates a new {recordInfo.Name} from a string value");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"value\">The string value to parse</param>");
            sb.AppendLine($"    /// <returns>A new {recordInfo.Name} instance</returns>");
            sb.AppendLine($"    public static {recordInfo.Name} From(string value)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var parsed = {GetParseExpression(recordInfo.UnderlyingType, "value")};");
            sb.AppendLine($"        return new {recordInfo.Name}(parsed);");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Add format provider overload for types that support it
            if (SupportsFormatProvider(recordInfo.UnderlyingType))
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Creates a new {recordInfo.Name} from a string value using the specified format provider");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    /// <param name=\"value\">The string value to parse</param>");
                sb.AppendLine("    /// <param name=\"provider\">An object that provides culture-specific formatting information</param>");
                sb.AppendLine($"    /// <returns>A new {recordInfo.Name} instance</returns>");
                sb.AppendLine($"    public static {recordInfo.Name} From(string value, IFormatProvider? provider)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var parsed = {GetParseExpressionWithProvider(recordInfo.UnderlyingType, "value", "provider")};");
                sb.AppendLine($"        return new {recordInfo.Name}(parsed);");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        if (recordInfo.GenerateTryParseMethod)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Tries to parse a string value into a {recordInfo.Name}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"value\">The string value to parse</param>");
            sb.AppendLine($"    /// <param name=\"result\">The parsed {recordInfo.Name} if successful, null otherwise</param>");
            sb.AppendLine("    /// <returns>True if parsing was successful, false otherwise</returns>");
            sb.AppendLine($"    public static bool TryParse(string? value, out {recordInfo.Name}? result)");
            sb.AppendLine("    {");
            sb.AppendLine("        result = null;");
            sb.AppendLine("        if (string.IsNullOrEmpty(value)) return false;");
            sb.AppendLine();

            var underlyingType = GetCSharpTypeName(recordInfo.UnderlyingType);
            if (underlyingType == "string")
            {
                sb.AppendLine($"        result = new {recordInfo.Name}(value);");
                sb.AppendLine("        return true;");
            }
            else
            {
                sb.AppendLine($"        if ({GetTryParseExpression(recordInfo.UnderlyingType, "value", "parsedValue")})");
                sb.AppendLine("        {");
                sb.AppendLine($"            result = new {recordInfo.Name}(parsedValue);");
                sb.AppendLine("            return true;");
                sb.AppendLine("        }");
                sb.AppendLine("        return false;");
            }

            sb.AppendLine("    }");
            sb.AppendLine();

            // Add format provider overload for types that support it
            if (SupportsFormatProvider(recordInfo.UnderlyingType))
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Tries to parse a string value into a {recordInfo.Name} using the specified format provider");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine("    /// <param name=\"value\">The string value to parse</param>");
                sb.AppendLine("    /// <param name=\"provider\">An object that provides culture-specific formatting information</param>");
                sb.AppendLine($"    /// <param name=\"result\">The parsed {recordInfo.Name} if successful, null otherwise</param>");
                sb.AppendLine("    /// <returns>True if parsing was successful, false otherwise</returns>");
                sb.AppendLine($"    public static bool TryParse(string? value, IFormatProvider? provider, out {recordInfo.Name}? result)");
                sb.AppendLine("    {");
                sb.AppendLine("        result = null;");
                sb.AppendLine("        if (string.IsNullOrEmpty(value)) return false;");
                sb.AppendLine();
                sb.AppendLine($"        if ({GetTryParseExpressionWithProvider(recordInfo.UnderlyingType, "value", "provider", "parsedValue")})");
                sb.AppendLine("        {");
                sb.AppendLine($"            result = new {recordInfo.Name}(parsedValue);");
                sb.AppendLine("            return true;");
                sb.AppendLine("        }");
                sb.AppendLine("        return false;");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        if (recordInfo.GenerateNewMethod)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Creates a new {recordInfo.Name} with a generated value");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    /// <returns>A new {recordInfo.Name} instance with a generated value</returns>");
            sb.AppendLine($"    public static {recordInfo.Name} New() => new({GetNewValueExpression(recordInfo.UnderlyingType)});");
            sb.AppendLine();
        }

        // Add Empty static property for types that have an empty value
        if (HasEmptyValue(recordInfo.UnderlyingType))
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Gets an empty {recordInfo.Name} instance");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static {recordInfo.Name} Empty {{ get; }} = new({GetEmptyValue(recordInfo.UnderlyingType)});");
            sb.AppendLine();
        }

        // Override ToString to return just the value
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Returns the string representation of the underlying value");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override string ToString() => Value.ToString() ?? string.Empty;");
        sb.AppendLine();

        // Add implicit conversion from underlying type
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Implicitly converts from {GetCSharpTypeName(recordInfo.UnderlyingType)} to {recordInfo.Name}");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static implicit operator {recordInfo.Name}({GetCSharpTypeName(recordInfo.UnderlyingType)} value) => new(value);");
        sb.AppendLine();

        // Add explicit conversion to underlying type
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Explicitly converts from {recordInfo.Name} to {GetCSharpTypeName(recordInfo.UnderlyingType)}");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static explicit operator {GetCSharpTypeName(recordInfo.UnderlyingType)}({recordInfo.Name} value) => value.Value;");
        sb.AppendLine();

        // Generate comparison operators if requested
        if (recordInfo.GenerateComparisons && IsComparable(recordInfo.UnderlyingType))
        {
            // Implement IComparable<T>.CompareTo
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Compares this instance to another instance");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    /// <param name=\"other\">The other {recordInfo.Name} to compare to</param>");
            sb.AppendLine("    /// <returns>A value indicating the relative order of the objects being compared</returns>");
            sb.AppendLine($"    public int CompareTo({recordInfo.Name}? other)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (other is null) return 1;");
            sb.AppendLine($"        return Comparer<{GetCSharpTypeName(recordInfo.UnderlyingType)}>.Default.Compare(Value, other.Value);");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Less than operator");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static bool operator <({recordInfo.Name} left, {recordInfo.Name} right)");
            sb.AppendLine($"        => Comparer<{GetCSharpTypeName(recordInfo.UnderlyingType)}>.Default.Compare(left.Value, right.Value) < 0;");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Less than or equal operator");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static bool operator <=({recordInfo.Name} left, {recordInfo.Name} right)");
            sb.AppendLine($"        => Comparer<{GetCSharpTypeName(recordInfo.UnderlyingType)}>.Default.Compare(left.Value, right.Value) <= 0;");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Greater than operator");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static bool operator >({recordInfo.Name} left, {recordInfo.Name} right)");
            sb.AppendLine($"        => Comparer<{GetCSharpTypeName(recordInfo.UnderlyingType)}>.Default.Compare(left.Value, right.Value) > 0;");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Greater than or equal operator");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static bool operator >=({recordInfo.Name} left, {recordInfo.Name} right)");
            sb.AppendLine($"        => Comparer<{GetCSharpTypeName(recordInfo.UnderlyingType)}>.Default.Compare(left.Value, right.Value) >= 0;");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateSupportCode(RecordInfo recordInfo)
    {
        var sb = new StringBuilder();

        // Add file header
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        // Add usings conditionally based on generated features
        if (recordInfo.GenerateJsonConverter || recordInfo.GenerateTypeConverter || recordInfo.GenerateExtensions)
        {
            sb.AppendLine("using System;");
        }

        if (recordInfo.GenerateExtensions)
        {
            sb.AppendLine("using System.Collections.Generic;");
        }

        if (recordInfo.GenerateTypeConverter)
        {
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine("using System.Globalization;");
        }

        if (recordInfo.GenerateExtensions)
        {
            sb.AppendLine("using System.Linq;");
        }

        if (recordInfo.GenerateJsonConverter)
        {
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Text.Json.Serialization;");
        }

        if (recordInfo.GenerateJsonConverter || recordInfo.GenerateTypeConverter || recordInfo.GenerateExtensions)
        {
            sb.AppendLine();
        }

        // Add namespace if needed
        if (!string.IsNullOrEmpty(recordInfo.Namespace) && recordInfo.Namespace != "<global namespace>")
        {
            sb.AppendLine($"namespace {recordInfo.Namespace};");
            sb.AppendLine();
        }

        // Generate JSON converter
        if (recordInfo.GenerateJsonConverter)
        {
            GenerateJsonConverter(sb, recordInfo);
        }

        // Generate type converter
        if (recordInfo.GenerateTypeConverter)
        {
            GenerateTypeConverter(sb, recordInfo);
        }

        // Generate extension methods
        if (recordInfo.GenerateExtensions)
        {
            GenerateExtensionMethods(sb, recordInfo);
        }

        // Generate TryConvert helper if needed for unsupported types
        if (NeedsTryConvertHelper(recordInfo))
        {
            GenerateTryConvertHelper(sb);
        }

        return sb.ToString();
    }

    private static void GenerateJsonConverter(StringBuilder sb, RecordInfo recordInfo)
    {
        var underlyingType = GetCSharpTypeName(recordInfo.UnderlyingType);

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// JSON converter for {recordInfo.Name}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {recordInfo.Name}JsonConverter : JsonConverter<{recordInfo.Name}>");
        sb.AppendLine("{");
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public override {recordInfo.Name} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var value = {GetJsonReadExpression(underlyingType)};");
        sb.AppendLine($"        return new {recordInfo.Name}(value);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public override void Write(Utf8JsonWriter writer, {recordInfo.Name} value, JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine($"        {GetJsonWriteExpression(underlyingType, "value.Value")};");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public override {recordInfo.Name} ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var stringValue = reader.GetString();");
        sb.AppendLine($"        return stringValue != null ? {recordInfo.Name}.From(stringValue) : throw new JsonException(\"Property name cannot be null\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public override void WriteAsPropertyName(Utf8JsonWriter writer, {recordInfo.Name} value, JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        writer.WritePropertyName(value.Value.ToString());");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateTypeConverter(StringBuilder sb, RecordInfo recordInfo)
    {
        var underlyingType = GetCSharpTypeName(recordInfo.UnderlyingType);

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Type converter for {recordInfo.Name}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {recordInfo.Name}TypeConverter : TypeConverter");
        sb.AppendLine("{");
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)");
        sb.AppendLine($"        => sourceType == typeof(string) || sourceType == typeof({underlyingType}) || base.CanConvertFrom(context, sourceType);");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)");
        sb.AppendLine("        => value switch");
        sb.AppendLine("        {");
        sb.AppendLine($"            string s when {recordInfo.Name}.TryParse(s, out var result) => result,");
        sb.AppendLine($"            {underlyingType} v => new {recordInfo.Name}(v),");
        sb.AppendLine("            _ => base.ConvertFrom(context, culture, value)");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)");
        sb.AppendLine($"        => destinationType == typeof(string) || destinationType == typeof({underlyingType}) || base.CanConvertTo(context, destinationType);");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (value is {recordInfo.Name} id)");
        sb.AppendLine("        {");
        sb.AppendLine("            return destinationType == typeof(string) ? id.ToString() :");
        sb.AppendLine($"                   destinationType == typeof({underlyingType}) ? id.Value :");
        sb.AppendLine("                   base.ConvertTo(context, culture, value, destinationType);");
        sb.AppendLine("        }");
        sb.AppendLine("        return base.ConvertTo(context, culture, value, destinationType);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateExtensionMethods(StringBuilder sb, RecordInfo recordInfo)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Extension methods for {recordInfo.Name}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {recordInfo.Name}Extensions");
        sb.AppendLine("{");

        // IsEmpty extension for applicable types
        if (HasEmptyValue(recordInfo.UnderlyingType))
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Determines whether the {recordInfo.Name} is empty");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static bool IsEmpty(this {recordInfo.Name} id) => id.Value.Equals({GetEmptyValue(recordInfo.UnderlyingType)});");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Determines whether the {recordInfo.Name} is not empty");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public static bool IsNotEmpty(this {recordInfo.Name} id) => !id.IsEmpty();");
            sb.AppendLine();
        }

        // Collection extensions
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Converts a collection of strongly typed IDs to their underlying values");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static IEnumerable<{GetCSharpTypeName(recordInfo.UnderlyingType)}> ToValues(this IEnumerable<{recordInfo.Name}> ids)");
        sb.AppendLine("        => ids.Select(id => id.Value);");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Converts a collection of strongly typed IDs to a HashSet of their underlying values");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static HashSet<{GetCSharpTypeName(recordInfo.UnderlyingType)}> ToValueSet(this IEnumerable<{recordInfo.Name}> ids)");
        sb.AppendLine("        => new(ids.Select(id => id.Value));");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Converts a collection of strongly typed IDs to a Dictionary using their underlying values as keys");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static Dictionary<{GetCSharpTypeName(recordInfo.UnderlyingType)}, T> ToValueDictionary<T>(this IEnumerable<{recordInfo.Name}> ids, Func<{recordInfo.Name}, T> valueSelector)");
        sb.AppendLine("        => ids.ToDictionary(id => id.Value, valueSelector);");

        sb.AppendLine("}");
        sb.AppendLine();
    }

    // Helper methods for parsing
    private static string GetParseExpression(string type, string value) => GetCSharpTypeName(type) switch
    {
        "Guid" => $"Guid.Parse({value})",
        "int" => $"int.Parse({value})",
        "long" => $"long.Parse({value})",
        "decimal" => $"decimal.Parse({value})",
        "short" => $"short.Parse({value})",
        "byte" => $"byte.Parse({value})",
        "double" => $"double.Parse({value})",
        "float" => $"float.Parse({value})",
        "string" => value,
        "DateTime" => $"DateTime.Parse({value})",
        "DateTimeOffset" => $"DateTimeOffset.Parse({value})",
        _ => $"({GetCSharpTypeName(type)})Convert.ChangeType({value}, typeof({GetCSharpTypeName(type)}))"
    };

    private static string GetTryParseExpression(string type, string value, string result)
    {
        var typeName = GetCSharpTypeName(type);
        return typeName switch
        {
            "Guid" => $"Guid.TryParse({value}, out var {result})",
            "int" => $"int.TryParse({value}, out var {result})",
            "long" => $"long.TryParse({value}, out var {result})",
            "decimal" => $"decimal.TryParse({value}, out var {result})",
            "short" => $"short.TryParse({value}, out var {result})",
            "byte" => $"byte.TryParse({value}, out var {result})",
            "double" => $"double.TryParse({value}, out var {result})",
            "float" => $"float.TryParse({value}, out var {result})",
            "DateTime" => $"DateTime.TryParse({value}, out var {result})",
            "DateTimeOffset" => $"DateTimeOffset.TryParse({value}, out var {result})",
            _ => GenerateTryParseWithConvert(typeName, value, result)
        };
    }

    private static string GenerateTryParseWithConvert(string type, string value, string result)
    {
        // For unsupported types, use the ConversionHelper
        return $@"ConversionHelper.TryConvert<{type}>({value}, out var {result})";
    }

    private static bool NeedsTryConvertHelper(RecordInfo recordInfo)
    {
        var typeName = GetCSharpTypeName(recordInfo.UnderlyingType);
        return recordInfo.GenerateTryParseMethod && typeName switch
        {
            "Guid" or "int" or "long" or "decimal" or "short" or "byte" or "double" or "float" or "DateTime" or "DateTimeOffset" or "string" => false,
            _ => true
        };
    }

    private static void GenerateTryConvertHelper(StringBuilder sb)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Helper method to try converting a string to a target type");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal static class ConversionHelper");
        sb.AppendLine("{");
        sb.AppendLine("    internal static bool TryConvert<T>(string value, out T result)");
        sb.AppendLine("    {");
        sb.AppendLine("        result = default!;");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            result = (T)Convert.ChangeType(value, typeof(T));");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static string GetNewValueExpression(string type) => GetCSharpTypeName(type) switch
    {
        "Guid" => "Guid.NewGuid()",
        "int" => "Random.Shared.Next()",
        "long" => "Random.Shared.NextInt64()",
        "decimal" => "(decimal)Random.Shared.NextDouble() * 1000000",
        "short" => "(short)Random.Shared.Next(short.MinValue, short.MaxValue)",
        "byte" => "(byte)Random.Shared.Next(byte.MinValue, byte.MaxValue)",
        "double" => "Random.Shared.NextDouble()",
        "float" => "(float)Random.Shared.NextDouble()",
        "string" => "Guid.NewGuid().ToString()",
        "DateTime" => "DateTime.UtcNow",
        "DateTimeOffset" => "DateTimeOffset.UtcNow",
        _ => $"default({GetCSharpTypeName(type)})"
    };

    // Helper methods
    private static string GetCSharpTypeName(string typeName) => typeName switch
    {
        "System.Guid" => "Guid",
        "System.Int32" => "int",
        "System.Int64" => "long",
        "System.Decimal" => "decimal",
        "System.Int16" => "short",
        "System.Byte" => "byte",
        "System.Double" => "double",
        "System.Single" => "float",
        "System.String" => "string",
        "System.DateTime" => "DateTime",
        "System.DateTimeOffset" => "DateTimeOffset",
        _ => typeName.Split('.').Last()
    };

    private static string GetJsonReadExpression(string type) => type switch
    {
        "Guid" => "reader.GetGuid()",
        "int" => "reader.GetInt32()",
        "long" => "reader.GetInt64()",
        "decimal" => "reader.GetDecimal()",
        "short" => "reader.GetInt16()",
        "byte" => "reader.GetByte()",
        "double" => "reader.GetDouble()",
        "float" => "reader.GetSingle()",
        "string" => "reader.GetString() ?? string.Empty",
        "DateTime" => "reader.GetDateTime()",
        "DateTimeOffset" => "reader.GetDateTimeOffset()",
        _ => "reader.GetString() ?? string.Empty"
    };

    private static string GetJsonWriteExpression(string type, string value) => type switch
    {
        "Guid" => $"writer.WriteStringValue({value})",
        "int" => $"writer.WriteNumberValue({value})",
        "long" => $"writer.WriteNumberValue({value})",
        "decimal" => $"writer.WriteNumberValue({value})",
        "short" => $"writer.WriteNumberValue({value})",
        "byte" => $"writer.WriteNumberValue({value})",
        "double" => $"writer.WriteNumberValue({value})",
        "float" => $"writer.WriteNumberValue({value})",
        "string" => $"writer.WriteStringValue({value})",
        "DateTime" => $"writer.WriteStringValue({value})",
        "DateTimeOffset" => $"writer.WriteStringValue({value})",
        _ => $"writer.WriteStringValue({value}?.ToString())"
    };

    private static bool HasEmptyValue(string type) => type switch
    {
        "System.Guid" or "Guid" => true,
        "System.DateTime" or "DateTime" => true,
        "System.DateTimeOffset" or "DateTimeOffset" => true,
        _ => false
    };

    private static string GetEmptyValue(string type) => type switch
    {
        "System.Guid" or "Guid" => "Guid.Empty",
        "System.DateTime" or "DateTime" => "DateTime.MinValue",
        "System.DateTimeOffset" or "DateTimeOffset" => "DateTimeOffset.MinValue",
        _ => "default"
    };

    private static bool IsComparable(string type) => type switch
    {
        "System.Guid" or "Guid" => true,
        "System.Int32" or "int" => true,
        "System.Int64" or "long" => true,
        "System.Decimal" or "decimal" => true,
        "System.Int16" or "short" => true,
        "System.Byte" or "byte" => true,
        "System.Double" or "double" => true,
        "System.Single" or "float" => true,
        "System.String" or "string" => true,
        "System.DateTime" or "DateTime" => true,
        "System.DateTimeOffset" or "DateTimeOffset" => true,
        _ => false
    };

    private static bool SupportsFormatProvider(string type) => GetCSharpTypeName(type) switch
    {
        "int" or "long" or "decimal" or "short" or "byte" or "double" or "float" => true,
        "DateTime" or "DateTimeOffset" => true,
        _ => false
    };

    private static string GetParseExpressionWithProvider(string type, string value, string provider) => GetCSharpTypeName(type) switch
    {
        "int" => $"int.Parse({value}, {provider})",
        "long" => $"long.Parse({value}, {provider})",
        "decimal" => $"decimal.Parse({value}, {provider})",
        "short" => $"short.Parse({value}, {provider})",
        "byte" => $"byte.Parse({value}, {provider})",
        "double" => $"double.Parse({value}, {provider})",
        "float" => $"float.Parse({value}, {provider})",
        "DateTime" => $"DateTime.Parse({value}, {provider})",
        "DateTimeOffset" => $"DateTimeOffset.Parse({value}, {provider})",
        _ => GetParseExpression(type, value)
    };

    private static string GetTryParseExpressionWithProvider(string type, string value, string provider, string result) => GetCSharpTypeName(type) switch
    {
        "int" => $"int.TryParse({value}, {provider}, out var {result})",
        "long" => $"long.TryParse({value}, {provider}, out var {result})",
        "decimal" => $"decimal.TryParse({value}, {provider}, out var {result})",
        "short" => $"short.TryParse({value}, {provider}, out var {result})",
        "byte" => $"byte.TryParse({value}, {provider}, out var {result})",
        "double" => $"double.TryParse({value}, {provider}, out var {result})",
        "float" => $"float.TryParse({value}, {provider}, out var {result})",
        "DateTime" => $"DateTime.TryParse({value}, {provider}, System.Globalization.DateTimeStyles.None, out var {result})",
        "DateTimeOffset" => $"DateTimeOffset.TryParse({value}, {provider}, System.Globalization.DateTimeStyles.None, out var {result})",
        _ => GetTryParseExpression(type, value, result)
    };

    private const string AttributeSource = """
        using System;

        namespace ErikLieben.FA.StronglyTypedIds;

        /// <summary>
        /// Generates strongly typed ID support code including converters, extensions, and utility methods
        /// </summary>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
        public class GenerateStronglyTypedIdSupportAttribute : Attribute
        {
            /// <summary>
            /// Gets or sets whether to generate a JSON converter
            /// </summary>
            public bool GenerateJsonConverter { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to generate a type converter
            /// </summary>
            public bool GenerateTypeConverter { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to generate a Parse method
            /// </summary>
            public bool GenerateParseMethod { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to generate a TryParse method
            /// </summary>
            public bool GenerateTryParseMethod { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to generate comparison operators
            /// </summary>
            public bool GenerateComparisons { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to generate a New method for creating new instances
            /// </summary>
            public bool GenerateNewMethod { get; set; } = true;

            /// <summary>
            /// Gets or sets whether to generate extension methods
            /// </summary>
            public bool GenerateExtensions { get; set; } = true;
        }
        """;

    private sealed class RecordInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Namespace { get; set; }
        public string UnderlyingType { get; set; } = string.Empty;
        public bool GenerateJsonConverter { get; set; }
        public bool GenerateTypeConverter { get; set; }
        public bool GenerateParseMethod { get; set; }
        public bool GenerateTryParseMethod { get; set; }
        public bool GenerateComparisons { get; set; }
        public bool GenerateNewMethod { get; set; }
        public bool GenerateExtensions { get; set; }
    }
}

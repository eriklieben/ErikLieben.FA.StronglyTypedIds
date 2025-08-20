namespace ErikLieben.FA.StronglyTypedIds;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class GenerateStronglyTypedIdSupportAttribute : Attribute
{
    public bool GenerateJsonConverter { get; set; } = true;
    public bool GenerateTypeConverter { get; set; } = true;
    public bool GenerateParseMethod { get; set; } = true;
    public bool GenerateTryParseMethod { get; set; } = true;
    public bool GenerateComparisons { get; set; } = true;
    public bool GenerateNewMethod { get; set; } = true;
    public bool GenerateExtensions { get; set; } = true;
}

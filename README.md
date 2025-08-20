# ErikLieben.FA.StronglyTypedIds

[![NuGet](https://img.shields.io/nuget/v/ErikLieben.FA.StronglyTypedIds?style=flat-square)](https://www.nuget.org/packages/ErikLieben.FA.StronglyTypedIds)
[![Changelog](https://img.shields.io/badge/Changelog-docs-informational?style=flat-square)](docs/CHANGELOG.md)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue?style=flat-square)](https://dotnet.microsoft.com/download/dotnet/9.0)


[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=eriklieben_ErikLieben.FA.StronglyTypedIds&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=eriklieben_ErikLieben.FA.StronglyTypedIds)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=eriklieben_ErikLieben.FA.StronglyTypedIds&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=eriklieben_ErikLieben.FA.StronglyTypedIds)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=eriklieben_ErikLieben.FA.StronglyTypedIds&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=eriklieben_ErikLieben.FA.StronglyTypedIds)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=eriklieben_ErikLieben.FA.StronglyTypedIds&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=eriklieben_ErikLieben.FA.StronglyTypedIds)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=eriklieben_ErikLieben.FA.StronglyTypedIds&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=eriklieben_ErikLieben.FA.StronglyTypedIds)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=eriklieben_ErikLieben.FA.StronglyTypedIds&metric=coverage)](https://sonarcloud.io/summary/new_code?id=eriklieben_ErikLieben.FA.StronglyTypedIds)
[![Known Vulnerabilities](https://snyk.io/test/github/eriklieben/ErikLieben.FA.StronglyTypedIds/badge.svg)](https://snyk.io/test/github/eriklieben/ErikLieben.FA.StronglyTypedIds)

> **Minimal, allocation-friendly strongly typed IDs for .NET, with a Roslyn source generator that adds JSON conversion, parsing, comparison operators, and helpful extensions.**

## 👋 A Friendly Note

This is an **opinionated library** built primarily for my own projects and coding style. You're absolutely free to use it (it's MIT licensed!), but please don't expect free support or feature requests. If it works for you, great! If not, there are many other excellent libraries in the .NET ecosystem.

That said, I do welcome bug reports and thoughtful contributions. If you're thinking about a feature or change, please open an issue first to discuss it - this helps avoid disappointment if it doesn't align with the library's direction. 😊

## 🚀 Why Strongly Typed IDs?

Raw GUIDs and primitive types in domain models lead to confusion and bugs. Strongly typed IDs provide:

- **🎯 Explicit intent** - `AccountId` vs `ProductId` instead of `Guid` vs `Guid`
- **🔒 Type safety** - Prevent accidental mixups between different entity IDs
- **🧪 Better testing** - Clear, expressive domain models that are easy to test
- **⚙️ First-class tooling** - JSON serialization, parsing, and conversions work seamlessly

Perfect for **Domain-Driven Design**, **clean architecture**, and any application where entity identity matters.

## ❌ When NOT to Use This Library

Consider alternatives when:

- **Performance is absolutely critical** - The abstraction adds minimal but measurable overhead
- **Simple applications** - Basic CRUD without complex domain modeling might not benefit
- **Team unfamiliarity** - Your team isn't comfortable with strongly typed patterns
- **Legacy constraints** - Existing systems heavily depend on primitive ID types
- **Over-engineering risk** - Adding typed IDs would increase complexity without clear benefit

## 📦 Installation

```bash
# Core library with base types
dotnet add package ErikLieben.FA.StronglyTypedIds

# Generator for JSON, parsing, and additional features (recommended)
dotnet add package ErikLieben.FA.StronglyTypedIds.Generator
```

## ⚡ Quick Start

Define your ID as a partial record and annotate with `[GenerateStronglyTypedIdSupport]`:

```csharp
using ErikLieben.FA.StronglyTypedIds;

[GenerateStronglyTypedIdSupport]
public partial record AccountId(Guid Value) : StronglyTypedId<Guid>(Value);

[GenerateStronglyTypedIdSupport]
public partial record ProductId(int Value) : StronglyTypedId<int>(Value);
```

Use them like value objects with generated capabilities:

```csharp
// Factory methods
var accountId = AccountId.New();           // Generates new Guid
var productId = ProductId.New();           // Random int

// Parsing
var parsed = AccountId.From("550e8400-e29b-41d4-a716-446655440000");
if (ProductId.TryParse("123", out var tryParsed))
{
    Console.WriteLine($"Parsed: {tryParsed}");
}

// Type safety - this won't compile!
// ProcessAccount(productId);  // ❌ Compiler error

ProcessAccount(accountId);     // ✅ Type safe

// JSON serialization works automatically
var json = JsonSerializer.Serialize(accountId);
var roundTrip = JsonSerializer.Deserialize<AccountId>(json);
```

## 🏗️ Core Architecture

### Base Types

```csharp
// Minimal base record
public abstract record StronglyTypedId<T>(T Value) : IStronglyTypedId<T> 
    where T : IEquatable<T>

// Interface for generic constraints
public interface IStronglyTypedId<out T>
{
    T Value { get; }
}
```

### Generator Attribute

```csharp
[GenerateStronglyTypedIdSupport]  // Enables all features by default
public partial record YourId(Type Value) : StronglyTypedId<Type>(Value);
```

## 🛠️ Supported Underlying Types

The generator provides tailored support for common ID types:

### Guid-based IDs (Most Common)

```csharp
[GenerateStronglyTypedIdSupport]
public partial record CustomerId(Guid Value) : StronglyTypedId<Guid>(Value);

var id = CustomerId.New();                    // Guid.NewGuid()
var parsed = CustomerId.From("guid-string");  // Validates and parses
bool isEmpty = id.IsEmpty();                  // Checks for Guid.Empty
```

### Integer-based IDs

```csharp
[GenerateStronglyTypedIdSupport]
public partial record ProductId(int Value) : StronglyTypedId<int>(Value);

var id = ProductId.New();        // Random.Shared.Next()
var specific = ProductId.From("42"); // int.Parse("42")

// Comparison operators work naturally
ProductId a = ProductId.From("1");
ProductId b = ProductId.From("2");
bool less = a < b;  // true
```

### Long-based IDs

```csharp
[GenerateStronglyTypedIdSupport]
public partial record OrderId(long Value) : StronglyTypedId<long>(Value);

var id = OrderId.New();  // Random.Shared.NextInt64()
```

### String-based IDs

```csharp
[GenerateStronglyTypedIdSupport]
public partial record ExternalKey(string Value) : StronglyTypedId<string>(Value);

var id = ExternalKey.New();              // Guid.NewGuid().ToString()
var custom = ExternalKey.From("ABC-123"); // Direct assignment
```

### DateTime-based IDs

```csharp
[GenerateStronglyTypedIdSupport]
public partial record TimestampId(DateTimeOffset Value) : StronglyTypedId<DateTimeOffset>(Value);

var id = TimestampId.New();     // DateTimeOffset.UtcNow
bool isEmpty = id.IsEmpty();    // Checks for DateTimeOffset.MinValue
```

## 🔧 Generated Features

The `[GenerateStronglyTypedIdSupport]` attribute generates:

### JSON Serialization

```csharp
[GenerateStronglyTypedIdSupport]
public partial record UserId(Guid Value) : StronglyTypedId<Guid>(Value);

var user = UserId.New();
var json = JsonSerializer.Serialize(user);
// Output: "550e8400-e29b-41d4-a716-446655440000"

var deserialized = JsonSerializer.Deserialize<UserId>(json);
// Works seamlessly with System.Text.Json
```

### Parsing Methods

```csharp
// Safe parsing with validation
if (UserId.TryParse("invalid-guid", out var userId))
{
    // Won't execute - invalid format
}

// Direct parsing (throws on invalid input)
var validId = UserId.From("550e8400-e29b-41d4-a716-446655440000");
```

### Comparison Operations

```csharp
var early = TimestampId.From("2024-01-01T00:00:00Z");
var later = TimestampId.From("2024-12-31T23:59:59Z");

bool isEarlier = early < later;   // true
bool isSame = early == later;     // false (record equality)
bool isLaterOrEqual = later >= early; // true
```

### Factory Methods

```csharp
// Type-specific intelligent defaults
var accountId = AccountId.New();    // New Guid
var productId = ProductId.New();    // Random int
var timestamp = TimestampId.New();  // Current UTC time
var key = ExternalKey.New();        // Guid as string
```

### Extension Methods

```csharp
// Empty checks for applicable types
var emptyGuid = new UserId(Guid.Empty);
bool isEmpty = emptyGuid.IsEmpty();     // true
bool hasValue = emptyGuid.IsNotEmpty(); // false

// Collection helpers
var userIds = new[] { UserId.New(), UserId.New(), UserId.New() };
Guid[] values = userIds.ToValues().ToArray();           // Extract underlying values
HashSet<Guid> uniqueValues = userIds.ToValueSet();      // Unique underlying values
Dictionary<Guid, string> lookup = userIds.ToValueDictionary(id => $"User-{id}");
```

## ⚙️ Customizing Generation

Control which features are generated using attribute properties:

```csharp
[GenerateStronglyTypedIdSupport(
    GenerateJsonConverter = true,    // System.Text.Json support
    GenerateTypeConverter = true,    // System.ComponentModel.TypeConverter
    GenerateParseMethod = true,      // From() method
    GenerateTryParseMethod = true,   // TryParse() method
    GenerateComparisons = true,      // <, <=, >, >= operators
    GenerateNewMethod = true,        // New() factory method
    GenerateExtensions = true        // IsEmpty(), collection helpers
)]
public partial record ConfigurableId(Guid Value) : StronglyTypedId<Guid>(Value);
```

### Feature Details

| Feature | When Enabled | When Disabled |
|---------|-------------|---------------|
| **JsonConverter** | Automatic serialization with System.Text.Json | Manual converter required |
| **TypeConverter** | Works with model binding, configuration | Manual conversion needed |
| **ParseMethod** | `YourId.From(string)` available | Create instances manually |
| **TryParseMethod** | `YourId.TryParse(string, out result)` available | Manual validation required |
| **Comparisons** | `<`, `<=`, `>`, `>=` operators work | Only equality (`==`, `!=`) available |
| **NewMethod** | `YourId.New()` factory available | Use constructor: `new YourId(value)` |
| **Extensions** | `IsEmpty()`, collection helpers available | Use `.Value` property directly |

## 📋 Complete Domain Example

```csharp
using System.Text.Json;
using ErikLieben.FA.StronglyTypedIds;

// Define strongly typed IDs for your domain
[GenerateStronglyTypedIdSupport]
public partial record CustomerId(Guid Value) : StronglyTypedId<Guid>(Value);

[GenerateStronglyTypedIdSupport]
public partial record OrderId(long Value) : StronglyTypedId<long>(Value);

[GenerateStronglyTypedIdSupport]
public partial record ProductId(int Value) : StronglyTypedId<int>(Value);

// Domain entities using typed IDs
public record Customer(CustomerId Id, string Name, string Email);
public record Product(ProductId Id, string Name, decimal Price);
public record OrderLine(ProductId ProductId, int Quantity, decimal UnitPrice);
public record Order(OrderId Id, CustomerId CustomerId, OrderLine[] Lines, DateTimeOffset CreatedAt);

// Service methods are type-safe
public class OrderService
{
    public Order CreateOrder(CustomerId customerId, OrderLine[] lines)
    {
        var orderId = OrderId.New();  // Generated factory method
        return new Order(orderId, customerId, lines, DateTimeOffset.UtcNow);
    }
    
    public Customer? FindCustomer(CustomerId id)
    {
        // Type safety prevents mixing up different ID types
        // This won't compile: FindCustomer(OrderId.New())
        return _customers.Find(c => c.Id == id);
    }
}

// JSON serialization works seamlessly
var customer = new Customer(CustomerId.New(), "John Doe", "john@example.com");
var json = JsonSerializer.Serialize(customer);
var deserialized = JsonSerializer.Deserialize<Customer>(json);

// Parse from external systems
if (CustomerId.TryParse(externalSystemId, out var parsedId))
{
    var customer = orderService.FindCustomer(parsedId);
}

// Work with collections
var customerIds = new[] { CustomerId.New(), CustomerId.New() };
var guidValues = customerIds.ToValues();  // Extract underlying Guids
var uniqueIds = customerIds.ToValueSet(); // HashSet<Guid>
```

## 🔍 How the Generator Works

The Roslyn source generator:

1. **Scans your compilation** for records inheriting from `StronglyTypedId<T>`
2. **Finds the attribute** `[GenerateStronglyTypedIdSupport]`
3. **Analyzes the underlying type** (Guid, int, string, etc.)
4. **Generates appropriate code** for each enabled feature
5. **Emits partial classes** that extend your ID types

Generated code includes:
- JSON converters compatible with System.Text.Json
- Type converters for model binding and configuration
- Static factory and parsing methods
- Comparison operators when applicable
- Extension methods for common operations

All generation happens at **compile time** - there's no runtime reflection or performance impact.

## 💡 Best Practices

### Do's ✅

- **Use descriptive names** - `CustomerId`, `ProductId`, `OrderId` instead of generic `Id`
- **Be consistent** - Use the same underlying type for similar concepts
- **Leverage type safety** - Let the compiler catch ID mixups at build time
- **Generate all features** - Unless you have specific performance concerns
- **Test with real IDs** - Use the `New()` factory in unit tests for realistic scenarios

### Don'ts ❌

- **Don't use for all primitives** - Only create typed IDs for entity identifiers
- **Don't mix underlying types** - Stick to one type (usually Guid) across your domain
- **Don't disable safety features** - Keep JSON and parsing support enabled unless necessary
- **Don't over-engineer** - Simple lookup keys might not need strongly typed IDs

## 📚 Inspiration & Prior Art

This library builds on foundational work in the .NET community around strongly typed IDs and combating primitive obsession:

### **Primitive Obsession (1999)**
The core concept was first formalized by **Martin Fowler** in his seminal book ["Refactoring: Improving the Design of Existing Code"](https://martinfowler.com/books/refactoring.html), where he identified "Primitive Obsession" as a code smell that occurs when primitive types are overused to represent domain concepts.

### **Andrew Lock's Pioneering Work**
This library was significantly inspired by **Andrew Lock's** groundbreaking blog series and StronglyTypedId library. His comprehensive work brought strongly typed IDs to mainstream .NET development:

**Original Blog Series (2019-2021):**
- [**Part 1:** An introduction to strongly-typed entity IDs](https://andrewlock.net/using-strongly-typed-entity-ids-to-avoid-primitive-obsession-part-1/) - The foundational article introducing the concept
- [**Part 2:** Adding JSON converters to strongly typed IDs](https://andrewlock.net/using-strongly-typed-entity-ids-to-avoid-primitive-obsession-part-2/) - ASP.NET Core integration
- [**Part 3:** Using strongly-typed entity IDs with EF Core](https://andrewlock.net/using-strongly-typed-entity-ids-to-avoid-primitive-obsession-part-3/) - Database integration challenges
- [**Part 4:** Strongly-typed IDs in EF Core (Revisited)](https://andrewlock.net/strongly-typed-ids-in-ef-core-using-strongly-typed-entity-ids-to-avoid-primitive-obsession-part-4/) - Solving EF Core issues
- [**Part 5:** Generating strongly-typed IDs at build-time with Roslyn](https://andrewlock.net/generating-strongly-typed-ids-at-build-time-with-roslyn-using-strongly-typed-entity-ids-to-avoid-primitive-obsession-part-5/) - First code generation approach

**Library Evolution Updates:**
- [**Part 6:** Strongly-typed ID update 0.2.1](https://andrewlock.net/strongly-typed-id-updates/) - Adding System.Text.Json support and new features (2020)
- [**Part 7:** Rebuilding StronglyTypedId as a source generator](https://andrewlock.net/rebuilding-stongly-typed-id-as-a-source-generator-1-0-0-beta-release/) - Major rewrite using .NET 5 source generators (2021)
- [**Part 8:** Updates to the StronglyTypedId library - simplification, templating, and CodeFixes](https://andrewlock.net/updates-to-the-stronglytypedid-library/) - Template system and maintainability improvements (2023)

**GitHub Repository:** [andrewlock/StronglyTypedId](https://github.com/andrewlock/StronglyTypedId)

Andrew's work demonstrated the value of strongly typed IDs and provided the first widely-adopted source generator solution for .NET. His library uses a struct-based approach with extensive customization options through a template system, evolving from CodeGeneration.Roslyn to native source generators.

While less customizable than Andrew's template system, this library aims to provide a simpler API that covers the majority of use cases for my use cases with minimal complexity.

## ❓ FAQ

**Q: Do I need the generator package?**
A: Technically no - the core types work without it. But you'll miss JSON serialization, parsing helpers, comparisons, and extensions that make strongly typed IDs practical.

**Q: Does this work with Entity Framework Core?**
A: Yes, but you may need custom value converters. The generated TypeConverter can help, or you can map to the underlying `Value` property directly.

**Q: Is this compatible with Native AOT?**
A: Yes! The generator produces regular C# code at compile time. No reflection or runtime code generation is used.

**Q: Can I use this with ASP.NET Core model binding?**
A: Yes, the generated TypeConverter enables automatic conversion from route parameters and form data.

**Q: What about performance?**
A: Minimal overhead - records are value types with efficient equality. The wrapper adds one level of indirection but optimizes well.

## 📄 License

MIT License - see the [LICENSE](LICENSE) file for details.

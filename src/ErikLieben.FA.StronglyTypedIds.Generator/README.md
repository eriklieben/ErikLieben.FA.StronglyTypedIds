# ErikLieben.FA.StronglyTypedIds.Generator

[![NuGet](https://img.shields.io/nuget/v/ErikLieben.FA.StronglyTypedIds.Generator?style=flat-square)](https://www.nuget.org/packages/ErikLieben.FA.StronglyTypedIds.Generator)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue?style=flat-square)](https://dotnet.microsoft.com/download/dotnet/9.0)

> **Roslyn source generator for ErikLieben.FA.StronglyTypedIds - provides compile-time code generation for strongly typed IDs.**

## 👋 A Friendly Note

This is an **opinionated library** built primarily for my own projects and coding style. You're absolutely free to use it (it's MIT licensed!), but please don't expect free support or feature requests. If it works for you, great! If not, there are many other excellent libraries in the .NET ecosystem.

That said, I do welcome bug reports and thoughtful contributions. If you're thinking about a feature or change, please open an issue first to discuss it - this helps avoid disappointment if it doesn't align with the library's direction. 😊

## 📄 About This Package

This package contains **only the source generator** for ErikLieben.FA.StronglyTypedIds. It provides compile-time code generation that adds JSON serialization, parsing methods, comparison operators, and helpful extensions to your strongly typed ID records.

**This is a build-time tool** - there are no runtime APIs to call directly.

## 📦 Installation & Usage

**Most users should install the main package instead:**

```bash
# Install the main package (includes both runtime and generator)
dotnet add package ErikLieben.FA.StronglyTypedIds
```

**Only install this generator package separately if you need to:**
- Reference the generator from a different project than your ID definitions
- Use custom package reference configurations
- Separate build-time dependencies in complex solutions

```bash
# Manual installation (advanced scenarios only)
dotnet add package ErikLieben.FA.StronglyTypedIds          # Runtime library
dotnet add package ErikLieben.FA.StronglyTypedIds.Generator # This generator
```

## 🔧 How It Works

The generator automatically detects:
- Partial records that inherit from `StronglyTypedId<T>`
- Records annotated with `[GenerateStronglyTypedIdSupport]`

And generates:
- JSON converters (System.Text.Json)
- Type converters for model binding
- Static methods: `New()`, `From()`, `TryParse()`
- Comparison operators (`<`, `<=`, `>`, `>=`)
- Extension methods (`IsEmpty()`, collection helpers)

## 📚 Documentation

For complete documentation, examples, and configuration options, see the main library:

**[📖 ErikLieben.FA.StronglyTypedIds Documentation](https://www.nuget.org/packages/ErikLieben.FA.StronglyTypedIds)**

## 🚀 Quick Example

```csharp
using ErikLieben.FA.StronglyTypedIds;

[GenerateStronglyTypedIdSupport]
public partial record UserId(Guid Value) : StronglyTypedId<Guid>(Value);

// Generator automatically adds:
// - UserId.New()
// - UserId.From(string)
// - UserId.TryParse(string, out UserId)
// - JSON serialization support
// - Type converter for model binding
// - And much more...
```

## ⚙️ Requirements

- **.NET 9.0+ SDK** (can target earlier frameworks)
- **C# 9.0+** (for records support)

## 📄 License

MIT License - see the [LICENSE](../../LICENSE) file for details.

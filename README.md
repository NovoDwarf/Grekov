# Grekov

> [!WARNING]
> This project was created for educational purposes and is still under development. It may contain bugs, incomplete features, and architectural flaws. The code is not production-ready and should be used with caution.

Grekov is a .NET library for loading packages with data and C# assemblies. It can serve as a foundation for mods: describe items, buildings, and other entities in files, add custom code, and declare dependencies between packages.

The idea is to keep application content in separate packages. You define data types in C# and their values in files. Grekov connects the two and gives the application access to the resulting objects.

Grekov is named after **Mitrofan Borisovich Grekov**, a Russian painter known for his works depicting military and historical subjects. Learn more about Mitrofan Grekov on [Wikipedia](https://en.wikipedia.org/wiki/Mitrofan_Grekov).

## How it works

A **package** is a folder or a **GPAK** archive containing a manifest, definitions, and optional assemblies. The manifest specifies the package ID, version, and dependencies.

A **definition (`Def`)** is an object loaded from XML, JSON, YAML, or TOML, such as a sword with a damage value. Its C# type is marked with `[DefType]`, and fields and properties loaded from files are marked with `[DefField]`.

At startup, Grekov discovers packages, checks their dependencies, and determines the load order. It then loads assemblies, reads definitions, resolves inheritance, and validates data and references. The resulting objects are available through `IDefCatalog` by ID, type, package, or resource path.

## What is implemented

- Packages from package Providers: FS (file system) and Binary (custom GPAK archive, including GPAK reading and writing).
- Definition from Readers: XML, JSON, YAML, and TOML. Definitions with inheritance, references to other definitions, value conversion, and validation. 
- Definition can Storage: In-Memory or in a DB (Entity Framework).
- Package dependencies with version constraints, and package enable/disable settings.
- Assembly loading through collectible `AssemblyLoadContext` instances and `IEntrypoint` callbacks: `OnLoad`, `OnLoaded`, and `OnUnload`.
- Integration with `Microsoft.Extensions.DependencyInjection` and support for custom package providers, format readers, storage implementations, converters, and validators.

Localization is partially implemented: string definitions and translation infrastructure exist, but the system is not complete yet.

## Example

Define a type in the application:

```csharp
[DefType("Sword")]
public sealed class SwordDef : Def
{
    [DefField]
    public int Damage { get; set; }
}
```

Add a `Definitions/Sword.xml` file to a package:

```xml
<Sword Id="sword_steel">
    <Damage>25</Damage>
</Sword>
```

After calling `IGrekovService.StartAsync()`, retrieve the definition from the catalog:

```csharp
var catalog = provider.GetRequiredService<IDefCatalog>();
var sword = catalog.Get<SwordDef>("sword_steel");
Console.WriteLine(sword.Damage); // 25
```

- [Grekov.Example](Examples/Grekov.Example) for a complete example with service registration, a manifest, and package loading. 
- [Grekov.Binary.Example](Examples/Grekov.Binary.Example) shows how to create and read a GPAK archive in memory.

## Modules

Choose the implementations you need:

| Module | Purpose |
| --- | --- |
| `Grekov` | Package, assembly, and definition loading; session lifecycle |
| `Grekov.Abstractions` | Shared types, attributes, and interfaces |
| `Grekov.Binary` | GPAK reading and writing |
| `Grekov.Providers.FileSystem` / `Binary` | Packages from folders or GPAK archives |
| `Grekov.Readers.Abstractions` | Shared foundation for format readers |
| `Grekov.Readers.Xml` / `Json` / `Yaml` / `Toml` | Definition readers for each format |
| `Grekov.Storages.InMemory` / `Database` | In-memory or EF Core storage |

Use a dedicated database for Grekov. At startup, the library creates the schema if it does not exist, without deleting the database. Schema migrations are the application's responsibility. Unloading removes package data from storage.

## Build and run

The projects target `net10.0` and `net11.0` and use preview C# language features. Building the full solution requires an SDK that supports both targets.

```shell
dotnet build Grekov.slnx -c Release
dotnet test Grekov.slnx -c Release
dotnet run --project Examples/Grekov.Example -f net11.0
dotnet run --project Examples/Grekov.Binary.Example -f net11.0
```

Local NuGet publication is opt-in for Debug builds: `-p:PublishLocalPackages=true -p:LocalNugetRepository=<directory>`.

## License

[**Grekov**](https://github.com/NovoDwarf/Grekov) is licensed under the [**MIT License**](), see [LICENSE](LICENSE) for more information.

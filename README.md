# Grekov

A modular package and definition framework for .NET applications.

> Under development. Not production-ready.

## Features

- File-system and GPAK packages with manifests and dependency ordering.
- Collectible assembly loading and package entrypoints.
- JSON, XML, YAML and TOML definitions with inheritance, references and validation.
- In-memory and Entity Framework Core storage.
- Localization infrastructure (incomplete).

## Usage

See [Grekov.Example](Examples/Grekov.Example) for configuration and loading,
and [Grekov.Binary.Example](Examples/Grekov.Binary.Example) for GPAK creation.

```json
{
    "$type": "Sword",
    "Id": "sword_steel",
    "Damage": 25
}
```

Resource paths are relative to their package. Specify the package when paths overlap:

```csharp
var definition = catalog.GetByResourcePath<Def>("base_package", "Items/Sword.json");
```

Required dependencies must be enabled, error-free and satisfy their version constraints.
Failed startup rolls back the attempted session; stop releases its packages and entrypoints.

Database storage never deletes the database on startup. Use a dedicated Grekov database;
schema migrations are the host application's responsibility.

## Build and test

The solution targets .NET 10 and .NET 11 and requires an SDK supporting both targets.

```shell
dotnet build Grekov.slnx -c Release
dotnet test Grekov.slnx -c Release
```

Local NuGet publication is opt-in for Debug builds:
`-p:PublishLocalPackages=true -p:LocalNugetRepository=<directory>`.

## License

[MIT](LICENSE).

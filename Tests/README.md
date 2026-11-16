# Tests

Tests use xUnit 2 and follow Arrange–Act–Assert.

- `Grekov.Tests.Shared` contains test data, factories, fixtures and resource cleanup.
- `IClassFixture<T>` shares read-only preparation; converters and reader diagnostics
  are created separately for each scenario.
- `DatabaseTestBase` and `BinaryDiscoveryTestBase` use `IAsyncLifetime` for per-test
  initialization and cleanup. Each test gets its own temporary directory and database.
- `EntrypointTestContext` owns one scenario's state and asynchronous disposal.
- `ClassData` with typed `TheoryData` supplies version and invalid-input cases.
  Small boolean cases remain `InlineData`.
- `Trait("Category", "Unit")` and `Trait("Category", "Integration")` support filtered runs.
- Package DLL fixtures remain separate projects so assembly unload checks use real assemblies.

```shell
dotnet test Grekov.slnx -c Release
dotnet test Grekov.slnx -c Release --filter Category=Unit
dotnet test Grekov.slnx -c Release --filter Category=Integration
```

Use the [xUnit shared-context guidance](https://xunit.net/docs/shared-context) when
choosing fixture scope. Keep mutable scenario state isolated and leave default test
parallelism enabled.

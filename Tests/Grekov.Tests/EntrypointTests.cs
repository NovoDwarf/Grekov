using Grekov.Tests.Shared.Assemblies;

namespace Grekov.Tests;

[Trait("Category", "Unit")]
public sealed class EntrypointTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PreservesLifecycleAndDisposalFailures(bool loading)
    {
        // Arrange
        var state = new EntrypointState { FailLoad = loading, FailUnload = !loading, FailDispose = true };
        await using var context = new EntrypointTestContext(state);

        if (!loading)
        {
            await context.Service.LoadAsync(context.Packages);
        }

        // Act
        var exception = await Record.ExceptionAsync(() => loading
            ? context.Service.LoadAsync(context.Packages) : context.Service.UnloadAsync(context.Packages));

        // Assert
        var failure = Assert.IsType<AggregateException>(exception);
        Assert.Contains(failure.InnerExceptions, error => error.Message == "Lifecycle failure");
        Assert.Contains(failure.InnerExceptions, error => error.Message == "Disposal failure");
        Assert.True(state.Disposed);
    }

    [Fact]
    public async Task AwaitsAsynchronousDisposal()
    {
        // Arrange
        await using var context = new EntrypointTestContext();
        await context.Service.LoadAsync(context.Packages);

        // Act
        await context.Service.UnloadAsync(context.Packages);

        // Assert
        Assert.True(context.State.Disposed);
    }
}

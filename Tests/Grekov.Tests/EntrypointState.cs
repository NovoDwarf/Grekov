using System.Runtime.Loader;
using Grekov;
using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Assemblies;
using Grekov.Assemblies.Services;
using Grekov.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Grekov.Tests;

public sealed class EntrypointState
{
    public bool FailLoad { get; init; }
    public bool FailUnload { get; init; }
    public bool FailDispose { get; init; }
    public bool Disposed { get; set; }
}

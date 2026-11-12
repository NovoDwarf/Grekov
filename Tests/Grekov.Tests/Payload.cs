using Grekov.Abstractions;
using Grekov.Abstractions.Interfaces.Definitions;
using Grekov.Definitions.Conversions;
using Grekov.Definitions.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Grekov.Tests;

public sealed record Payload(int Amount);

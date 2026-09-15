using System.Reflection;
using System.Runtime.Loader;

namespace Grekov.Assemblies.Entities;

internal sealed record LoadedAssembly(Assembly Assembly, AssemblyLoadContext LoadContext);
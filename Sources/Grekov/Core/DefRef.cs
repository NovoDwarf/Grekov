namespace Grekov.Core;

public readonly record struct DefRef<T>(DefId Id) where T : Def;
namespace Grekov.Tests.Shared.Infrastructure;

public static class GarbageCollection
{
    public static void CollectUntilReleased(WeakReference reference)
    {
        for (var attempt = 0; attempt < 10 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}

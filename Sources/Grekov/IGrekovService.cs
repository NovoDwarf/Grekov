namespace Grekov;

public interface IGrekovService
{
	Task Start(CancellationToken token = default);

	Task Stop(CancellationToken token = default);
}
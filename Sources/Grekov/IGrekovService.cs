namespace Grekov;

public interface IGrekovService
{
	public Task StartAsync(CancellationToken token = default);

	public Task StopAsync(CancellationToken token = default);
}
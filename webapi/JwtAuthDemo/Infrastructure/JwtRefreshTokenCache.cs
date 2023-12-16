namespace JwtAuthDemo.Infrastructure;

public class JwtRefreshTokenCache(IJwtAuthManager jwtAuthManager) : IHostedService, IDisposable
{
    private Timer _timer = null!;

    public Task StartAsync(CancellationToken stoppingToken)
    {
        // remove expired refresh tokens from cache every minute
        _timer = new Timer(DoWork!, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        jwtAuthManager.RemoveExpiredRefreshTokens(DateTime.Now);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}
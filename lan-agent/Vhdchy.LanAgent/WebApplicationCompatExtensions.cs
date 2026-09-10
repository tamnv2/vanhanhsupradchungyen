using Microsoft.AspNetCore.Builder;

namespace Vhdchy.LanAgent;

internal static class WebApplicationCompatExtensions
{
    public static async Task StopAsync(this WebApplication app, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        await app.StopAsync(cts.Token);
    }
}

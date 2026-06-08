using System.Text.Json;

namespace OpenMacroSwift.Desktop.Services;

public interface IMacroIpcClient : IAsyncDisposable
{
    bool IsCoreRunning { get; }
    Task EnsureCoreStartedAsync(CancellationToken cancellationToken = default);
    Task<JsonDocument?> SendAsync(string command, object? payload = null, CancellationToken cancellationToken = default);
}


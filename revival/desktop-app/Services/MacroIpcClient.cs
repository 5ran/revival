using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;

namespace OpenMacroSwift.Desktop.Services;

public sealed class MacroIpcClient : IMacroIpcClient
{
    private const string PipeName = "OpenMacroSwift.Core";
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private Process? coreProcess;
    private NamedPipeClientStream? pipe;
    private StreamReader? reader;
    private StreamWriter? writer;

    public bool IsCoreRunning => coreProcess is { HasExited: false };

    public async Task EnsureCoreStartedAsync(CancellationToken cancellationToken = default)
    {
        if (IsCoreRunning) return;

        string? executable = ResolveCorePath();
        if (executable is null) return;

        coreProcess = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        await Task.Delay(250, cancellationToken);
    }

    public async Task<JsonDocument?> SendAsync(string command, object? payload = null, CancellationToken cancellationToken = default)
    {
        await EnsureCoreStartedAsync(cancellationToken);
        await sendLock.WaitAsync(cancellationToken);

        try
        {
            if (!await EnsureConnectedAsync(cancellationToken))
            {
                return null;
            }

            var request = JsonSerializer.Serialize(new
            {
                command,
                key = payload?.GetType().GetProperty("Key")?.GetValue(payload)?.ToString(),
                value = payload?.GetType().GetProperty("Value")?.GetValue(payload)?.ToString()
            });

            await writer!.WriteLineAsync(request);
            await writer.FlushAsync();

            string? response = await reader!.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(response))
            {
                return null;
            }

            return JsonDocument.Parse(response);
        }
        catch
        {
            ResetConnection();
            return null;
        }
        finally
        {
            sendLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (coreProcess is { HasExited: false })
            {
                await SendAsync("StopMacro");
                coreProcess.Dispose();
            }
        }
        finally
        {
            ResetConnection();
            sendLock.Dispose();
        }
    }

    private async Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (pipe is { IsConnected: true } && reader is not null && writer is not null)
        {
            return true;
        }

        ResetConnection();

        try
        {
            pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(1000, cancellationToken);
            reader = new StreamReader(pipe);
            writer = new StreamWriter(pipe) { AutoFlush = true };
            return true;
        }
        catch
        {
            ResetConnection();
            return false;
        }
    }

    private void ResetConnection()
    {
        try { writer?.Dispose(); } catch { }
        try { reader?.Dispose(); } catch { }
        try { pipe?.Dispose(); } catch { }
        writer = null;
        reader = null;
        pipe = null;
    }

    private static string? ResolveCorePath()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDir, "openmacro_core_ipc.exe"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "cpp-macro-port", "build-qt-vcpkg-short", "Release", "openmacro_core_ipc.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "cpp-macro-port", "build-vs", "Release", "openmacro_core_ipc.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "cpp-macro-port", "build-qt-vcpkg-short", "Debug", "openmacro_core_ipc.exe")),
        ];

        return candidates.FirstOrDefault(File.Exists);
    }
}

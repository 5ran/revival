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
    private readonly string debugLogPath = Path.Combine(AppContext.BaseDirectory, "macro_ipc_client_debug.log");

    public bool IsCoreRunning => coreProcess is { HasExited: false };

    public async Task EnsureCoreStartedAsync(CancellationToken cancellationToken = default)
    {
        if (IsCoreRunning) return;

        if (coreProcess is not null)
        {
            try { coreProcess.Dispose(); } catch { }
            coreProcess = null;
        }

        string? executable = ResolveCorePath();
        if (executable is null) return;

        coreProcess = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Log($"ensure_core_started path={executable}");

        await Task.Delay(250, cancellationToken);
    }

    public async Task<JsonDocument?> SendAsync(string command, object? payload = null, CancellationToken cancellationToken = default)
    {
        await EnsureCoreStartedAsync(cancellationToken);
        await sendLock.WaitAsync(cancellationToken);

        try
        {
            Log($"send.begin command={command}");
            if (!await EnsureConnectedAsync(cancellationToken))
            {
                Log($"send.connect_failed command={command}");
                Log($"send.core_state running={IsCoreRunning}");
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
            Log($"send.wrote command={command} request={request}");

            string? response = await reader!.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(response))
            {
                Log($"send.empty_response command={command}");
                return null;
            }

            Log($"send.response command={command} response={response}");
            return JsonDocument.Parse(response);
        }
        catch
        {
            Log($"send.exception command={command}");
            Log($"send.core_state running={IsCoreRunning}");
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
                Log("dispose core_running=true");
                try
                {
                    coreProcess.Kill(true);
                }
                catch
                {
                }

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

    private void Log(string message)
    {
        try
        {
            File.AppendAllText(debugLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    private static string? ResolveCorePath()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDir, "openmacro_core_ipc.exe"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "native-core", "build-qt-vcpkg-short", "Release", "openmacro_core_ipc.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "native-core", "build-vs", "Release", "openmacro_core_ipc.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "native-core", "build-qt-vcpkg-short", "Debug", "openmacro_core_ipc.exe")),
        ];

        return candidates.FirstOrDefault(File.Exists);
    }
}

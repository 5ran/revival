using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

[assembly: InternalsVisibleTo("Launcher.Tests")]

namespace Launcher;

internal static class Program
{
    private static readonly string ClientExeName = "Client.exe";

    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        var installDir = AppContext.BaseDirectory;
        var clientExe = Path.Combine(installDir, ClientExeName);

        if (!File.Exists(clientExe))
        {
            MessageBox.Show(
                "Client.exe is missing from the release folder.\n\nThe launcher cannot start the app until the client binary is published.",
                "Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Log.Error("Client.exe missing. Cannot start.");
            return 1;
        }

        Log.Info("Launching Client.exe");
        return await SwiftRunner.RunAsync(clientExe, installDir, updatePreviewVersion: null);
    }
}

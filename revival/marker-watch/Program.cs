using System;
using System.Windows.Forms;

namespace MarkerWatch;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MarkerWatchForm());
    }
}

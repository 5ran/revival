using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Client.Services.Fishing;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		DebugLog.Write("Program.Main", $"startup exeDir={GetExeDirectory()} base={AppContext.BaseDirectory} processPath={Environment.ProcessPath ?? "null"}");
		var offsetsPath = Path.Combine(GetExeDirectory(), "offsets.hpp");
		if (File.Exists(offsetsPath))
		{
			OffsetsSourceProvider.Register(new FileOffsetsSource(offsetsPath));
		}
		else
		{
			OffsetsSourceProvider.Register(new EmbeddedOffsetsSource());
		}
		try
		{
			if (!DiscordRoleGate.EnsureAuthorized("Fish"))
			{
				DebugLog.Write("Program.Main", "auth denied or cancelled");
				return;
			}
		}
		catch (Exception ex)
		{
			DebugLog.Write("Program.Main", $"auth failed: {ex.Message}");
			MessageBox.Show(ex.Message, "Discord Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		DebugLog.Write("Program.Main", "launch overlay");
		Application.Run(new ReelControlForm());
	}

	private static string GetExeDirectory()
	{
		try
		{
			var processPath = Environment.ProcessPath;
			if (!string.IsNullOrWhiteSpace(processPath))
			{
				var directory = Path.GetDirectoryName(processPath);
				if (!string.IsNullOrWhiteSpace(directory))
				{
					return directory;
				}
			}
		}
		catch
		{
		}

		try
		{
			var processPath = Process.GetCurrentProcess().MainModule?.FileName;
			if (!string.IsNullOrWhiteSpace(processPath))
			{
				var directory = Path.GetDirectoryName(processPath);
				if (!string.IsNullOrWhiteSpace(directory))
				{
					return directory;
				}
			}
		}
		catch
		{
		}

		return AppContext.BaseDirectory;
	}
}

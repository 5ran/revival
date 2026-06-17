using System;
using System.IO;
using System.Windows.Forms;
using Client.Services.Fishing;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		var offsetsPath = Path.Combine(AppContext.BaseDirectory, "offsets.hpp");
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
				return;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Discord Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		Application.Run(new ReelControlForm());
	}
}

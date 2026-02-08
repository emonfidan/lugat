using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace LugatimApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            TryCreateDesktopShortcut();
        }

        private static void TryCreateDesktopShortcut()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                // 👉 KISAYOL ADI
                string shortcutPath = Path.Combine(desktop, "Lugatim.lnk");

                if (File.Exists(shortcutPath))
                    return;

                string exePath = Environment.ProcessPath!;
                string exeDir = Path.GetDirectoryName(exePath)!;

                // 👉 KARINCA İKONU
                string iconPath = Path.Combine(exeDir, "ant.ico");

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = exeDir;
                shortcut.Description = "Lugatim";
                shortcut.IconLocation = File.Exists(iconPath)
                    ? iconPath
                    : exePath;

                shortcut.Save();
            }
            catch
            {
                // sessiz geç
            }
        }
    }
}

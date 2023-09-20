﻿using Microsoft.Win32;
using Rectify11Installer.Win32;
using System.IO;
using System.Threading.Tasks;
using static Rectify11Installer.Win32.NativeMethods;

namespace Rectify11Installer.Core
{
    public class Installer
    {
        #region Public Methods
        public async Task<bool> Install(FrmWizard frm)
        {
            frm.InstallerProgress = "Preparing Installation";
            Logger.WriteLine("Preparing Installation");
            Logger.WriteLine("──────────────────────");

            if (!Directory.Exists(Variables.r11Folder))
                Directory.CreateDirectory(Variables.r11Folder);

            // goofy fix
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE", true)
                ?.CreateSubKey("Rectify11", true)
                ?.DeleteValue("x86PendingFiles", false);

            if (!Common.WriteFiles(false, false))
            {
                Logger.WriteLine("WriteFiles() failed.");
                return false;
            }
            Logger.WriteLine("WriteFiles() succeeded.");

            if (!Common.CreateDirs())
            {
                Logger.WriteLine("CreateDirs() failed.");
                return false;
            }
            Logger.WriteLine("CreateDirs() succeeded.");

            try
            {
                // create restore point
                frm.InstallerProgress = "Begin creating a restore point";
                CreateSystemRestorePoint(false);
            }
            catch
            {
                Logger.Warn("Error creating a restore point.");
            }

            // runtimes
            frm.InstallerProgress = "Installing runtimes";
            if (!Common.InstallRuntimes())
            {
                Logger.WriteLine("InstallRuntimes() failed.");
                return false;
            }
            if (Variables.vcRedist && Variables.core31)
            {
                Logger.WriteLine("InstallRuntimes() succeeded.");
            }
            else if (!Variables.vcRedist)
            {
                Logger.Warn("vcredist.exe installation failed.");
                Common.RuntimeInstallError("Visual C++ Runtime", "Visual C++ Runtime is used for MicaForEveryone and AccentColorizer.", "https://aka.ms/vs/17/release/vc_redist.x64.exe");
            }
            else if (!Variables.core31)
            {
                Logger.Warn("core31.exe installation failed.");
                Common.RuntimeInstallError(".NET Core 3.1", ".NET Core 3.1 is used for MicaForEveryone.", "https://dotnet.microsoft.com/en-us/download/dotnet/3.1");
            }
            Logger.WriteLine("══════════════════════════════════════════════");

            // some random issue where the installer's frame gets extended
            if (!Theme.IsUsingDarkMode) DarkMode.UpdateFrame(frm, false);


            // theme
            if (InstallOptions.InstallThemes)
            {
                frm.InstallerProgress = "Installing Themes";
                if (!Themes.Install()) return false;
            }

            // extras
            if (InstallOptions.InstallExtras())
            {
                frm.InstallerProgress = "Installing extras";
                if (!Extras.Install(frm)) return false;
            }

            // Icons
            if (InstallOptions.iconsList.Count > 0)
            {
                if (!Icons.Install(frm)) return false;
            }

            frm.InstallerProgress = "Creating uninstaller";
            if (!Common.CreateUninstall())
            {
                Logger.WriteLine("CreateUninstall() failed");
                return false;
            }
            Logger.WriteLine("CreateUninstall() succeeded");

            InstallStatus.IsRectify11Installed = true;
            Logger.WriteLine("══════════════════════════════════════════════");

            try
            {
                // create restore point
                frm.InstallerProgress = "End creating a restore point";
                await Task.Run(() => CreateSystemRestorePoint(true));
            }
            catch
            {
                //ignored
            }

            // cleanup
            frm.InstallerProgress = "Cleaning up...";
            Logger.WriteLine("Cleaning up");
            Logger.WriteLine("───────────");
            if (!await Task.Run(() => Common.Cleanup()))
            {
                Logger.WriteLine("Cleanup() failed");
                return false;
            }
            Logger.WriteLine("Cleanup() succeeded");
            Logger.WriteLine("══════════════════════════════════════════════");
            Logger.CommitLog();
            return true;
        }
        #endregion
    }
}

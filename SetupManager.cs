using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;

namespace ZeroFN;

public static class SetupManager
{
    private const string InterceptionReleasesApi = "https://github.com/oblitum/Interception/releases/latest";
    private const string FallbackDownloadUrl = "https://github.com/oblitum/Interception/releases/download/v1.0.1/Interception.zip";

    /// <summary>
    /// Ensures interception.dll exists and the driver is installed.
    /// Returns true if all requirements are met; false if the app should exit.
    /// </summary>
    public static bool EnsureRequirements()
    {
        string baseDir = AppContext.BaseDirectory;
        string dllPath = Path.Combine(baseDir, "interception.dll");

        // Step 1 — Ensure interception.dll exists
        if (!File.Exists(dllPath))
        {
            if (!DownloadInterceptionDll(baseDir))
                return false;
        }

        // Step 2 — Check if driver is installed
        IntPtr ctx;
        try
        {
            ctx = Interception.CreateContext();
        }
        catch (DllNotFoundException)
        {
            MessageBox.Show(
                "interception.dll could not be loaded.\n\nPlease ensure the x64 version of interception.dll is placed next to ZeroFN.exe.",
                "ZeroFN — DLL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (ctx == IntPtr.Zero)
        {
            // Driver not installed
            return PromptDriverInstall(baseDir);
        }

        // Driver works — clean up the test context
        Interception.DestroyContext(ctx);
        return true;
    }

    private static bool DownloadInterceptionDll(string baseDir)
    {
        var result = MessageBox.Show(
            "Interception driver files are missing.\n\nDownload them automatically from GitHub?",
            "ZeroFN — First Run Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            MessageBox.Show(
                "Please download Interception manually from:\nhttps://github.com/oblitum/Interception/releases\n\nPlace interception.dll (x64) next to ZeroFN.exe.",
                "ZeroFN — Manual Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        string? zipPath = null;
        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(30);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ZeroFN/1.0");

            // Download the release zip
            zipPath = Path.Combine(Path.GetTempPath(), "Interception_download.zip");

            using (var response = http.GetAsync(FallbackDownloadUrl).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                using var fs = File.Create(zipPath);
                response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
            }

            // Extract interception.dll and install-interception.exe
            ExtractFromZip(zipPath, baseDir);

            if (!File.Exists(Path.Combine(baseDir, "interception.dll")))
            {
                MessageBox.Show(
                    "Download succeeded but interception.dll was not found in the archive.\n\nPlease download manually from:\nhttps://github.com/oblitum/Interception/releases",
                    "ZeroFN — Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not download required files.\n\n{ex.Message}\n\nPlease download Interception manually from:\nhttps://github.com/oblitum/Interception/releases\n\nPlace interception.dll next to ZeroFN.exe.",
                "ZeroFN — Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            try { if (zipPath != null && File.Exists(zipPath)) File.Delete(zipPath); } catch { }
        }
    }

    private static void ExtractFromZip(string zipPath, string baseDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        // Find interception.dll (x64 version)
        var dllEntry = archive.Entries.FirstOrDefault(e =>
            e.FullName.Contains("x64", StringComparison.OrdinalIgnoreCase) &&
            e.Name.Equals("interception.dll", StringComparison.OrdinalIgnoreCase));

        // Fallback: any interception.dll
        dllEntry ??= archive.Entries.FirstOrDefault(e =>
            e.Name.Equals("interception.dll", StringComparison.OrdinalIgnoreCase));

        if (dllEntry != null)
        {
            dllEntry.ExtractToFile(Path.Combine(baseDir, "interception.dll"), overwrite: true);
        }

        // Find install-interception.exe (the command-line installer)
        var installerEntry = archive.Entries.FirstOrDefault(e =>
            e.Name.Equals("install-interception.exe", StringComparison.OrdinalIgnoreCase));

        if (installerEntry != null)
        {
            string installerDir = Path.Combine(baseDir, "setup");
            Directory.CreateDirectory(installerDir);
            installerEntry.ExtractToFile(Path.Combine(installerDir, "install-interception.exe"), overwrite: true);
        }
    }

    private static bool PromptDriverInstall(string baseDir)
    {
        string installerPath = Path.Combine(baseDir, "setup", "install-interception.exe");

        if (!File.Exists(installerPath))
        {
            MessageBox.Show(
                "The Interception driver is not installed, and the installer was not found.\n\nPlease download Interception from:\nhttps://github.com/oblitum/Interception/releases\n\nRun 'install-interception.exe /install' as administrator, then reboot.",
                "ZeroFN — Driver Not Installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        var result = MessageBox.Show(
            "The Interception driver needs to be installed.\n\nThis requires administrator privileges and a reboot.\n\nInstall now?",
            "ZeroFN — Driver Installation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            MessageBox.Show(
                "ZeroFN cannot run without the Interception driver.\n\nYou can install it manually by running:\ninstall-interception.exe /install\n\nThen reboot your PC.",
                "ZeroFN — Setup Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/install",
                Verb = "runas",
                UseShellExecute = true,
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
            {
                MessageBox.Show(
                    $"The installer exited with code {process?.ExitCode}.\n\nPlease try running install-interception.exe /install manually as administrator.",
                    "ZeroFN — Installation Issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Prompt reboot
            var rebootResult = MessageBox.Show(
                "Driver installed successfully!\n\nA reboot is required for it to take effect.\n\nReboot now?",
                "ZeroFN — Reboot Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (rebootResult == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 5",
                    UseShellExecute = true,
                });
            }
            else
            {
                MessageBox.Show(
                    "Please reboot manually before using ZeroFN.",
                    "ZeroFN — Reboot Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return false; // App should exit either way — driver not active until reboot
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined UAC prompt
            MessageBox.Show(
                "Administrator privileges are required to install the driver.\n\nPlease run ZeroFN again and accept the UAC prompt.",
                "ZeroFN — Elevation Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }
}

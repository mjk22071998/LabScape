using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LabScape.Setup;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        bool isSilent = false;
        foreach (var arg in args)
        {
            if (arg.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/silent", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/quiet", StringComparison.OrdinalIgnoreCase))
            {
                isSilent = true;
            }
        }

        // Verify Administrator Privileges
        if (!IsAdministrator())
        {
            if (isSilent)
            {
                Environment.Exit(1);
            }
            else
            {
                MessageBox.Show(
                    "Administrator privileges are required to install LabScape into Program Files.\nPlease re-run this setup as an Administrator.",
                    "LabScape Setup — Elevation Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        if (isSilent)
        {
            PerformInstallation(null, null);
        }
        else
        {
            Application.Run(new SetupForm());
        }
    }

    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void PerformInstallation(Action<int, string>? progressCallback, Action<bool>? completionCallback)
    {
        try
        {
            string installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "LabScape");

            progressCallback?.Invoke(10, "Preparing installation directory...");
            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            // Extract payload
            progressCallback?.Invoke(25, "Extracting application binaries and offline vector stencils...");
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("LabScape.Setup.Resources.payload.zip"))
            {
                if (stream != null)
                {
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                    archive.ExtractToDirectory(installDir, overwriteFiles: true);
                }
                else
                {
                    // Fallback to neighboring dist/publish if running during local development
                    var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "dist", "publish");
                    if (Directory.Exists(fallbackPath))
                    {
                        CopyDirectory(fallbackPath, installDir);
                    }
                }
            }

            // Create Shortcuts
            progressCallback?.Invoke(65, "Creating Start Menu and Desktop shortcuts...");
            string appExe = Path.Combine(installDir, "LabScape.App.exe");
            string commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            string startMenuDir = Path.Combine(commonStartMenu, "LabScape");
            if (!Directory.Exists(startMenuDir))
            {
                Directory.CreateDirectory(startMenuDir);
            }

            CreateShortcut(Path.Combine(startMenuDir, "LabScape.lnk"), appExe, "Enterprise Physical Floor Plan Layout Designer");
            CreateShortcut(Path.Combine(startMenuDir, "LabScape Web Viewer.lnk"), Path.Combine(installDir, "web-viewer", "index.html"), "LabScape HTML5 2D Canvas Web Viewer");

            string commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            CreateShortcut(Path.Combine(commonDesktop, "LabScape.lnk"), appExe, "LabScape Layout Designer");

            // Register File Associations
            progressCallback?.Invoke(80, "Configuring .layout file associations...");
            RegisterFileAssociation(".layout", "LabScape.LayoutDocument", "LabScape Floorplan Document", appExe);

            // Register Add/Remove Programs Uninstaller
            progressCallback?.Invoke(90, "Registering Windows Uninstaller...");
            RegisterUninstaller(installDir, appExe);

            progressCallback?.Invoke(100, "Installation complete!");
            completionCallback?.Invoke(true);
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke(0, "Error: " + ex.Message);
            completionCallback?.Invoke(false);
            if (progressCallback == null)
            {
                throw;
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(sourceDir, targetDir));
        }
        foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(sourceDir, targetDir), true);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Description = description;
            shortcut.Save();
        }
        catch
        {
            // Ignore shortcut creation failures if COM is unavailable
        }
    }

    private static void RegisterFileAssociation(string extension, string progId, string description, string appPath)
    {
        try
        {
            using var extKey = Registry.ClassesRoot.CreateSubKey(extension);
            extKey.SetValue("", progId);

            using var progIdKey = Registry.ClassesRoot.CreateSubKey(progId);
            progIdKey.SetValue("", description);

            using var defaultIcon = progIdKey.CreateSubKey("DefaultIcon");
            defaultIcon.SetValue("", $"\"{appPath}\",0");

            using var command = progIdKey.CreateSubKey(@"shell\open\command");
            command.SetValue("", $"\"{appPath}\" \"%1\"");
        }
        catch
        {
            // Registry write optional fallback
        }
    }

    private static void RegisterUninstaller(string installDir, string appPath)
    {
        try
        {
            const string uninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LabScape";
            using var key = Registry.LocalMachine.CreateSubKey(uninstallKeyPath);
            key.SetValue("DisplayName", "LabScape Enterprise Layout Designer");
            key.SetValue("DisplayVersion", "1.0.0");
            key.SetValue("Publisher", "LabScape Solutions");
            key.SetValue("InstallLocation", installDir);
            key.SetValue("DisplayIcon", $"\"{appPath}\",0");
            key.SetValue("UninstallString", $"cmd.exe /c rmdir /s /q \"{installDir}\"");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
        catch
        {
            // Fallback
        }
    }
}

public class SetupForm : Form
{
    private readonly Label _lblHeader;
    private readonly Label _lblStatus;
    private readonly ProgressBar _progressBar;
    private readonly Button _btnInstall;
    private readonly Button _btnCancel;
    private readonly CheckBox _chkLaunch;
    private readonly TextBox _txtPath;

    public SetupForm()
    {
        Text = "LabScape Setup — v1.0.0";
        Width = 560;
        Height = 360;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = System.Drawing.Color.FromArgb(249, 250, 251);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = System.Drawing.Color.FromArgb(30, 41, 59)
        };
        _lblHeader = new Label
        {
            Text = "LabScape Floorplan Designer Setup",
            ForeColor = System.Drawing.Color.White,
            Font = new System.Drawing.Font("Segoe UI", 13, System.Drawing.FontStyle.Bold),
            Location = new System.Drawing.Point(20, 15),
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Offline Enterprise Workstation & Hardware Layout Designer",
            ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
            Font = new System.Drawing.Font("Segoe UI", 9),
            Location = new System.Drawing.Point(22, 40),
            AutoSize = true
        };
        pnlHeader.Controls.Add(_lblHeader);
        pnlHeader.Controls.Add(lblSub);
        Controls.Add(pnlHeader);

        // Content
        var lblDest = new Label
        {
            Text = "Installation Folder:",
            Location = new System.Drawing.Point(24, 90),
            Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold),
            AutoSize = true
        };
        Controls.Add(lblDest);

        string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LabScape");
        _txtPath = new TextBox
        {
            Text = defaultPath,
            ReadOnly = true,
            Location = new System.Drawing.Point(24, 115),
            Width = 490,
            Font = new System.Drawing.Font("Segoe UI", 9)
        };
        Controls.Add(_txtPath);

        _lblStatus = new Label
        {
            Text = "Click 'Install' to begin offline installation.",
            Location = new System.Drawing.Point(24, 160),
            Width = 490,
            Font = new System.Drawing.Font("Segoe UI", 9),
            ForeColor = System.Drawing.Color.FromArgb(75, 85, 99)
        };
        Controls.Add(_lblStatus);

        _progressBar = new ProgressBar
        {
            Location = new System.Drawing.Point(24, 185),
            Width = 490,
            Height = 22,
            Style = ProgressBarStyle.Continuous
        };
        Controls.Add(_progressBar);

        _chkLaunch = new CheckBox
        {
            Text = "Launch LabScape when installation completes",
            Location = new System.Drawing.Point(24, 220),
            Checked = true,
            AutoSize = true,
            Font = new System.Drawing.Font("Segoe UI", 9)
        };
        Controls.Add(_chkLaunch);

        // Action Buttons
        _btnInstall = new Button
        {
            Text = "Install",
            Location = new System.Drawing.Point(340, 270),
            Width = 85,
            Height = 30,
            BackColor = System.Drawing.Color.FromArgb(37, 99, 235),
            ForeColor = System.Drawing.Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
        };
        _btnInstall.Click += BtnInstall_Click;
        Controls.Add(_btnInstall);

        _btnCancel = new Button
        {
            Text = "Cancel",
            Location = new System.Drawing.Point(435, 270),
            Width = 80,
            Height = 30,
            Font = new System.Drawing.Font("Segoe UI", 9)
        };
        _btnCancel.Click += (s, e) => Close();
        Controls.Add(_btnCancel);
    }

    private void BtnInstall_Click(object? sender, EventArgs e)
    {
        _btnInstall.Enabled = false;
        _btnCancel.Enabled = false;

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            Program.PerformInstallation(
                (pct, msg) => Invoke(() =>
                {
                    _progressBar.Value = Math.Clamp(pct, 0, 100);
                    _lblStatus.Text = msg;
                }),
                success => Invoke(() =>
                {
                    if (success)
                    {
                        _lblStatus.Text = "Installation finished successfully!";
                        _btnCancel.Text = "Finish";
                        _btnCancel.Enabled = true;

                        if (_chkLaunch.Checked)
                        {
                            string appExe = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                "LabScape",
                                "LabScape.App.exe");
                            if (File.Exists(appExe))
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = appExe,
                                    UseShellExecute = true
                                });
                            }
                        }
                    }
                    else
                    {
                        _lblStatus.Text = "Installation failed. See error details.";
                        _btnCancel.Enabled = true;
                    }
                }));
        });
    }
}

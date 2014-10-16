using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Mihari
{
    using Microsoft.Win32;
    using System.Collections.ObjectModel;
    using System.Deployment.Application;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Threading;

    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private const string MESSAGE_FORMAT = "{0} at {1}"; 

        private static System.Windows.Forms.NotifyIcon notifyIcon = null;
        private static string exeName = string.Empty;
        private static string exeFullPath = string.Empty;
        private static string exeDir = string.Empty;
        private static string exeVersion = string.Empty;
        private static string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Mihari.log");
        private ObservableCollection<FileOperationModel> log = new ObservableCollection<FileOperationModel>();

        public ObservableCollection<FileOperationModel> Log { get { return log; } }
        public string AppName { get { return exeName; } }
        public string AppVersion { get { return exeVersion.ToString(); } }
        public string FileExtsToWatch { get { return Mihari.Properties.Settings.Default.FileExtsToWatch; } }
        public bool IgnoreRerecycleBin { get { return Mihari.Properties.Settings.Default.IgnoreRerecycleBin; } }
        public bool IgnoreCreated { get { return Mihari.Properties.Settings.Default.IgnoreCreated; } }
        public bool IgnoreDeleted { get { return Mihari.Properties.Settings.Default.IgnoreDeleted; } }
        public bool IgnoreChanged { get { return Mihari.Properties.Settings.Default.IgnoreChanged; } }
        public bool IgnoreRenamed { get { return Mihari.Properties.Settings.Default.IgnoreRenamed; } }
        public static bool ToastEnabled { get { return Mihari.Properties.Settings.Default.ToastEnabled; } }
        public static bool RegisterStartUp { get { return Mihari.Properties.Settings.Default.RegisterStartUp; } }
        public int HowLongLogKeeps { get { return Mihari.Properties.Settings.Default.HowLongLogKeeps; } }

        [DllImportAttribute("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImportAttribute("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImportAttribute("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        public static void Main(string[] args)
        {
            const string appID = "d508ee35-ad63-45a1-8403-909d4b784a9b";

            exeName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            exeFullPath = Environment.GetCommandLineArgs()[0];
            exeDir = Path.GetDirectoryName(exeFullPath);
            exeVersion = GetVersion();

            RegisterShortcutToStartMenu(exeName); // Need for Toast
            RegisterStartup(RegisterStartUp);

            using (var mutex = new Mutex(false, appID))
            {
                if (mutex.WaitOne(TimeSpan.Zero, false))
                {
                    var app = new App();
                    app.InitializeComponent();
                    app.Run();
                }
                else
                {
                    Notify(exeName, "Already running.");
                }

                mutex.ReleaseMutex();
            }
        }

        private void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Record(e.FullPath, e.ChangeType);
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Record(e.FullPath, e.ChangeType);
        }

        private void Record(string fullPath, WatcherChangeTypes changeType)
        {
            if (IgnoreRerecycleBin && fullPath.Contains(true, @"\$RECYCLE.BIN\")) return;
            if (IgnoreCreated && changeType == WatcherChangeTypes.Created) return;
            if (IgnoreDeleted && changeType == WatcherChangeTypes.Deleted) return;
            if (IgnoreChanged && changeType == WatcherChangeTypes.Changed) return;
            if (IgnoreRenamed && changeType == WatcherChangeTypes.Renamed) return;

            var now = DateTime.Now;

            Application.Current.Dispatcher.Invoke(
                new Action(() => { 
                    log.Insert(0, new FileOperationModel() 
                    { 
                        DateTime = now.ToString(), 
                        FilePath = fullPath, 
                        ChangeType = changeType, 
                    });
                }), 
                DispatcherPriority.Background, null
            );

            var fileName = Path.GetFileName(fullPath);
            var message = string.Format(MESSAGE_FORMAT, changeType, now);
            Console.WriteLine(message);

            Notify(fileName, message);

            SaveLogFile(logPath);
        }

        private static void Notify(string title, string message)
        {
            if (ToastEnabled && Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
                ShellHelpers.Toast.Show(exeName, title, message);
            else
                notifyIcon.ShowBalloonTip(3 * 1000, title, message, System.Windows.Forms.ToolTipIcon.Warning);
        }

        private void SaveLogFile(string filename)
        {
            try
            {
                var content = log.Aggregate(string.Empty, (working, next) =>
                {
                    if (DateTime.Parse(next.DateTime) > DateTime.Now.AddDays(-HowLongLogKeeps))
                        return working + string.Format("{0}\t{1}\t{2}\r\n", next.DateTime, next.ChangeType, next.FilePath);
                    else
                        return working;
                });

                var dir = Path.GetDirectoryName(filename);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllText(filename, content);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
            }
        }

        private void LoadLogFile(string filename)
        {
            try
            {
                var content = File.ReadAllText(filename);
                var lines = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var values = line.Split('\t');
                    log.Add(new FileOperationModel
                    {
                        DateTime = values[0],
                        ChangeType = (WatcherChangeTypes)Enum.Parse(typeof(WatcherChangeTypes), values[1]),
                        FilePath = values[2],
                    });
                }
            }
            catch
            {

            }
        }

        private MainWindow logWindow = null;
        private MainWindow LogWindow { get { if (logWindow == null) logWindow = new MainWindow(); return logWindow; } }
        private SettingWindow settingWindow = null;
        private SettingWindow SettingWindow { get { if (settingWindow == null) settingWindow = new SettingWindow(); return settingWindow; } }

        private void SetupNitfyIcon()
        {
            var menuStrip = new System.Windows.Forms.ContextMenuStrip();
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Show Setting Dialog", null, (s, a) => { SettingWindow.ShowForeground(); }));
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Show Log Window", null, (s, a) => { LogWindow.ShowForeground(); }));
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Exit", null, (s, a) => { Shutdown(); }));

            notifyIcon = new System.Windows.Forms.NotifyIcon()
            {
                Text = exeName,
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(exeFullPath),
                Visible = true,
                ContextMenuStrip = menuStrip,
            };

            notifyIcon.MouseDoubleClick += (s, a) =>
            {
                LogWindow.ShowForeground();
            };
        }

        private static IEnumerable<FileSystemWatcher> watchers = null;

        public void SetupWatcher()
        {
            if (watchers != null)
            {
                foreach (var watcher in watchers)
                {
                    watcher.EnableRaisingEvents = false; // これをやってもイベントが重複して飛んでくる。うまくうごかん。
                    watcher.Dispose();
                }
            }

            watchers = FileExtsToWatch.Split(';').SelectMany(ext => Directory.GetLogicalDrives().Select(drive =>
            {
                {
                    var watcher = new FileSystemWatcher()
                    {
                        Path = drive,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                        Filter = ext,
                        IncludeSubdirectories = true,
                    };

                    watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                    watcher.Created += new FileSystemEventHandler(watcher_Changed);
                    watcher.Deleted += new FileSystemEventHandler(watcher_Changed);
                    watcher.Renamed += new RenamedEventHandler(watcher_Renamed);

                    return watcher;
                }
            }));

            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        private static string GetVersion()
        {
            // ClickOnce のバージョンとアセンブリのバージョン、なんで別やねん

            if (!ApplicationDeployment.IsNetworkDeployed) return String.Empty;

            var version = ApplicationDeployment.CurrentDeployment.CurrentVersion;
            return string.Format(
                "{0}.{1}.{2}.{3}",
                version.Major, version.Minor, version.Build, version.Revision
            );
        }

        private static void RegisterShortcutToStartMenu(string APP_ID)
        {
            var shortcutPath = string.Format(
                @"{0}\{1}\{1}.lnk",
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                APP_ID);

            if (!File.Exists(shortcutPath))
            {
                ShellHelpers.Shortcut.Install(APP_ID, shortcutPath);
            }
        }

        private static void RegisterStartup(bool enabled)
        {
            var registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (enabled)
            {
                registryKey.SetValue(exeName, exeFullPath);
            }
            else
            {
                registryKey.DeleteValue(exeName);
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadLogFile(logPath);

            SetupNitfyIcon();
            SetupWatcher();

            Notify(exeName, "Monitoring is started.");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SaveLogFile(logPath);

            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }
        }
    }
}

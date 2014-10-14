using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Mihari
{
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

        private static Mutex mutex = null;
        private static MainWindow mainWindow = null;
        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        private string exeName = string.Empty;
        private string exePath = string.Empty;
        private string exeDir = string.Empty;
        private string exeVersion = string.Empty;
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
        public bool ToastEnabled { get { return Mihari.Properties.Settings.Default.ToastEnabled; } }
        public int HowLongLogKeeps { get { return Mihari.Properties.Settings.Default.HowLongLogKeeps; } }

        [DllImportAttribute("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImportAttribute("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImportAttribute("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void ShowToFront(string windowName)
        {
            IntPtr firstInstance = FindWindow(null, windowName);
            ShowWindow(firstInstance, 1);
            SetForegroundWindow(firstInstance);
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
            if (IgnoreRerecycleBin && fullPath.ToUpper().Contains(@"\$RECYCLE.BIN\")) return;
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

            var content = log.Aggregate(string.Empty, (working, next) =>
            {
                if (DateTime.Parse(next.DateTime) > DateTime.Now.AddDays(-HowLongLogKeeps))
                    return working + string.Format("{0}\t{1}\t{2}\r\n", next.DateTime, next.ChangeType, next.FilePath);
                else
                    return working;
            });

            try
            {
                File.WriteAllText(Path.Combine(exeDir, "log.txt"), content);
            }
            catch(Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
            }
        }

        private void Notify(string title, string message)
        {
            if (ToastEnabled && Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
                ShellHelpers.Toast.Show(exeName, title, message);
            else
                notifyIcon.ShowBalloonTip(3 * 1000, title, message, System.Windows.Forms.ToolTipIcon.Warning);
        }

        private void LoadLogFile(string filename = "log.txt")
        {
            try
            {
                var content = File.ReadAllText(Path.Combine(exeDir, filename));
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

        private void SetupNitfyIcon()
        {
            var menuStrip = new System.Windows.Forms.ContextMenuStrip();
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Show Setting Dialog", null, (s, a) => { new SettingWindow().ShowDialog(); }));
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Show Log Window", null, (s, a) => { mainWindow.Show(); }));
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Exit", null, (s, a) => { Shutdown(); }));

            notifyIcon = new System.Windows.Forms.NotifyIcon()
            {
                Text = exeName,
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath),
                Visible = true,
                ContextMenuStrip = menuStrip,
            };

            notifyIcon.MouseDoubleClick += (s, a) =>
            {
                mainWindow.Show();
            };
        }

        private static IEnumerable<FileSystemWatcher> watchers = null;

        public void SetupWatcher()
        {
            watchers = FileExtsToWatch.Split(';').SelectMany(ext => Directory.GetLogicalDrives().Select(drive =>
            {
                {
                    var watcher = new FileSystemWatcher()
                    {
                        Path = drive,
                        NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName,
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

            foreach (var watcher in watchers) watcher.EnableRaisingEvents = true;

            Notify(exeName, "Monitoring is started.");
        }

        private string GetVersion()
        {
            if (!ApplicationDeployment.IsNetworkDeployed) return String.Empty;

            var version = ApplicationDeployment.CurrentDeployment.CurrentVersion;
            return string.Format(
                "{0}.{1}.{2}.{3}",
                version.Major,
                version.Minor,
                version.Build,
                version.Revision
            );
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // foreach (string arg in e.Args)
            // {
            //     switch (arg)
            //     {
            //         case "/start":
            // 
            //             break;
            //         case "/stop":
            // 
            //             break;
            //         default:
            // 
            //             break;
            //     }
            // }

            exeName = this.GetType().Assembly.GetName().Name;
            exePath = Environment.GetCommandLineArgs().First();
            exeDir = Path.GetDirectoryName(exePath);
            exeVersion = GetVersion(); // this.GetType().Assembly.GetName().Version.ToString();

            mutex = new Mutex(false, exeName);

            if (!mutex.WaitOne(TimeSpan.Zero, false))
            {
                ShowToFront(exeName);
                // mutex.ReleaseMutex();
                mutex.Close();
                mutex = null;
                Shutdown();
                return;
            }

            mainWindow = new MainWindow();

            LoadLogFile();
            ShellHelpers.Shortcut.Create(exeName); // Need for Toast
            SetupNitfyIcon();
            SetupWatcher();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
                mutex = null;
            }

            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }
        }
    }
}

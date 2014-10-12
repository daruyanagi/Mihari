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
    using System.IO;
    using System.Threading;
    using System.Windows.Threading;

    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private const string MESSAGE_FORMAT = "{0} at {1}"; 

        private Mutex mutex = null;
        private MainWindow mainWindow = null;
        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        private string exeName = string.Empty;
        private string exePath = string.Empty;
        private string exeDir = string.Empty;
        private string exeVersion = string.Empty;
        private string exeNameAndVersion = string.Empty;
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
            if (IgnoreRerecycleBin && fullPath.Contains(@"$Recycle.Bin\")) return;
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
            File.WriteAllText(Path.Combine(exeDir, "log.txt"), content);
        }

        private void Notify(string title, string message)
        {
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2 && ToastEnabled)
                ShellHelpers.Toast.Show(exeName, title, message);
            else
                notifyIcon.ShowBalloonTip(3 * 1000, title, message, System.Windows.Forms.ToolTipIcon.Warning);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            foreach (string arg in e.Args)
            {
                switch (arg)
                {
                    case "/start":

                        break;
                    case "/stop":

                        break;
                    default:

                        break;
                }
            }

            exeName = this.GetType().Assembly.GetName().Name;
            exePath = Environment.GetCommandLineArgs().First();
            exeDir = Path.GetDirectoryName(exePath);
            exeVersion = this.GetType().Assembly.GetName().Version.ToString();
            exeNameAndVersion = string.Format("{0} v{1}", exeName, exeVersion);

            mutex = new Mutex(false, exeName);

            if (!mutex.WaitOne(TimeSpan.Zero, false))
            {
                mainWindow.Show();
                mutex.Close();
                Shutdown();
                return;
            }

            try
            {
                var content = File.ReadAllText(Path.Combine(exeDir, "log.txt"));
                var lines = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var values = line.Split('\t');
                    log.Add(new FileOperationModel
                    {
                        DateTime = values[0],
                        ChangeType = (WatcherChangeTypes) Enum.Parse(typeof(WatcherChangeTypes), values[1]),
                        FilePath = values[2], 
                    });
                }
            }
            catch
            {

            }

            ShellHelpers.Shortcut.Create(exeName); // Need for Toast

            var watchers = FileExtsToWatch.Split(';').SelectMany(ext => Directory.GetLogicalDrives().Select(drive => {
            {
                var watcher = new FileSystemWatcher();
                watcher.Path = drive;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
                watcher.Filter = ext;
                watcher.IncludeSubdirectories = true;

                watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                watcher.Created += new FileSystemEventHandler(watcher_Changed);
                watcher.Deleted += new FileSystemEventHandler(watcher_Changed);
                watcher.Renamed += new RenamedEventHandler(watcher_Renamed);
                return watcher;
            }}));

            mainWindow = new MainWindow();
            var menuStrip = new System.Windows.Forms.ContextMenuStrip();
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Show", null, (s, a) => { mainWindow.Show(); }));
            menuStrip.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Hide", null, (s, a) => { mainWindow.Hide(); }));
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
                mainWindow.Visibility = (mainWindow.Visibility == Visibility.Visible)
                    ? Visibility.Hidden
                    : Visibility.Visible;
            };

            foreach (var watcher in watchers) watcher.EnableRaisingEvents = true;

            Notify(exeNameAndVersion, "Monitoring is started.");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
                mutex = null;
            }

            notifyIcon.Dispose();
        }
    }
}

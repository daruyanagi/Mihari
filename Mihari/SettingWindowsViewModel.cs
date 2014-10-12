using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mihari
{
    public class SettingWindowsViewModel : BindableBase
    {
        public string FileExtsToWatch
        {
            get { return Mihari.Properties.Settings.Default.FileExtsToWatch; }
            set { Mihari.Properties.Settings.Default.FileExtsToWatch = value; RaisePropertyChanged("FileExtsToWatch"); }
        }

        public bool IgnoreRerecycleBin
        {
            get { return Mihari.Properties.Settings.Default.IgnoreRerecycleBin; }
            set { Mihari.Properties.Settings.Default.IgnoreRerecycleBin = value; RaisePropertyChanged("IgnoreRerecycleBin"); }
        }

        public bool IgnoreCreated
        {
            get { return Mihari.Properties.Settings.Default.IgnoreCreated; }
            set { Mihari.Properties.Settings.Default.IgnoreCreated = value; RaisePropertyChanged("IgnoreCreated"); }
        }

        public bool IgnoreDeleted
        {
            get { return Mihari.Properties.Settings.Default.IgnoreDeleted; }
            set { Mihari.Properties.Settings.Default.IgnoreDeleted = value; RaisePropertyChanged("IgnoreDeleted"); }
        }

        public bool IgnoreChanged
        {
            get { return Mihari.Properties.Settings.Default.IgnoreChanged; }
            set { Mihari.Properties.Settings.Default.IgnoreChanged = value; RaisePropertyChanged("IgnoreChanged"); }
        }

        public bool IgnoreRenamed
        {
            get { return Mihari.Properties.Settings.Default.IgnoreRenamed; }
            set { Mihari.Properties.Settings.Default.IgnoreRenamed = value; RaisePropertyChanged("IgnoreRenamed"); }
        }

        public bool ToastEnabled
        {
            get { return Mihari.Properties.Settings.Default.ToastEnabled; }
            set { Mihari.Properties.Settings.Default.ToastEnabled = value; RaisePropertyChanged("ToastEnabled"); }
        }

        public int HowLongLogKeeps
        {
            get { return Mihari.Properties.Settings.Default.HowLongLogKeeps; }
            set { Mihari.Properties.Settings.Default.HowLongLogKeeps = value; RaisePropertyChanged("HowLongLogKeeps"); }
        }

        public string MaliciousExes
        {
            get { return Mihari.Properties.Settings.Default.MaliciousExes; }
            set { Mihari.Properties.Settings.Default.MaliciousExes = value; RaisePropertyChanged("MaliciousExes"); }
        }
    }
}

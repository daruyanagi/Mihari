using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mihari
{
    public class MainWindowViewModel: BindableBase
    {
        public string AppName
        {
            get { return (App.Current as App).AppName; }
            set { }
        }

        public string AppVersion
        {
            get { return (App.Current as App).AppVersion; }
            set { }
        }

        private string filter = string.Empty;
        public string Filter
        {
            get { return filter; }
            set { SetProperty(ref filter, value); }
        }

        private bool isFilterStartDateEnabled = false;
        public bool IsFilterStartDateEnabled
        {
            get { return isFilterStartDateEnabled; }
            set { SetProperty(ref isFilterStartDateEnabled, value); }
        }

        private DateTime filterStartDate = DateTime.Today;
        public DateTime FilterStartDate
        {
            get { return filterStartDate; }
            set { SetProperty(ref filterStartDate, value); }
        }

        private bool isFilterEndDateEnabled = false;
        public bool IsFilterEndDateEnabled
        {
            get { return isFilterEndDateEnabled; }
            set { SetProperty(ref isFilterEndDateEnabled, value); }
        }

        private DateTime filterEndDate = DateTime.Today;
        public DateTime FilterEndDate
        {
            get { return filterEndDate; }
            set { SetProperty(ref filterEndDate, value); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mihari
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel = new MainWindowViewModel();
        private ICollectionView defaultView = null;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = viewModel;

            var log = (App.Current as App).Log;

            defaultView = CollectionViewSource.GetDefaultView(log);

            defaultView.Filter = (_) => 
            {
                var obj = _ as FileOperationModel;

                if (viewModel.IsFilterStartDateEnabled && DateTime.Parse(obj.DateTime) <= viewModel.FilterStartDate)
                    return false;

                if (viewModel.IsFilterEndDateEnabled && viewModel.FilterEndDate.AddDays(1) < DateTime.Parse(obj.DateTime))
                    return false;

                if (string.IsNullOrEmpty(viewModel.Filter))
                    return true;

                if (obj.FilePath.WildcardMatch(viewModel.Filter))
                    return true;
                
                return false;
            };

            listView.ItemsSource = defaultView;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var file = ((Hyperlink) sender).Tag as string;

            if (File.Exists(file))
            {
                var info = new ProcessStartInfo("explorer.exe", string.Format(@"/select,""{0}""", file));
                var process = new Process() { StartInfo = info, };
                process.Start();
            }
            else
            {
                var dir = Path.GetDirectoryName(file);

                if (Directory.Exists(dir))
                {
                    var info = new ProcessStartInfo("explorer.exe", dir);
                    var process = new Process() { StartInfo = info, };
                    process.Start();
                }
                else
                {
                    MessageBox.Show(
                        "Directory not found.\r\n" + dir, "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            defaultView.Refresh();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            defaultView.Refresh();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            defaultView.Refresh();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void Window_StateChanged(object sender, EventArgs e)
        {

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingWindow().ShowDialog();
        }
    }
}

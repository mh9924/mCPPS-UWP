using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using mCPPS.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace mCPPS.Views
{
    public sealed partial class ServerPage : Page, INotifyPropertyChanged
    {
        // TODO WTS: Change the grid as appropriate to your app.
        // For help see http://docs.telerik.com/windows-universal/controls/raddatagrid/gettingstarted
        // You may also want to extend the grid to work with the RadDataForm http://docs.telerik.com/windows-universal/controls/raddataform/dataform-gettingstarted
        public ServerPage()
        {
            InitializeComponent();
            Loaded += ServerPage_Loaded;
        }

        private void ServerPage_Loaded(object sender, RoutedEventArgs e)
        {
            grid.ItemsSource = MServer.logs;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private async void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!MServer.IsStarted())
            {
                MServer.StartServers();
                server_toggle.IsEnabled = false;

                await Task.Delay(600);

                if (MServer.IsStarted())
                    server_toggle.Content = "Stop Server";

                server_toggle.IsEnabled = true;
            }
            else
            {
                await MServer.StopServers();

                server_toggle.IsEnabled = true;
                server_toggle.Content = "Start Server";
            }

        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

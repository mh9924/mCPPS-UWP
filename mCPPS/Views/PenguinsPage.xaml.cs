using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using mCPPS.Models;

using Microsoft.Toolkit.Uwp.UI.Controls;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace mCPPS.Views
{
    public sealed partial class MasterDetailPage : Page, INotifyPropertyChanged
    {
        private Penguin _selected;

        public Penguin Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public MasterDetailPage()
        {
            InitializeComponent();
            Loaded += MasterDetailPage_Loaded;
        }

        private void MasterDetailPage_Loaded(object sender, RoutedEventArgs e)
        {
            MasterDetailsViewControl.ItemsSource = MServer.penguins;

            if (MasterDetailsViewControl.ViewState == MasterDetailsViewState.Both && MServer.penguins.Count() != 0)
            {
                Selected = MServer.penguins.First();
            }
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

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

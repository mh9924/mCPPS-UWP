using System;

using mCPPS.Models;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace mCPPS.Views
{
    public sealed partial class MasterDetailDetailControl : UserControl
    {
        public Penguin MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as Penguin; }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public static readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem", typeof(Penguin), typeof(MasterDetailDetailControl), new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));

        public MasterDetailDetailControl()
        {
            InitializeComponent();
        }

        private static void OnMasterMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MasterDetailDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
        }

        private void TextBlock_SelectionChanged()
        {

        }
    }
}

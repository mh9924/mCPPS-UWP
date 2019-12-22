using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using mCPPS.Models;
using mCPPS.Services;

using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace mCPPS.Views
{
    public sealed partial class ConfigurationPage : Page, INotifyPropertyChanged
    {
        //// TODO WTS: Add other settings as necessary. For help see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/pages/settings-codebehind.md
        //// TODO WTS: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere

        private string _versionDescription;

        ApplicationDataContainer config = MServer.GetConfiguration();

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { Set(ref _versionDescription, value); }
        }

        public ConfigurationPage()
        {
            InitializeComponent();
            Loaded += ConfigurationPage_Loaded;
        }

        private void ConfigurationPage_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
            DB_Type_Combo.Items.Add("SQlite");
            DB_Type_Combo.Items.Add("MySQL");
            DB_Type_Combo.SelectedItem = "SQlite";
            max_penguins_Txt.IsEnabled = false;
            max_penguins_per_ip_Txt.IsEnabled = false;
            DB_Type_Combo.IsEnabled = false;

            ApplicationDataCompositeValue webServer = (ApplicationDataCompositeValue)config.Values["WebServer"];

            webserver_Ch.Toggled -= Webserver_Ch_Toggled;
            webserver_Ch.IsOn = (bool)webServer["enabled"];
            webserver_Ch.Toggled += Webserver_Ch_Toggled;

            LoadServerConfigs();
        }

        private void Initialize()
        {
            VersionDescription = GetVersionDescription();
        }

        private string GetVersionDescription()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{package.DisplayName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
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

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
            LoadServerConfigs();
        }

        private void SaveConfiguration()
        {
            Debug.WriteLine("Saving configuration");

            int a = 0;
            int w = 0;

            Int32.TryParse(auth_port_Txt.Text, out a);

            if (a >= 1024 && a <= 65535)
            {
                ApplicationDataCompositeValue DefaultAuth = new ApplicationDataCompositeValue
                {
                    ["type"] = "auth",
                    ["port"] = auth_port_Txt.Text
                };

                config.Values["DefaultAuth"] = DefaultAuth;
            }
            else
            {
                LoadServerConfigs();
                return;
            }

            string worldString = world_server_Txt.Text;
            string[] worlds = worldString.Split('|');

            foreach (string world in worlds)
            {
                try
                {
                    string first = world.Split(':')[0];
                    string second = world.Split(':')[1];
                }
                catch (IndexOutOfRangeException)
                {
                    LoadServerConfigs();
                    return;
                }
            }

            foreach (KeyValuePair<string, object> server in MServer.GetConfiguration().Values)
            {
                ApplicationDataCompositeValue curServer = (ApplicationDataCompositeValue)server.Value;

                if ((string)curServer["type"] == "world")
                    config.Values.Remove(server.Key);
            }

            List<string> usedPorts = new List<string>();

            foreach (string world in worlds)
            {
                string name = world.Split(':')[0];
                string port = world.Split(':')[1];

                Int32.TryParse(port, out w);

                if (w >= 1024 && w <= 65535 && name != "" && port != auth_port_Txt.Text && !usedPorts.Contains(port))
                {
                    ApplicationDataCompositeValue NewWorld = new ApplicationDataCompositeValue
                    {
                        ["name"] = name,
                        ["type"] = "world",
                        ["port"] = port
                    };

                    config.Values[name] = NewWorld;
                    usedPorts.Add(port);
                }
                else
                {
                    LoadServerConfigs();
                }

            }
        }

        private void LoadServerConfigs()
        {
            ApplicationDataCompositeValue auth = (ApplicationDataCompositeValue)config.Values["DefaultAuth"];

            auth_port_Txt.Text = (string)auth["port"];
            world_server_Txt.Text = "";

            int i = 0;

            foreach (KeyValuePair<string, object> server in MServer.GetConfiguration().Values)
            {
                ApplicationDataCompositeValue curServer = (ApplicationDataCompositeValue)server.Value;

                if ((string)curServer["type"] == "world")
                {
                    if (i > 0)
                        world_server_Txt.Text += "|";

                    world_server_Txt.Text += (string)curServer["name"] + ":" + (string)curServer["port"];
                    i++;
                }
            }
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void Webserver_Ch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationDataCompositeValue webServer = (ApplicationDataCompositeValue)config.Values["WebServer"];

            webServer["enabled"] = !(bool)webServer["enabled"];
            config.Values["WebServer"] = webServer;
        }
    }
}

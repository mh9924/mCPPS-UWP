using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace mCPPS.Models
{
    public static class MServer
    {
        private static Boolean started = false;
        private static ApplicationDataContainer configuration_settings = ApplicationData.Current.LocalSettings;
        private static List<MattsCPPS> servers = new List<MattsCPPS>();

        public static Boolean IsStarted() { return started; }
        public static void ToggleStarted() { started = !started; }
        public static ApplicationDataContainer GetConfiguration() { return configuration_settings; }
        public static List<MattsCPPS> GetServers() { return servers; }
        public static ObservableCollection<Penguin> penguins = new ObservableCollection<Penguin>();
        public static ObservableCollection<Log> logs = new ObservableCollection<Log>();

        static MServer()
        {
            ReloadServersFromConfig();
        }

        public static string GetCurrentLoginPort()
        {
            if (started)
                foreach (MattsCPPS server in servers)
                    if (server.GetType() == typeof(Auth))
                        return server.port;

            return null;
        }

        public static Dictionary<string, string> GetCurrentWorldNamesAndPorts()
        {
            Dictionary<string, string> worlds = new Dictionary<string, string>();

            if (started)
                foreach (MattsCPPS server in servers)
                    if (server.GetType() == typeof(World))
                        worlds[server.server_name] = server.port;

            return worlds;
        }

        public static void StartServers()
        {
            ReloadServersFromConfig();
            started = true;
            Database.InitializeDatabase("mCPPS.db");

            foreach (MattsCPPS server in servers)
                server.StartServer();

            ApplicationDataCompositeValue webServer = (ApplicationDataCompositeValue)configuration_settings.Values["WebServer"];

            if ((bool)webServer["enabled"])
            {
                HTTP http = new HTTP("DefaultHTTP", (string)webServer["port"]);
                http.StartServer();
                servers.Add(http);
            }
        }

        public static async Task StopServers()
        {
            started = false;

            foreach (MattsCPPS server in servers)
                await server.StopServer();

            servers.Clear();
            penguins.Clear();
        }

        private static void ReloadServersFromConfig()
        {
            servers.Clear();
            penguins.Clear();

            foreach (string setting_name in configuration_settings.Values.Keys)
            {
                if (configuration_settings.Values[setting_name] is ApplicationDataCompositeValue)
                {
                    ApplicationDataCompositeValue server = (ApplicationDataCompositeValue)configuration_settings.Values[setting_name];

                    string serverType = (string)server["type"];
                    string serverPort = (string)server["port"];

                    if (serverType != null)
                    {
                        if (serverType == "auth")
                        {
                            Auth auth = new Auth(setting_name, serverPort);
                            servers.Add(auth);
                        }
                        else if (serverType == "world")
                        {
                            World world = new World(setting_name, serverPort);
                            servers.Add(world);
                        }
                    }
                }
            }

            if (servers.Count == 0)
            {
                Debug.WriteLine("Server configuration settings not found. Storing default servers.");
                StoreDefaultServers();
                ReloadServersFromConfig();
            }

            if (configuration_settings.Values["WebServer"] == null)
            {
                Debug.WriteLine("Web server enabled");

                ApplicationDataCompositeValue WebServer = new ApplicationDataCompositeValue
                {
                    ["type"] = "web",
                    ["port"] = "8080",
                    ["enabled"] = true
                };

                configuration_settings.Values["WebServer"] = WebServer;
            }
        }

        private static void StoreDefaultServers()
        {
            ApplicationDataCompositeValue DefaultAuth = new ApplicationDataCompositeValue
            {
                ["type"] = "auth",
                ["port"] = "6112"
            };
            ApplicationDataCompositeValue Default1 = new ApplicationDataCompositeValue
            {
                ["name"] = "Matt's CPPS",
                ["type"] = "world",
                ["port"] = "6113"
            };
            ApplicationDataCompositeValue Default2 = new ApplicationDataCompositeValue
            {
                ["name"] = "PartyCP",
                ["type"] = "world",
                ["port"] = "6114"
            };

            configuration_settings.Values["DefaultAuth"] = DefaultAuth;
            configuration_settings.Values["Matt's CPPS"] = Default1;
            configuration_settings.Values["PartyCP"] = Default2;
        }

        public static async Task LogMessage(string type, string server_name, string message)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                if (logs.Count == 500)
                    logs.RemoveAt(logs.Count - 1);

                logs.Insert(0, new Log(type, server_name, message));
            });
        }
    }
}

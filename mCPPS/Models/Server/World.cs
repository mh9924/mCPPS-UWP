using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace mCPPS.Models
{
    class World : MattsCPPS
    {

        public World(string server_name, string port) : base(server_name, port)
        {
        }

        protected override async Task HandleLogin(Penguin penguin, string packet)
        {
            await Task.CompletedTask;
        }

        protected override async Task RemovePenguin(Penguin penguin)
        {
            if (penguins.ContainsKey(penguin.socket))
            {
                await MServer.LogMessage("INFO", server_name, "Removing penguin");
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Debug.WriteLine("Removing penguin");
                    penguins.Remove(penguin.socket);
                    MServer.penguins.Remove(penguin);

                    latestPackets.Remove(penguin);
                });
            }
        }
    }
}

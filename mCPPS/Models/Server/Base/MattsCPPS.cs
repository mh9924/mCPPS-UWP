using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.UI.Core;

namespace mCPPS.Models
{
    abstract public class MattsCPPS
    {
        private const int TICK_INTERVAL_MS = 600; // Tick rate of server - default 600ms (100 ticks per min). Not configurable for a specific Auth or World server.

        public string server_name, port;
        public Dictionary<StreamSocket, Penguin> penguins = new Dictionary<StreamSocket, Penguin>();

        /* Store each client's most recent packet for each packet type (e.g. s#uph => %xt%s%s#uph%interal_room%item_id%) in latestPackets,
        as long as a handler for that packet type exists. Only the last received packet for each type will be handled from any one client during a game tick. */
        public Dictionary<Penguin, Dictionary<string, string>> latestPackets = new Dictionary<Penguin, Dictionary<string, string>>();

        protected StreamSocketListener listener;

        protected virtual async Task HandleLogin(Penguin penguin, string packet) { await Task.CompletedTask; }
        abstract protected Task RemovePenguin(Penguin penguin);

        public MattsCPPS(string server_name, string port)
        {
            this.server_name = server_name;
            this.port = port;
        }

        public async void StartServer()
        {
            try
            {
                listener = new StreamSocketListener();

                listener.ConnectionReceived += StreamSocketListener_ConnectionReceived;
                await listener.BindServiceNameAsync(port);
                await MServer.LogMessage("FINE", server_name, "Server is running on port " + port + " (allow this port in firewall)");   
            }
            catch (Exception ex)
            {
                SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
                await MServer.LogMessage("FATAL", server_name, "Server failed to start. Ensure that another service is not running on port " + port + ".");
                Debug.WriteLine(server_name + " failed to start.");
                if (MServer.IsStarted()) await MServer.StopServers();
            }

            await LoopServer();
        }

        public async Task StopServer()
        {
            List<Penguin> penguinsToRemove = new List<Penguin>();

            foreach (KeyValuePair<StreamSocket, Penguin> penguin in penguins)
                penguinsToRemove.Add(penguin.Value);

            foreach (Penguin penguin in penguinsToRemove)
            {
                Debug.WriteLine("Penguins:");
                Debug.WriteLine(penguin);
            }

            foreach (Penguin penguin in penguinsToRemove)
                await RemovePenguin(penguin);

            await MServer.LogMessage("FINE", server_name, "Server " + server_name + " has been stopped.");
            listener.Dispose();
        }

        protected virtual async void StreamSocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Debug.WriteLine("Accepting penguin");
            Penguin penguin = new Penguin(this, args.Socket);
            await MServer.LogMessage("INFO", server_name, "Accepting new penguin");
            await AddPenguin(penguin);

            using (var stream = args.Socket.InputStream.AsStreamForRead())
            {
                while (penguins.ContainsKey(args.Socket))
                {
                    byte[] data = new byte[256];
                    int numBytesRead = await stream.ReadAsync(data, 0, data.Length);

                    if (numBytesRead == 0)
                    {
                        await RemovePenguin(penguin);
                        return;
                    }

                    String message = Encoding.UTF8.GetString(data).Substring(0, numBytesRead);
                    Stack<string> packets = new Stack<string>(message.Split('\0'));
                    packets.Pop();

                    foreach (string packet in packets)
                    {
                        Debug.WriteLine("[RECV] " + packet);
                        await HandlePacket(penguin, packet);
                    }
                }
            }
        }

        private async Task TickServer()
        {
            int numPenguins = latestPackets.Count;

            if (numPenguins != 0)
                Debug.WriteLine("TICK (port " + port + ") - Handling packets for " + numPenguins + " penguins");

            foreach (KeyValuePair<Penguin, Dictionary<string, string>> penguin in latestPackets)
            {
                List<string> packetTypesToRemove = new List<string>();

                foreach (KeyValuePair<string, string> packetType in penguin.Value)
                {
                    if (packetType.Key == "<")
                        await HandleXmlPacket(penguin.Key, packetType.Value);
                    else { }
                        // await HandlePlayPacket(penguin.Key, packetType.Value)

                    packetTypesToRemove.Add(packetType.Key);
                }

                foreach (string packetType in packetTypesToRemove)
                    latestPackets[penguin.Key].Remove(packetType);
            }   
        }

        private async Task LoopServer()
        {
            while (true)
            {
                await TickServer();

                await Task.Delay(TICK_INTERVAL_MS);
            }
        }

        private async Task StoreXmlPacket(Penguin penguin, string packet)
        {
            latestPackets[penguin]["<"] = packet;

            await Task.CompletedTask;
        }

        private async Task HandlePacket(Penguin penguin, string packet)
        {
            await MServer.LogMessage("INFO", server_name, "Received: " + packet);
            string start = packet.Substring(0, 1);

            switch (start)
            {
                case "<":
                    await StoreXmlPacket(penguin, packet);
                    break;
                case "%":
                    // await StorePlayPacket(penguin, packet);
                    break;
            }

            return;
        }

        private async Task HandleXmlPacket(Penguin penguin, string packet)
        {
            switch (packet)
            {
                case "<policy-file-request/>":
                    await HandlePolicy(penguin);
                    return;
                case "<msg t='sys'><body action='verChk' r='0'><ver v='153' /></body></msg>":
                    await HandleVersionCheck(penguin);
                    return;
                case "<msg t='sys'><body action='rndK' r='-1'></body></msg>":
                    await HandleRandomKey(penguin);
                    return;
            }

            if (packet.Substring(0, 40) == "<msg t='sys'><body action='login' r='0'>")
                await HandleLogin(penguin, packet);
        }

        private async Task HandlePolicy(Penguin penguin)
        {
            if (penguin.LoginStep == null)
            {
                await penguin.Send("<cross-domain-policy><allow-access-from domain='*' to-ports='*' /></cross-domain-policy>");
                return;
            }

            await RemovePenguin(penguin);
        }

        private async Task HandleVersionCheck(Penguin penguin)
        {
            await penguin.Send("<msg t='sys'><body action='apiOK' r='0'></body></msg>");
            penguin.LoginStep = "Versioncheck";
        }

        private async Task HandleRandomKey(Penguin penguin)
        {
            if (penguin.LoginStep == "Versioncheck")
            {
                penguin.Randkey = "e4a2dbcca10a7246817a83cdmCPPS";
                await penguin.Send("<msg t='sys'><body action='rndK' r='-1'><k>" + penguin.Randkey + "</k></body></msg>");
                penguin.LoginStep = "Randkey";
                return;
            }
            await RemovePenguin(penguin);
        }

        private async Task AddPenguin(Penguin penguin)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                penguins.Add(penguin.socket, penguin);
                MServer.penguins.Add(penguin);

                latestPackets[penguin] = new Dictionary<string, string>();
            });
        }
    }
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace mCPPS.Models
{
    public class Penguin
    {

        public MattsCPPS parent;
        public StreamSocket socket;

        public int ID { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public string Randkey { get; set; }
        public string LoginStep;


        public Penguin(MattsCPPS parent, StreamSocket socket)
        {
            this.parent = parent;
            this.socket = socket;
        }

        public async Task Send(string message)
        {
            Debug.WriteLine("[SENT] " + message);
            await MServer.LogMessage("INFO", parent.server_name, "Sent: " + message);
            message = message + "\0";
            byte[] data = Encoding.UTF8.GetBytes(message);
            await socket.OutputStream.WriteAsync(data.AsBuffer());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.UI.Core;
using Windows.Storage.Streams;

namespace mCPPS.Models
{
    class HTTP : MattsCPPS
    {
		/* You must enter the URL of a media server to cache from if using built-in web server. 
		My modified boots.swf must be on the root of said server.
		https://github.com/mh9924/boots
		*/
        private const string cache_server = "~ URL HERE ~";

        private StorageFolder local_folder = ApplicationData.Current.LocalFolder;
        private Dictionary<string, string> allowed_extensions = new Dictionary<string, string>
        {
            {"swf", "application/x-shockwave-flash"},
            {"php", "text/html" },
            {"json", "application/json" },
            {"xml", "text/xml" }
        };

        public HTTP(string server_name, string port) : base(server_name, port)
        {
            CreateWWWFolder();
        }

        protected override async void StreamSocketListener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Debug.WriteLine("Handling HTTP connection");
            byte[] data = new byte[256];
            try
            {
                var stream = args.Socket.InputStream.AsStreamForRead();
                int numBytesRead = await stream.ReadAsync(data, 0, data.Length);
                String message = Encoding.UTF8.GetString(data).Substring(0, numBytesRead);

                await HandleRequest(args.Socket, message);
            } catch (Exception ex)
            {
                Debug.WriteLine("Web server could not read request." + ex);
            }
        }

        private async void CreateWWWFolder()
        {
            try
            {
                await local_folder.GetFolderAsync("Www");
            } catch (FileNotFoundException)
            { 
                await local_folder.CreateFolderAsync("Www");
                Debug.WriteLine("Www folder did not exist so it was created.");
            }
        }

        private async Task HandleRequest(StreamSocket socket, string packet)
        {
            string[] request = packet.Split(' ');

            try
            {
                if (request[0] == "GET")
                {
                    switch (request[1])
                    {
                        case "/":
                            await GenerateBoots(socket, MServer.GetCurrentLoginPort(), MServer.GetCurrentWorldNamesAndPorts());return;
                        case "/LICENSE":
                            await GenerateBootsLicense(socket);return;
                        case "/register":
                            await RegisterGet(socket);return;
                    }

                    if (!request[1].Contains('.'))
                    {
                        await SendResponse(socket, Encoding.UTF8.GetBytes("mCPPS"), "text/html");
                        return;
                    }

                    List<String> folders = request[1].Split('/').ToList();
                    string filename = folders[folders.Count - 1];
                    string extension = filename.Split('.')[1];
                    folders.RemoveAt(0);
                    folders.RemoveAt(folders.Count - 1);

                    if (!allowed_extensions.ContainsKey(extension))
                    {
                        await SendResponse(socket, Encoding.UTF8.GetBytes("mCPPS"), "text/html");
                        return;
                    }

                    
					/* Check if file is cached locally. 
					If it is not, download it from the master cache_server first.
					If it is, send it to user from cache.
					*/
					try
                    {
                        StorageFile file = await local_folder.GetFileAsync("Www\\" + request[1].Replace("/", "\\"));
                        await ReplyFileData(socket, file, allowed_extensions[extension]);
                    }
                    catch (FileNotFoundException)
                    {
                        try
                        {
                            HttpClient client = new HttpClient();
                            byte[] buffer = await client.GetByteArrayAsync(cache_server + request[1]);
                            StorageFolder tempfolder = await local_folder.GetFolderAsync("Www");

                            foreach (string folder in folders)
                            {
                                try
                                {
                                    tempfolder = await tempfolder.GetFolderAsync(folder);
                                }
                                catch (Exception)
                                {
                                    await tempfolder.CreateFolderAsync(folder);
                                    tempfolder = await tempfolder.GetFolderAsync(folder);
                                }
                            }

                            StorageFile file = await tempfolder.CreateFileAsync(filename);

                            using (Stream stream = await file.OpenStreamForWriteAsync())
                                stream.Write(buffer, 0, buffer.Length);

                            await ReplyFileData(socket, file, allowed_extensions[extension]);
                        }
                        catch (Exception)
                        {
                            await SendResponse(socket, Encoding.UTF8.GetBytes("mCPPS"), "text/html");
                        }
                    }
                }
                else if (request[0] == "POST")
                {
                    switch (request[1])
                    {
                        case "/register":
                            await RegisterPost(socket);return;
                    }
                }
            } catch (Exception)
            {
                await SendResponse(socket, Encoding.UTF8.GetBytes("mCPPS"), "text/html");
            }
        }

        private async Task ReplyFileData(StreamSocket socket, StorageFile file, string mediatype)
        {
            IBuffer b = await FileIO.ReadBufferAsync(file);
            DataReader reader = DataReader.FromBuffer(b);
            byte[] fileContent = new byte[reader.UnconsumedBufferLength];

            reader.ReadBytes(fileContent);

            await SendResponse(socket, fileContent, mediatype);
        }

        private async Task RegisterGet(StreamSocket socket)
        {
            string register = "";

            register += "<!DOCTYPE html>";
            register += "<html>";
            register += "<head>";
            register += "<meta charset=\"UTF-8\">";
            register += "<title>Register</title>";
            register += "<script type=\"text/javascript\" src=\"//cdnjs.cloudflare.com/ajax/libs/swfobject/2.2/swfobject.min.js\"></script>";
            register += "</head>";
            register += "<body>";
            register += "<div style=\"max-width: 960px;margin: auto;text-align: center;font-size: 18px;font-family: Segoe UI;\">";
            register += "<div><a href=\"/\">Play</a> | <a href=\"register\">Register</a></div><br><br>";
            register += "<form method=\"POST\">";
            register += "<input type=\"text\" name=\"username\" id=\"username\" placeholder=\"Enter a username\"><br>";
            register += "<input type=\"password\" name=\"password\" id=\"password\" placeholder=\"Enter a password\"><br>";
            register += "<input type=\"password\" name=\"password2\" id=\"password2\" placeholder=\"Enter the password again\"><br>";
            register += "<input type=\"submit\" value=\"Register\">";
            register += "</div>";
            register += "</body>";
            register += "</html>";

            await SendResponse(socket, Encoding.UTF8.GetBytes(register), "text/html");
        }

        private async Task RegisterPost(StreamSocket socket)
        {
            string register = "";

            register += "Register not implemented";

            await SendResponse(socket, Encoding.UTF8.GetBytes(register), "text/html");
        }

        private async Task GenerateBoots(StreamSocket socket, string loginport, Dictionary<string, string> worlds)
        {
            string boots = "";
            string ip = await new HttpClient().GetStringAsync("https://api.ipify.org/");

            boots += "<!DOCTYPE html>";
            boots += "<html>";
            boots += "<head>";
            boots += "<meta charset=\"UTF-8\">";
            boots += "<title>Play</title>";
            boots += "<script type=\"text/javascript\" src=\"//cdnjs.cloudflare.com/ajax/libs/swfobject/2.2/swfobject.min.js\"></script>";
            boots += "<script>";
            boots += "function loadCP(){var game=document.querySelector(\"#game\");game.toggleDebug();game.startup(\""
                + ip + "\", " + loginport + ", [";


            foreach (KeyValuePair<string, string> world in worlds)
            {
                boots += "[\"" + world.Key.Replace("\"", "\\\"") + "\", \"" + ip + "\", \"" + world.Value + "\"]";

                if (world.Value != worlds.Values.Last())
                    boots += ",";
            }

            boots +="], \"/\")} var flash = { }; var params={ menu: \"false\",allowScriptAccess: \"always\"}; " +
                "var attrs = { id:\"game\",name: \"game\",}; swfobject.embedSWF(\"boots.swf\",\"game\",\"960px\",\"640px\",\"11.0.0\",!1,flash,params,attrs)";
            boots += "</script>";
            boots += "</head>";
            boots += "<body>";
            boots += "<div style=\"max-width: 960px;margin: auto;text-align: center;font-size: 18px;font-family: Segoe UI;\">";
            boots += "<div><a href=\"/\">Play</a> | <a href=\"register\">Register</a></div><br><br>";
            boots += "<div id=\"game\" style=\"width: 960px; height: 640px;\">";
            boots += "<strong><em>Please enable Flash to play</em></strong>";
            boots += "</div>";
            boots += "<br><a style=\"font-size: 10px;\" href =\"LICENSE\" target=\"_blank\">Boots Loader License</a>";
            boots += "</div>";
            boots += "</body>";
            boots += "</html>";

            await SendResponse(socket, Encoding.UTF8.GetBytes(boots), "text/html");
        }

        private async Task GenerateBootsLicense(StreamSocket socket)
        {
            string boots = "";
            boots += "Copyright 2017 widd, systocrat<br><br>";
            boots += "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), " +
                "to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, " +
                "and to permit persons to whom the Software is furnished to do so, subject to the following conditions:<br><br>";
            boots += "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.<br><br>";
            boots += "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. " +
                "IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, " +
                "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";

            await SendResponse(socket, Encoding.UTF8.GetBytes(boots), "text/html");
        }

        private async Task SendResponse(StreamSocket socket, byte[] data, string mediatype)
        {
            try
            {
                string[] lines = new string[] {
                    "HTTP/1.1 200 OK",
                    "Server: Microsoft-IIS/8.5",
                    "Content-Type: " + mediatype,
                    "Content-Length: " + data.Length,
                    "Connection: close",
                    "\r\n" };

                string response = String.Join("\r\n", lines);
                byte[] bytes = Encoding.UTF8.GetBytes(response);
                int length = bytes.Length + data.Length;
                byte[] total = new byte[length];
                bytes.CopyTo(total, 0);
                data.CopyTo(total, bytes.Length);

                await socket.OutputStream.WriteAsync(total.AsBuffer());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not send web server response." + ex);
            }
        }

        protected override async Task RemovePenguin(Penguin penguin)
        {
            if (penguins.ContainsKey(penguin.socket))
            {
                await MServer.LogMessage("INFO", server_name, "Closing web server request");

                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Debug.WriteLine("Removing client from web server");
                    penguins.Remove(penguin.socket);
                    MServer.penguins.Remove(penguin);

                    latestPackets.Remove(penguin);
                });
            }
        }
    }
}

 using Chilkat;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;
using Windows.UI.Core;

namespace mCPPS.Models
{
    class Auth : MattsCPPS
    {
        public Auth(string server_name, string port) : base(server_name, port)
        {
        }

        protected override async Task HandleLogin(Penguin penguin, string packet)
        {
            if (penguin.LoginStep != "Randkey")
            {
                await RemovePenguin(penguin);
                return;
            }

            XmlDocument login_xml = new XmlDocument();
            login_xml.LoadXml(packet);

            string username, password, dbpassword, dbpassword_encrypted;

            username = login_xml.GetElementsByTagName("nick")[0].InnerText;
            password = login_xml.GetElementsByTagName("pword")[0].InnerText;

            if(!await Database.UsernameExists(username))
            {
                // await penguin.Send("%xt%e%-1%100%");
                // await RemovePenguin(penguin);
                await penguin.Send("%xt%gs%-1%65.184.60.189:6113:mCPPS:4%");
                await penguin.Send("%xt%l%-1%1%97debb64dcb0b0f1598f605318bc28fc%0%");

                return;
            }

            Crypt2 crypt = new Crypt2();

            dbpassword = (string)await Database.GetColumnFromUsername(username, "Password");
            dbpassword_encrypted = GetAuthenticationHash(dbpassword, penguin.Randkey);

            if (!crypt.BCryptVerify(password, dbpassword))
            {
                await penguin.Send("%xt%e%-1%101%");
                return;
            }


        }

        private string GetAuthenticationHash(string password, string randkey)
        {
            string hash = EncryptPassword(password, false);
            hash += randkey + "Y(02.>\'H}t\":E1" + EncryptPassword(password, true);

            return hash;
        }

        private string EncryptPassword(string password, bool md5)
        {
            if (md5)
            {
                Crypt2 crypt = new Crypt2
                {
                    HashAlgorithm = "md5",
                    EncodingMode = "hex"
                };
                password = crypt.HashStringENC(password);
            }

            return password.Substring(16, 16) + password.Substring(0, 16);
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

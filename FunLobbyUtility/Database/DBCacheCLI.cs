using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Text;

namespace FunLobbyUtils.Database
{
    public class DBCacheCLI
    {
        public class CacheCmd
        {
            public enum METHOD
            {
                Heartbeat,
                SET,
                GET,
                DEL
            }

            public METHOD Method { get; set; }
            public string Key { get; set; }
            public byte[] Bytes { get; set; }

            public CacheCmd(METHOD method, string key, byte[] bytes = null)
            {
                this.Method = method;
                this.Key = key;
                this.Bytes = bytes;
            }
        }

        public string Domain { get; protected set; }
        public int Port { get; protected set; }

        public DBCacheCLI(string domain, int port)
        {
            this.Domain = domain;
            this.Port = port;
        }

        public bool Heartbeat()
        {
            bool bConnected = false;
            try
            {
                Task task = new Task(new Action(() =>
                {
                    CacheCmd packet = new CacheCmd(CacheCmd.METHOD.Heartbeat, null, null);
                    string strContent = JsonConvert.SerializeObject(packet);
                    UdpClient udpClient = Utils.SendPacket_UDP(Encoding.UTF8.GetBytes(strContent), this.Domain, this.Port);

                    byte[] bytes = this.Receive(udpClient);
                    string resContent = bytes != null ? Encoding.UTF8.GetString(bytes) : null;
                    if (resContent == "success") bConnected = true;
                }));
                task.Start();
                bool bRet = task.Wait(100);
                if (!bRet) throw new Exception("timeout");
            }
            catch(Exception ex)
            {

            }
            return bConnected;
        }

        public byte[] Receive(UdpClient udpClient)
        {
            System.Net.IPEndPoint endPoint = null;
            byte[] bytes = Utils.ReceivePacket_UDP(udpClient, ref endPoint);
            return bytes;
        }

        public UdpClient Set(string key, byte[] bytes)
        {
            UdpClient udpClient = null;
            try
            {
                CacheCmd packet = new CacheCmd(CacheCmd.METHOD.SET, key, bytes);
                string strContent = JsonConvert.SerializeObject(packet);
                udpClient = Utils.SendPacket_UDP(Encoding.UTF8.GetBytes(strContent), this.Domain, this.Port);
                //// if sent done, check response
                //byte[] retBytes = this.Receive(udpClient);
                //string strResult = retBytes != null ? Encoding.UTF8.GetString(retBytes) : null;
                //if (strResult != "success") udpClient = Utils.SendPacket_UDP(Encoding.UTF8.GetBytes(strContent), this.Domain, this.Port);
            }
            catch(Exception ex)
            {

            }
            return udpClient;
        }

        public UdpClient Set(string key, string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            return Set(key, bytes);
        }

        public UdpClient Set(string key, JObject obj)
        {
            return Set(key, JsonConvert.SerializeObject(obj));
        }

        public byte[] GetBytes(string key)
        {
            byte[] bytes = null;
            try
            {
                CacheCmd packet = new CacheCmd(CacheCmd.METHOD.GET, key);
                string strContent = JsonConvert.SerializeObject(packet);
                UdpClient udpClient = Utils.SendPacket_UDP(Encoding.UTF8.GetBytes(strContent), this.Domain, this.Port);
                bytes = this.Receive(udpClient);
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("DBCacheCLI.GetBytes got {0}", ex.Message));
            }
            return bytes;
        }

        public string GetString(string key)
        {
            string content = null;
            try
            {
                byte[] bytes = GetBytes(key);
                if (bytes != null) content = Encoding.UTF8.GetString(bytes);
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("DBCacheCLI.GetString got {0}", ex.Message));
            }
            return content;
        }

        public JObject GetJObject(string key)
        {
            JObject obj = null;
            try
            {
                string content = GetString(key);
                if (content != null) obj = JObject.Parse(content);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("DBCacheCLI.GetJObject got {0}", ex.Message));
            }
            return obj;
        }

        public void Del(string key)
        {
            CacheCmd packet = new CacheCmd(CacheCmd.METHOD.DEL, key);
            string strContent = JsonConvert.SerializeObject(packet);
            Utils.SendPacket_UDP(Encoding.UTF8.GetBytes(strContent), this.Domain, this.Port);
        }
    }
}

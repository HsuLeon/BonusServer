using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Timers;

namespace FunLobbyUtils
{
    public class MiddleServer
    {
        public delegate void CallbackHandler<T>(T data);

        public delegate void ResponseConnectEventHandler(object sender, Notification.Status args);
        public delegate void SendHeartbeatTriggeredEventHandler(object sender, string content);
        public delegate void SendHeartbeatResultEventHandler(object sender, Notification.Status args);
        public delegate void ConnectionStatusEventHandler(object sender, string content);         // Client 端存活連線訊息, args 為 connecting;remote 的字串
        public delegate void ErrorEventHandler(object sender, string content);

        public event ResponseConnectEventHandler? ResponseConnect = null;
        public event SendHeartbeatTriggeredEventHandler? SendHeartbeatTriggered = null;
        public event SendHeartbeatResultEventHandler? SendHeartbeatResult = null;
        public event ConnectionStatusEventHandler? ConnectionStatus = null;
        public event ErrorEventHandler? Error = null;

        public string Name { get; protected set; }
        public string? ConnectionId { get; protected set; }
        public string? EncodeKey { get; protected set; }
        public bool IsConnected { get; protected set; }
        public int ErrConnectionRetryCnt { get; set; }
        public int HeartbeatSentCnt { get; set; }

        protected bool mbValidate = false;
        protected bool mOnReconnect = false;
        protected System.Timers.Timer? HeartbeatTimer { get; set; }

        //
        protected MiddleServer(string name)
        {
            this.Name = name;
            this.ConnectionId = null;
            this.IsConnected = false;
            this.ErrConnectionRetryCnt = 0;
            this.HeartbeatSentCnt = 0;
            this.mbValidate = true;
        }

        public void Dispose()
        {
            mbValidate = false;
        }

        public virtual void Connect(string host, int port)
        {
            throw new System.ArgumentNullException();
        }

        public virtual void RequestStop()
        {
            throw new System.ArgumentNullException();
        }

        protected virtual Task Invoke(string cmd, string data, CallbackHandler<Task> fncResponse = null)
        {
            throw new System.ArgumentNullException();
        }

        public virtual Task IdentifyDevice()
        {
            Task task = null;
            if (this.IsConnected)
            {
                try
                {
                    JObject obj = new JObject();
                    obj["deviceName"] = this.Name;
                    obj["connectionId"] = this.ConnectionId;
                    task = this.Invoke(ProtocolCmd.IdentifyDevice, JsonConvert.SerializeObject(obj));
                }
                catch (Exception ex)
                {

                }
            }
            return task;
        }

        public virtual Task Heartbeat(JObject obj)
        {
            Task task = null;
            if (this.IsConnected)
            {
                try
                {
                    if (obj == null) obj = new JObject();
                    obj["connectionId"] = this.ConnectionId;
                    task = this.Invoke(ProtocolCmd.Heartbeat, JsonConvert.SerializeObject(obj));
                    if (task != null)
                    {
                        task.ContinueWith(heartbeatTask =>
                        {
                            if (heartbeatTask.IsFaulted) throw new Exception("heartbeatTask failed");
                            OnSendHeartbeatResult(this, Notification.Status.onSendHeartbeatSuccess);
                        });
                    }
                }
                catch (Exception ex)
                {
                    OnSendHeartbeatResult(this, Notification.Status.onSendHeartbeatFault);
                    Log.StoreMsg(string.Format("RemoteServer:Heartbeat ex:{0}", ex.Message));
                    Log.Flush();
                }
            }
            return task;
        }

        public Task CloseSocket()
        {
            Console.WriteLine("CloseSocket");
            Task task = null;
            try
            {
                Log.StoreMsg("Close Socket");
                JObject obj = new JObject();
                obj["requestcloseSocket"] = "reqclosesocket";

                task = this.Invoke(ProtocolCmd.CloseSocket, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("RemoteServer:closeSocket ex:{0}", ex.Message));
            }
            return task;
        }

        protected void OnResponseConnect(object sender, Notification.Status args)
        {
            if (args == Notification.Status.onConnectSuccess)
            {
                this.IdentifyDevice();
            }

            ResponseConnect?.Invoke(sender, args);
        }

        protected void OnHeartbeatTriggered(object sender, ElapsedEventArgs e)
        {
            SendHeartbeatTriggered?.Invoke(sender, "");
        }

        public void OnSendHeartbeatResult(object sender, Notification.Status args)
        {
            SendHeartbeatResult?.Invoke(sender, args);
        }

        protected void OnConnectionStatus(object sender, string args)
        {
            ConnectionStatus?.Invoke(sender, args);
        }

        protected void OnError(object sender, string args)
        {
            Log.StoreMsg(args);
            Error?.Invoke(sender, args);
        }
    }
}

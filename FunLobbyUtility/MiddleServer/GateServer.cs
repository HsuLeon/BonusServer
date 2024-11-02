using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;

namespace FunLobbyUtils
{
    public class GateServer : MiddleServer
    {
        public const string Request = "Request";
        public const string Response = "Response";

        public delegate void GetNotifyJoinEventHandler(object sender, string content);
        public delegate void GetNotifyLeaveEventHandler(object sender, string content);
        public delegate void NotifyConnectionDetailsEventHandler(object sender, string content);
        public delegate void ResponseGroupInfoEventHandler(object sender, string content);
        public delegate void NotifyConfigEventHandler(object sender, string content);
        public delegate void NotifyLobbiesEventHandler(object sender, string content);
        public delegate void ScreenTimerEventHandler(object sender, bool content);
        public delegate void GetActionEventHandler(object sender, string content);
        public delegate void GetRequestEventHandler(object sender, string content);
        public delegate void GetResponseEventHandler(object sender, string content);
        public delegate void GetIISAckEventHandler(object sender, string content);

        public event GetNotifyJoinEventHandler? GetNotifyJoin;
        public event GetNotifyLeaveEventHandler? GetNotifyLeave;
        public event NotifyConnectionDetailsEventHandler? NotifyConnectionDetails;
        public event ResponseGroupInfoEventHandler? ResponseGroupInfo;
        public event NotifyConfigEventHandler? NotifyConfig;
        public event NotifyLobbiesEventHandler? NotifyLobbies;
        public event GetActionEventHandler? GetAction;
        public event GetRequestEventHandler? GetRequest;
        public event GetResponseEventHandler? GetResponse;
        public event GetIISAckEventHandler? GetIISAck;

        private TcpClient? mTcpClient = null;
        //private CallbackHandler<GateServer> mFncDisconnected = null;
        private Task? mTask = null;
        private byte[]? mBufPacket = null;
        public string? Host { get; protected set; }
        public int Port { get; protected set; }
        public string RoomId { get; protected set; }

        protected static GateServer? mInstance = null;
        public static GateServer? Instance { get { return mInstance; } }
        public static GateServer Create(string name)
        {
            if (mInstance == null)
            {
                mInstance = new GateServer(name);
            }
            return mInstance;
        }

        protected GateServer(string name) : base(name)
        {
            // create task to receive packets
            mTask = new Task(new Action(() =>
            {
                while (mbValidate)
                {
                    TcpClient tcpClient = mTcpClient;
                    // switch to another task
                    System.Threading.Thread.Sleep(1);
                    // if connection not ready, skip receiving
                    if (tcpClient == null || tcpClient.Connected == false) continue;

                    try
                    {
                        // since tcp socket may combine contents from multi-packet,
                        // so separate them to List<string>
                        string strReceived = "";
                        NetworkStream ns = tcpClient.GetStream();
                        if (ns.CanRead)
                        {
                            byte[] bufBytes = new byte[tcpClient.ReceiveBufferSize];
                            int bufRead = 0;
                            do
                            {
                                int numberOfBytesRead = ns.Read(bufBytes, bufRead, tcpClient.ReceiveBufferSize - bufRead);
                                bufRead += numberOfBytesRead;
                                // push received to strReceived
                                strReceived += CommunicationBase.DecodeString(bufBytes, bufRead);
                                bufRead = 0;
                            } while (ns.DataAvailable);
                        }
                        List<string> strPacketList = CommunicationBase.ParsePackets(strReceived, ref mBufPacket);
                        // process packet one by one
                        for (int i = 0; i < strPacketList.Count; i++)
                        {
                            JObject resObj = JObject.Parse(strPacketList[i]);
                            string cmd = resObj.ContainsKey("cmd") ? resObj["cmd"].Value<string>() : null;
                            string data = resObj.ContainsKey("data") ? resObj["data"].Value<string>() : null;

                            serverEventHandler(cmd, data);
                        }
                    }
                    catch (Exception ex)
                    {
                        //OnError(this, "無法連接伺服器:(" + ex.Message + ")");
                        //Log.StoreMsg(string.Format("GateServer:Connect ex:{0}", ex.Message));
                        if (tcpClient == null ||
                            tcpClient.Connected == false)
                        {
                            OnResponseConnect(this, Notification.Status.onConnectFailed);
                        }
                        else
                        {
                            //mStrPacket = string.Empty;
                        }
                    }
                }
            }));
            mTask.Start();

            this.Host = null;
            this.Port = 0;
            this.RoomId = "";
        }

        public override void Connect(string host, int port)
        {
            // if no valid host or port, skip calling
            if (host == null || port == 0)
                return;

            this.Host = host;
            this.Port = port;
            if (this.IsConnected == false)
            {
                //Console.WriteLine("Connect");
                // destroy old heartbeat timer
                if (this.HeartbeatTimer != null)
                {
                    this.HeartbeatTimer.Stop();
                    this.HeartbeatTimer.Elapsed -= this.OnHeartbeatTriggered!;
                    this.HeartbeatTimer.Dispose();
                    this.HeartbeatTimer = null;
                }

                Task task = new Task(new Action(() =>
                {
                    //開始連線
                    try
                    {
                        //先建立IPAddress物件,IP為欲連線主機之IP
                        IPAddress ipa;
                        if (this.Host.ToLower() == "localhost")
                        {
                            ipa = IPAddress.Parse("127.0.0.1");
                        }
                        else
                        {
                            try
                            {
                                // try ip
                                ipa = IPAddress.Parse(this.Host);
                            }
                            catch (Exception ex)
                            {
                                // try domain name
                                IPHostEntry ipHostInfo = Dns.GetHostEntry(this.Host);
                                ipa = ipHostInfo.AddressList[0];
                            }
                        }
                        //建立IPEndPoint
                        IPEndPoint ipe = new IPEndPoint(ipa, this.Port);

                        //先建立一個TcpClient;
                        TcpClient tcpClient = new TcpClient();
                        //this.mFncDisconnected = fncDisconnected;

                        //Console.WriteLine("主機IP=" + ipa.ToString());
                        //Console.WriteLine("連線至主機中...\n");
                        tcpClient.Connect(ipe);

                        if (tcpClient.Connected)
                        {
                            this.IsConnected = true;
                            Console.WriteLine("連線成功!");

                            // send Handshaking
                            Handshake(tcpClient, this.Host, this.Port);
                            // assign mTcpClient
                            mTcpClient = tcpClient;
                            // proceed callback
                            OnResponseConnect(this, Notification.Status.onConnectSuccess);
                        }
                        else
                        {
                            throw new Exception("tcpClient.Connected is false 111");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.StoreMsg(string.Format("GateServer:Connect ex:{0}", ex.Message));
                        RequestStop();
                        OnResponseConnect(this, Notification.Status.onConnectFailed);
                    }
                }));
                task.Start();
            }
            else if (this.mOnReconnect == false)
            {
                this.mOnReconnect = true;
                Console.WriteLine("Reconnect");

                if (mTcpClient != null)
                {
                    mTcpClient.Close();
                    mTcpClient = null;
                }
                this.ConnectionId = null;

                Task task = new Task(new Action(() =>
                {
                    try
                    {
                        //先建立IPAddress物件,IP為欲連線主機之IP
                        IPAddress ipa;
                        if (this.Host.ToLower() == "localhost")
                        {
                            ipa = IPAddress.Parse("127.0.0.1");
                        }
                        else
                        {
                            try
                            {
                                // try ip
                                ipa = IPAddress.Parse(this.Host);
                            }
                            catch (Exception ex)
                            {
                                // try domain name
                                IPHostEntry ipHostInfo = Dns.GetHostEntry(this.Host);
                                ipa = ipHostInfo.AddressList[0];
                            }
                        }
                        //建立IPEndPoint
                        IPEndPoint ipe = new IPEndPoint(ipa, this.Port);

                        //先建立一個TcpClient;
                        TcpClient tcpClient = new TcpClient();
                        //this.mFncDisconnected = fncDisconnected;

                        //Console.WriteLine("主機IP=" + ipa.ToString());
                        //Console.WriteLine("連線至主機中...\n");
                        tcpClient.Connect(ipe);

                        if (tcpClient.Connected)
                        {
                            this.mOnReconnect = false;
                            Console.WriteLine("連線成功!");

                            // send Handshaking
                            Handshake(tcpClient, this.Host, this.Port);
                            // assign mTcpClient
                            mTcpClient = tcpClient;
                            // proceed callback
                            OnResponseConnect(this, Notification.Status.onConnectSuccess);
                        }
                        else
                        {
                            throw new Exception("tcpClient.Connected is false 222");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.mOnReconnect = false;
                        Log.StoreMsg(string.Format("GateServer:Reconnect ex:{0}", ex.Message));
                        OnResponseConnect(this, Notification.Status.onConnectFailed);
                    }
                }));
                task.Start();
            }
        }
		
        //private byte[] KeepAlive(int onOff, int keepAliveTime, int keepAliveInterval)
        //{
        //    byte[] buffer = new byte[12];
        //    BitConverter.GetBytes(onOff).CopyTo(buffer, 0);
        //    BitConverter.GetBytes(keepAliveTime).CopyTo(buffer, 4);//（毫秒）没有数据就开始发送心跳包，有数据传递的时候不发送心跳包
        //    BitConverter.GetBytes(keepAliveInterval).CopyTo(buffer, 8);//（毫秒）发送一个心跳包，发5次（系统默认值）
        //    return buffer;
        //}

        public override void RequestStop()
        {
            int errCode = 100;
            try
            {
                Log.StoreMsg("RequestStop");
                // destroy old heartbeat timer
                if (this.HeartbeatTimer != null)
                {
                    this.HeartbeatTimer.Stop();
                    this.HeartbeatTimer.Elapsed -= this.OnHeartbeatTriggered!;
                    this.HeartbeatTimer.Dispose();
                    this.HeartbeatTimer = null;
                }
                errCode = 200;
                Task task = Leave();
                if (task != null)
                {
                    task.Wait();
                }
                
                errCode = 250;
                /*
                Task task1 = CloseSocket();
                if (task1 != null)
                {
                    task1.Wait();
                }
                */
                
                // 
                if (mTcpClient != null)
                {
                    errCode = 300;
                    mTcpClient.Close();
                    mTcpClient = null;
                    errCode = 400;
                }
                // 
                errCode = 500;
                this.ConnectionId = null;
                this.IsConnected = false;
                errCode = 600;
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("RequestStop got err:{0}, errCode:{1}", ex.Message, errCode);
                Log.StoreMsg("RequestStop");
            }
            Log.Flush();
        }

        protected void Handshake(TcpClient tmpTcpClient, string host, int port)
        {
            NetworkStream ns = tmpTcpClient.GetStream();
            while (!ns.CanWrite)
            {
                System.Threading.Thread.Sleep(1);
            }
            // send Handshaking to server
            string handshaking = string.Format(
                "GET / TCP/\r\n" +
                "Host: {0}:{1}\r\n" +
                "Name: {2}",
                host, port, this.Name
            );
            byte[] msgByte = CommunicationBase.EncodeString(handshaking);
            // send handshaking
            ns.Write(msgByte, 0, msgByte.Length);
            ns.Flush();

            // wait for server's Handshaking
            while (!ns.CanRead)
            {
                System.Threading.Thread.Sleep(1);
            }
            byte[] bufBytes = new byte[tmpTcpClient.ReceiveBufferSize];
            int bufRead = 0;
            do
            {
                int numberOfBytesRead = ns.Read(bufBytes, bufRead, tmpTcpClient.ReceiveBufferSize - bufRead);
                bufRead += numberOfBytesRead;
            } while (ns.DataAvailable);
            string content = CommunicationBase.DecodeString(bufBytes);
            JObject objRes = JObject.Parse(content);
            string clientType = objRes.ContainsKey("clientType") ? objRes["clientType"].Value<string>() : null;
            string connectionId = objRes.ContainsKey("connectionId") ? objRes["connectionId"].Value<string>() : null;
            string encodeKey = objRes.ContainsKey("encodeKey") ? objRes["encodeKey"].Value<string>() : null;

            this.ConnectionId = connectionId;
            this.EncodeKey = encodeKey;
            // reset parameters
            mBufPacket = null;
        }

        protected override Task Invoke(string cmd, string data, CallbackHandler<Task>? fncResponse = null)
        {
            if (mTcpClient == null) return null;

            Task task = new Task(new Action(() =>
            {
                JObject reqObj = new JObject();
                reqObj["cmd"] = cmd;
                reqObj["data"] = data;
                CommunicationBase.SendMsg(mTcpClient, JsonConvert.SerializeObject(reqObj));
            }));
            task.Start();

            if (fncResponse == null)
                return task;
            else
                return task.ContinueWith(nextTask =>
                {
                    fncResponse?.Invoke(nextTask);
                });
        }

        public override Task Heartbeat(JObject obj)
        {
            if (obj == null) obj = new JObject();
            obj["roomId"] = this.RoomId;
            return base.Heartbeat(obj);
        }

        //
        //
        //
        public Task RequestConnectionDetails()
        {
            Task? task = null;
            try
            {
                JObject obj = new JObject();
                task = this.Invoke(ProtocolCmd.RequestConnectionDetails, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:RequestConnectionDetails ex:{0}", ex.Message));
            }
            return task;
        }

        public Task RequestConfig(string mgrPassword = "")
        {
            Task? task = null;
            try
            {
                JObject obj = new JObject();
                obj["password"] = mgrPassword;
                task = this.Invoke(ProtocolCmd.RequestConfig, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:RequestConfig ex:{0}", ex.Message));
            }
            return task;
        }

        public Task RequestLobbies(string mgrPassword = "")
        {
            Task? task = null;
            try
            {
                JObject obj = new JObject();
                obj["password"] = mgrPassword;
                task = this.Invoke(ProtocolCmd.RequestLobbies, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:RequestConfig ex:{0}", ex.Message));
            }
            return task;
        }

        public Task Kickout(string password, string pinCode)
        {
            Task? task = null;
            try
            {
                JObject obj = new JObject();
                obj["password"] = password;
                obj["pinCode"] = pinCode;
                task = this.Invoke(ProtocolCmd.Kickout, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:Kickout ex:{0}", ex.Message));
            }
            return task;
        }

        public Task UpdateConfig(string password, string newPassword, List<string> allowedRooms, List<string> blockedRooms, int kicktimeout)
        {
            Task? task = null;
            try
            {
                // allowed
                JArray allowedIds = new JArray();
                if (allowedRooms != null)
                {
                    for (int i = 0; i < allowedRooms.Count; i++)
                    {
                        allowedIds.Add(allowedRooms[i]);
                    }
                }
                // blocked
                JArray blockedIds = new JArray();
                if (blockedRooms != null)
                {
                    for (int i = 0; i < blockedRooms.Count; i++)
                    {
                        blockedIds.Add(blockedRooms[i]);
                    }
                }
                JObject obj = new JObject();
                obj["password"] = password;
                obj["newPassword"] = newPassword;
                obj["kicktimeout"] = kicktimeout;

                task = this.Invoke(ProtocolCmd.UpdateConfig, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:UpdateConfig ex:{0}", ex.Message));
            }
            return task;
        }

        public Task RequestGroupInfo()
        {
            Task? task = null;
            try
            {
                JObject obj = new JObject();
                obj["groupID"] = this.RoomId;

                task = this.Invoke(ProtocolCmd.RequestGroupInfo, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:RequestGroupInfo ex:{0}", ex.Message));
            }
            return task;
        }

        public Task Join(string pinCode, bool isRoomOwner, JObject config = null)
        {
            Task? task = null;
            try
            {
                Console.WriteLine("Join");
                //if (this.mOnReconnect == false)
                //{
                Log.StoreMsg("GateServer Join");

                JObject obj = new JObject();
                obj["clientName"] = this.Name;
                obj["pinCode"] = pinCode;
                obj["config"] = config;
                obj["verCode"] = Utils.VerCode();
                obj["newRoom"] = isRoomOwner ? "true" : "false";

                task = this.Invoke(ProtocolCmd.Join, JsonConvert.SerializeObject(obj));
                //}
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:Join ex:{0}", ex.Message));
            }
            return task;
        }

        public Task Leave()
        {
            //Console.WriteLine("Leave");
            Task? task = null;
            try
            {
                Log.StoreMsg("GateServer Leave");
                JObject obj = new JObject();
                obj["pinCode"] = this.RoomId;

                task = this.Invoke(ProtocolCmd.Leave, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:Leave ex:{0}", ex.Message));
            }
            return task;
        }

        public Task ClientRequest(JObject request)
        {
            Task? task = null;
            try
            {
                string strContent = JsonConvert.SerializeObject(request);
                task = this.Invoke(ProtocolCmd.Request, strContent);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:ClientRequest ex:{0}", ex.Message));
            }
            return task;
        }

        public Task SendAction(JObject action)
        {
            Task? task = null;
            try
            {
                string strContent = JsonConvert.SerializeObject(action);
                task = this.Invoke(ProtocolCmd.SendAction, strContent);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:SendAction ex:{0}", ex.Message));
            }
            return task;
        }

        public Task ServerResponse(JObject response, string request, string connectionId, bool bNeedAck = false)
        {
            Task? task = null;
            try
            {
                response["handler"] = this.Name;
                response["targetId"] = connectionId;
                response["request"] = request;
                if (bNeedAck)
                {
                    response["ack"] = true;
                    response["dtMgr"] = DateTime.UtcNow;
                }
                string content = JsonConvert.SerializeObject(response);
                task = this.Invoke(ProtocolCmd.ServerResponse, content);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("GateServer:ServerResponse ex:{0}", ex.Message));
            }
            return task;
        }

        //
        //
        //
        protected void OnNotifyJoin(object sender, string content)
        {
            try
            {
                JObject obj = JObject.Parse(content);
                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string message = obj.ContainsKey("message") ? obj["message"].Value<string>() : null;
                    throw new Exception(message != null ? message : "未知的錯誤");
                }
                string joinType = obj.ContainsKey("JoinType") ? obj["JoinType"].Value<string>() : null;
                bool isCreater = obj.ContainsKey("IsCreater") ? (obj["IsCreater"].Value<string>().ToLower() == "true" ? true : false) : false;
                string groupID = obj.ContainsKey("groupID") ? obj["groupID"].Value<string>() : null;
                string connectionId = obj.ContainsKey("connectionId") ? obj["connectionId"].Value<string>() : null;

                if (joinType == "self")
                {
                    //this.ConnectionId = connectionId;
                    Log.StoreMsg(string.Format("OnNotifyJoin got connectionId:{0}", this.ConnectionId));
                    // prepare heartbeat timer
                    if (this.HeartbeatTimer == null)
                    {
                        this.HeartbeatTimer = new System.Timers.Timer();
                        this.HeartbeatTimer.Elapsed += this.OnHeartbeatTriggered;
                        this.HeartbeatTimer.Start();
                    }
                    this.HeartbeatTimer.Interval = 5000;
                }

                this.RoomId = groupID;
            }
            catch (Exception ex)
            {
                JObject obj = new JObject();
                obj["status"] = "failed";
                obj["message"] = ex.Message;
                content = JsonConvert.SerializeObject(obj);
            }
            GetNotifyJoin?.Invoke(sender, content);
        }

        protected void OnNotifyLeave(object sender, string content)
        {
            JObject obj = JObject.Parse(content);
            string eventType = obj.ContainsKey("Event") ? obj["Event"].Value<string>() : null;
            bool isCreater = obj.ContainsKey("IsCreater") ? (obj["IsCreater"].Value<string>().ToLower() == "true" ? true : false) : false;
            string connectionId = obj.ContainsKey("connectionId") ? obj["connectionId"].Value<string>() : null;
            //if (this.ConnectionId == connectionId)
            //{
            //    this.ConnectionId = null;
            //}
            GetNotifyLeave?.Invoke(sender, content);
        }

        protected void OnNotifyConnectionDetails(object sender, string content)
        {
            NotifyConnectionDetails?.Invoke(sender, content);
        }

        protected void OnResponseGroupInfo(object sender, string content)
        {
            ResponseGroupInfo?.Invoke(sender, content);
        }

        protected void OnGetAction(object sender, string args)
        {
            GetAction?.Invoke(sender, args);
        }

        protected void OnNotifyConfig(object sender, string content)
        {
            JObject obj = JObject.Parse(content);
            string message = obj.ContainsKey("message") ? obj["message"].Value<string>() : null;
            string connectionId = obj.ContainsKey("connectionId") ? obj["connectionId"].Value<string>() : null;
            if (connectionId != null)
            {
                //this.ConnectionId = connectionId;
                Console.WriteLine("this.ConnectionId:" + this.ConnectionId);
            }
            NotifyConfig?.Invoke(sender, content);
        }

        protected void OnNotifyLobbies(object sender, string content)
        {
            JObject obj = JObject.Parse(content);
            string message = obj.ContainsKey("message") ? obj["message"].Value<string>() : null;
            string connectionId = obj.ContainsKey("connectionId") ? obj["connectionId"].Value<string>() : null;
            if (connectionId != null)
            {
                //this.ConnectionId = connectionId;
                Console.WriteLine("this.ConnectionId:" + this.ConnectionId);
            }
            NotifyLobbies?.Invoke(sender, content);
        }

        protected void OnGetRequest(object sender, string args)
        {
            GetRequest?.Invoke(sender, args);
        }

        protected void OnGetResponse(object sender, string args)
        {
            GetResponse?.Invoke(sender, args);
        }

        protected void OnIISAck(object sender, string args)
        {
            GetIISAck?.Invoke(sender, args);
        }

        void serverEventHandler(string cmd, string content)
        {
            switch (cmd)
            {
                case ProtocolCmd.IdentifyDevice:
                    this.IdentifyDevice();
                    break;
                case ProtocolCmd.NotifyJoin:
                    OnNotifyJoin(this, content);
                    break;
                // 通知配對的App已離線
                case ProtocolCmd.NotifyLeave:
                    OnNotifyLeave(this, content);
                    break;
                case ProtocolCmd.NotifyConnectionDetails:
                    OnNotifyConnectionDetails(this, content);
                    break;
                case ProtocolCmd.NotifyConfig:
                    OnNotifyConfig(this, content);
                    break;
                case ProtocolCmd.NotifyLobbies:
                    OnNotifyLobbies(this, content);
                    break;
                // 取得群中所有的連線資訊
                case ProtocolCmd.ResponseGroupInfo:
                    OnResponseGroupInfo(this, content);
                    break;
                // 所有的 Actions 通知在這裡
                case ProtocolCmd.Action:
                    OnGetAction(this, content);
                    break;
                // 傳入字串 [connecting];[Device] = connecting;remote
                case ProtocolCmd.ConnectionStatus:
                    //Commend redtea 
                    //Client端 連線成功會一直傳送 connecting;remote
                    OnConnectionStatus(this, content);
                    break;
                case ProtocolCmd.Request:
                    OnGetRequest(this, content);
                    break;
                // 所有的 onResponse 通知在這裡
                case ProtocolCmd.Response:
                    OnGetResponse(this, content);
                    break;
                case ProtocolCmd.IISAck:
                    OnIISAck(this, content);
                    break;
            }
        }
    }
}

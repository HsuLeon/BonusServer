using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;

namespace FunLobbyUtils
{
    public class EventServer : MiddleServer
    {
        public const string Request = "Request";
        public const string Response = "Response";

        public enum GameState
        {
            Idle         = 0x0,
            NewRound     = 0x2,
            BetCountDown = 0x20,
            StopBet      = 0x40,
            ShowCard     = 0x60,
            BetResult    = 0x80,
            WinHistory   = 0xa0,
            ServerOff    = 0xff
        }

        public enum Card_Type
        {
            Heart = 0, //紅心
            Diamond = 1, //方塊
            Spade = 2, //黑桃
            Club = 3 //梅花
        }

        public class WinResult
        {
            public enum WinType
            {
                None,       //無資料
                Player,     //閒贏
                Banker,     //莊贏
                Tie,        //和
                Lucky6,     //幸運六
                PlayerPair, //閒對
                BankerPair  //莊對
            }
            public int RoundId { get; protected set; }

            public List<WinResult.WinType> Wins { get; protected set; }
            public int Points { get; set; }

            public WinResult(int roundId)
            {
                this.RoundId = roundId;
                this.Wins = new List<WinResult.WinType>();
                this.Points = -1;
            }

            public void AddWin(WinResult.WinType winType)
            {
                if (this.Wins.Contains(winType) == false)
                {
                    this.Wins.Add(winType);
                }
            }

            public void Reset()
            {
                this.Wins = new List<WinResult.WinType>();
                this.Points = -1;
            }
        }

        public class CardInfo
        {
            public string? Target { get; set; }
            public int Index { get; set; }
            public Card_Type CardType { get; set; }
            public int CardId { get; set; }

            public CardInfo()
            {
                this.Target = null;
                this.Index = -1;
                this.CardType = Card_Type.Heart;
                this.CardId = 0;
            }
        }

        public delegate void GetNotifyJoinEventHandler(object sender, string content);
        public delegate void GetNotifyLeaveEventHandler(object sender, string content);
        public delegate void GetRequestEventHandler(object sender, string content);
        public delegate void GetResponseEventHandler(object sender, string content);

        public event GetNotifyJoinEventHandler? GetNotifyJoin;
        public event GetNotifyLeaveEventHandler? GetNotifyLeave;
        public event GetRequestEventHandler? GetRequest;
        public event GetResponseEventHandler? GetResponse;

        private TcpClient? mTcpClient = null;
        //private CallbackHandler<EventServer> mFncDisconnected = null;
        private Task? mTask = null;
        private byte[]? mBufPacket = null;
        public string? Host { get; protected set; }
        public int Port { get; protected set; }

        protected static EventServer? mInstance = null;
        public static EventServer? Instance { get { return mInstance; } }
        public static EventServer Create(string name)
        {
            if (mInstance == null)
            {
                mInstance = new EventServer(name);
            }
            return mInstance;
        }

        protected EventServer(string name) : base(name)
        {
            // create task for receiving packets
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
                        //Log.StoreMsg(string.Format("RemoteServer:Connect ex:{0}", ex.Message));
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
                Console.WriteLine("Connect");
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

                        Console.WriteLine("主機IP=" + ipa.ToString());
                        Console.WriteLine("連線至主機中...\n");
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
                        Log.StoreMsg(string.Format("RemoteServer:Connect ex:{0}", ex.Message));
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

                        Console.WriteLine("主機IP=" + ipa.ToString());
                        Console.WriteLine("連線至主機中...\n");
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
                        Log.StoreMsg(string.Format("RemoteServer:Reconnect ex:{0}", ex.Message));
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

        public Task Join(JObject? config = null)
        {
            Task? task = null;
            try
            {
                Console.WriteLine("Join");
                //if (this.mOnReconnect == false)
                //{
                Log.StoreMsg("EventServer Join");

                JObject obj = new JObject();
                obj["clientName"] = this.Name;
                obj["config"] = config;
                obj["verCode"] = Utils.VerCode();

                task = this.Invoke(ProtocolCmd.Join, JsonConvert.SerializeObject(obj));
                //}
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("EventServer:Join ex:{0}", ex.Message));
            }
            return task;
        }

        public Task Leave()
        {
            Console.WriteLine("Leave");
            Task? task = null;
            try
            {
                Log.StoreMsg("EventServer Leave");
                JObject obj = new JObject();
                obj["clientName"] = this.Name;

                task = this.Invoke(ProtocolCmd.Leave, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("EventServer:Leave ex:{0}", ex.Message));
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
                Log.StoreMsg(string.Format("EventServer:ClientRequest ex:{0}", ex.Message));
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
                Log.StoreMsg(string.Format("EventServer:SendAction ex:{0}", ex.Message));
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
                Log.StoreMsg(string.Format("EventServer:ServerResponse ex:{0}", ex.Message));
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
                string connectionId = obj.ContainsKey("connectionId") ? obj["connectionId"].Value<string>() : null;

                //this.ConnectionId = connectionId;
                Log.StoreMsg(string.Format("OnNotifyJoin got connectionId:{0}", this.ConnectionId));
                // prepare heartbeat timer
                if (this.HeartbeatTimer == null)
                {
                    this.HeartbeatTimer = new System.Timers.Timer();
                    this.HeartbeatTimer.Elapsed += this.OnHeartbeatTriggered!;
                    this.HeartbeatTimer.Start();
                }
                this.HeartbeatTimer.Interval = 5000;
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

        protected void OnGetRequest(object sender, string args)
        {
            GetRequest?.Invoke(sender, args);
        }

        protected void OnGetResponse(object sender, string args)
        {
            GetResponse?.Invoke(sender, args);
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
            }
        }
    }
}

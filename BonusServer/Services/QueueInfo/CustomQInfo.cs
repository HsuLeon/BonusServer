using FunLobbyUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;

namespace BonusServer.Services.QueueInfo
{
    public class CustomQInfo : QueueInterface
    {
        const string Heartbeat = "Heartbeat";
        const string QueueName = "BonusAgent-RabbitMQ";
        const string ChannelSpinA = "SpinA";
        const string ChannelSpinB = "SpinB";
        const string ChannelSpinCR = "SpinCR";

        Dictionary<string, List<string>> mDicQueue = new Dictionary<string, List<string>>();
        Mutex mMutexQueue = new Mutex();
        Task? mTaskQueue = null;
        CancellationTokenSource? mRequestCancelToken = null;
        bool mRunTask = false;
        TimeSpan mQueueTimeout;
        DateTime mQueueSpinASendTime;
        DateTime mQueueSpinAReceiveTime;
        DateTime mQueueSpinBSendTime;
        DateTime mQueueSpinBReceiveTime;
        DateTime mQueueSpinCRSendTime;
        DateTime mQueueSpinCRReceiveTime;
        DateTime mExportTime = DateTime.Now;

        public CustomQInfo()
        {
            // assign timeout
            mQueueTimeout = TimeSpan.FromSeconds(30);
            // update datetime
            mQueueSpinASendTime = DateTime.Now;
            mQueueSpinAReceiveTime = DateTime.Now;
            mQueueSpinBSendTime = DateTime.Now;
            mQueueSpinBReceiveTime = DateTime.Now;
            mQueueSpinCRSendTime = DateTime.Now;
            mQueueSpinCRReceiveTime = DateTime.Now;
        }

        public void Start(string server, string userName, string password)
        {
            // set task's cancel token
            mRequestCancelToken = new CancellationTokenSource();
            mRunTask = true;

            int errCode = 200;
            mTaskQueue = Task.Run(() =>
            {
                while (mRunTask)
                {
                    mMutexQueue.WaitOne();
                    try
                    {
                        foreach (var item in mDicQueue)
                        {
                            string key = item.Key;
                            List<string> list = item.Value;
                            foreach (var content in list)
                            {
                                switch (key)
                                {
                                    case ChannelSpinA:
                                        // refresh mQueueSpinAReceiveTime
                                        mQueueSpinAReceiveTime = DateTime.Now;
                                        try
                                        {
                                            // parse queue message
                                            JObject obj = JObject.Parse(content);
                                            string? account = obj.ContainsKey("account") ? obj["account"]?.Value<string>() : null;
                                            if (account == QueueName)
                                            {
                                                // handle queue event
                                                string? queueEvent = obj["event"]?.Value<string>();
                                                if (queueEvent == Heartbeat)
                                                {
                                                    // handle heartbeat event
                                                }
                                            }
                                            else
                                            {
                                                BonusAgent.OnChannelSpinA(obj);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.StoreMsg(string.Format("ChannelSpinA got {0}", ex.Message));
                                        }
                                        break;
                                    case ChannelSpinB:
                                        // refresh mQueueSpinBReceiveTime
                                        mQueueSpinBReceiveTime = DateTime.Now;
                                        try
                                        {
                                            // parse queue message
                                            JObject obj = JObject.Parse(content);
                                            string? account = obj.ContainsKey("account") ? obj["account"]?.Value<string>() : null;
                                            if (account == QueueName)
                                            {
                                                // handle queue event
                                                string? queueEvent = obj["event"]?.Value<string>();
                                                if (queueEvent == Heartbeat)
                                                {
                                                    // handle heartbeat event
                                                }
                                            }
                                            else
                                            {
                                                BonusAgent.OnChannelSpinB(obj);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.StoreMsg(string.Format("ChannelSpinB got {0}", ex.Message));
                                        }
                                        break;
                                    case ChannelSpinCR:
                                        // refresh mQueueSpinCRReceiveTime
                                        mQueueSpinCRReceiveTime = DateTime.Now;
                                        try
                                        {
                                            // parse queue message
                                            JObject obj = JObject.Parse(content);
                                            string? account = obj.ContainsKey("account") ? obj["account"]?.Value<string>() : null;
                                            if (account == QueueName)
                                            {
                                                // handle queue event
                                                string? queueEvent = obj["event"]?.Value<string>();
                                                if (queueEvent == Heartbeat)
                                                {
                                                    // handle heartbeat event
                                                }
                                            }
                                            else
                                            {
                                                BonusAgent.OnChannelSpinCR(obj);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.StoreMsg(string.Format("ChannelSpinCR got {0}", ex.Message));
                                        }
                                        break;
                                }
                            }
                        }
                        mDicQueue.Clear();
                    }
                    catch (Exception ex)
                    {

                    }
                    mMutexQueue.ReleaseMutex();
                    // sleep to reduce CPU loading
                    Thread.Sleep(1000);

                    errCode = 700;
                    // export cache records
                    TimeSpan ts = DateTime.Now - mExportTime;
                    if (ts.TotalSeconds > 60)
                    {
                        errCode = 800;
                        BonusAgent.ExportRecords();
                        // update 
                        mExportTime = DateTime.Now;
                    }
                    errCode = 900;
                }
            }, mRequestCancelToken.Token);
        }

        public void Stop()
        {
            // stop & close old cpnnection
            if (mTaskQueue != null)
            {
                mRunTask = false;
                if (mRequestCancelToken != null) mRequestCancelToken.Cancel();
                int retryCnt = 100;
                while (retryCnt > 0)
                {
                    Thread.Sleep(100);
                    switch (mTaskQueue.Status)
                    {
                        case TaskStatus.Canceled:
                        case TaskStatus.Faulted:
                        case TaskStatus.RanToCompletion:
                            retryCnt = 0;
                            break;
                    }
                }
            }
        }

        public string? Publish(string channel, string message)
        {
            string? errMsg = null;
            mMutexQueue.WaitOne();
            try
            {
                if (channel == null || channel.Length == 0) throw new Exception("invalid channel");
                if (message == null || message.Length == 0) throw new Exception("invalid message");

                if (mDicQueue.ContainsKey(channel) == false) mDicQueue[channel] = new List<string>();
                switch (channel)
                {
                    case ChannelSpinA:
                        mDicQueue[channel].Add(message);
                        mQueueSpinASendTime = DateTime.Now;
                        break;
                    case ChannelSpinB:
                        mDicQueue[channel].Add(message);
                        mQueueSpinBSendTime = DateTime.Now;
                        break;
                    case ChannelSpinCR:
                        mDicQueue[channel].Add(message);
                        mQueueSpinCRSendTime = DateTime.Now;
                        break;
                }
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            mMutexQueue.ReleaseMutex();
            return errMsg;
        }

        public bool FireHeartbeat()
        {
            bool bIsValid = true;
            // random to choose channel
            Random rand = new Random();
            switch (rand.Next() % 3)
            {
                case 0:
                    {
                        TimeSpan ts = DateTime.Now - mQueueSpinAReceiveTime;
                        if (ts.TotalSeconds > mQueueTimeout.TotalSeconds)
                        {
                            // restart CustomQ...
                            bIsValid = false;
                        }
                        else
                        {
                            ts = DateTime.Now - mQueueSpinASendTime;
                            if (ts.TotalSeconds >= 5)
                            {
                                try
                                {
                                    JObject obj = new JObject();
                                    obj["account"] = QueueName;
                                    obj["event"] = Heartbeat;
                                    string message = JsonConvert.SerializeObject(obj);
                                    string? error = Publish(ChannelSpinA, message);
                                    if (error != null) throw new Exception(error);
                                }
                                catch (Exception ex)
                                {
                                    Log.StoreMsg(string.Format("ChannelSpinA heartbeat got {0}", ex.Message));
                                }
                                mQueueSpinASendTime = DateTime.Now;
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        TimeSpan ts = DateTime.Now - mQueueSpinBReceiveTime;
                        if (ts.TotalSeconds > mQueueTimeout.TotalSeconds)
                        {
                            // restart CustomQ...
                            bIsValid = false;
                        }
                        else
                        {
                            ts = DateTime.Now - mQueueSpinBSendTime;
                            if (ts.TotalSeconds >= 5)
                            {
                                try
                                {
                                    JObject obj = new JObject();
                                    obj["account"] = QueueName;
                                    obj["event"] = Heartbeat;
                                    string message = JsonConvert.SerializeObject(obj);
                                    string? error = Publish(ChannelSpinB, message);
                                    if (error != null) throw new Exception(error);
                                }
                                catch (Exception ex)
                                {
                                    Log.StoreMsg(string.Format("ChannelSpinB heartbeat got {0}", ex.Message));
                                }
                                mQueueSpinBSendTime = DateTime.Now;
                            }
                        }
                    }
                    break;
                case 2:
                    {
                        TimeSpan ts = DateTime.Now - mQueueSpinCRReceiveTime;
                        if (ts.TotalSeconds > mQueueTimeout.TotalSeconds)
                        {
                            // restart CustomQ...
                            bIsValid = false;
                        }
                        else
                        {
                            ts = DateTime.Now - mQueueSpinCRSendTime;
                            if (ts.TotalSeconds >= 5)
                            {
                                try
                                {
                                    JObject obj = new JObject();
                                    obj["account"] = QueueName;
                                    obj["event"] = Heartbeat;
                                    string message = JsonConvert.SerializeObject(obj);
                                    string? error = Publish(ChannelSpinCR, message);
                                    if (error != null) throw new Exception(error);
                                }
                                catch (Exception ex)
                                {
                                    Log.StoreMsg(string.Format("ChannelSpinCR heartbeat got {0}", ex.Message));
                                }
                                mQueueSpinCRSendTime = DateTime.Now;
                            }
                        }
                    }
                    break;
            }
            return bIsValid;
        }
    }
}

using FunLobbyUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace BonusServer.Services.QueueInfo
{
    public class RabbitMQInfo : QueueInterface
    {
        const string Heartbeat = "Heartbeat";
        const string QueueName = "BonusAgent-RabbitMQ";
        const string ChannelSpinA = "SpinA";
        const string ChannelSpinB = "SpinB";
        const string ChannelSpinCR = "SpinCR";

        ConnectionFactory? mFactory = null;
        IModel? mChannelSpinA = null;
        IModel? mChannelSpinB = null;
        IModel? mChannelSpinCR = null;
        IConnection? mConnection = null;
        Mutex mMutexQueue = new Mutex();
        Task? mTaskQueue = null;
        CancellationTokenSource? mRequestCancelToken = null;
        TimeSpan mQueueTimeout;
        DateTime mQueueSpinASendTime;
        DateTime mQueueSpinAReceiveTime;
        DateTime mQueueSpinBSendTime;
        DateTime mQueueSpinBReceiveTime;
        DateTime mQueueSpinCRSendTime;
        DateTime mQueueSpinCRReceiveTime;
        DateTime mExportTime = DateTime.Now;

        public RabbitMQInfo()
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

            mFactory = new ConnectionFactory()
            {
                HostName = server,
                UserName = userName,
                Password = password
            };

            int errCode = 200;
            mTaskQueue = Task.Run(() =>
            {
                while (mFactory != null)
                {
                    mMutexQueue.WaitOne();
                    try
                    {
                        if (mConnection == null ||
                            mConnection.IsOpen == false)
                        {
                            errCode = 300;
                            if (mChannelSpinA != null)
                            {
                                errCode = 310;
                                mChannelSpinA.Close();
                                mChannelSpinA = null;
                                errCode = 320;
                            }
                            if (mChannelSpinB != null)
                            {
                                errCode = 330;
                                mChannelSpinB.Close();
                                mChannelSpinB = null;
                                errCode = 340;
                            }
                            if (mChannelSpinCR != null)
                            {
                                errCode = 350;
                                mChannelSpinCR.Close();
                                mChannelSpinCR = null;
                                errCode = 360;
                            }
                            errCode = 370;
                            //if (mConnection != null) mConnection.Close();
                            errCode = 380;
                            mConnection = mFactory.CreateConnection();
                            errCode = 390;
                        }

                        // handle UpdateUserScores
                        if (mChannelSpinA == null ||
                            mChannelSpinA.IsOpen == false)
                        {
                            errCode = 400;
                            if (mChannelSpinA != null) mChannelSpinA.Close();
                            mChannelSpinA = mConnection.CreateModel();
                            mChannelSpinA.QueueDeclare(queue: ChannelSpinA,
                                                       durable: false,
                                                       exclusive: false,
                                                       autoDelete: false,
                                                       arguments: null);

                            EventingBasicConsumer consumer = new EventingBasicConsumer(mChannelSpinA);
                            consumer.Received += (model, ea) =>
                            {
                                // refresh mQueueSpinAReceiveTime
                                mQueueSpinAReceiveTime = DateTime.Now;
                                // call back
                                byte[] body = ea.Body.ToArray();
                                string content = Encoding.UTF8.GetString(body);
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
                                // acknowledge
                                IModel? channel = (model as EventingBasicConsumer)?.Model;
                                if (channel != null && channel.IsClosed == false)
                                {
                                    channel.BasicAck(ea.DeliveryTag, false);
                                }
                            };
                            mChannelSpinA.BasicConsume(queue: ChannelSpinA,
                                                       autoAck: false,
                                                       consumer: consumer);
                            errCode = 410;
                        }

                        // handle UpdateGMScores
                        if (mChannelSpinB == null ||
                            mChannelSpinB.IsOpen == false)
                        {
                            errCode = 500;
                            if (mChannelSpinB != null) mChannelSpinB.Close();
                            mChannelSpinB = mConnection.CreateModel();
                            mChannelSpinB.QueueDeclare(queue: ChannelSpinB,
                                                       durable: false,
                                                       exclusive: false,
                                                       autoDelete: false,
                                                       arguments: null);

                            EventingBasicConsumer consumer = new EventingBasicConsumer(mChannelSpinB);
                            consumer.Received += (model, ea) =>
                            {
                                // refresh mQueueSpinBReceiveTime
                                mQueueSpinBReceiveTime = DateTime.Now;
                                // call back
                                byte[] body = ea.Body.ToArray();
                                string content = Encoding.UTF8.GetString(body);
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
                                // acknowledge
                                IModel? channel = (model as EventingBasicConsumer)?.Model;
                                if (channel != null && channel.IsClosed == false)
                                {
                                    channel.BasicAck(ea.DeliveryTag, false);
                                }
                            };
                            mChannelSpinB.BasicConsume(queue: ChannelSpinB,
                                                       autoAck: false,
                                                       consumer: consumer);
                            errCode = 510;
                        }

                        // handle UpdateUserScores
                        if (mChannelSpinCR == null ||
                            mChannelSpinCR.IsOpen == false)
                        {
                            errCode = 600;
                            if (mChannelSpinCR != null) mChannelSpinCR.Close();
                            mChannelSpinCR = mConnection.CreateModel();
                            mChannelSpinCR.QueueDeclare(queue: ChannelSpinCR,
                                                        durable: false,
                                                        exclusive: false,
                                                        autoDelete: false,
                                                        arguments: null);

                            EventingBasicConsumer consumer = new EventingBasicConsumer(mChannelSpinCR);
                            consumer.Received += (model, ea) =>
                            {
                                // refresh mQueueSpinCRReceiveTime
                                mQueueSpinCRReceiveTime = DateTime.Now;
                                // call back
                                byte[] body = ea.Body.ToArray();
                                string content = Encoding.UTF8.GetString(body);
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
                                // acknowledge
                                IModel? channel = (model as EventingBasicConsumer)?.Model;
                                if (channel != null && channel.IsClosed == false)
                                {
                                    channel.BasicAck(ea.DeliveryTag, false);
                                }
                            };
                            mChannelSpinCR.BasicConsume(queue: ChannelSpinCR,
                                                        autoAck: false,
                                                        consumer: consumer);
                            errCode = 610;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.StoreMsg(string.Format("while(mFactory != null) in InitQueues got {0}, errCode:{1}", ex.Message, errCode));
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
                if (mConnection != null)
                {
                    mConnection.Close();
                    mConnection = null;
                }
                errCode = 1000;
            }, mRequestCancelToken.Token);
        }

        public void Stop()
        {
            // stop & close old cpnnection
            if (mTaskQueue != null)
            {
                mFactory = null;
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
            if (mChannelSpinA != null)
            {
                mChannelSpinA.Close();
                mChannelSpinA = null;
            }
            if (mChannelSpinB != null)
            {
                mChannelSpinB.Close();
                mChannelSpinB = null;
            }
            if (mChannelSpinCR != null)
            {
                mChannelSpinCR.Close();
                mChannelSpinCR = null;
            }
            if (mConnection != null)
            {
                mConnection.Close();
                mConnection = null;
            }
        }

        public string? Publish(string channel, string message)
        {
            string? errMsg = null;
            mMutexQueue.WaitOne();
            try
            {
                switch (channel)
                {
                    case ChannelSpinA:
                        {
                            if (mChannelSpinA == null ||
                                mChannelSpinA.IsOpen == false)
                            {
                                throw new Exception("ChannelSpinA is invalid");
                            }

                            byte[] body = Encoding.UTF8.GetBytes(message);
                            mChannelSpinA.BasicPublish(exchange: "",
                                                    routingKey: ChannelSpinA,
                                                    basicProperties: null,
                                                    body: body);
                            mQueueSpinASendTime = DateTime.Now;
                        }
                        break;
                    case ChannelSpinB:
                        {
                            if (mChannelSpinB == null ||
                                mChannelSpinB.IsOpen == false)
                            {
                                throw new Exception("ChannelSpinB is invalid");
                            }

                            byte[] body = Encoding.UTF8.GetBytes(message);
                            mChannelSpinB.BasicPublish(exchange: "",
                                                    routingKey: ChannelSpinB,
                                                    basicProperties: null,
                                                    body: body);
                            mQueueSpinBSendTime = DateTime.Now;
                        }
                        break;
                    case ChannelSpinCR:
                        {
                            if (mChannelSpinCR == null ||
                                mChannelSpinCR.IsOpen == false)
                            {
                                throw new Exception("ChannelSpinCR is invalid");
                            }

                            byte[] body = Encoding.UTF8.GetBytes(message);
                            mChannelSpinCR.BasicPublish(exchange: "",
                                                    routingKey: ChannelSpinCR,
                                                    basicProperties: null,
                                                    body: body);
                            mQueueSpinCRSendTime = DateTime.Now;
                        }
                        break;
                }
            }
            catch (Exception ex)
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
                            // restart RabbitMQ...
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
                                    Log.StoreMsg(string.Format("mChannelSpinA heartbeat got {0}", ex.Message));
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
                            // restart RabbitMQ...
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
                                    Log.StoreMsg(string.Format("mChannelSpinB heartbeat got {0}", ex.Message));
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
                            // restart RabbitMQ...
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
                                    Log.StoreMsg(string.Format("mChannelSpinCR heartbeat got {0}", ex.Message));
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils
{
    public class BroadcastMsg
    {
        public delegate void FncTriggered(BroadcastMsg data);
        public delegate void FncExpired(BroadcastMsg data);
        public delegate void FncModified(BroadcastMsg data);
        public delegate void FncRemoved(BroadcastMsg data);

        DateTime mPreUpdateTime;
        bool mbNewMsg = true;

        public DateTime BeginTime { get; private set; }
        float mDurationInSecond;
        public float Duration
        {
            get { return mDurationInSecond / 60 / 60; }
            set { mDurationInSecond = value * 60 * 60; }
        }
        public string? MsgContent { get; private set; }
        public string? MsgId { get; private set; }
        public bool IsTriggered { get; private set; }
        public bool IsExpired { get; private set; }
        public FncTriggered? OnTriggered { get; set; }
        public FncExpired? OnExpired { get; set; }
        public FncModified? OnModified { get; set; }
        public FncRemoved? OnRemoved { get; set; }

        public static BroadcastMsg Create(FncTriggered? fncTriggered, FncExpired? fncExpired, FncModified? fncModified, FncRemoved? fncRemoved)
        {
            BroadcastMsg msg = new BroadcastMsg();
            msg.OnTriggered = fncTriggered;
            msg.OnExpired = fncExpired;
            msg.OnModified = fncModified;
            msg.OnRemoved = fncRemoved;
            return msg;
        }

        public static BroadcastMsg? FromJson(JObject? obj)
        {
            BroadcastMsg? msg = null;
            try
            {
                msg = BroadcastMsg.Create(null, null, null, null);
                msg.BeginTime = obj["BeginTime"].Value<DateTime>();
                DateTime.SpecifyKind(msg.BeginTime, DateTimeKind.Utc);
                msg.BeginTime = msg.BeginTime.ToLocalTime();
                msg.Duration = obj["Duration"].Value<float>();
                msg.MsgContent = obj["MsgContent"].Value<string>();
                msg.MsgId = obj["MsgId"].Value<string>();
                msg.IsTriggered = obj["IsTriggered"].Value<bool>();
                msg.IsExpired = obj["IsExpired"].Value<bool>();
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("BroadcastMsg FromJson got {0}, obj:{1}", ex.Message, JsonConvert.SerializeObject(obj)));
                msg = null;
            }
            return msg;
        }

        public JObject ToJson()
        {
            JObject obj = new JObject();
            obj["BeginTime"] = Utils.dateTimeToString(this.BeginTime);
            obj["Duration"] = this.Duration;
            obj["MsgContent"] = this.MsgContent;
            obj["MsgId"] = this.MsgId;
            obj["IsTriggered"] = this.IsTriggered;
            obj["IsExpired"] = this.IsExpired;
            return obj;
        }

        protected BroadcastMsg()
        {
            this.IsTriggered = false;
            this.IsExpired = false;
            mPreUpdateTime = DateTime.Now;
            this.MsgId = Utils.generateRandom(16);
        }

        public void Dispose()
        {
            this.OnRemoved?.Invoke(this);
        }

        public string? SetData(DateTime beginTime, float durationInHour, string? msgContent)
        {
            // make sure to use local time
            beginTime = beginTime.ToLocalTime();

            DateTime curTime = DateTime.Now;
            TimeSpan ts = beginTime - curTime;
            string? errMsg = null;
            // if time picker returns expected time earlier than current time, skip request
            if (ts.TotalMinutes <= 0)
            {
                errMsg = "設定錯誤，已超過公告開始時間。";
            }
            else if (msgContent == null || msgContent.Length == 0 || durationInHour == 0)
            {
                errMsg = "公告內容不可為空。";
            }
            else
            {
                this.BeginTime = beginTime;
                mDurationInSecond = durationInHour * 60 * 60;
                this.MsgContent = msgContent;

                if (this.mbNewMsg == false) this.OnModified?.Invoke(this);
                else this.mbNewMsg = false;
            }
            return errMsg;
        }

        public void Update()
        {
            if (this.IsExpired == true)
                return;

            DateTime curTime = DateTime.Now;
            TimeSpan ts = curTime - this.BeginTime;
            if (ts.TotalSeconds >= 0)
            {
                if (this.IsTriggered == false)
                {
                    if (this.BeginTime > mPreUpdateTime)
                    {
                        this.IsTriggered = true;
                        OnTriggered?.Invoke(this);
                    }
                }
                else
                {
                    if (ts.TotalSeconds > mDurationInSecond)
                    {
                        this.IsExpired = true;
                        OnExpired?.Invoke(this);
                    }
                }
            }
            mPreUpdateTime = curTime;
        }
    }
}

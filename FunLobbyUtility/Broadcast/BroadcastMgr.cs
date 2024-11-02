using Newtonsoft.Json.Linq;

namespace FunLobbyUtils
{
    public class BroadcastMgr
    {
        static BroadcastMgr? mInstance = null;

        public static BroadcastMgr Instance
        {
            get
            {
                if (mInstance == null) mInstance = new BroadcastMgr();
                return mInstance;
            }
        }

        List<BroadcastMsg> mBroadcastMsgs = new List<BroadcastMsg>();
        public List<BroadcastMsg> BroadcastMsgs { get { return mBroadcastMsgs; } }

        protected BroadcastMgr()
        {

        }

        public void Update()
        {
            // the Update may invoke remove event,
            // so loop list from end to head
            for (int i = this.BroadcastMsgs.Count-1; i >= 0; i--)
            {
                BroadcastMsg msg = this.BroadcastMsgs[i];
                msg.Update();
            }
        }

        public BroadcastMsg AddBroadcastMsg(DateTime beginDate, float duration, string content, BroadcastMsg.FncTriggered fncTriggered, BroadcastMsg.FncExpired fncExpired, BroadcastMsg.FncModified fncModified, BroadcastMsg.FncRemoved fncRemoved)
        {
            BroadcastMsg msg = BroadcastMsg.Create(fncTriggered, fncExpired, fncModified, fncRemoved);
            string? errMsg = msg.SetData(beginDate, duration, content);
            if (errMsg == null)
            {
                this.BroadcastMsgs.Add(msg);
                return msg;
            }
            else
            {
                throw new Exception(errMsg);
            }
        }

        public BroadcastMsg? ModifyBroadcastMsg(string? msgId, DateTime beginDate, float duration, string? content)
        {
            BroadcastMsg? msg = this.FindBroadcastMsg(msgId);
            if (msg != null && msg.IsTriggered == false && msg.IsExpired == false)
            {
                string? errMsg = msg.SetData(beginDate, duration, content);
                if (errMsg == null) return msg;
                else
                {
                    throw new Exception("errMsg");
                }
            }
            else
            {
                throw new Exception(string.Format("未發現指定公告", msgId));
            }
        }

        public BroadcastMsg? FindBroadcastMsg(string? msgId)
        {
            BroadcastMsg? broadcastMsg = null;
            for (int i = 0; i < this.BroadcastMsgs.Count; i++)
            {
                // skip unmatched msg
                if (this.BroadcastMsgs[i].MsgId != msgId) continue;
                broadcastMsg = this.BroadcastMsgs[i];
                break;
            }
            return broadcastMsg;
        }

        public bool RemoveBroadcastMsg(string msgId)
        {
            for (int i = 0; i < mBroadcastMsgs.Count; i++)
            {
                BroadcastMsg msg = mBroadcastMsgs[i];
                // skip unmatched msg
                if (msg.MsgId != msgId) continue;
                // Dispose will trigger CB, so remove from list before calling Dispose
                mBroadcastMsgs.Remove(msg);
                msg.Dispose();
                return true;
            }
            return false;
        }

        public void Sync(JArray broadcasts)
        {
            mBroadcastMsgs = new List<BroadcastMsg>();
            for (int i = 0; i < broadcasts.Count; i++)
            {
                JObject? objMsg = broadcasts[i].Value<JObject>();
                BroadcastMsg? newBroadcastMsg = BroadcastMsg.FromJson(objMsg);
                if (newBroadcastMsg != null) this.BroadcastMsgs.Add(newBroadcastMsg);
            }
        }
    }
}

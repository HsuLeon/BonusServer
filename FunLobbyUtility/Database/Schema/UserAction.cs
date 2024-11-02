
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class UserAction
    {
        public const string Undefined = "未定義";
        public const string Login = "登入";
        public const string Logout = "登出";
        public const string LockMachine = "進入機台";
        public const string UnlockMachine = "離開機台";
        public const string StartGuoZhao = "開招";
        public const string StopGuoZhao = "解招";
        public const string IncreaseScore = "加分";
        public const string DecreaseScore = "減分";
        public const string IncreaseTrialScore = "加試玩分";
        public const string DecreaseTrialScore = "減試玩分";
        public const string IncreaseGuoZhaoScore = "加過招分";
        public const string DecreaseGuoZhaoScore = "減過招分";
        public const string Kickedout = "用戶被踢出";
        public const string SummarizeScore = "總結開洗分";
        public const string ChangePassword = "變更密碼";
        public const string ExchangeTrialToFormal = "銀幣換金幣";
        public const string ExchangeFormalToTrial = "金幣換銀幣";
        public const string ExchangeBetRewardToFormal = "返水換金幣";
        public const string StartUniPlay = "開始跨館";
        public const string StopUniPlay = "結束跨館";
        public const string TransferUniPlayScore = "跨館轉出金幣";
        public const string RetriveUniPlayScore = "跨館取回金幣";
        public const string SummarizeUniPlay = "跨館總結";
        public const string UniPlayProfits = "跨館收益";

        public ObjectId _id { get; set; }
        public string UserId { get; set; }
        public string Lobby { get; set; }
        public string Machine { get; set; }
        public string Action { get; set; }
        public string Note { get; set; }
        public DateTime CreateTime { get; set; }

        public UserAction()
        {
            _id = ObjectId.Empty;
            UserId = "";
            Lobby = "";
            Machine = "";
            Action = "";
            Note = "";
            CreateTime = DateTime.UtcNow;
        }

        public static UserAction FromJson(JObject obj)
        {
            UserAction userAction = null;
            if (obj != null)
            {
                try
                {
                    userAction = new UserAction();
                    userAction._id = ObjectId.Parse(obj["_id"].Value<string>());
                    userAction.UserId = obj.ContainsKey("UserId") ? obj["UserId"].Value<string>() : "";
                    userAction.Lobby = obj.ContainsKey("Lobby") ? obj["Lobby"].Value<string>() : "";
                    userAction.Machine = obj.ContainsKey("Machine") ? obj["Machine"].Value<string>() : "";
                    userAction.Action = obj.ContainsKey("Action") ? obj["Action"].Value<string>() : "";
                    userAction.Note = obj.ContainsKey("Note") ? obj["Note"].Value<string>() : "";
                    userAction.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    userAction = null;
                }
            }
            return userAction;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

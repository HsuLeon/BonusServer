
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class GMAction
    {
        public const string Undefined = "未定義";
        public const string Login = "登入";
        public const string Logout = "登出";
        public const string CreateUser = "新增用戶";
        public const string DeleteUser = "刪除用戶";
        public const string SaveUser = "儲存用戶資料";
        public const string CreateGM = "新增管理者";
        public const string DeleteGM = "刪除管理者";
        public const string SaveGM = "儲存管理者資料";
        public const string ChangePassword = "變更密碼";
        public const string AddUserCoins = "增加點數";
        public const string SubUserCoins = "減少點數";
        public const string AddUserTrialCoins = "增加試玩點數";
        public const string SubUserTrialCoins = "減少試玩點數";
        public const string AddGMCoins = "增加管理者點數";
        public const string SubGMCoins = "減少管理者點數";
        public const string AddGMTrialCoins = "增加管理者試玩點數";
        public const string SubGMTrialCoins = "減少管理者試玩點數";
        public const string IncreaseScore = "加分";
        public const string DecreaseScore = "減分";
        public const string IncreaseTrialScore = "加試玩分";
        public const string DecreaseTrialScore = "減試玩分";
        public const string CalBill = "結帳";
        public const string Permission = "變更權限";
        public const string TerminateScoring = "中斷開洗分";
        public const string KickoutClient = "踢出用戶";
        public const string KickoutGuoZhao = "踢除過招用戶";
        public const string KickoutUniPlay = "停止用戶跨館";
        public const string UnbindMachine = "解綁機台";
        public const string BookMachine = "機台訂位";
        public const string UnbookMachine = "解除訂位";
        public const string CloseApp = "關閉管理程式";

        public ObjectId _id { get; set; }
        public string Account { get; set; }
        public string Action { get; set; }
        public string Note { get; set; }
        DateTime mCreateTime;
        public DateTime CreateTime
        {
            get { return mCreateTime.ToLocalTime(); }
            set { mCreateTime = value.ToUniversalTime(); }
        }

        public GMAction()
        {
            _id = ObjectId.Empty;
            Account = "";
            Action = "";
            Note = "";
            CreateTime = DateTime.UtcNow;
        }

        public static GMAction FromJson(JObject obj)
        {
            GMAction gmAction = null;
            if (obj != null)
            {
                try
                {
                    gmAction = new GMAction();
                    gmAction._id = ObjectId.Parse(obj["_id"].Value<string>());
                    gmAction.Account = obj.ContainsKey("Account") ? obj["Account"].Value<string>() : "";
                    gmAction.Action = obj.ContainsKey("Action") ? obj["Action"].Value<string>() : "";
                    gmAction.Note = obj.ContainsKey("Note") ? obj["Note"].Value<string>() : "";
                    gmAction.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow; ;
                }
                catch (Exception ex)
                {
                    gmAction = null;
                }
            }
            return gmAction;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

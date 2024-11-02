
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class UserAward
    {
        public const string Coins = "coins";
        public const string TrialCoins = "trialCoins";

        public ObjectId _id { get; set; }
        public string Account { get; set; }
        public string Reason { get; set; }
        public string AwardType { get; set; }
        public int Award { get; set; }
        public string Note { get; set; }
        public DateTime CreateTime { get; set; }

        public UserAward()
        {
            _id = ObjectId.Empty;
            Account = "";
            Reason = "";
            AwardType = "";
            Award = 0;
            Note = "";
            CreateTime = DateTime.UtcNow;
        }

        public static UserAward FromJson(JObject obj)
        {
            UserAward userAward = null;
            if (obj != null)
            {
                try
                {
                    userAward = new UserAward();
                    userAward._id = ObjectId.Parse(obj["_id"].Value<string>());
                    userAward.Account = obj.ContainsKey("Account") ? obj["Account"].Value<string>() : "";
                    userAward.Reason = obj.ContainsKey("Reason") ? obj["Reason"].Value<string>() : "";
                    userAward.AwardType = obj.ContainsKey("AwardType") ? obj["AwardType"].Value<string>() : "";
                    userAward.Award = obj.ContainsKey("Award") ? obj["Award"].Value<int>() : 0;
                    userAward.Note = obj.ContainsKey("Note") ? obj["Note"].Value<string>() : "";
                    userAward.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    userAward = null;
                }
            }
            return userAward;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

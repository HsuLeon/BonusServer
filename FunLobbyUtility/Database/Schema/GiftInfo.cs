
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class GiftInfo
    {
        public const string keyType = "type";
        public const string keyCoins = "coins";
        public const string keyFee = "fee";

        public const int GIFTTYPE_NONE = 0;
        public const int GIFTTYPE_FORMALSCORE = 1;
        public const int GIFTTYPE_TRIALSCORE = 2;

        public ObjectId _id { get; set; }
        public string User { get; set; }
        public string Target { get; set; }
        public int Type { get; private set; }
        public string Content { get; private set; }
        public DateTime CreateTime { get; set; }

        public int Coins
        {
            get
            {
                JObject obj = JObject.Parse(Content);
                int coins = 0;
                if (obj.ContainsKey(keyType) &&
                    obj[keyType].Value<int>() == GiftInfo.GIFTTYPE_FORMALSCORE)
                {
                    coins = obj.ContainsKey(keyCoins) ? obj[keyCoins].Value<int>() : 0;
                }
                return coins;
            }
        }

        public GiftInfo(JObject objGift = null)
        {
            _id = ObjectId.Empty;
            User = "";
            Target = "";
            if (objGift != null)
            {
                Type = objGift.ContainsKey(keyType) ? objGift[keyType].Value<int>() : GiftInfo.GIFTTYPE_NONE;
                Content = JsonConvert.SerializeObject(objGift);
            }
            else
            {
                Type = GiftInfo.GIFTTYPE_NONE;
                Content = null;
            }
            CreateTime = DateTime.UtcNow;
        }

        public static GiftInfo FromJson(JObject obj)
        {
            GiftInfo giftInfo = null;
            if (obj != null)
            {
                try
                {
                    giftInfo = new GiftInfo();
                    giftInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                    giftInfo.User = obj.ContainsKey("User") ? obj["User"].Value<string>() : "";
                    giftInfo.Target = obj.ContainsKey("Target") ? obj["Target"].Value<string>() : "";
                    giftInfo.Type = obj.ContainsKey("Type") ? obj["Type"].Value<int>() : GiftInfo.GIFTTYPE_NONE;
                    giftInfo.Content = obj.ContainsKey("Content") ? obj["Content"].Value<string>() : null;
                    giftInfo.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    giftInfo = null;
                }
            }
            return giftInfo;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

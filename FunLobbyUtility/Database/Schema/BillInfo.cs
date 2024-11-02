
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class BillInfo
    {
        public ObjectId _id { get; set; }
        public string GM { get; set; }
        public string Note { get; set; }
        public DateTime CreateTime { get; set; }

        public BillInfo()
        {
            _id = ObjectId.Empty;
            GM = "";
            Note = "";
            CreateTime = DateTime.UtcNow;
        }

        public static BillInfo FromJson(JObject obj)
        {
            BillInfo billInfo = null;
            if (obj != null)
            {
                try
                {
                    billInfo = new BillInfo();
                    billInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                    billInfo.GM = obj.ContainsKey("GM") ? obj["GM"].Value<string>() : "";
                    billInfo.Note = obj.ContainsKey("Note") ? obj["Note"].Value<string>() : "";
                    billInfo.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    billInfo = null;
                }
            }
            return billInfo;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

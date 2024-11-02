
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class WainZhu
    {
        public ObjectId _id { get; set; }
        public string User { get; set; }
        public int ScoreCnt { get; set; }
        public DateTime CreateTime { get; set; }

        public WainZhu()
        {
            _id = ObjectId.Empty;
            User = "";
            ScoreCnt = 0;
            CreateTime = DateTime.UtcNow;
        }

        public static WainZhu FromJson(JObject obj)
        {
            WainZhu wainZhu = null;
            if (obj != null)
            {
                try
                {
                    wainZhu = new WainZhu();
                    wainZhu._id = ObjectId.Parse(obj["_id"].Value<string>());
                    wainZhu.User = obj.ContainsKey("User") ? obj["User"].Value<string>() : "";
                    wainZhu.ScoreCnt = obj.ContainsKey("ScoreCnt") ? obj["ScoreCnt"].Value<int>() : 0;
                    wainZhu.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    wainZhu = null;
                }
            }
            return wainZhu;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

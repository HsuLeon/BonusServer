
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class ScoreInfo
    {
        public ObjectId _id { get; set; }
        public string UserId { get; set; }
        public string Lobby { get; set; }
        public string Machine { get; set; }
        public int ArduinoId { get; set; }
        public int MachineId { get; set; }
        public long TotalInScore { get; set; }
        public long TotalOutScore { get; set; }
        public DateTime CreateTime { get; set; }

        public ScoreInfo()
        {
            _id = ObjectId.Empty;
            UserId = "";
            Lobby = "";
            Machine = "";
            ArduinoId = -1;
            MachineId = -1;
            TotalInScore = 0;
            TotalOutScore = 0;
            CreateTime = DateTime.UtcNow;
        }

        public static ScoreInfo FromJson(JObject obj)
        {
            ScoreInfo scoreInfo = null;
            if (obj != null)
            {
                try
                {
                    scoreInfo = new ScoreInfo();
                    scoreInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                    scoreInfo.UserId = obj.ContainsKey("UserId") ? obj["UserId"].Value<string>() : "";
                    scoreInfo.Lobby = obj.ContainsKey("Lobby") ? obj["Lobby"].Value<string>() : "";
                    scoreInfo.Machine = obj.ContainsKey("Machine") ? obj["Machine"].Value<string>() : "";
                    scoreInfo.ArduinoId = obj.ContainsKey("ArduinoId") ? obj["ArduinoId"].Value<int>() : -1;
                    scoreInfo.MachineId = obj.ContainsKey("MachineId") ? obj["MachineId"].Value<int>() : -1;
                    scoreInfo.TotalInScore = obj.ContainsKey("TotalInScore") ? obj["TotalInScore"].Value<int>() : 0;
                    scoreInfo.TotalOutScore = obj.ContainsKey("TotalOutScore") ? obj["TotalOutScore"].Value<int>() : 0;
                    scoreInfo.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow; ;
                }
                catch (Exception ex)
                {
                    scoreInfo = null;
                }
            }
            return scoreInfo;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

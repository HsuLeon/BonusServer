
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class UnknownOutScore
    {
        public ObjectId _id { get; set; }
        public string LobbyName { get; set; }
        public string MachineName { get; set; }
        public int ArduinoId { get; set; }
        public int MachineId { get; set; }
        public int SingleOutScore { get; set; }
        public long OutScoreCnt { get; set; }
        public DateTime CreateTime { get; set; }

        public UnknownOutScore()
        {
            _id = ObjectId.Empty;
            LobbyName = "";
            MachineName = "";
            ArduinoId = -1;
            MachineId = -1;
            SingleOutScore = 0;
            OutScoreCnt = 0;
            CreateTime = DateTime.UtcNow;
        }

        public static UnknownOutScore FromJson(JObject obj)
        {
            UnknownOutScore unknownOutScore = null;
            if (obj != null)
            {
                try
                {
                    unknownOutScore = new UnknownOutScore();
                    unknownOutScore._id = ObjectId.Parse(obj["_id"].Value<string>());
                    unknownOutScore.LobbyName = obj.ContainsKey("LobbyName") ? obj["LobbyName"].Value<string>() : "";
                    unknownOutScore.MachineName = obj.ContainsKey("MachineName") ? obj["MachineName"].Value<string>() : "";
                    unknownOutScore.ArduinoId = obj.ContainsKey("ArduinoId") ? obj["ArduinoId"].Value<int>() : -1;
                    unknownOutScore.MachineId = obj.ContainsKey("MachineId") ? obj["MachineId"].Value<int>() : -1;
                    unknownOutScore.SingleOutScore = obj.ContainsKey("SingleOutScore") ? obj["SingleOutScore"].Value<int>() : 0;
                    unknownOutScore.OutScoreCnt = obj.ContainsKey("OutScoreCnt") ? obj["OutScoreCnt"].Value<int>() : 0;
                    unknownOutScore.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    unknownOutScore = null;
                }
            }
            return unknownOutScore;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

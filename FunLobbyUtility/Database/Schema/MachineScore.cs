
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class MachineScore
    {
        public ObjectId _id { get; set; }
        public string LobbyName { get; set; }
        public int ArduinoId { get; set; }
        public int MachineId { get; set; }
        public long In { get; set; }
        public long Out { get; set; }
        public string UserAccount { get; set; }
        DateTime mCreateTime;
        public DateTime CreateTime
        {
            get { return mCreateTime.ToLocalTime(); }
            set { mCreateTime = value.ToUniversalTime(); }
        }
        DateTime mUpdateTime;
        public DateTime UpdateTime
        {
            get { return mUpdateTime.ToLocalTime(); }
            set { mUpdateTime = value.ToUniversalTime(); }
        }

        public MachineScore()
        {
            _id = ObjectId.Empty;
            LobbyName = "";
            ArduinoId = -1;
            MachineId = -1;
            In = 0;
            Out = 0;
            UserAccount = "";
            CreateTime = DateTime.UtcNow;
            UpdateTime = DateTime.UtcNow;
        }

        public static MachineScore FromJson(JObject obj)
        {
            MachineScore machineScore = null;
            if (obj != null)
            {
                try
                {
                    machineScore = new MachineScore();
                    machineScore._id = ObjectId.Parse(obj["_id"].Value<string>());
                    machineScore.LobbyName = obj.ContainsKey("LobbyName") ? obj["LobbyName"].Value<string>() : "";
                    machineScore.ArduinoId = obj.ContainsKey("ArduinoId") ? obj["ArduinoId"].Value<int>() : -1;
                    machineScore.MachineId = obj.ContainsKey("MachineId") ? obj["MachineId"].Value<int>() : -1;
                    machineScore.In = obj.ContainsKey("In") ? obj["In"].Value<long>() : 0;
                    machineScore.Out = obj.ContainsKey("Out") ? obj["Out"].Value<long>() : 0;
                    machineScore.UserAccount = obj.ContainsKey("UserAccount") ? obj["UserAccount"].Value<string>() : "";
                    machineScore.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                    machineScore.UpdateTime = obj.ContainsKey("UpdateTime") ? Convert.ToDateTime(obj["UpdateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    machineScore = null;
                }
            }
            return machineScore;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

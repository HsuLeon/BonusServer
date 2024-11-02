
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class MachineLog
    {
        public const string Lock = "鎖定";
        public const string ReLock = "重鎖定";
        public const string Unlock = "解鎖";
        public const string InScore = "開分";
        public const string OutScore = "洗分";
        public const string Summarize = "結算";

        public ObjectId _id { get; set; }
        public string Action { get; set; }
        public string LobbyName { get; set; }
        public int ArduinoId { get; set; }
        public int MachineId { get; set; }
        public string Log { get; set; }
        public string UserAccount { get; set; }
        DateTime mCreateTime;
        public DateTime CreateTime
        {
            get { return mCreateTime.ToLocalTime(); }
            set { mCreateTime = value.ToUniversalTime(); }
        }

        public MachineLog()
        {
            _id = ObjectId.Empty;
            Action = "";
            LobbyName = "";
            ArduinoId = -1;
            MachineId = -1;
            Log = null;
            UserAccount = "";
            CreateTime = DateTime.UtcNow;
        }

        public static MachineLog FromJson(JObject obj)
        {
            MachineLog machineLog = null;
            if (obj != null)
            {
                try
                {
                    machineLog = new MachineLog();
                    machineLog._id = ObjectId.Parse(obj["_id"].Value<string>());
                    machineLog.Action = obj.ContainsKey("Action") ? obj["Action"].Value<string>() : "";
                    machineLog.LobbyName = obj.ContainsKey("LobbyName") ? obj["LobbyName"].Value<string>() : "";
                    machineLog.ArduinoId = obj.ContainsKey("ArduinoId") ? obj["ArduinoId"].Value<int>() : -1;
                    machineLog.MachineId = obj.ContainsKey("MachineId") ? obj["MachineId"].Value<int>() : -1;
                    machineLog.Log = obj.ContainsKey("Log") ? obj["Log"].Value<string>() : null;
                    machineLog.UserAccount = obj.ContainsKey("UserAccount") ? obj["UserAccount"].Value<string>() : "";
                    machineLog.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;

                    if (machineLog.Action == "")
                    {
                        try
                        {
                            JObject objLog = JObject.Parse(machineLog.Log);
                            machineLog.Action = objLog.ContainsKey("action") ? objLog["action"].Value<string>() : "";
                        }
                        catch (Exception) { }
                    }
                }
                catch (Exception ex)
                {
                    machineLog = null;
                }
            }
            return machineLog;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

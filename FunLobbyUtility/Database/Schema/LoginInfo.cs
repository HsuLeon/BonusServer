
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class LoginInfo
    {
        public ObjectId _id { get; set; }
        public string AgentId { get; set; }
        public string AppId { get; set; }
        public string Token { get; set; }
        public string LobbyName { get; set; }
        public DateTime CreateDate { get; set; }

        public LoginInfo()
        {
            AgentId = "";
            AppId = "";
            Token = "";
            LobbyName = "";
            CreateDate = DateTime.UtcNow;
        }

        public static LoginInfo FromJson(JObject obj)
        {
            LoginInfo loginInfo = null;
            try
            {
                loginInfo = new LoginInfo();
                loginInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                loginInfo.AgentId = obj.ContainsKey("AgentId") ? obj["AgentId"].Value<String>() : "";
                loginInfo.AppId = obj.ContainsKey("AppId") ? obj["AppId"].Value<String>() : "";
                loginInfo.Token = obj.ContainsKey("Token") ? obj["Token"].Value<String>() : "";
                loginInfo.LobbyName = obj.ContainsKey("LobbyName") ? obj["LobbyName"].Value<String>() : "";
                loginInfo.CreateDate = obj.ContainsKey("CreateDate") ? Convert.ToDateTime(obj["CreateDate"]) : DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                loginInfo = null;
            }
            return loginInfo;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    };
}


using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class UnparsedLog
    {
        public ObjectId _id { get; set; }
        public string Logger { get; set; }
        public string ApiUrl { get; set; }
        public string Content { get; set; }
        public string Error { get; set; }
        public DateTime CreateDate { get; set; }

        public UnparsedLog()
        {
            _id = ObjectId.Empty;
            Logger = "";
            ApiUrl = "";
            Content = "";
            Error = "";
            CreateDate = DateTime.UtcNow;
        }

        public static UnparsedLog FromJson(JObject obj)
        {
            UnparsedLog unparsedLog = null;
            if (obj != null)
            {
                try
                {
                    unparsedLog = new UnparsedLog();
                    unparsedLog._id = ObjectId.Parse(obj["_id"].Value<string>());
                    unparsedLog.Logger = obj.ContainsKey("Logger") ? obj["Logger"].Value<string>() : "";
                    unparsedLog.ApiUrl = obj.ContainsKey("ApiUrl") ? obj["ApiUrl"].Value<string>() : "";
                    unparsedLog.Content = obj.ContainsKey("Content") ? obj["Content"].Value<string>() : "";
                    unparsedLog.Error = obj.ContainsKey("Error") ? obj["Error"].Value<string>() : "";
                    unparsedLog.CreateDate = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    unparsedLog = null;
                }
            }
            return unparsedLog;
        }
    }

    public class UserProfile : UnparsedLog
    {
        public string Name { get; set; }
        public string PhoneNo { get; set; }
        public string Address { get; set; }
        public DateTime Birthday { get; set; }

        public UserProfile() : base()
        {
            Name = "";
            PhoneNo = "";
            Address = "";
            Birthday = Utils.zeroDateTime();
        }

        public static new UserProfile FromJson(JObject obj)
        {
            UserProfile userProfile = null;
            if (obj != null)
            {
                try
                {
                    userProfile = new UserProfile();
                    userProfile._id = ObjectId.Parse(obj["_id"].Value<string>());
                    userProfile.Logger = obj.ContainsKey("Logger") ? obj["Logger"].Value<string>() : "";
                    userProfile.ApiUrl = obj.ContainsKey("ApiUrl") ? obj["ApiUrl"].Value<string>() : "";
                    userProfile.Content = obj.ContainsKey("Content") ? obj["Content"].Value<string>() : "";
                    userProfile.Error = obj.ContainsKey("Error") ? obj["Error"].Value<string>() : "";
                    userProfile.CreateDate = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;

                    userProfile.Name = obj.ContainsKey("Name") ? obj["Name"].Value<string>() : "";
                    userProfile.PhoneNo = obj.ContainsKey("PhoneNo") ? obj["PhoneNo"].Value<string>() : "";
                    userProfile.Address = obj.ContainsKey("Address") ? obj["Address"].Value<string>() : "";
                    userProfile.Birthday = obj.ContainsKey("Birthday") ? Convert.ToDateTime(obj["Birthday"]) : Utils.zeroDateTime();
                }
                catch (Exception ex)
                {
                    userProfile = null;
                }
            }
            return userProfile;
        }
    }
}

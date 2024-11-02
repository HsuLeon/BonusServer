using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class UniPlayInfo
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public string UrlLogo { get; set; }
        public string Desc { get; set; }
        public int ProfitPayRatio { get; set; }
        public float ExchangeRatioToNTD { get; set; }
        public bool Appliable { get; set; }
        public bool Acceptable { get; set; }
        DateTime mCreateDate;
        public DateTime CreateDate
        {
            get { return mCreateDate.ToLocalTime(); }
            set { mCreateDate = value.ToUniversalTime(); }
        }
        public bool Validate { get; set; }

        public UniPlayInfo()
        {
            this.Name = "";
            this.Domain = "";
            this.UrlLogo = "";
            this.Desc = "";
            this.ProfitPayRatio = 100;
            this.ExchangeRatioToNTD = 1.0f;
            this.Appliable = false;
            this.Acceptable = false;
            this.CreateDate = DateTime.UtcNow;
            this.Validate = true;
        }

        public static UniPlayInfo FromJson(JObject obj)
        {
            UniPlayInfo uniPlayInfo = null;
            if (obj.ContainsKey("Name") &&
                obj.ContainsKey("Domain") &&
                obj.ContainsKey("UrlLogo") &&
                obj.ContainsKey("Desc"))
            {
                uniPlayInfo = new UniPlayInfo();
                uniPlayInfo.Name = obj["Name"].Value<string>();
                uniPlayInfo.Domain = obj["Domain"].Value<string>();
                uniPlayInfo.UrlLogo = obj["UrlLogo"].Value<string>();
                uniPlayInfo.Desc = obj["Desc"].Value<string>();
                uniPlayInfo.ProfitPayRatio = obj["ProfitPayRatio"].Value<int>();
                uniPlayInfo.ExchangeRatioToNTD = obj["ExchangeRatioToNTD"].Value<float>();
                uniPlayInfo.Appliable = obj.ContainsKey("Appliable") ? obj["Appliable"].Value<bool>() : false;
                uniPlayInfo.Acceptable = obj.ContainsKey("Acceptable") ? obj["Acceptable"].Value<bool>() : false;
                uniPlayInfo.CreateDate = obj.ContainsKey("CreateDate") ? Convert.ToDateTime(obj["CreateDate"]) : Utils.zeroDateTime();
                uniPlayInfo.Validate = obj.ContainsKey("Validate") ? obj["Validate"].Value<bool>() : false;
            }
            return uniPlayInfo;
        }
    }
}

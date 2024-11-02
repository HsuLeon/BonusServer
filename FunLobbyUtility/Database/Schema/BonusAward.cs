using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class BonusAward
    {
        public ObjectId _id { get; set; }

        public string WinType { get; set; }
        public float TotalBet { get; set; }
        public float ScoreInterval { get; set; }
        public string AwardInfo { get; set; }
        public string ApiTransferPoints { get; set; }
        public string ApiTransferResponse { get; set; }
        DateTime mCreateTime;
        public DateTime CreateTime
        {
            get { return mCreateTime.ToLocalTime(); }
            set { mCreateTime = value.ToUniversalTime(); }
        }

        public BonusAward()
        {
            this.WinType = "";
            this.TotalBet = 0;
            this.ScoreInterval = 0;
            this.AwardInfo = "";
            this.ApiTransferPoints = "";
            this.ApiTransferResponse = "";
            this.CreateTime = DateTime.UtcNow;
        }

        public static BonusAward? FromJson(JObject obj)
        {
            BonusAward? bonusAward = null;
            if (obj != null)
            {
                try
                {
                    bonusAward = new BonusAward();
                    bonusAward._id = ObjectId.Parse(obj["_id"].Value<string>());
                    bonusAward.WinType = obj.ContainsKey("WinType") ? obj["WinType"].Value<string>() : "";
                    bonusAward.TotalBet = obj.ContainsKey("TotalBet") ? obj["TotalBet"].Value<float>() : 0;
                    bonusAward.ScoreInterval = obj.ContainsKey("ScoreInterval") ? obj["ScoreInterval"].Value<float>() : 0;
                    bonusAward.AwardInfo = obj.ContainsKey("AwardInfo") ? obj["AwardInfo"].Value<string>() : "";
                    bonusAward.ApiTransferPoints = obj.ContainsKey("ApiTransferPoints") ? obj["ApiTransferPoints"].Value<string>() : "";
                    bonusAward.ApiTransferResponse = obj.ContainsKey("ApiTransferResponse") ? obj["ApiTransferResponse"].Value<string>() : "";
                    bonusAward.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    bonusAward = null;
                }
            }
            return bonusAward;
        }
    }
}

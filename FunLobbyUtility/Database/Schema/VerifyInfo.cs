
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class VerifyInfo
    {
        public ObjectId _id { get; set; }
        public string Account { get; set; }
        public string PhoneNo { get; set; }
        public string Code { get; set; }
        public int OneDayUpdateCnt { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime CreateTime { get; set; }

        public VerifyInfo()
        {
            _id = ObjectId.Empty;
            Account = "";
            PhoneNo = "";
            Code = "";
            OneDayUpdateCnt = 0;
            UpdateTime = DateTime.UtcNow;
            CreateTime = DateTime.UtcNow;
        }

        public static VerifyInfo? FromJson(JObject obj)
        {
            VerifyInfo? verifyInfo = null;
            if (obj != null)
            {
                try
                {
                    verifyInfo = new VerifyInfo();
                    verifyInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                    verifyInfo.Account = obj.ContainsKey("Account") ? obj["Account"].Value<string>() : "";
                    verifyInfo.PhoneNo = obj.ContainsKey("PhoneNo") ? obj["PhoneNo"].Value<string>() : "";
                    verifyInfo.Code = obj.ContainsKey("Code") ? obj["Code"].Value<string>() : "";
                    verifyInfo.OneDayUpdateCnt = obj.ContainsKey("OneDayUpdateCnt") ? obj["OneDayUpdateCnt"].Value<int>() : 0;
                    verifyInfo.UpdateTime = obj.ContainsKey("UpdateTime") ? Convert.ToDateTime(obj["UpdateTime"]) : DateTime.UtcNow;
                    verifyInfo.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    verifyInfo = null;
                }
            }
            return verifyInfo;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }

        static public string GenerateCode()
        {
            Random random = new Random();
            string strCode = "";
            List<string> numList = new List<string>();
            numList.Add("0"); numList.Add("1"); numList.Add("2"); numList.Add("3"); numList.Add("4");
            numList.Add("5"); numList.Add("6"); numList.Add("7"); numList.Add("8"); numList.Add("9");
            for (int i = 0; i < 6; i++)
            {
                int index = random.Next(0, 5);
                strCode += numList[index];
            }
            return strCode;
        }
    }
}


using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class AgentInfo
    {
        public ObjectId _id { get; set; }
        public string Account { get; protected set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public bool MultiLogin { get; set; }
        public ulong FinalizeTimeOut { get; set; }
        public ulong UserNoHeartbeatAckKickout { get; set; }
        public int MaxDailyReceivedGolds { get; set; }
        public int MaxDailyGuoZhaoCnt { get; set; }
        public int PeriodMaxGuoZhaoCnt { get; set; }
        public string GuoZhaoPeriods { get; set; }
        public bool CanExchangeTrialToFormal { get; set; }
        public bool CanExchangeFormalToTrial { get; set; }
        public int ExchangeRatio { get; set; }
        public int ExchangeMinLeft { get; set; }
        public int ExchangeReceiverRate { get; set; }
        public int GiftTaxRate { get; set; }
        public int DailyAward { get; set; }
        public int MaxDailyAward { get; set; }
        public int AwardType { get; set; }
        public int AwardDuration { get; set; }
        public int StopAwardAt { get; set; }
        public int MaxPlayerScore { get; set; }
        public bool VerifyUserInfo { get; set; }
        public bool MachineStopGuoZhaoTEMP { get; set; }
        public int WainZhuMinCnt { get; set; }
        public int WainZhuMaxCnt { get; set; }
        public int InScoreBonusBase { get; set; }
        public int InScoreBonusFormal { get; set; }
        public int InScoreBonusTrial { get; set; }
        public int MinExchangeBetReward { get; set; }
        public int MaxExchangeBetReward { get; set; }
        public string SMSInfo { get; set; }
        public string SetAfterVerifyUser { get; set; }
        public bool EnableUserInfoSortIndex { get; set; }
        public int FrontEndVersion { get; set; }
        public bool DefUserAwardEnable { get; set; }
        public bool ShowBetReward { get; set; }
        public bool ShowPlayerLevel { get; set; }
        public string UserLevels { get; set; }
        public float ScoreRatioToNTD { get; set; }
        public bool Validate { get; set; }
        public DateTime CreateDate { get; set; }

        public AgentInfo(string account, string password)
        {
            JObject objSMSInfo = new JObject();
            objSMSInfo["UID"] = "";
            objSMSInfo["PWD"] = "";

            JObject objSetAfterVerifyUser = new JObject();
            objSetAfterVerifyUser["Verified"] = true;
            objSetAfterVerifyUser["GuoZhaoEnabled"] = true;

            JObject objUserLevels = new JObject();
            JObject objLevel0 = new JObject();
            objLevel0["level"] = "銀卡";
            objLevel0["maxPay"] = 2000;
            objUserLevels[UserInfo.Permission_VIP0.ToString()] = objLevel0;
            JObject objLevel1 = new JObject();
            objLevel1["level"] = "金卡";
            objLevel1["maxPay"] = 5000;
            objUserLevels[UserInfo.Permission_VIP1.ToString()] = objLevel1;
            JObject objLevel2 = new JObject();
            objLevel2["level"] = "鉑金卡";
            objLevel2["maxPay"] = 10000;
            objUserLevels[UserInfo.Permission_VIP2.ToString()] = objLevel2;

            this.Account = account;
            this.Password = password;
            this.Domain = "";
            this.ScoreRatioToNTD = 1.0f;
            this.SMSInfo = JsonConvert.SerializeObject(objSMSInfo);
            this.SetAfterVerifyUser = JsonConvert.SerializeObject(objSetAfterVerifyUser);
            this.UserLevels = JsonConvert.SerializeObject(objUserLevels);
            this.Validate = true;
            this.CreateDate = DateTime.UtcNow;
        }

        public static AgentInfo FromJson(JObject obj)
        {
            AgentInfo agentInfo = null;
            if (obj != null)
            {
                try
                {
                    agentInfo = new AgentInfo("WinLobby", "WinLobby");
                    agentInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                    if (obj.ContainsKey("Account")) agentInfo.Account = obj["Account"].Value<string>();
                    if (obj.ContainsKey("Password")) agentInfo.Password = obj["Password"].Value<string>();
                    if (obj.ContainsKey("Domain")) agentInfo.Domain = obj["Domain"].Value<string>();
                    if (obj.ContainsKey("ScoreRatioToNTD")) agentInfo.ScoreRatioToNTD = obj["ScoreRatioToNTD"].Value<float>();
                    if (obj.ContainsKey("MultiLogin")) agentInfo.MultiLogin = obj["MultiLogin"].Value<bool>();
                    if (obj.ContainsKey("FinalizeTimeOut")) agentInfo.FinalizeTimeOut = obj["FinalizeTimeOut"].Value<ulong>();
                    if (obj.ContainsKey("UserNoHeartbeatAckKickout")) agentInfo.UserNoHeartbeatAckKickout = obj["UserNoHeartbeatAckKickout"].Value<ulong>();
                    if (obj.ContainsKey("MaxDailyReceivedGolds")) agentInfo.MaxDailyReceivedGolds = obj["MaxDailyReceivedGolds"].Value<int>();
                    if (obj.ContainsKey("PeriodMaxGuoZhaoCnt"))
                    {
                        agentInfo.PeriodMaxGuoZhaoCnt = obj["PeriodMaxGuoZhaoCnt"].Value<int>();
                        agentInfo.MaxDailyGuoZhaoCnt = agentInfo.PeriodMaxGuoZhaoCnt;
                    }
                    if (obj.ContainsKey("GuoZhaoPeriods")) agentInfo.GuoZhaoPeriods = obj["GuoZhaoPeriods"].Value<string>();
                    if (obj.ContainsKey("CanExchangeTrialToFormal")) agentInfo.CanExchangeTrialToFormal = obj["CanExchangeTrialToFormal"].Value<bool>();
                    if (obj.ContainsKey("CanExchangeFormalToTrial")) agentInfo.CanExchangeFormalToTrial = obj["CanExchangeFormalToTrial"].Value<bool>();
                    if (obj.ContainsKey("ExchangeRatio")) agentInfo.ExchangeRatio = obj["ExchangeRatio"].Value<int>();
                    if (obj.ContainsKey("ExchangeMinLeft")) agentInfo.ExchangeMinLeft = obj["ExchangeMinLeft"].Value<int>();
                    if (obj.ContainsKey("ExchangeReceiverRate")) agentInfo.ExchangeReceiverRate = obj["ExchangeReceiverRate"].Value<int>();
                    if (obj.ContainsKey("GiftTaxRate")) agentInfo.GiftTaxRate = obj["GiftTaxRate"].Value<int>();
                    if (obj.ContainsKey("DailyAward")) agentInfo.DailyAward = obj["DailyAward"].Value<int>();
                    if (obj.ContainsKey("MaxDailyAward")) agentInfo.MaxDailyAward = obj["MaxDailyAward"].Value<int>();
                    if (obj.ContainsKey("AwardType")) agentInfo.AwardType = obj["AwardType"].Value<int>();
                    if (obj.ContainsKey("AwardDuration")) agentInfo.AwardDuration = obj["AwardDuration"].Value<int>();
                    if (obj.ContainsKey("StopAwardAt")) agentInfo.StopAwardAt = obj["StopAwardAt"].Value<int>();
                    if (obj.ContainsKey("MaxPlayerScore")) agentInfo.MaxPlayerScore = obj["MaxPlayerScore"].Value<int>();
                    if (obj.ContainsKey("VerifyUserInfo")) agentInfo.VerifyUserInfo = obj["VerifyUserInfo"].Value<bool>();
                    if (obj.ContainsKey("MachineStopGuoZhaoTEMP")) agentInfo.MachineStopGuoZhaoTEMP = obj["MachineStopGuoZhaoTEMP"].Value<bool>();
                    if (obj.ContainsKey("WainZhuMinCnt")) agentInfo.WainZhuMinCnt = obj["WainZhuMinCnt"].Value<int>();
                    if (obj.ContainsKey("WainZhuMaxCnt")) agentInfo.WainZhuMaxCnt = obj["WainZhuMaxCnt"].Value<int>();
                    if (obj.ContainsKey("InScoreBonusBase")) agentInfo.InScoreBonusBase = obj["InScoreBonusBase"].Value<int>();
                    if (obj.ContainsKey("InScoreBonusFormal")) agentInfo.InScoreBonusFormal = obj["InScoreBonusFormal"].Value<int>();
                    if (obj.ContainsKey("InScoreBonusTrial")) agentInfo.InScoreBonusTrial = obj["InScoreBonusTrial"].Value<int>();
                    if (obj.ContainsKey("MinExchangeBetReward")) agentInfo.MinExchangeBetReward = obj["MinExchangeBetReward"].Value<int>();
                    if (obj.ContainsKey("MaxExchangeBetReward")) agentInfo.MaxExchangeBetReward = obj["MaxExchangeBetReward"].Value<int>();
                    if (obj.ContainsKey("SMSInfo")) agentInfo.SMSInfo = obj["SMSInfo"].Value<string>();
                    if (obj.ContainsKey("SetAfterVerifyUser")) agentInfo.SetAfterVerifyUser = obj["SetAfterVerifyUser"].Value<string>();
                    if (obj.ContainsKey("EnableUserInfoSortIndex")) agentInfo.EnableUserInfoSortIndex = obj["EnableUserInfoSortIndex"].Value<bool>();
                    if (obj.ContainsKey("FrontEndVersion")) agentInfo.FrontEndVersion = obj["FrontEndVersion"].Value<int>();
                    if (obj.ContainsKey("ShowBetReward")) agentInfo.ShowBetReward = obj["ShowBetReward"].Value<bool>();
                    if (obj.ContainsKey("ShowPlayerLevel")) agentInfo.ShowPlayerLevel = obj["ShowPlayerLevel"].Value<bool>();
                    if (obj.ContainsKey("UserLevels")) agentInfo.UserLevels = obj["UserLevels"].Value<string>();
                    if (obj.ContainsKey("ScoreRatioToNTD")) agentInfo.ScoreRatioToNTD = obj["ScoreRatioToNTD"].Value<float>();
                    if (obj.ContainsKey("Validate")) agentInfo.Validate = obj["Validate"].Value<bool>();
                    if (obj.ContainsKey("CreateDate")) agentInfo.CreateDate = Convert.ToDateTime(obj["CreateDate"]);
                    if (obj.ContainsKey("DefUserAwardEnable")) agentInfo.DefUserAwardEnable = obj["DefUserAwardEnable"].Value<bool>();
                    
                }
                catch (Exception ex)
                {
                    agentInfo = null;
                }
            }
            return agentInfo;
        }

        public MainSettings GetMainSettings()
        {
            MainSettings mainSettings = new MainSettings();

            mainSettings.Domain = this.Domain;
            mainSettings.ScoreRatioToNTD = this.ScoreRatioToNTD;
            mainSettings.MultiLogin = this.MultiLogin;
            mainSettings.UserNoHeartbeatAckKickout = this.UserNoHeartbeatAckKickout;
            mainSettings.FinalizeTimeOut = this.FinalizeTimeOut;
            mainSettings.MaxDailyReceivedGolds = this.MaxDailyReceivedGolds;
            mainSettings.PeriodMaxGuoZhaoCnt = this.PeriodMaxGuoZhaoCnt;
            mainSettings.GuoZhaoPeriods = JsonConvert.DeserializeObject<List<MainSettings.GuoZhaoPeriod>>(this.GuoZhaoPeriods);
            mainSettings.InScoreBonusBase = this.InScoreBonusBase;
            mainSettings.InScoreBonusFormal = this.InScoreBonusFormal;
            mainSettings.InScoreBonusTrial = this.InScoreBonusTrial;
            mainSettings.CanExchangeTrialToFormal = this.CanExchangeTrialToFormal;
            mainSettings.CanExchangeFormalToTrial = this.CanExchangeFormalToTrial;
            mainSettings.ExchangeRatio = this.ExchangeRatio;
            mainSettings.ExchangeMinLeft = this.ExchangeMinLeft;
            mainSettings.ExchangeReceiverRate = this.ExchangeReceiverRate;
            mainSettings.GiftTaxRate = this.GiftTaxRate;
            mainSettings.DailyAward = this.DailyAward;
            mainSettings.AwardType = (MainSettings.Award_Type)this.AwardType;
            mainSettings.MaxDailyAward = this.MaxDailyAward;
            mainSettings.AwardDuration = this.AwardDuration;
            mainSettings.StopAwardAt = this.StopAwardAt;
            mainSettings.MaxPlayerScore = this.MaxPlayerScore;
            mainSettings.VerifyUserInfo = this.VerifyUserInfo;
            mainSettings.WainZhuMinCnt = this.WainZhuMinCnt;
            mainSettings.WainZhuMaxCnt = this.WainZhuMaxCnt;
            mainSettings.MachineStopGuoZhaoTEMP = this.MachineStopGuoZhaoTEMP;
            mainSettings.MinExchangeBetReward = this.MinExchangeBetReward;
            mainSettings.MaxExchangeBetReward = this.MaxExchangeBetReward;
            mainSettings.EnableUserInfoSortIndex = this.EnableUserInfoSortIndex;
            mainSettings.ShowBetReward = this.ShowBetReward;
            mainSettings.ShowPlayerLevel = this.ShowPlayerLevel;
            mainSettings.UserLevels = this.UserLevels.Length > 0 ? JsonConvert.DeserializeObject<Dictionary<string, MainSettings.UserLevel>>(this.UserLevels) : new Dictionary<string, MainSettings.UserLevel>();

            return mainSettings;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

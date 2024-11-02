
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class UserInfo
    {
        public const int Permission_Unknown = -1;
        public const int Permission_VIP0 = 0;
        public const int Permission_VIP1 = 1;
        public const int Permission_VIP2 = 2;
        public const int Permission_Engineer = 100;

        public class UserGuoZhaoInfo
        {
            public string Lobby { get; set; }
            public string Machine { get; set; }
            public int DeviceId { get; set; }
            public int MachineId { get; set; }
            public int In { get; set; }
            public int Out { get; set; }
            public int Unlock { get; set; }
            public SCORE_TYPE ScoreType { get; set; }
            public DateTime LockTime { get; set; }

            public UserGuoZhaoInfo()
            {
                this.Lobby = null;
                this.Machine = null;
                this.DeviceId = -1;
                this.MachineId = -1;
                this.In = 0;
                this.Out = 0;
                this.Unlock = 0;
                this.ScoreType = SCORE_TYPE.None;
                this.LockTime = Utils.zeroDateTime();
            }
        }

        public class CBindedMachine
        {
            public string Lobby { get; set; }
            public int DeviceId { get; set; }
            public int MachineId { get; set; }
            public string MachineName { get; set; }
            public DateTime BeginDate { get; set; }

            public CBindedMachine()
            {
                this.Lobby = null;
                this.DeviceId = -1;
                this.MachineId = -1;
                this.MachineName = "";
                this.BeginDate = Utils.zeroDateTime();
            }
        }

        public class CUniPlayInfo
        {
            public string Domain { get; set; }
            public string Account { get; set; }
            public string Password { get; set; }
            public DateTime BeginDate { get; set; }

            public CUniPlayInfo()
            {
                this.Domain = null;
                this.Account = null;
                this.Password = null;
                this.BeginDate = Utils.zeroDateTime();
            }

            static public CUniPlayInfo FromJson(JObject obj)
            {
                CUniPlayInfo uniPlayInfo = null;
                if (obj != null &&
                    obj.ContainsKey("Domain") &&
                    obj.ContainsKey("Account") &&
                    obj.ContainsKey("Password"))
                {
                    string strBeginDate = obj["BeginDate"].Value<string>();

                    uniPlayInfo = new CUniPlayInfo();
                    uniPlayInfo.Domain = obj["Domain"].Value<string>();
                    uniPlayInfo.Account = obj["Account"].Value<string>();
                    uniPlayInfo.Password = obj["Password"].Value<string>();
                    uniPlayInfo.BeginDate = DateTime.Parse(strBeginDate);
                }
                return uniPlayInfo;
            }
        }


        public class CAvatarInfo
        {
            public string SrcUrl { get; set; }
            public string SrcAccount { get; set; }
            public string SrcPassword { get; set; }
            public int SrcPermission { get; set; }
            public float ExchangeRatio { get; set; }
            public int ProfitPayRatio { get; set; }

            public CAvatarInfo()
            {
                this.SrcUrl = null;
                this.SrcAccount = null;
                this.SrcPassword = null;
                this.SrcPermission = UserInfo.Permission_Unknown;
                this.ExchangeRatio = 1.0f;
                this.ProfitPayRatio = 100;
            }

            static public CAvatarInfo FromJson(JObject obj)
            {
                CAvatarInfo avatarInfo = null;
                if (obj != null &&
                    obj.ContainsKey("SrcUrl") &&
                    obj.ContainsKey("SrcAccount") &&
                    obj.ContainsKey("SrcPassword") &&
                    obj.ContainsKey("ExchangeRatio") &&
                    obj.ContainsKey("ProfitPayRatio"))
                {
                    avatarInfo = new CAvatarInfo();
                    avatarInfo.SrcUrl = obj["SrcUrl"].Value<string>();
                    avatarInfo.SrcAccount = obj["SrcAccount"].Value<string>();
                    avatarInfo.SrcPassword = obj["SrcPassword"].Value<string>();
                    avatarInfo.SrcPermission = obj.ContainsKey("Permission") ? obj["Permission"].Value<int>() : Permission_Unknown;
                    avatarInfo.ExchangeRatio = obj["ExchangeRatio"].Value<float>();
                    avatarInfo.ProfitPayRatio = obj["ProfitPayRatio"].Value<int>();
                }
                return avatarInfo;
            }
        }

        public ObjectId _id { get; set; }
        public string AgentId { get; set; }
        public string GM { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public int Permission { get; set; }
        public string NickName { get; set; }
        DateTime mBirthday;
        public DateTime Birthday
        {
            get { return mBirthday.ToLocalTime(); }
            set { mBirthday = value.ToUniversalTime(); }
        }
        public Int64 InCoins { get; set; }
        public Int64 OutCoins { get; set; }
        public Int64 InTrialCoins { get; set; }
        public Int64 OutTrialCoins { get; set; }
        public Int64 InGuoZhaoCoins { get; set; }
        public Int64 OutGuoZhaoCoins { get; set; }
        public int LoginErrCnt { get; set; }
        public string PhoneNo { get; set; }
        public string Note { get; set; }
        DateTime mCreateDate;
        public DateTime CreateDate
        {
            get { return mCreateDate.ToLocalTime(); }
            set { mCreateDate = value.ToUniversalTime(); }
        }
        DateTime mLastUpdateDate;
        public DateTime LastUpdateDate
        {
            get { return mLastUpdateDate.ToLocalTime(); }
            set { mLastUpdateDate = value.ToUniversalTime(); }
        }
        public bool GiftEnabled { get; set; }
        public bool UserAwardEnable { get; set; }
        public bool Validate { get; set; }
        public bool Verified { get; set; }
        public bool GuoZhaoEnabled { get; set; }
        public bool Blocked { get; set; }
        public string UniPlayInfo { get; set; }
        public string AvatarInfo { get; set; }
        public string Token { get; set; }
        DateTime mTokenTime;
        public DateTime TokenTime
        {
            get { return mTokenTime.ToLocalTime(); }
            set { mTokenTime = value.ToUniversalTime(); }
        }
        DateTime mLoginAwardDate;
        public DateTime LoginAwardDate
        {
            get { return mLoginAwardDate.ToLocalTime(); }
            set { mLoginAwardDate = value.ToUniversalTime(); }
        }
        public int PaiedAward { get; set; }
        DateTime mDailyReceivedGoldsDate;
        public DateTime DailyReceivedGoldsDate
        {
            get { return mDailyReceivedGoldsDate.ToLocalTime(); }
            set { mDailyReceivedGoldsDate = value.ToUniversalTime(); }
        }
        public int DailyReceivedGolds { get; set; }
        public int MaxDailyReceivedGolds { get; set; }
        public int GuoZhaoCnt { get; set; }
        public string GuoZhaoInfo { get; set; }
        DateTime mLastGuoZhaoDate;
        public DateTime LastGuoZhaoDate
        {
            get { return mLastGuoZhaoDate.ToLocalTime(); }
            set { mLastGuoZhaoDate = value.ToUniversalTime(); }
        }
        public string BindedMachines { get; set; }
        public double BetReward { get; set; }
        public List<WainZhu> WainZhuRecords { get; set; }
        public int SortIndex { get; set; }

        public UserInfo()
        {
            _id = ObjectId.Empty;
            AgentId = "";
            GM = "";
            Account = "";
            Password = "";
            Permission = Permission_VIP0;
            NickName = "";
            Birthday = DateTime.UtcNow;
            InCoins = 0;
            OutCoins = 0;
            InTrialCoins = 0;
            OutTrialCoins = 0;
            InGuoZhaoCoins = 0;
            OutGuoZhaoCoins = 0;
            LoginErrCnt = 0;
            PhoneNo = "";
            Note = "";
            CreateDate = DateTime.UtcNow;
            LastUpdateDate = DateTime.UtcNow;
            GiftEnabled = true;
            UserAwardEnable = false;
            Validate = true;
            Verified = false;
            GuoZhaoEnabled = false;
            Blocked = false;
            UniPlayInfo = JsonConvert.SerializeObject(new JObject());
            AvatarInfo = JsonConvert.SerializeObject(new JObject());
            Token = "";
            TokenTime = Utils.zeroDateTime();
            LoginAwardDate = Utils.zeroDateTime();
            PaiedAward = 0;
            DailyReceivedGoldsDate = Utils.zeroDateTime();
            DailyReceivedGolds = 0;
            MaxDailyReceivedGolds = 0;
            GuoZhaoCnt = 0;
            LastGuoZhaoDate = Utils.zeroDateTime();
            GuoZhaoInfo = "";
            BindedMachines = "";
            BetReward = 0.0f;
            WainZhuRecords = new List<WainZhu>();
            SortIndex = 0;
        }

        public static UserInfo FromJson(JObject obj)
        {
            UserInfo userInfo = null;
            if (obj != null)
            {
                try
                {
                    userInfo = new UserInfo();
                    userInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                    userInfo.AgentId = obj.ContainsKey("AgentId") ? obj["AgentId"].Value<String>() : "NaN";
                    userInfo.GM = obj.ContainsKey("GM") ? obj["GM"].Value<String>() : "NaN";
                    userInfo.Account = obj.ContainsKey("Account") ? obj["Account"].Value<String>() : "NaN";
                    userInfo.Password = obj.ContainsKey("Password") ? obj["Password"].Value<String>() : "NaN";
                    userInfo.Permission = obj.ContainsKey("Permission") ? obj["Permission"].Value<int>() : Permission_VIP0;
                    userInfo.NickName = obj.ContainsKey("NickName") ? obj["NickName"].Value<String>() : "NaN";
                    userInfo.Birthday = obj.ContainsKey("Birthday") ? Convert.ToDateTime(obj["Birthday"]) : Utils.zeroDateTime();
                    userInfo.InCoins = obj.ContainsKey("InCoins") ? obj["InCoins"].Value<Int64>() : 0;
                    userInfo.OutCoins = obj.ContainsKey("OutCoins") ? obj["OutCoins"].Value<Int64>() : 0;
                    userInfo.InTrialCoins = obj.ContainsKey("InTrialCoins") ? obj["InTrialCoins"].Value<Int64>() : 0;
                    userInfo.OutTrialCoins = obj.ContainsKey("OutTrialCoins") ? obj["OutTrialCoins"].Value<Int64>() : 0;
                    userInfo.InGuoZhaoCoins = obj.ContainsKey("InGuoZhaoCoins") ? obj["InGuoZhaoCoins"].Value<Int64>() : 0;
                    userInfo.OutGuoZhaoCoins = obj.ContainsKey("OutGuoZhaoCoins") ? obj["OutGuoZhaoCoins"].Value<Int64>() : 0;
                    userInfo.LoginErrCnt = obj.ContainsKey("LoginErrCnt") ? obj["LoginErrCnt"].Value<int>() : 0;
                    userInfo.PhoneNo = obj.ContainsKey("PhoneNo") ? obj["PhoneNo"].Value<string>() : "NaN";
                    userInfo.Note = obj.ContainsKey("Note") ? obj["Note"].Value<string>() : "NaN";
                    userInfo.CreateDate = obj.ContainsKey("CreateDate") ? Convert.ToDateTime(obj["CreateDate"]) : Utils.zeroDateTime();
                    userInfo.LastUpdateDate = obj.ContainsKey("LastUpdateDate") ? Convert.ToDateTime(obj["LastUpdateDate"]) : Utils.zeroDateTime();
                    userInfo.GiftEnabled = obj.ContainsKey("GiftEnabled") ? obj["GiftEnabled"].Value<bool>() : true;
                    userInfo.UserAwardEnable = obj.ContainsKey("UserAwardEnable") ? obj["UserAwardEnable"].Value<bool>() : true;
                    userInfo.Validate = obj.ContainsKey("Validate") ? obj["Validate"].Value<bool>() : false;
                    userInfo.Verified = obj.ContainsKey("Verified") ? obj["Verified"].Value<bool>() : false;
                    userInfo.GuoZhaoEnabled = obj.ContainsKey("GuoZhaoEnabled") ? obj["GuoZhaoEnabled"].Value<bool>() : false;
                    userInfo.Blocked = obj.ContainsKey("Blocked") ? obj["Blocked"].Value<bool>() : false;
                    userInfo.UniPlayInfo = obj.ContainsKey("UniPlayInfo") ? obj["UniPlayInfo"].Value<string>() : JsonConvert.SerializeObject(new JObject());
                    userInfo.AvatarInfo = obj.ContainsKey("AvatarInfo") ? obj["AvatarInfo"].Value<string>() : JsonConvert.SerializeObject(new JObject());
                    userInfo.Token = obj.ContainsKey("Token") ? obj["Token"].Value<string>() : "";
                    userInfo.TokenTime = obj.ContainsKey("TokenTime") ? Convert.ToDateTime(obj["TokenTime"]) : Utils.zeroDateTime();
                    userInfo.LoginAwardDate = obj.ContainsKey("LoginAwardDate") ? Convert.ToDateTime(obj["LoginAwardDate"]) : Utils.zeroDateTime();
                    userInfo.PaiedAward = obj.ContainsKey("PaiedAward") ? obj["PaiedAward"].Value<int>() : 0;
                    userInfo.DailyReceivedGoldsDate = obj.ContainsKey("DailyReceivedGoldsDate") ? Convert.ToDateTime(obj["DailyReceivedGoldsDate"]) : Utils.zeroDateTime();
                    userInfo.DailyReceivedGolds = obj.ContainsKey("DailyReceivedGolds") ? obj["DailyReceivedGolds"].Value<int>() : 0;
                    userInfo.MaxDailyReceivedGolds = obj.ContainsKey("MaxDailyReceivedGolds") ? obj["MaxDailyReceivedGolds"].Value<int>() : 0;
                    userInfo.GuoZhaoCnt = obj.ContainsKey("GuoZhaoCnt") ? obj["GuoZhaoCnt"].Value<int>() : 0;
                    userInfo.LastGuoZhaoDate = obj.ContainsKey("LastGuoZhaoDate") ? Convert.ToDateTime(obj["LastGuoZhaoDate"]) : Utils.zeroDateTime();
                    // make sure to convert "ArduinoId" to "DeviceId"
                    string strGuoZhaoInfo = obj.ContainsKey("GuoZhaoInfo") ? obj["GuoZhaoInfo"].Value<string>() : "";
                    if (strGuoZhaoInfo.Length > 0)
                    {
                        JObject objGuoZhaoInfo = JObject.Parse(strGuoZhaoInfo);
                        if (objGuoZhaoInfo.ContainsKey("DeviceId") == false &&
                            objGuoZhaoInfo.ContainsKey("ArduinoId"))
                        {
                            objGuoZhaoInfo["DeviceId"] = objGuoZhaoInfo["ArduinoId"];
                            objGuoZhaoInfo.Remove("ArduinoId");
                            objGuoZhaoInfo["DeviceId"] = objGuoZhaoInfo["ArduinoId"];
                            strGuoZhaoInfo = JsonConvert.SerializeObject(objGuoZhaoInfo);
                        }
                    }
                    userInfo.GuoZhaoInfo = strGuoZhaoInfo;
                    userInfo.BindedMachines = obj.ContainsKey("BindedMachines") ? obj["BindedMachines"].Value<string>() : "";
                    userInfo.BetReward = obj.ContainsKey("BetReward") ? Math.Round(obj["BetReward"].Value<float>(), 2) : 0.0f;
                    if (obj.ContainsKey("WainZhuRecords"))
                    {
                        JArray wainZhus = obj["WainZhuRecords"] as JArray;
                        if (wainZhus != null)
                        {
                            for (int i = 0; i < wainZhus.Count; i++)
                            {
                                JObject objWainZhu = wainZhus[i].Value<JObject>();
                                WainZhu wainZhu = WainZhu.FromJson(objWainZhu);
                                userInfo.WainZhuRecords.Add(wainZhu);
                            }
                        }
                    }
                    userInfo.SortIndex = obj.ContainsKey("SortIndex") ? obj["SortIndex"].Value<int>() : 0;
                }
                catch (Exception ex)
                {
                    userInfo = null;
                }
            }
            return userInfo;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }

        public UserGuoZhaoInfo GetGuoZhaoInfo()
        {
            UserGuoZhaoInfo userGuoZhaoInfo = null;
            try
            {
                if (this.GuoZhaoInfo.Length > 0)
                {
                    userGuoZhaoInfo = JsonConvert.DeserializeObject<UserGuoZhaoInfo>(this.GuoZhaoInfo);
                }
            }
            catch(Exception ex) {}
            return userGuoZhaoInfo;
        }

        public List<CBindedMachine> GetBindedMachines()
        {
            List<CBindedMachine> userMachineInfos;
            try
            {
                userMachineInfos = this.BindedMachines.Length > 0 ? JsonConvert.DeserializeObject<List<CBindedMachine>>(this.BindedMachines) : new List<CBindedMachine>();
            }
            catch(Exception ex)
            {
                userMachineInfos = new List<CBindedMachine>();
            }
            return userMachineInfos;
        }

        public CUniPlayInfo GetUniPlayInfo()
        {
            CUniPlayInfo uniPlayInfo = null;
            try
            {
                if (this.UniPlayInfo != null && this.UniPlayInfo.Length > 0)
                {
                    JObject obj = JObject.Parse(this.UniPlayInfo);
                    uniPlayInfo = CUniPlayInfo.FromJson(obj);
                }
            }
            catch (Exception ex)
            {
            }
            return uniPlayInfo;
        }

        public CAvatarInfo GetAvatarInfo()
        {
            CAvatarInfo avatarInfo = null;
            try
            {
                if (this.AvatarInfo != null && this.AvatarInfo.Length > 0)
                {
                    JObject obj = JObject.Parse(this.AvatarInfo);
                    avatarInfo = CAvatarInfo.FromJson(obj);
                }
            }
            catch(Exception ex)
            {
            }
            return avatarInfo;
        }
    }
}


using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class GMInfo
    {
        public class PERMISSION
        {
            static Dictionary<string, int> mPermission = new Dictionary<string, int>();

            public const int None = 0x0;
            //public const int Employee = 0x1; //1 => 0x1 << 0
            //public const int Leader = 0x4;   //4 => 0x1 << 2
            //public const int Manager = 0x20; //32 => 0x1 << 5
            //public const int Root = 0x80;    //128 => 0x1 << 7

            public const int CtrlManager = 0x1 << 8;
            public const int CtrlMachine = 0x1 << 9;
            public const int StopGuoZhao = 0x1 << 10;
            public const int CreateUserInfo = 0x1 << 11;
            public const int ModifyUserInfo = 0x1 << 12;
            public const int ViewUserActions = 0x1 << 13;
            public const int DeleteUserInfo = 0x1 << 14;
            public const int AddUserScore = 0x1 << 15;
            public const int SubUserScore = 0x1 << 16;
            public const int CreateGMInfo = 0x1 << 17;
            public const int ModifyGMInfo = 0x1 << 18;
            public const int ViewGMActions = 0x1 << 19;
            public const int DeleteGMInfo = 0x1 << 20;
            public const int AddGMScore = 0x1 << 21;
            public const int SubGMScore = 0x1 << 22;
            public const int InfiniteScore = 0x1 << 23;
            public const int MainSetting = 0x1 << 24;
            public const int ViewReport = 0x1 << 25;
            public const int RemoveData = 0x1 << 26;
            public const int Broadcast = 0x1 << 27;

            static public int ConvertPermission(int permission)
            {
                const int oldEmployee = 0x1;
                const int oldLeader = 0x4;
                const int oldManager = 0x20;
                const int oldRoot = 0x80;
                switch (permission)
                {
                    case oldRoot:
                        return PERMISSION.Root;
                    case oldManager:
                        return PERMISSION.Manager;
                    case oldLeader:
                        return PERMISSION.Leader;
                    case oldEmployee:
                        return PERMISSION.Employee;
                    default:
                        return permission;
                }
            }

            static public List<int> ToList(int permissions)
            {
                List<int> list = new List<int>();
                for (int i = 8; i < 32; i++)
                {
                    int permissionCode = 0x1 << i;
                    if ((permissions & permissionCode) != 0x0)
                    {
                        list.Add(permissionCode);
                    }
                }
                return list;
            }

            static public int Permission(string role)
            {
                int permission = PERMISSION.None;
                if (mPermission.ContainsKey(role)) permission = mPermission[role];
                return permission;
            }

            static public void RegisterPermission(string role, int permission)
            {
                if (mPermission.ContainsKey(role) == false)
                    mPermission[role] = PERMISSION.None;
                mPermission[role] |= permission;
            }

            static public void UnregisterPermission(string role, int permission)
            {
                if (mPermission.ContainsKey(role) == false)
                    mPermission[role] = PERMISSION.None;
                mPermission[role] &= ~permission;
            }

            static public int Root
            {
                get
                {
                    if (mPermission.ContainsKey("Root") == false)
                    {
                        RegisterPermission("Root", PERMISSION.CtrlManager);
                        RegisterPermission("Root", PERMISSION.CtrlMachine);
                        RegisterPermission("Root", PERMISSION.StopGuoZhao);

                        RegisterPermission("Root", PERMISSION.CreateUserInfo);
                        RegisterPermission("Root", PERMISSION.ModifyUserInfo);
                        RegisterPermission("Root", PERMISSION.ViewUserActions);
                        RegisterPermission("Root", PERMISSION.DeleteUserInfo);
                        RegisterPermission("Root", PERMISSION.AddUserScore);
                        RegisterPermission("Root", PERMISSION.SubUserScore);

                        RegisterPermission("Root", PERMISSION.CreateGMInfo);
                        RegisterPermission("Root", PERMISSION.ModifyGMInfo);
                        RegisterPermission("Root", PERMISSION.ViewGMActions);
                        RegisterPermission("Root", PERMISSION.DeleteGMInfo);
                        RegisterPermission("Root", PERMISSION.AddGMScore);
                        RegisterPermission("Root", PERMISSION.SubGMScore);

                        RegisterPermission("Root", PERMISSION.InfiniteScore);
                        RegisterPermission("Root", PERMISSION.MainSetting);
                        RegisterPermission("Root", PERMISSION.ViewReport);
                        RegisterPermission("Root", PERMISSION.RemoveData);
                    }

                    return mPermission["Root"];
                }
            }

            static public int Manager
            {
                get
                {
                    if (mPermission.ContainsKey("Manager") == false)
                    {
                        RegisterPermission("Manager", PERMISSION.CtrlManager);
                        RegisterPermission("Manager", PERMISSION.CtrlMachine);
                        RegisterPermission("Manager", PERMISSION.StopGuoZhao);

                        RegisterPermission("Manager", PERMISSION.CreateUserInfo);
                        RegisterPermission("Manager", PERMISSION.ModifyUserInfo);
                        RegisterPermission("Manager", PERMISSION.ViewUserActions);
                        RegisterPermission("Manager", PERMISSION.DeleteUserInfo);
                        RegisterPermission("Manager", PERMISSION.AddUserScore);
                        RegisterPermission("Manager", PERMISSION.SubUserScore);

                        RegisterPermission("Manager", PERMISSION.CreateGMInfo);
                        RegisterPermission("Manager", PERMISSION.ModifyGMInfo);
                        RegisterPermission("Manager", PERMISSION.ViewGMActions);
                        RegisterPermission("Manager", PERMISSION.DeleteGMInfo);
                        RegisterPermission("Manager", PERMISSION.AddGMScore);
                        RegisterPermission("Manager", PERMISSION.SubGMScore);

                        RegisterPermission("Manager", PERMISSION.MainSetting);
                        RegisterPermission("Manager", PERMISSION.ViewReport);
                        RegisterPermission("Manager", PERMISSION.RemoveData);
                    }
                    return mPermission["Manager"];
                }
            }

            static public int Leader
            {
                get
                {
                    if (mPermission.ContainsKey("Leader") == false)
                    {
                        RegisterPermission("Leader", PERMISSION.CtrlMachine);
                        RegisterPermission("Leader", PERMISSION.StopGuoZhao);

                        RegisterPermission("Leader", PERMISSION.CreateUserInfo);
                        RegisterPermission("Leader", PERMISSION.ModifyUserInfo);
                        RegisterPermission("Leader", PERMISSION.ViewUserActions);
                        RegisterPermission("Leader", PERMISSION.DeleteUserInfo);
                        RegisterPermission("Leader", PERMISSION.AddUserScore);
                        RegisterPermission("Leader", PERMISSION.SubUserScore);
                    }
                    return mPermission["Leader"];
                }
            }

            static public int Employee
            {
                get
                {
                    if (mPermission.ContainsKey("Employee") == false)
                    {
                        RegisterPermission("Employee", PERMISSION.CtrlMachine);
                        RegisterPermission("Employee", PERMISSION.StopGuoZhao);

                        RegisterPermission("Employee", PERMISSION.ModifyUserInfo);
                        RegisterPermission("Employee", PERMISSION.ViewUserActions);
                        RegisterPermission("Employee", PERMISSION.AddUserScore);
                        RegisterPermission("Employee", PERMISSION.SubUserScore);
                    }
                    return mPermission["Employee"];
                }
            }
        }

        public ObjectId _id { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public string NickName { get; set; }
        public string AgentId { get; set; }
        public string GM { get; set; }
        public string Title { get; set; }
        public int Permission { get; set; }
        public string PhoneNo { get; set; }
        public string Prefix { get; set; }
        public string Note { get; set; }
        public float BenefitRatio { get; set; }
        public Int64 InCoins { get; set; }
        public Int64 OutCoins { get; set; }
        public Int64 InTrialCoins { get; set; }
        public Int64 OutTrialCoins { get; set; }
        public int LoginErrCnt { get; set; }
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
        public bool Validate { get; set; }
        public string Token { get; set; }
        DateTime mTokenTime;
        public DateTime TokenTime
        {
            get { return mTokenTime.ToLocalTime(); }
            set { mTokenTime = value.ToUniversalTime(); }
        }

        public GMInfo()
        {
            _id = ObjectId.Empty;
            Account = "";
            Password = "";
            NickName = "";
            AgentId = "WinLobby";
            GM = "WinLobby";
            Title = "";
            Permission = PERMISSION.None;
            PhoneNo = "";
            Prefix = "";
            Note = "";
            BenefitRatio = 0;
            InCoins = 0;
            OutCoins = 0;
            InTrialCoins = 0;
            OutTrialCoins = 0;
            LoginErrCnt = 0;
            CreateDate = DateTime.UtcNow;
            LastUpdateDate = DateTime.UtcNow;
            Validate = true;
            Token = "";
            TokenTime = Utils.zeroDateTime();
        }

        public static GMInfo FromJson(JObject obj)
        {
            GMInfo gmInfo = null;
            if (obj != null)
            {
                try
                {
                    gmInfo = new GMInfo();
                    gmInfo._id = ObjectId.Parse(obj["_id"].Value<string>());
                    gmInfo.Account = obj.ContainsKey("Account") ? obj["Account"].Value<string>() : "";
                    gmInfo.Password = obj.ContainsKey("Password") ? obj["Password"].Value<string>() : "";
                    gmInfo.NickName = obj.ContainsKey("NickName") ? obj["NickName"].Value<string>() : "";
                    gmInfo.AgentId = obj.ContainsKey("AgenrId") ? obj["AgenrId"].Value<string>() : "WinLobby";
                    gmInfo.GM = obj.ContainsKey("GM") ? obj["GM"].Value<string>() : "WinLobby";
                    gmInfo.Title = obj.ContainsKey("Title") ? obj["Title"].Value<string>() : "";
                    gmInfo.Permission = obj.ContainsKey("Permission") ? obj["Permission"].Value<int>() : PERMISSION.None;
                    gmInfo.PhoneNo = obj.ContainsKey("PhoneNo") ? obj["PhoneNo"].Value<string>() : "";
                    gmInfo.Prefix = obj.ContainsKey("Prefix") ? obj["Prefix"].Value<string>() : "";
                    gmInfo.Note = obj.ContainsKey("Note") ? obj["Note"].Value<string>() : "";
                    gmInfo.BenefitRatio = obj.ContainsKey("BenefitRatio") ? obj["BenefitRatio"].Value<float>() : 0;
                    gmInfo.InCoins = obj.ContainsKey("InCoins") ? obj["InCoins"].Value<Int64>() : 0;
                    gmInfo.OutCoins = obj.ContainsKey("OutCoins") ? obj["OutCoins"].Value<Int64>() : 0;
                    gmInfo.InTrialCoins = obj.ContainsKey("InTrialCoins") ? obj["InTrialCoins"].Value<Int64>() : 0;
                    gmInfo.OutTrialCoins = obj.ContainsKey("OutTrialCoins") ? obj["OutTrialCoins"].Value<Int64>() : 0;
                    gmInfo.LoginErrCnt = obj.ContainsKey("LoginErrCnt") ? obj["LoginErrCnt"].Value<int>() : 0;
                    gmInfo.CreateDate = obj.ContainsKey("CreateDate") ? Convert.ToDateTime(obj["CreateDate"]) : DateTime.UtcNow;
                    gmInfo.LastUpdateDate = obj.ContainsKey("LastUpdateDate") ? Convert.ToDateTime(obj["LastUpdateDate"]) : DateTime.UtcNow;
                    gmInfo.Validate = obj["Validate"].Value<bool>();
                    gmInfo.Token = obj.ContainsKey("Token") ? obj["Token"].Value<string>() : "";
                    gmInfo.TokenTime = obj.ContainsKey("TokenTime") ? Convert.ToDateTime(obj["TokenTime"]) : Utils.zeroDateTime();
                }
                catch (Exception ex)
                {
                    gmInfo = null;
                }
            }
            return gmInfo;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}

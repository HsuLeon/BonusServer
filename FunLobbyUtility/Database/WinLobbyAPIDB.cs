using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FunLobbyUtils.Database.Schema;

namespace FunLobbyUtils.Database
{
    public class WinLobbyAPIDB : WinLobbyDB
    {
        class APICommand
        {
            public const string GetAgentInfo = "GetAgentInfo";
            public const string SaveAgent = "SaveAgent";
            public const string CreateUser = "CreateUser";
            public const string UpdateUser = "UpdateUser";
            public const string UpdateUserScores = "UpdateUserScores";
            public const string VerifyUser = "VerifyUser";
            public const string QueryUser = "QueryUser";
            public const string QueryUserByTel = "QueryUserByTel";
            public const string AwardUser = "AwardUser";
            public const string QueryAwards = "QueryAwards";
            public const string GetValidateUserInfos = "GetValidateUserInfos";
            public const string AddLoginInfo = "AddLoginInfo";
            public const string CheckLoginInfo = "CheckLoginInfo";
            public const string RecordUserAction = "RecordUserAction";
            public const string GetUserActions = "GetUserActions";
            public const string UserSendGift = "UserSendGift";
            public const string GetUserGiftRecords = "GetUserGiftRecords";
            public const string RecordUserFreeScore = "RecordUserFreeScore";
            public const string GetUserFreeScores = "GetUserFreeScores";
            public const string RecordBonusAward = "RecordBonusAward";
            public const string RecordScore = "RecordScore";
            public const string SummarizeScore = "SummarizeScore";
            public const string SummarizeUniPlay = "SummarizeUniPlay";
            public const string GetAddSubCoinsRecords = "GetAddSubCoinsRecords";

            public const string CreateGM = "CreateGM";
            public const string UpdateGM = "UpdateGM";
            public const string UpdateGMScores = "UpdateGMScores";
            public const string QueryGM = "QueryGM";
            public const string GetValidateGMInfos = "GetValidateGMInfos";
            public const string RecordGMAction = "RecordGMAction";
            public const string GetGMActions = "GetGMActions";

            public const string AddMachineScore = "AddMachineScore";
            public const string GetMachineScore = "GetMachineScore";
            public const string ResetMachineScore = "ResetMachineScore";
            public const string RecordMachineLog = "RecordMachineLog";
            public const string GetMachineLog = "GetMachineLog";
            public const string GetUnbillingAddSubCoinsRecords = "GetUnbillingAddSubCoinsRecords";
            public const string RecordBill = "RecordBill";
            public const string GetBills = "GetBills";
            public const string GetTimeFromServer = "GetTimeFromServer";
            public const string RecordWainZhu = "RecordWainZhu";
            public const string GetWainZhuRecords = "GetWainZhuRecords";
            public const string ClearWainZhuRecords = "ClearWainZhuRecords";
            public const string RecordUnknownOutScore = "RecordUnknownOutScore";
            public const string GetUnknownOutScore = "GetUnknownOutScore";

            public const string RemoveDataInCollection = "RemoveDataInCollection";

            public const string VerifySMSSender = "VerifySMSSender";
            public const string CreateVerifyInfo = "CreateVerifyInfo";
            public const string ConfirmVerifyInfo = "ConfirmVerifyInfo";

            public const string CollectLog = "CollectLog";
            public const string PatchDB = "PatchDB";
        }

        string? mAPIDomain = null;
        int mAPIPort = 0;
        AgentInfo? mAgent = null;

        public string DBType() { return typeof(WinLobbyAPIDB).ToString(); }
        public string Domain() { return mAPIDomain; }
        public int Port() { return mAPIPort; }
        public AgentInfo Agent() { return mAgent; }
        public string AgentAccount() { return mAgent?.Account; }

        public WinLobbyAPIDB(string domain, int port)
        {
            mAPIDomain = domain != "127.0.0.1" ? domain : "localhost";// domain;
            mAPIPort = port;
            try
            {
                mAgent = QueryAgent();
            }
            catch(Exception ex)
            {
                string errMsg = string.Format("WinLobbyAPIDB got {0}", ex.Message);
                Log.StoreMsg(errMsg);
            }
        }

        public void Dispose()
        {
        }

        public void CollectLog(string logger, JObject objContent)
        {
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.CollectLog, APIHandle.MethodType.POST);
                apiHandle.AddParam("Logger", logger);
                apiHandle.AddParam("Content", JsonConvert.SerializeObject(objContent));
                JObject? obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string error = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
                    throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("CollectLog got {0}", errMsg));
            }
        }

        public Dictionary<string, List<JObject>> CheckInScoreCheating()
        {
            Dictionary<string, List<JObject>> inScoreCheatings = new Dictionary<string, List<JObject>>();
            return inScoreCheatings;
        }

        public string PatchDB()
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.PatchDB, APIHandle.MethodType.POST);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string error = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
                    throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public DateTime GetTimeFromServer()
        {
            DateTime serverTime;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetTimeFromServer, APIHandle.MethodType.POST);
                JObject obj = apiHandle.Fire();
                if (obj == null || obj.ContainsKey("ServerTime") == false) throw new Exception("obj is null");
                serverTime = DateTime.Parse(obj["ServerTime"].Value<string>());
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetTimeFromServer got {0}", errMsg));
                serverTime = DateTime.Now; 
            }
            return serverTime;
        }

        public AgentInfo QueryAgent()
        {
            AgentInfo? agentInfo = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetAgentInfo, APIHandle.MethodType.POST);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string strAgentInfo = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                agentInfo = AgentInfo.FromJson(JObject.Parse(strAgentInfo));
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("QueryAgent got {0}", errMsg));
                agentInfo = null;
            }
            mAgent = agentInfo;
            return agentInfo;
        }

        public JObject SaveAgent(AgentInfo agentInfo)
        {
            JObject resObj = new JObject();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.SaveAgent, APIHandle.MethodType.POST);
                apiHandle.AddParam("AgentInfo", Utils.Encrypt(JsonConvert.SerializeObject(agentInfo), "WinLobby", "620718"));
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status == "success" && obj.ContainsKey("message"))
                {
                    string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                    resObj = JObject.Parse(strContent);

                    if (resObj.ContainsKey("agentInfo"))
                    {
                        JObject objAgentInfo = resObj["agentInfo"].Value<JObject>();
                        // refresh mAgent
                        if (objAgentInfo["Account"].Value<string>() == mAgent.Account)
                        {
                            mAgent = AgentInfo.FromJson(objAgentInfo);
                        }
                    }
                }
                else
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("SaveAgent got {0}", errMsg));
                resObj["status"] = "failed";
                resObj["error"] = errMsg;
            }
            return resObj;
        }

        public JObject CreateUser(string gm, string account, string password, string nickName, string phoneNo, DateTime birthday,
            string note, bool bGiftEnabled, int maxDailyReceivedGolds, string avatarInfo)
        {
            JObject resObj = new JObject();
            try
            {
                bool bUserAwardEnable = false;
                if (mAgent != null)
                    bUserAwardEnable = mAgent.DefUserAwardEnable != null ? mAgent.DefUserAwardEnable : true;
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.CreateUser, APIHandle.MethodType.POST);
                apiHandle.AddParam("GM", gm);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("Password", password);
                apiHandle.AddParam("NickName", nickName);
                apiHandle.AddParam("PhoneNo", phoneNo);
                apiHandle.AddParam("Birthday", string.Format("{0}-{1}-{2} {3}:{4}:{5}", birthday.Year, birthday.Month, birthday.Day, birthday.Hour, birthday.Minute, birthday.Second));
                apiHandle.AddParam("Note", note);
                apiHandle.AddParam("GiftEnabled", bGiftEnabled ? "true" : "false");
                apiHandle.AddParam("UserAwardEnable", bUserAwardEnable);
                apiHandle.AddParam("MaxDailyReceivedGolds", maxDailyReceivedGolds);
                apiHandle.AddParam("AvatarInfo", avatarInfo);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status == "success" && obj.ContainsKey("message"))
                {
                    string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                    resObj = JObject.Parse(strContent);
                }
                else
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("CreateUser got {0}", errMsg));
                resObj["status"] = "failed";
                resObj["error"] = errMsg;
            }
            return resObj;
        }

        //public JObject SaveUser(UserInfo userInfo)
        //{
        //    JObject resObj = new JObject();
        //    try
        //    {
        //        APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.SaveUser, APIHandle.MethodType.POST);
        //        apiHandle.AddParam("UserInfo", Utils.Encrypt(JsonConvert.SerializeObject(userInfo), "WinLobby", "620718"));
        //        JObject obj = apiHandle.Fire();
        //        if (obj == null) throw new Exception("obj is null");

        //        string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
        //        if (status == "success" && obj.ContainsKey("message"))
        //        {
        //            string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
        //            resObj = JObject.Parse(strContent);
        //        }
        //        else
        //        {
        //            string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
        //            throw new Exception(errMsg);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string errMsg = ex.Message;
        //        Log.StoreMsg(string.Format("SaveUser got {0}", errMsg));
        //        resObj["status"] = "failed";
        //        resObj["error"] = errMsg;
        //    }
        //    return resObj;
        //}

        public string UpdateUser(string account, JObject objParams)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.UpdateUser, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("ObjParams", Utils.Encrypt(JsonConvert.SerializeObject(objParams), "WinLobby", "620718"));
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("UpdateUser got {0}", errMsg));
            }
            return errMsg;
        }

        public JObject VerifyUser(string token)
        {
            JObject resObj = new JObject();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.VerifyUser, APIHandle.MethodType.POST);
                apiHandle.AddParam("Token", token);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status == "success" && obj.ContainsKey("message"))
                {
                    string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                    resObj = JObject.Parse(strContent);
                }
                else
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("VerifyUser got {0}", errMsg));
                resObj["status"] = "failed";
                resObj["error"] = errMsg;
            }
            return resObj;
        }

        public UserInfo QueryUser(string account, string id, string token)
        {
            UserInfo? userInfo = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.QueryUser, APIHandle.MethodType.POST);
                if (account != null) apiHandle.AddParam("Account", account);
                if (id != null) apiHandle.AddParam("Id", id);
                if (token != null) apiHandle.AddParam("Token", token);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string? strUserInfo = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                if (strUserInfo == null) throw new Exception("null message");
                userInfo = UserInfo.FromJson(JObject.Parse(strUserInfo));
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("QueryUser got {0}", errMsg));
                userInfo = null;
            }
            return userInfo;
        }

        public UserInfo QueryUserByTel(string PhoneNo)
        {
            UserInfo? userInfo = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.QueryUserByTel, APIHandle.MethodType.POST);
                if (PhoneNo != null) apiHandle.AddParam("PhoneNo", PhoneNo);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string strUserInfo = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                userInfo = UserInfo.FromJson(JObject.Parse(strUserInfo));
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("QueryUserByTel got {0}", errMsg));
                userInfo = null;
            }
            return userInfo;
        }

        public JObject AwardUser(string account, string reason, string awardType, int award)
        {
            JObject resObj = new JObject();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.AwardUser, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("Reason", reason);
                apiHandle.AddParam("AwardType", awardType);
                apiHandle.AddParam("Award", award);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                resObj = JObject.Parse(strContent);
            }
            catch(Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("AwardUser got {0}", errMsg));
                resObj["status"] = "failed";
                resObj["error"] = errMsg;
            }
            return resObj;
        }

        public List<UserAward> QueryAwards(string targetUser, List<string> awardTypes, DateTime sTime, DateTime eTime)
        {
            List<UserAward> userAwards = new List<UserAward>();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.QueryAwards, APIHandle.MethodType.POST);
                if (targetUser != null) apiHandle.AddParam("Target", targetUser);
                if (awardTypes != null) apiHandle.AddParam("AwardTypes", JsonConvert.SerializeObject(awardTypes));
                apiHandle.AddParam("STime", sTime.Ticks);
                apiHandle.AddParam("ETime", eTime.Ticks);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objAward in list)
                {
                    UserAward userAward = UserAward.FromJson(objAward);
                    userAwards.Add(userAward);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("QueryAwards got {0}", errMsg));
            }
            return userAwards;
        }

        public List<UserInfo> GetValidateUserInfos(string gmAccount)
        {
            List<UserInfo>? userInfoList = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetValidateUserInfos, APIHandle.MethodType.POST);
                apiHandle.AddParam("GM", gmAccount);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                    throw new Exception(errMsg);
                }

                userInfoList = new List<UserInfo>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objUserInfo in list)
                {
                    UserInfo userInfo = UserInfo.FromJson(objUserInfo);
                    userInfoList.Add(userInfo);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetValidateUserInfos got {0}", errMsg));
                userInfoList = null;
            }
            return userInfoList;
        }

        public string AddLoginInfo(string agentId, string appId, string token, string lobbyName)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.AddLoginInfo, APIHandle.MethodType.POST);
                apiHandle.AddParam("AgentId", agentId);
                apiHandle.AddParam("AppId", appId);
                apiHandle.AddParam("Token", token);
                apiHandle.AddParam("LobbyName", lobbyName);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public JObject CheckLoginInfo(string agent, string appId)
        {
            JObject resObj = new JObject();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.CheckLoginInfo, APIHandle.MethodType.POST);
                apiHandle.AddParam("AgentId", agent);
                apiHandle.AddParam("AppId", appId);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                resObj = JObject.Parse(strContent);
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("CheckLoginInfo got {0}", errMsg));
                resObj["status"] = "failed";
                resObj["error"] = errMsg;
            }
            return resObj;
        }

        public string UpdateUserScores(string account, JObject objScores)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.UpdateUserScores, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("ObjScores", Utils.Encrypt(JsonConvert.SerializeObject(objScores), "WinLobby", "620718"));
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordUserAction got {0}", errMsg));
            }
            return errMsg;
        }

        public string RecordUserAction(string userId, string lobby, string machine, string userAction, string note)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordUserAction, APIHandle.MethodType.POST);
                apiHandle.AddParam("UserId", userId);
                apiHandle.AddParam("Lobby", lobby);
                apiHandle.AddParam("Machine", machine);
                apiHandle.AddParam("Action", userAction);
                apiHandle.AddParam("Note", note);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordUserAction got {0}", errMsg));
            }
            return errMsg;
        }

        public List<UserAction> GetUserActions(string account, List<string> actionFilter, int dataCount)
        {
            List<UserAction> userActions = new List<UserAction>();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetUserActions, APIHandle.MethodType.POST);
                if (account != null) apiHandle.AddParam("Account", account);
                if (actionFilter != null) apiHandle.AddParam("ActionFilter", JsonConvert.SerializeObject(actionFilter));
                apiHandle.AddParam("DataCount", dataCount);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objUserAction in list)
                {
                    UserAction userAction = UserAction.FromJson(objUserAction);
                    userActions.Add(userAction);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetUserActions 1 got {0}", errMsg));
            }
            return userActions;
        }

        public List<UserAction> GetUserActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime)
        {
            List<UserAction> userActions = new List<UserAction>();
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetUserActions, APIHandle.MethodType.POST);
                if (account != null) apiHandle.AddParam("Account", account);
                if (actionFilter != null) apiHandle.AddParam("ActionFilter", JsonConvert.SerializeObject(actionFilter));
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objUserAction in list)
                {
                    UserAction userAction = UserAction.FromJson(objUserAction);
                    userActions.Add(userAction);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetUserActions 2 got {0}", errMsg));
            }
            return userActions;
        }

        public string UserSendGift(string account, string target, JObject objGift)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.UserSendGift, APIHandle.MethodType.POST);
                apiHandle.AddParam("User", account);
                apiHandle.AddParam("Target", target);
                apiHandle.AddParam("Content", JsonConvert.SerializeObject(objGift));
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string msg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(msg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("UserSendGift got {0}", errMsg));
            }
            return errMsg;
        }

        public List<GiftInfo> GetUserGiftRecords(string account, string target, DateTime sTime, DateTime eTime)
        {
            List<GiftInfo>? giftRecords = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetUserGiftRecords, APIHandle.MethodType.POST);
                if (account != null) apiHandle.AddParam("User", account);
                if (target != null) apiHandle.AddParam("Target", target);
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                giftRecords = new List<GiftInfo>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objGiftInfo in list)
                {
                    GiftInfo giftInfo = GiftInfo.FromJson(objGiftInfo);
                    giftRecords.Add(giftInfo);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetUserGiftRecords got {0}", errMsg));
                giftRecords = null;
            }
            return giftRecords;
        }

        public string RecordUserFreeScore(string account, SCORE_TYPE scoreType, int scores, string note)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.UserSendGift, APIHandle.MethodType.POST);
                apiHandle.AddParam("User", account);
                apiHandle.AddParam("ScoreType", scoreType);
                apiHandle.AddParam("Scores", scores);
                apiHandle.AddParam("Note", note);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string msg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(msg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("UserSendGift got {0}", errMsg));
            }
            return errMsg;
        }

        public List<UserFreeScore> GetUserFreeScores(string account, DateTime sTime, DateTime eTime)
        {
            List<UserFreeScore>? userFreeScores = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetUserFreeScores, APIHandle.MethodType.POST);
                if (account != null) apiHandle.AddParam("User", account);
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                userFreeScores = new List<UserFreeScore>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objFreeScores in list)
                {
                    UserFreeScore userFreeScore = UserFreeScore.FromJson(objFreeScores);
                    userFreeScores.Add(userFreeScore);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetUserGiftRecords got {0}", errMsg));
                userFreeScores = null;
            }
            return userFreeScores;
        }


        public string? RecordBonusAward(string winType, float totalBet, float scoreInterval,
            string awardInfo, string urlTransferPoints, string strTransferResponse)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordBonusAward, APIHandle.MethodType.POST);
                apiHandle.AddParam("WinType", winType);
                apiHandle.AddParam("TotalBet", totalBet);
                apiHandle.AddParam("ScoreInterval", scoreInterval);
                apiHandle.AddParam("AwardInfo", awardInfo);
                apiHandle.AddParam("ApiTransferPoints", urlTransferPoints);
                apiHandle.AddParam("ApiTransferResponse", strTransferResponse);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string error = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public string RecordScore(string account, string lobby, string machine, int deviceId, int machineId, long totalInScore, long totalOutScore)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordScore, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("Lobby", lobby);
                apiHandle.AddParam("Machine", machine);
                apiHandle.AddParam("ArduinoId", deviceId);
                apiHandle.AddParam("DeviceId", deviceId);
                apiHandle.AddParam("MachineId", machineId);
                apiHandle.AddParam("TotalInScore", totalInScore);
                apiHandle.AddParam("TotalOutScore", totalOutScore);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordScore got {0}", errMsg));
            }
            return errMsg;
        }

        //public List<ScoreInfo> GetScores(string account, DateTime sTime, DateTime eTime)
        //{
        //    List<ScoreInfo> scoreInfos = null;
        //    try
        //    {
        //        sTime = sTime.ToUniversalTime();
        //        eTime = eTime.ToUniversalTime();

        //        APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetScores, APIHandle.MethodType.POST);
        //        apiHandle.AddParam("Account", account);
        //        apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
        //        apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
        //        apiHandle.AddParam("STick", sTime.Ticks);
        //        apiHandle.AddParam("ETick", eTime.Ticks); 

        //        JObject obj = apiHandle.Fire();
        //        if (obj == null) throw new Exception("obj is null");

        //        string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
        //        if (status != "success")
        //        {
        //            string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
        //            throw new Exception(errMsg);
        //        }

        //        scoreInfos = new List<ScoreInfo>();
        //        string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
        //        List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
        //        foreach (JObject objScoreInfo in list)
        //        {
        //            ScoreInfo scoreInfo = ScoreInfo.FromJson(objScoreInfo);
        //            scoreInfos.Add(scoreInfo);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        scoreInfos = null;
        //    }
        //    return scoreInfos;
        //}

        public string SummarizeScore(string account, string lobbyName, string machine, int deviceId, int machineId, SCORE_TYPE scoreType, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.SummarizeScore, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("LobbyName", lobbyName);
                apiHandle.AddParam("Machine", machine);
                apiHandle.AddParam("ArduinoId", deviceId);
                apiHandle.AddParam("DeviceId", deviceId);
                apiHandle.AddParam("MachineId", machineId);
                apiHandle.AddParam("ScoreType", (int)scoreType);
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string message = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(message);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("SummarizeScore got {0}", errMsg));
            }
            return errMsg;
        }

        public string SummarizeUniPlay(string account, string uniPlayDomain, string uniPlayAccount, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.SummarizeUniPlay, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("UniPlayDomain", uniPlayDomain);
                apiHandle.AddParam("UniPlayAccount", uniPlayAccount);
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string message = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(message);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("SummarizeUniPlay got {0}", errMsg));
            }
            return errMsg;
        }

        //=======================================================================================================

        public JObject CreateGM(string gm, string account, string password, string nickName, int permission,
            string phoneNo, string prefix, float benefitRatio, string note)
        {
            JObject resObj = new JObject();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.CreateGM, APIHandle.MethodType.POST);
                apiHandle.AddParam("GM", gm);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("Password", password);
                apiHandle.AddParam("NickName", nickName);
                apiHandle.AddParam("Permission", permission);
                apiHandle.AddParam("PhoneNo", phoneNo);
                apiHandle.AddParam("Prefix", prefix);
                apiHandle.AddParam("BenefitRatio", benefitRatio);
                apiHandle.AddParam("Note", note);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string error = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
                    throw new Exception(error);
                }

                string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                resObj = JObject.Parse(strContent);
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("CreateGM got {0}", errMsg));
                resObj["status"] = "failed";
                resObj["error"] = errMsg;
            }
            return resObj;
        }

        //public JObject SaveGM(GMInfo gmInfo)
        //{
        //    JObject resObj = new JObject();
        //    try
        //    {
        //        APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.SaveGM, APIHandle.MethodType.POST);
        //        apiHandle.AddParam("GMInfo", Utils.Encrypt(JsonConvert.SerializeObject(gmInfo), "WinLobby", "620718"));

        //        JObject obj = apiHandle.Fire();
        //        if (obj == null) throw new Exception("obj is null");

        //        string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
        //        if (status != "success")
        //        {
        //            string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤:{0}";
        //            throw new Exception(errMsg);
        //        }

        //        string strContent = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
        //        resObj = JObject.Parse(strContent);
        //    }
        //    catch (Exception ex)
        //    {
        //        string errMsg = ex.Message;
        //        Log.StoreMsg(string.Format("SaveGM got {0}", errMsg));
        //        resObj["status"] = "failed";
        //        resObj["error"] = errMsg;
        //    }
        //    return resObj;
        //}

        public string UpdateGM(string account, JObject objParams)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.UpdateGM, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("ObjParams", Utils.Encrypt(JsonConvert.SerializeObject(objParams), "WinLobby", "620718"));
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("UpdateGM got {0}", errMsg));
            }
            return errMsg;
        }

        public GMInfo QueryGM(string account, string id, string token)
        {
            GMInfo? gmInfo = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.QueryGM, APIHandle.MethodType.POST);
                if (account != null) apiHandle.AddParam("Account", account);
                if (id != null) apiHandle.AddParam("Id", id);
                if (token != null) apiHandle.AddParam("Token", token);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string strGMInfo = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                gmInfo = GMInfo.FromJson(JObject.Parse(strGMInfo));
                // convert old permission value to new settings
                int newPermission = GMInfo.PERMISSION.ConvertPermission(gmInfo.Permission);
                if (newPermission != gmInfo.Permission)
                {
                    switch (gmInfo.Permission)
                    {
                        case 0x80: gmInfo.Title = "(老闆)Root"; break;
                        case 0x20: gmInfo.Title = "(經理)Manager"; break;
                        case 0x4: gmInfo.Title = "(領班)Leader"; break;
                        case 0x1: gmInfo.Title = "(員工)Employee"; break;
                    }
                    gmInfo.Permission = newPermission;

                    JObject objParams = new JObject();
                    objParams["Title"] = gmInfo.Title;
                    objParams["Permission"] = gmInfo.Permission;
                    this.UpdateGM(gmInfo.Account, objParams);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("QueryGM got {0}", errMsg));
                gmInfo = null;
            }
            return gmInfo;
        }

        public List<GMInfo> GetValidateGMInfos(string gmAccount)
        {
            List<GMInfo>? gmInfos = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetValidateGMInfos, APIHandle.MethodType.POST);
                apiHandle.AddParam("GM", gmAccount);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                gmInfos = new List<GMInfo>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objGMInfo in list)
                {
                    GMInfo gmInfo = GMInfo.FromJson(objGMInfo);
                    gmInfos.Add(gmInfo);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetValidateGMInfos got {0}", errMsg));
                gmInfos = null;
            }
            return gmInfos;
        }

        public string UpdateGMScores(string account, JObject objScores)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.UpdateGMScores, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("ObjScores", Utils.Encrypt(JsonConvert.SerializeObject(objScores), "WinLobby", "620718"));

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string message = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(message);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("SummarizeScore got {0}", errMsg));
            }
            return errMsg;
        }

        public List<GMAction> GetGMActions(string account, List<string> actionFilter, int dataCount)
        {
            List<GMAction> gmActions = new List<GMAction>();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetGMActions, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                if (actionFilter != null) apiHandle.AddParam("ActionFilter", JsonConvert.SerializeObject(actionFilter));
                apiHandle.AddParam("DataCount", dataCount);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objGMAction in list)
                {
                    GMAction gmAction = GMAction.FromJson(objGMAction);
                    gmActions.Add(gmAction);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetGMActions 1 got {0}", errMsg));
                gmActions = null;
            }
            return gmActions;
        }

        public List<GMAction> GetGMActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime)
        { 
            List<GMAction>? gmActions = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetGMActions, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                if (actionFilter != null) apiHandle.AddParam("ActionFilter", JsonConvert.SerializeObject(actionFilter));
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks); 
                
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                gmActions = new List<GMAction>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objGMAction in list)
                {
                    GMAction gmAction = GMAction.FromJson(objGMAction);
                    gmActions.Add(gmAction);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetGMActions 2 got {0}", errMsg));
                gmActions = null;
            }
            return gmActions;
        }

        public string RecordGMAction(string account, string gmAction, string note)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordGMAction, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("Action", gmAction);
                apiHandle.AddParam("Note", note);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordGMAction got {0}", errMsg));
            }
            return errMsg;
        }

        public string AddMachineScore(string lobbyName, int deviceId, int machineId, long inCount, long outCount, string userAccount)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.AddMachineScore, APIHandle.MethodType.POST);
                apiHandle.AddParam("LobbyName", lobbyName);
                apiHandle.AddParam("ArduinoId", deviceId);
                apiHandle.AddParam("DeviceId", deviceId);
                apiHandle.AddParam("MachineId", machineId);
                apiHandle.AddParam("In", inCount);
                apiHandle.AddParam("Out", outCount);
                apiHandle.AddParam("UserAccount", userAccount);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("AddMachineScore got {0}", errMsg));
            }
            return errMsg;
        }

        public List<MachineScore> GetMachineScore(DateTime sTime, DateTime eTime, string lobbyName, int deviceId, int machineId, string userAccount)
        {
            List<MachineScore>? machineScores = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetMachineScore, APIHandle.MethodType.POST);
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks);
                if (lobbyName != null) apiHandle.AddParam("LobbyName", lobbyName);
                if (deviceId >= 0)
                {
                    apiHandle.AddParam("ArduinoId", deviceId);
                    apiHandle.AddParam("DeviceId", deviceId);
                }
                if (machineId >= 0) apiHandle.AddParam("MachineId", machineId);
                if (userAccount != null && userAccount.Length > 0) apiHandle.AddParam("UserAccount", userAccount);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                machineScores = new List<MachineScore>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objMachineScore in list)
                {
                    MachineScore machineScore = MachineScore.FromJson(objMachineScore);
                    machineScores.Add(machineScore);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetMachineScore got {0}", errMsg));
                machineScores = null;
            }
            return machineScores;
        }

        public string ResetMachineScore(string lobbyName, int deviceId, int machineId)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.ResetMachineScore, APIHandle.MethodType.POST);
                if (lobbyName != null) apiHandle.AddParam("LobbyName", lobbyName);
                if (deviceId >= 0)
                {
                    apiHandle.AddParam("ArduinoId", deviceId);
                    apiHandle.AddParam("DeviceId", deviceId);
                }
                if (machineId >= 0) apiHandle.AddParam("MachineId", machineId);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("ResetMachineScore got {0}", errMsg));
            }
            return errMsg;
        }

        public string RecordMachineLog(string lobbyName, int deviceId, int machineId, string action, JObject logObj, string userAccount)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordMachineLog, APIHandle.MethodType.POST);
                apiHandle.AddParam("Action", action);
                apiHandle.AddParam("LobbyName", lobbyName);
                apiHandle.AddParam("ArduinoId", deviceId);
                apiHandle.AddParam("DeviceId", deviceId);
                apiHandle.AddParam("MachineId", machineId);
                apiHandle.AddParam("Log", JsonConvert.SerializeObject(logObj));
                apiHandle.AddParam("UserAccount", userAccount);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordMachineLog got {0}", errMsg));
            }
            return errMsg;
        }

        public List<MachineLog> GetMachineLog(DateTime sTime, DateTime eTime, List<string> actions, string userAccount)
        {
            List<MachineLog>? machineLogs = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetMachineLog, APIHandle.MethodType.POST);
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks);
                if (userAccount != null && userAccount.Length > 0) apiHandle.AddParam("UserAccount", userAccount);

                if (actions != null)
                {
                    string strActions = "";
                    for (int i = 0; i < actions.Count; i++)
                    {
                        if (strActions.Length > 0) strActions += "/";
                        strActions += actions[i];
                    }
                    apiHandle.AddParam("Actions", strActions);
                }

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                machineLogs = new List<MachineLog>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objMachineLog in list)
                {
                    MachineLog machineLog = MachineLog.FromJson(objMachineLog);
                    machineLogs.Add(machineLog);
                }
            }
            catch(Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetMachineLog got {0}", errMsg));
                machineLogs = null;
            }
            return machineLogs;
        }

        public JObject GetUnbillingAddSubCoinsRecords(string gm)
        {
            JObject resObj = new JObject();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetUnbillingAddSubCoinsRecords, APIHandle.MethodType.POST);
                apiHandle.AddParam("GM", gm);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : "未知的錯誤";
                    throw new Exception(errMsg);
                }

                string strObject = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                resObj = JObject.Parse(strObject);
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetUnbillingAddSubCoinsRecords got {0}", errMsg));
                resObj["status"] = "failed";
                resObj["error"] = errMsg;
            }
            return resObj;
        }

        public string RecordBill(string account, int totalCoins, string note)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordBill, APIHandle.MethodType.POST);
                apiHandle.AddParam("GM", account);
                apiHandle.AddParam("TotalCoins", totalCoins);
                apiHandle.AddParam("Note", note);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordBill got {0}", errMsg));
            }
            return errMsg;
        }

        public List<BillInfo> GetBills(string account, bool bFiltGM, DateTime sTime, DateTime eTime)
        {
            List<BillInfo>? billInfos = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetBills, APIHandle.MethodType.POST);
                apiHandle.AddParam("GM", account);
                apiHandle.AddParam("FiltGM", bFiltGM);
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks); 
                
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }

                billInfos = new List<BillInfo>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject obBillInfo in list)
                {
                    BillInfo billInfo = BillInfo.FromJson(obBillInfo);
                    billInfos.Add(billInfo);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetBills got {0}", errMsg));
                billInfos = null;
            }
            return billInfos;
        }

        public JArray GetAddSubCoinsRecords(string account, List<string> actions, DateTime sTime, DateTime eTime)
        {
            JArray? records = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetAddSubCoinsRecords, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("QueriedActions", JsonConvert.SerializeObject(actions));
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks); 
                
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }

                records = obj.ContainsKey("records") ? obj["records"].Value<JArray>() : null;
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetAddSubCoinsRecords got {0}", errMsg));
                records = null;
            }
            return records;
        }

        public string RecordWainZhu(string account, int scoreCnt, int minCnt, int maxCnt)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordWainZhu, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account);
                apiHandle.AddParam("ScoreCnt", scoreCnt);
                apiHandle.AddParam("MinCnt", minCnt);
                apiHandle.AddParam("MaxCnt", maxCnt);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordWainZhu got {0}", errMsg));
            }
            return errMsg;
        }

        public List<WainZhu> GetWainZhuRecords(string account)
        {
            List<WainZhu> wainZhuList = new List<WainZhu>();
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetWainZhuRecords, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", account != null ? account : "");
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }

                string strRecords = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                if (strRecords != null)
                {
                    JArray records = JsonConvert.DeserializeObject<JArray>(strRecords);
                    for (int i = 0; i < records.Count; i++)
                    {
                        WainZhu wainZhu = WainZhu.FromJson(records[i].Value<JObject>());
                        wainZhuList.Add(wainZhu);
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetWainZhuRecords got {0}", errMsg));
            }
            return wainZhuList;
        }

        public string ClearWainZhuRecords()
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.ClearWainZhuRecords, APIHandle.MethodType.POST);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("ClearWainZhuRecords got {0}", errMsg));
            }
            return errMsg;
        }

        public string RecordUnknownOutScore(string lobbyName, string machineName, int deviceId, int machineId, int singleOutScore, long outScoreCnt)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RecordUnknownOutScore, APIHandle.MethodType.POST);
                apiHandle.AddParam("LobbyName", lobbyName);
                apiHandle.AddParam("MachineName", machineName);
                apiHandle.AddParam("ArduinoId", deviceId);
                apiHandle.AddParam("DeviceId", deviceId);
                apiHandle.AddParam("MachineId", machineId);
                apiHandle.AddParam("SingleOutScore", singleOutScore);
                apiHandle.AddParam("OutScoreCnt", outScoreCnt);
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordUnknownOutScore got {0}", errMsg));
            }
            return errMsg;
        }

        public List<UnknownOutScore> GetUnknownOutScore(DateTime sTime, DateTime eTime)
        {
            List<UnknownOutScore>? unknowOutScoreInfos = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.GetUnknownOutScore, APIHandle.MethodType.POST);
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks); 
                
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }

                unknowOutScoreInfos = new List<UnknownOutScore>();
                string content = Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718");
                List<JObject> list = JsonConvert.DeserializeObject<List<JObject>>(content);
                foreach (JObject objUnknowOutScoreInfo in list)
                {
                    UnknownOutScore unknowOutScore = UnknownOutScore.FromJson(objUnknowOutScoreInfo);
                    unknowOutScoreInfos.Add(unknowOutScore);
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("GetUnknownOutScore got {0}", errMsg));
                unknowOutScoreInfos = null;
            }
            return unknowOutScoreInfos;
        }

        public string RemoveDataInCollection(string collection, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.RemoveDataInCollection, APIHandle.MethodType.POST);
                apiHandle.AddParam("Collection", collection);
                apiHandle.AddParam("STime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", sTime.Year, sTime.Month, sTime.Day, sTime.Hour, sTime.Minute, sTime.Second));
                apiHandle.AddParam("ETime", string.Format("{0}-{1}-{2} {3}:{4}:{5}", eTime.Year, eTime.Month, eTime.Day, eTime.Hour, eTime.Minute, eTime.Second));
                apiHandle.AddParam("STick", sTime.Ticks);
                apiHandle.AddParam("ETick", eTime.Ticks); 
                
                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    errMsg = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RemoveDataInCollection got {0}", errMsg));
            }
            return errMsg;
        }

        public string VerifySMSSender(string ip, int maxCnt)
        {
            string? errMsg = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.VerifySMSSender, APIHandle.MethodType.POST);
                apiHandle.AddParam("ip", ip);
                apiHandle.AddParam("maxCnt", maxCnt);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string resultCode = obj.ContainsKey("ResultCode") ? obj["ResultCode"].Value<string>() : null;
                if (resultCode != "OK")
                {
                    string resultObject = obj.ContainsKey("ResultObject") ? obj["ResultObject"].Value<string>() : "未知的錯誤";
                    throw new Exception(resultObject);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("VerifySMSSender got {0}", errMsg));
            }
            return errMsg;
        }

        public VerifyInfo CreateVerifyInfo(string userAccount, string phoneNo)
        {
            VerifyInfo? verifyInfo = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.CreateVerifyInfo, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", userAccount);
                apiHandle.AddParam("PhoneNo", phoneNo);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string error = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(error);
                }
                string strVerifyInfo = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                verifyInfo = VerifyInfo.FromJson(JObject.Parse(strVerifyInfo));
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("CreateVerifyInfo got {0}", errMsg));
                verifyInfo = null;
            }
            return verifyInfo;
        }

        public VerifyInfo ConfirmVerifyInfo(string userAccount, string code, string ip)
        {
            VerifyInfo? verifyInfo = null;
            try
            {
                APIHandle apiHandle = new APIHandle(mAPIDomain, mAPIPort, APICommand.ConfirmVerifyInfo, APIHandle.MethodType.POST);
                apiHandle.AddParam("Account", userAccount);
                apiHandle.AddParam("Code", code);
                apiHandle.AddParam("IP", ip);

                JObject obj = apiHandle.Fire();
                if (obj == null) throw new Exception("obj is null");

                string status = obj.ContainsKey("status") ? obj["status"].Value<string>() : "failed";
                if (status != "success")
                {
                    string error = obj.ContainsKey("message") ? obj["message"].Value<string>() : string.Format("未知的錯誤:{0}", status);
                    throw new Exception(error);
                }
                string strVerifyInfo = obj.ContainsKey("message") ? Utils.Decrypt(obj["message"].Value<string>(), "WinLobby", "620718") : null;
                verifyInfo = VerifyInfo.FromJson(JObject.Parse(strVerifyInfo));
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                Log.StoreMsg(string.Format("ConfirmVerifyInfo got {0}", errMsg));
                verifyInfo = null;
            }
            return verifyInfo;
        }

        public string CreateUniPlay(string name, string domain, string urlLogo, string desc, int profitPayRatio, float exchangeRatioToNTD, bool bAllowable, bool bAcceptable)
        {
            return "no API supported";
        }

        public string UpdateUniPlay(string domain, JObject objParams)
        {
            return "no API supported";
        }

        public List<UniPlayInfo> QueryUniPlay(string name, string domain)
        {
            return null;
        }

        public void Test()
        {

        }
    }
}

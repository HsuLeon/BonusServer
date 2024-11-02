using FunLobbyUtils.Database.Schema;
using FunLobbyUtils.Database;
using FunLobbyUtils;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;

namespace BonusServer.Services
{
    public class DBAgent
    {
        static readonly int mTokenExpiredTimeSpan = 24 * 60 * 60; // 1 day
        static DateTime mStartTime = new DateTime(0);
        static WinLobbyDB? mWinLobbyDB = null;
        static ConnectionFactory? mFactory = null;
        static IConnection? mConnection = null;
        static IModel? mChannelUserScore = null;
        static IModel? mChannelGMScore = null;

        static public bool InitDB(string dbHost, int dbPort, bool useDBCache)
        {
            mStartTime = DateTime.Now;
            try
            {
                DBCacheCLI? dbCacheCLI = useDBCache ? new DBCacheCLI("localhost", 59478) : null;
                mWinLobbyDB = new WinLobbyMongoDB(dbHost, dbPort, dbCacheCLI);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("DBAgent InitDB got {0}", ex.Message));
                mWinLobbyDB = null;
            }
            return mWinLobbyDB != null ? true : false;
        }

        public static DateTime StartTime { get { return mStartTime; } }

        public static WinLobbyDB? FindDB()
        {
            return mWinLobbyDB;
        }

        public static AgentInfo? QueryAgent()
        {
            return mWinLobbyDB?.QueryAgent();
        }

        public static string? AddLoginInfo(string agentId, string appId, string token, string lobbyName)
        {
            return mWinLobbyDB?.AddLoginInfo(agentId, appId, token, lobbyName);
        }

        public static JObject? CheckLoginInfo(string agentId, string appId)
        {
            return mWinLobbyDB?.CheckLoginInfo(agentId, appId);
        }

        public static DateTime GetTimeFromServer()
        {
            return mWinLobbyDB != null ? mWinLobbyDB.GetTimeFromServer() : DateTime.Now;
        }

        public static AgentInfo? Agent()
        {
            return mWinLobbyDB?.Agent();
        }

        public static JObject? SaveAgent(AgentInfo agentInfo)
        {
            return mWinLobbyDB?.SaveAgent(agentInfo);
        }

        public static JObject? CreateUser(string gm, string account, string password, string nickName, string phoneNo,
            DateTime birthday, string note, bool bGiftEnabled, int maxDailyReceivedGolds, string avatarInfo)
        {
            return mWinLobbyDB?.CreateUser(gm, account, password, nickName, phoneNo, birthday, note, bGiftEnabled, maxDailyReceivedGolds, avatarInfo);
        }

        public static string? DeleteUser(string gmAccount, string userAccount)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception(TextConverter.Convert("資料庫未連線"));

                GMInfo gmInfo = mWinLobbyDB.QueryGM(gmAccount, null, null);
                if (gmInfo == null || gmInfo.Validate == false)
                {
                    throw new Exception(string.Format("無效的管理員:{0}", gmAccount));
                }
                if ((gmInfo.Permission & GMInfo.PERMISSION.DeleteUserInfo) == 0x0)
                {
                    throw new Exception(string.Format("管理員:{0}不具備刪除用戶權限", gmAccount));
                }

                UserInfo userInfo = mWinLobbyDB.QueryUser(userAccount, null, null);
                if (userInfo == null || userInfo.Validate == false)
                {
                    throw new Exception(string.Format("{0}:{1}", TextConverter.Convert("無效的用戶"), userAccount));
                }
                //if (CanGMUpdateAllUserInfo(gmInfo, userInfo) == false)
                //{
                //    throw new Exception(string.Format("用戶:{0}與管理員:{1}無關係", userAccount, gmAccount));
                //}
                if (userInfo.InCoins - userInfo.OutCoins > 0)
                {
                    throw new Exception(string.Format("{0}仍有未回收點數{1}，請先回收剩餘點數", userAccount, userInfo.InCoins - userInfo.OutCoins));
                }

                JObject objParams = new JObject();
                objParams["Token"] = "";
                objParams["TokenTime"] = Utils.zeroDateTime();
                objParams["Permission"] = UserInfo.Permission_VIP0;
                objParams["Validate"] = false;
                objParams["Verified"] = false;
                objParams["GiftEnabled"] = false;
                objParams["UserAwardEnable"] = false;
                objParams["InCoins"] = 0;
                objParams["OutCoins"] = 0;
                objParams["InTrialCoins"] = 0;
                objParams["OutTrialCoins"] = 0;
                objParams["MaxDailyReceivedGolds"] = 0;
                objParams["PaiedAward"] = 0;
                objParams["LoginAwardDate"] = Utils.zeroDateTime();
                objParams["GuoZhaoInfo"] = "";
                objParams["GuoZhaoCnt"] = 0;
                objParams["LastGuoZhaoDate"] = Utils.zeroDateTime();
                objParams["LoginErrCnt"] = 0;
                string error = mWinLobbyDB.UpdateUser(userAccount, objParams);
                if (error != null) throw new Exception(error);

                JObject resObj = new JObject();
                resObj["Target"] = userAccount;
                resObj["Note"] = string.Format("{0}刪除用戶{1}資料", gmAccount, userAccount);
                RecordGMAction(gmAccount, GMAction.DeleteUser, JsonConvert.SerializeObject(resObj));
                resObj["status"] = "success";
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static JObject SaveUser(UserInfo srcUuserInfo)
        {
            JObject resObj = new JObject();
            try
            {
                if (srcUuserInfo == null) throw new Exception("srcUuserInfo is null");
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");

                string userAccount = srcUuserInfo.Account;
                UserInfo? userInfo = mWinLobbyDB.QueryUser(userAccount, null, null);
                if (userInfo == null) throw new Exception(string.Format("User {0} doesn't exist", userAccount));

                JObject objParams = new JObject();
                if (srcUuserInfo.Password != userInfo.Password) objParams["Password"] = srcUuserInfo.PaiedAward;
                if (srcUuserInfo.Permission != userInfo.Permission) objParams["Permission"] = srcUuserInfo.Permission;
                if (srcUuserInfo.NickName != userInfo.NickName) objParams["NickName"] = srcUuserInfo.NickName;
                if (srcUuserInfo.Birthday != userInfo.Birthday) objParams["Birthday"] = srcUuserInfo.Birthday;
                if (srcUuserInfo.InCoins != userInfo.InCoins) objParams["InCoins"] = srcUuserInfo.InCoins;
                if (srcUuserInfo.OutCoins != userInfo.OutCoins) objParams["OutCoins"] = srcUuserInfo.OutCoins;
                if (srcUuserInfo.InTrialCoins != userInfo.InTrialCoins) objParams["InTrialCoins"] = srcUuserInfo.InTrialCoins;
                if (srcUuserInfo.OutTrialCoins != userInfo.OutTrialCoins) objParams["OutTrialCoins"] = srcUuserInfo.OutTrialCoins;
                if (srcUuserInfo.InGuoZhaoCoins != userInfo.InGuoZhaoCoins) objParams["InGuoZhaoCoins"] = srcUuserInfo.InGuoZhaoCoins;
                if (srcUuserInfo.OutGuoZhaoCoins != userInfo.OutGuoZhaoCoins) objParams["OutGuoZhaoCoins"] = srcUuserInfo.OutGuoZhaoCoins;
                if (srcUuserInfo.LoginErrCnt != userInfo.LoginErrCnt) objParams["LoginErrCnt"] = srcUuserInfo.LoginErrCnt;
                if (srcUuserInfo.PhoneNo != userInfo.PhoneNo) objParams["PhoneNo"] = srcUuserInfo.PhoneNo;
                if (srcUuserInfo.Note != userInfo.Note) objParams["Note"] = srcUuserInfo.Note;
                if (srcUuserInfo.GiftEnabled != userInfo.GiftEnabled) objParams["GiftEnabled"] = srcUuserInfo.GiftEnabled;
                if (srcUuserInfo.UserAwardEnable != userInfo.UserAwardEnable) objParams["UserAwardEnable"] = srcUuserInfo.UserAwardEnable;
                if (srcUuserInfo.Validate != userInfo.Validate) objParams["Validate"] = srcUuserInfo.Validate;
                if (srcUuserInfo.Verified != userInfo.Verified) objParams["Verified"] = srcUuserInfo.Verified;
                if (srcUuserInfo.GuoZhaoEnabled != userInfo.GuoZhaoEnabled) objParams["GuoZhaoEnabled"] = srcUuserInfo.GuoZhaoEnabled;
                if (srcUuserInfo.Token != userInfo.Token) objParams["Token"] = srcUuserInfo.Token;
                if (srcUuserInfo.TokenTime != userInfo.TokenTime) objParams["TokenTime"] = srcUuserInfo.TokenTime;
                if (srcUuserInfo.LoginAwardDate != userInfo.LoginAwardDate) objParams["LoginAwardDate"] = srcUuserInfo.LoginAwardDate;
                if (srcUuserInfo.PaiedAward != userInfo.PaiedAward) objParams["PaiedAward"] = srcUuserInfo.PaiedAward;
                if (srcUuserInfo.DailyReceivedGoldsDate != userInfo.DailyReceivedGoldsDate) objParams["DailyReceivedGoldsDate"] = srcUuserInfo.DailyReceivedGoldsDate;
                if (srcUuserInfo.DailyReceivedGolds != userInfo.DailyReceivedGolds) objParams["DailyReceivedGolds"] = srcUuserInfo.DailyReceivedGolds;
                if (srcUuserInfo.MaxDailyReceivedGolds != userInfo.MaxDailyReceivedGolds) objParams["MaxDailyReceivedGolds"] = srcUuserInfo.MaxDailyReceivedGolds;
                if (srcUuserInfo.GuoZhaoCnt != userInfo.GuoZhaoCnt) objParams["GuoZhaoCnt"] = srcUuserInfo.GuoZhaoCnt;
                if (srcUuserInfo.LastGuoZhaoDate != userInfo.LastGuoZhaoDate) objParams["LastGuoZhaoDate"] = srcUuserInfo.LastGuoZhaoDate;
                if (srcUuserInfo.GuoZhaoInfo != userInfo.GuoZhaoInfo) objParams["GuoZhaoInfo"] = srcUuserInfo.GuoZhaoInfo;
                if (srcUuserInfo.BetReward != userInfo.BetReward) objParams["BetReward"] = srcUuserInfo.BetReward;
                if (JsonConvert.SerializeObject(srcUuserInfo.WainZhuRecords) != JsonConvert.SerializeObject(userInfo.WainZhuRecords))
                {
                    JArray wainZhus = new JArray();
                    foreach (WainZhu wainZhu in srcUuserInfo.WainZhuRecords)
                    {
                        string content = JsonConvert.SerializeObject(wainZhu);
                        JObject objWainZhu = JObject.Parse(content);
                        wainZhus.Add(objWainZhu);
                    }
                    objParams["WainZhuRecords"] = wainZhus;
                }
                string error = mWinLobbyDB.UpdateUser(userAccount, objParams);
                if (error != null) throw new Exception(string.Format("SaveUser got {0}", error));

                userInfo = mWinLobbyDB.QueryUser(userAccount, null, null);
                if (userInfo == null) throw new Exception(string.Format("user {0} doesn't exist", userAccount));

                resObj["status"] = "success";
                resObj["userInfo"] = JObject.Parse(JsonConvert.SerializeObject(userInfo));
            }
            catch (Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = ex.Message;
            }
            return resObj;
        }

        public static string? UpdateUser(string account, JObject objParams)
        {
            string? errMsg = null;
            try
            {
                if (account == null) throw new Exception("account is null");
                if (objParams == null) throw new Exception("objParams is null");
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");

                string error = mWinLobbyDB.UpdateUser(account, objParams);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static JObject LoginUser(string agent, string account, string password, int maxErrRetryCnt)
        {
            JObject resObj = new JObject();
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                UserInfo userInfo = mWinLobbyDB.QueryUser(account, null, null);
                if (userInfo == null || userInfo.Validate == false) throw new Exception(string.Format("無效的帳號{0} {1}", account, "errID-0"));
                if (maxErrRetryCnt > 0 && userInfo.LoginErrCnt >= maxErrRetryCnt) throw new Exception(string.Format("密碼錯誤次數超過{0}次，請聯絡管理員重置密碼 {1}", userInfo.LoginErrCnt, "errID-1"));
                if (userInfo.Password != password)
                {
                    userInfo.LoginErrCnt++;
                    if (maxErrRetryCnt > 0 && userInfo.Password == userInfo.PhoneNo)
                    {
                        throw new Exception(string.Format("密碼錯誤，已被重置 {0}", "errID-2"));
                    }
                    else
                    {
                        JObject tmpObjParams = new JObject();
                        tmpObjParams["LoginErrCnt"] = userInfo.LoginErrCnt;
                        mWinLobbyDB.UpdateUser(userInfo.Account, tmpObjParams);
                        throw new Exception(string.Format("密碼錯誤，次數:{0} {1}", userInfo.LoginErrCnt, "errID-3"));
                    }
                }

                // generate token...
                TimeSpan tokenSpan = DateTime.Now - userInfo.TokenTime;
                if (userInfo.Token == "" ||
                    tokenSpan.TotalSeconds > mTokenExpiredTimeSpan)
                {
                    const int tokenLength = 32;
                    // generate token
                    string token = Utils.Encrypt(userInfo.Account + DateTime.Now.ToString("HH:mm:ss"), "WinLobby", "620718");
                    if (token.Length < tokenLength)
                    {
                        token = Utils.generateRandom(tokenLength - token.Length) + token;
                    }
                    else
                    {
                        token = token.Substring(0, tokenLength);
                    }
                    userInfo.Token = token;
                }
                // SaveUser will auto update toekn time
                userInfo.LoginErrCnt = 0;
                JObject objParams = new JObject();
                objParams["Token"] = userInfo.Token;
                objParams["LoginErrCnt"] = userInfo.LoginErrCnt;
                string error = mWinLobbyDB.UpdateUser(userInfo.Account, objParams);
                if (error != null) throw new Exception(error);

                resObj["status"] = "success";
                resObj["userInfo"] = JObject.Parse(JsonConvert.SerializeObject(userInfo));
            }
            catch (Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = string.Format("LoginUser got {0}", ex.Message);
            }
            return resObj;
        }

        public static JObject LogoutUser(string token)
        {
            JObject resObj = new JObject();
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                UserInfo userInfo = mWinLobbyDB.QueryUser(null, null, token);
                if (userInfo == null || userInfo.Validate == false)
                {
                    throw new Exception(string.Format("無效的Token{0}", token));
                }

                userInfo.Token = "";
                userInfo.TokenTime = Utils.zeroDateTime();
                JObject objParams = new JObject();
                objParams["Token"] = userInfo.Token;
                objParams["TokenTime"] = userInfo.TokenTime;
                string error = mWinLobbyDB.UpdateUser(userInfo.Account, objParams);
                if (error != null) throw new Exception(error);

                resObj["status"] = "success";
                resObj["userInfo"] = JObject.Parse(JsonConvert.SerializeObject(userInfo));
            }
            catch (Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = string.Format("LogoutUser got {0}", ex.Message);
            }
            return resObj;
        }

        public static JObject? VerifyUser(string token)
        {
            return mWinLobbyDB?.VerifyUser(token);
        }

        public static UserInfo? QueryUser(string account, string id, string token)
        {
            UserInfo? userInfo = mWinLobbyDB?.QueryUser(account, id, token);
            return userInfo;
        }

        public static UserInfo? QueryUserByTel(string PhoneNo)
        {
            UserInfo? userInfo = mWinLobbyDB?.QueryUserByTel(PhoneNo);
            return userInfo;
        }

        public static JObject? AwardUser(string account, string reason, string awardType, int award)
        {
            return mWinLobbyDB?.AwardUser(account, reason, awardType, award);
        }

        public static List<UserAward>? QueryAwards(string targetUser, List<string> awardTypes, DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.QueryAwards(targetUser, awardTypes, sTime, eTime);
        }

        public static List<UserInfo>? GetValidateUserInfos(string gmAccount)
        {
            List<UserInfo>? userInfos = mWinLobbyDB?.GetValidateUserInfos(gmAccount);
            return userInfos;
        }

        public static string? UpdateUserScores(string account, JObject objScores)
        {
            string? errMsg = null;
            try
            {
                if (account == null) throw new Exception("account is null");
                if (objScores == null) throw new Exception("objScores is null");

                try
                {
                    if (mFactory == null) throw new Exception("not use RabbitMQ");
                    if (mConnection == null || mConnection.IsOpen == false)
                    {
                        if (mChannelUserScore != null)
                        {
                            mChannelUserScore.Close();
                            mChannelUserScore = null;
                        }
                        if (mChannelGMScore != null)
                        {
                            mChannelGMScore.Close();
                            mChannelGMScore = null;
                        }
                        if (mConnection != null) mConnection.Close();
                        mConnection = mFactory.CreateConnection();
                    }

                    if (mChannelUserScore == null ||
                        mChannelUserScore.IsOpen == false)
                    {
                        if (mChannelUserScore != null) mChannelUserScore.Close();
                        mChannelUserScore = mConnection.CreateModel();
                        mChannelUserScore.QueueDeclare(queue: "UpdateUserScores",
                                                durable: false,
                                                exclusive: false,
                                                autoDelete: false,
                                                arguments: null);
                    }

                    JObject obj = new JObject();
                    obj["account"] = account;
                    obj["objScores"] = objScores;
                    string message = JsonConvert.SerializeObject(obj);
                    byte[] body = Encoding.UTF8.GetBytes(message);
                    mChannelUserScore.BasicPublish(exchange: "",
                                            routingKey: "UpdateUserScores",
                                            basicProperties: null,
                                            body: body);

                }
                catch (Exception ex)
                {
                    if (mWinLobbyDB == null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                    string error = mWinLobbyDB.UpdateUserScores(account, objScores);
                    if (error != null) throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static JObject? CreateGM(string gm, string account, string password, string nickName,
            int permission, string phoneNo, string prefix, float benefitRatio, string note)
        {
            return mWinLobbyDB?.CreateGM(gm, account, password, nickName, permission, phoneNo, prefix, benefitRatio, note);
        }

        public static string? LoginGM(string account, string password, int maxErrRetryCnt)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                GMInfo? gmInfo = mWinLobbyDB.QueryGM(account, null, null);
                if (gmInfo == null || gmInfo.Validate == false)
                {
                    if (account == "WinLobby")
                    {
                        // first time to create GM "WinLobby"
                        JObject response = mWinLobbyDB.CreateGM("WinLobby", "WinLobby", "WinLobby", "老大", GMInfo.PERMISSION.Root, "", "", 100, "最高管理者");
                        if (response["status"]?.Value<string>() == "success")
                        {
                            JObject objGMInfo = response.ContainsKey("gmInfo") ? response["gmInfo"].Value<JObject>() : null;
                            gmInfo = objGMInfo != null ? GMInfo.FromJson(objGMInfo) : mWinLobbyDB.QueryGM(account, null, null);
                            if (gmInfo.Password == password)
                            {
                                TimeSpan tokenSpan = DateTime.Now - gmInfo.TokenTime;
                                if (gmInfo.Token == "" ||
                                    tokenSpan.TotalSeconds > mTokenExpiredTimeSpan)
                                {
                                    const int tokenLength = 32;
                                    // generate token
                                    string token = Utils.Encrypt(gmInfo.Account + DateTime.Now.ToString("HH:mm:ss"), "WinLobby", "620718");
                                    if (token.Length < tokenLength)
                                    {
                                        token = Utils.generateRandom(tokenLength - token.Length) + token;
                                    }
                                    else
                                    {
                                        token = token.Substring(0, tokenLength);
                                    }
                                    gmInfo.Token = token;
                                }
                                // 
                                gmInfo.LoginErrCnt = 0;
                                JObject objParams = new JObject();
                                objParams["Token"] = gmInfo.Token;
                                objParams["LoginErrCnt"] = gmInfo.LoginErrCnt;
                                string error = mWinLobbyDB.UpdateGM(gmInfo.Account, objParams);
                                if (error != null) throw new Exception(error);
                            }
                            else
                            {
                                gmInfo.LoginErrCnt++;
                                JObject objParams = new JObject();
                                objParams["LoginErrCnt"] = gmInfo.LoginErrCnt;
                                mWinLobbyDB.UpdateGM(gmInfo.Account, objParams);
                                //
                                throw new Exception(string.Format("密碼錯誤，次數:{0}", gmInfo.LoginErrCnt));
                            }
                        }
                        else
                        {
                            string error = response.ContainsKey("error") ? response["error"].Value<string>() : "未知的錯誤";
                            throw new Exception(error);
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("無效的帳號{0}", account));
                    }
                }
                else if (maxErrRetryCnt > 0 && gmInfo.LoginErrCnt >= maxErrRetryCnt)
                {
                    JObject objParams = new JObject();
                    objParams["Password"] = gmInfo.PhoneNo;
                    mWinLobbyDB.UpdateGM(gmInfo.Account, objParams);

                    throw new Exception(string.Format("密碼錯誤次數超過{0}次，密碼已被重置", maxErrRetryCnt));
                }
                else if (gmInfo.Password != password)
                {
                    gmInfo.LoginErrCnt++;
                    if (maxErrRetryCnt > 0 && gmInfo.Password == gmInfo.PhoneNo)
                    {
                        throw new Exception(string.Format("密碼錯誤，已被重置"));
                    }
                    else
                    {
                        JObject objParams = new JObject();
                        objParams["LoginErrCnt"] = gmInfo.LoginErrCnt;
                        mWinLobbyDB.UpdateGM(gmInfo.Account, objParams);
                        throw new Exception(string.Format("密碼錯誤，次數:{0}", gmInfo.LoginErrCnt));
                    }
                }
                else
                {
                    TimeSpan tokenSpan = DateTime.Now - gmInfo.TokenTime;
                    if (gmInfo.Token == "" ||
                        tokenSpan.TotalSeconds > mTokenExpiredTimeSpan)
                    {
                        const int tokenLength = 32;
                        // generate token
                        string token = Utils.Encrypt(gmInfo.Account + DateTime.Now.ToString("HH:mm:ss"), "WinLobby", "620718");
                        if (token.Length < tokenLength)
                        {
                            token = Utils.generateRandom(tokenLength - token.Length) + token;
                        }
                        else
                        {
                            token = token.Substring(0, tokenLength);
                        }
                        gmInfo.Token = token;
                    }
                    // 
                    gmInfo.LoginErrCnt = 0;
                    if (gmInfo.Account == "WinLobby") gmInfo.BenefitRatio = 100;

                    JObject objParams = new JObject();
                    objParams["Token"] = gmInfo.Token;
                    objParams["LoginErrCnt"] = gmInfo.LoginErrCnt;
                    objParams["BenefitRatio"] = gmInfo.BenefitRatio;
                    string error = mWinLobbyDB.UpdateGM(gmInfo.Account, objParams);
                    if (error != null) throw new Exception(error);
                }

                //if (Config.Instance.CanLoginPasswordSameAsPhone == false &&
                //    gmInfo.Password == gmInfo.PhoneNo)
                //{
                //    throw new Exception(TextConverter.Convert("密碼與電話不可相同"));
                //}
                // record GM action
                RecordGMAction(gmInfo.Account, GMAction.Login, "");
            }
            catch (Exception ex)
            {
                errMsg = string.Format("LoginGM got {0}", ex.Message);
            }
            return errMsg;
        }

        public static string? LogoutGM(string token)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                GMInfo? gmInfo = mWinLobbyDB.QueryGM(null, null, token);
                if (gmInfo == null || gmInfo.Validate == false)
                {
                    throw new Exception(string.Format("無效的Token{0}", token));
                }

                gmInfo.Token = "";
                gmInfo.TokenTime = Utils.zeroDateTime();
                JObject objParams = new JObject();
                objParams["Token"] = gmInfo.Token;
                objParams["TokenTime"] = gmInfo.TokenTime;
                string error = mWinLobbyDB.UpdateGM(gmInfo.Account, objParams);
                if (error != null) throw new Exception(error);

                RecordGMAction(gmInfo.Account, GMAction.Logout, "");
            }
            catch (Exception ex)
            {
                errMsg = string.Format("LogoutGM got {0}", ex.Message);
            }
            return errMsg;
        }

        public static GMInfo? QueryGM(string account, string id, string token)
        {
            GMInfo? gmInfo = mWinLobbyDB?.QueryGM(account, id, token);
            return gmInfo;
        }

        public static JObject SaveGM(GMInfo srcGMInfo)
        {
            JObject resObj = new JObject();
            try
            {
                if (srcGMInfo == null) throw new Exception("srcGMInfo is null");
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");

                string gmAccount = srcGMInfo.Account;
                GMInfo? gmInfo = mWinLobbyDB.QueryGM(gmAccount, null, null);
                if (gmInfo == null) throw new Exception(string.Format("GM {0} doesn't exist", gmAccount));

                JObject objParams = new JObject();
                if (srcGMInfo.Password != gmInfo.Password) objParams["Password"] = srcGMInfo.Password;
                if (srcGMInfo.NickName != gmInfo.NickName) objParams["NickName"] = srcGMInfo.NickName;
                if (srcGMInfo.Title != gmInfo.Title) objParams["Title"] = srcGMInfo.Title;
                if (srcGMInfo.Permission != gmInfo.Permission) objParams["Permission"] = srcGMInfo.Permission;
                if (srcGMInfo.PhoneNo != gmInfo.PhoneNo) objParams["PhoneNo"] = srcGMInfo.PhoneNo;
                if (srcGMInfo.Note != gmInfo.Note) objParams["Note"] = srcGMInfo.Note;
                if (srcGMInfo.BenefitRatio != gmInfo.BenefitRatio) objParams["BenefitRatio"] = srcGMInfo.BenefitRatio;
                if (srcGMInfo.InCoins != gmInfo.InCoins) objParams["InCoins"] = srcGMInfo.InCoins;
                if (srcGMInfo.OutCoins != gmInfo.OutCoins) objParams["OutCoins"] = srcGMInfo.OutCoins;
                if (srcGMInfo.InTrialCoins != gmInfo.InTrialCoins) objParams["InTrialCoins"] = srcGMInfo.InTrialCoins;
                if (srcGMInfo.OutTrialCoins != gmInfo.OutTrialCoins) objParams["OutTrialCoins"] = srcGMInfo.OutTrialCoins;
                if (srcGMInfo.LoginErrCnt != gmInfo.LoginErrCnt) objParams["LoginErrCnt"] = srcGMInfo.LoginErrCnt;
                if (srcGMInfo.LastUpdateDate != gmInfo.LastUpdateDate) objParams["LastUpdateDate"] = srcGMInfo.LastUpdateDate;
                if (srcGMInfo.Validate != gmInfo.Validate) objParams["Validate"] = srcGMInfo.Validate;
                if (srcGMInfo.Token != gmInfo.Token) objParams["Token"] = srcGMInfo.Token;
                if (srcGMInfo.TokenTime != gmInfo.TokenTime) objParams["TokenTime"] = srcGMInfo.TokenTime;

                string error = mWinLobbyDB.UpdateGM(gmAccount, objParams);
                if (error != null) throw new Exception(error);

                resObj["status"] = "success";
                resObj["gmInfo"] = JObject.Parse(JsonConvert.SerializeObject(gmInfo));
            }
            catch (Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = ex.Message;
            }
            return resObj;
        }

        public static string? UpdateGM(string account, JObject objParams)
        {
            string? errMsg = null;
            try
            {
                if (account == null) throw new Exception("account is null");
                if (objParams == null) throw new Exception("objParams is null");
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");

                string error = mWinLobbyDB.UpdateGM(account, objParams);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<GMInfo>? GetValidateGMInfos(string gmAccount)
        {
            List<GMInfo>? gmInfos = mWinLobbyDB?.GetValidateGMInfos(gmAccount);
            return gmInfos;
        }

        public static string? UpdateGMScores(string account, JObject objScores)
        {
            string? errMsg = null;
            try
            {
                if (account == null) throw new Exception("account is null");
                if (objScores == null) throw new Exception("objScores is null");

                try
                {
                    if (mFactory == null) throw new Exception("not use RabbitMQ");
                    if (mConnection == null || mConnection.IsOpen == false)
                    {
                        mConnection = mFactory.CreateConnection();
                        if (mChannelUserScore != null)
                        {
                            mChannelUserScore.Close();
                            mChannelUserScore = null;
                        }
                        if (mChannelGMScore != null)
                        {
                            mChannelGMScore.Close();
                            mChannelGMScore = null;
                        }
                    }

                    if (mChannelGMScore == null ||
                        mChannelGMScore.IsOpen == false)
                    {
                        if (mChannelGMScore != null) mChannelGMScore.Close();
                        mChannelGMScore = mConnection.CreateModel();
                        mChannelGMScore.QueueDeclare(queue: "UpdateGMScores",
                                                durable: false,
                                                exclusive: false,
                                                autoDelete: false,
                                                arguments: null);
                    }

                    JObject obj = new JObject();
                    obj["account"] = account;
                    obj["objScores"] = objScores;
                    string message = JsonConvert.SerializeObject(obj);
                    byte[] body = Encoding.UTF8.GetBytes(message);
                    mChannelGMScore.BasicPublish(exchange: "",
                                            routingKey: "UpdateGMScores",
                                            basicProperties: null,
                                            body: body);
                }
                catch (Exception ex)
                {
                    if (mWinLobbyDB == null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                    string error = mWinLobbyDB.UpdateGMScores(account, objScores);
                    if (error != null) throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? RecordUserAction(string userId, string lobby, string machine, string action, string note)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string error = mWinLobbyDB.RecordUserAction(userId, lobby, machine, action, note);
                if (error != null) throw new Exception(error);
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<UserAction>? GetUserActions(string account, List<string> actionFilter, int dataCount)
        {
            return mWinLobbyDB?.GetUserActions(account, actionFilter, dataCount);
        }

        public static List<UserAction>? GetUserActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.GetUserActions(account, actionFilter, sTime, eTime);
        }

        public static string? UserSendGift(string account, string target, JObject objGift)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string error = mWinLobbyDB.UserSendGift(account, target, objGift);
                if (error != null) throw new Exception(error);
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<GiftInfo>? GetUserGiftRecords(string account, string target, DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.GetUserGiftRecords(account, target, sTime, eTime);
        }

        public static string? RecordUserFreeScore(string account, SCORE_TYPE scoreType, int scores, string note)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordUserFreeScore(account, scoreType, scores, note);
                if (error != null) throw new Exception(error);
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<UserFreeScore>? GetUserFreeScores(string account, DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.GetUserFreeScores(account, sTime, eTime);
        }

        public static string? RecordBonusAward(string winType, float totalBet, float scoreInterval,
            JObject bonusAward, string urlTransferPoints, string strTransferResponse)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordBonusAward(winType, totalBet, scoreInterval,
                    JsonConvert.SerializeObject(bonusAward),
                    urlTransferPoints,
                    strTransferResponse);
                if (error != null) throw new Exception(error);
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("RecordBonusAward got {0}", errMsg));
            }
            return errMsg;
        }

        public static string? RecordGMAction(string account, string action, string note)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordGMAction(account, action, note);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<GMAction>? GetGMActions(string account, List<string> actionFilter, int dataCount)
        {
            return mWinLobbyDB?.GetGMActions(account, actionFilter, dataCount);
        }

        public static List<GMAction>? GetGMActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.GetGMActions(account, actionFilter, sTime, eTime);
        }

        public static string? RecordScore(string account, string lobby, string machine, int deviceId, int machineId, long totalInScore, long totalOutScore)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordScore(account, lobby, machine, deviceId, machineId, totalInScore, totalOutScore);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? SummarizeScore(string account, string lobbyName, string machine, int deviceId, int machineId, SCORE_TYPE scoreType, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.SummarizeScore(account, lobbyName, machine, deviceId, machineId, scoreType, sTime, eTime);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? SummarizeUniPlay(string account, string uniPlayDomain, string uniPlayAccount, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.SummarizeUniPlay(account, uniPlayDomain, uniPlayAccount, sTime, eTime);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? AddMachineScore(string lobbyName, int deviceId, int machineId, long inCount, long outCount, string userAccount)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.AddMachineScore(lobbyName, deviceId, machineId, inCount, outCount, userAccount);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<MachineScore>? GetMachineScore(DateTime sTime, DateTime eTime, string lobbyName, int deviceId, int machineId, string userAccount)
        {
            return mWinLobbyDB?.GetMachineScore(sTime, eTime, lobbyName, deviceId, machineId, userAccount);
        }

        public static string? ResetMachineScore(string lobbyName, int deviceId, int machineId)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.ResetMachineScore(lobbyName, deviceId, machineId);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? RecordMachineLog(string lobbyName, int deviceId, int machineId, string action, JObject logObj, string userAccount)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordMachineLog(lobbyName, deviceId, machineId, action, logObj, userAccount);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<MachineLog>? GetMachineLog(DateTime sTime, DateTime eTime, List<string> actions, string userAccount)
        {
            return mWinLobbyDB?.GetMachineLog(sTime, eTime, actions, userAccount);
        }

        public static JObject? GetUnbillingAddSubCoinsRecords(string gm)
        {
            return mWinLobbyDB?.GetUnbillingAddSubCoinsRecords(gm);
        }

        public static string? RecordBill(string account, int totalCoins, string note)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordBill(account, totalCoins, note);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<BillInfo>? GetBills(string account, bool bFiltGM, DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.GetBills(account, bFiltGM, sTime, eTime);
        }

        public static JArray? GetAddSubCoinsRecords(string account, List<string> actions, DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.GetAddSubCoinsRecords(account, actions, sTime, eTime);
        }

        public static string? RecordWainZhu(string account, int scoreCnt, int minCnt, int maxCnt)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordWainZhu(account, scoreCnt, minCnt, maxCnt);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<WainZhu>? GetWainZhuRecords(string account)
        {
            return mWinLobbyDB?.GetWainZhuRecords(account);
        }

        public static string? ClearWainZhuRecords()
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.ClearWainZhuRecords();
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? RecordUnknownOutScore(string lobbyName, string machineName, int deviceId, int machineId, int singleOutScore, long outScoreCnt)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RecordUnknownOutScore(lobbyName, machineName, deviceId, machineId, singleOutScore, outScoreCnt);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static List<UnknownOutScore>? GetUnknownOutScore(DateTime sTime, DateTime eTime)
        {
            return mWinLobbyDB?.GetUnknownOutScore(sTime, eTime);
        }

        public static string? RemoveDataInCollection(string collection, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.RemoveDataInCollection(collection, sTime, eTime);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? VerifySMSSender(string ip, int maxCnt)
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.VerifySMSSender(ip, maxCnt);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static VerifyInfo? CreateVerifyInfo(string userAccount, string phoneNo)
        {
            return mWinLobbyDB?.CreateVerifyInfo(userAccount, phoneNo);
        }

        public static VerifyInfo? ConfirmVerifyInfo(string userAccount, string code, string ip)
        {
            return mWinLobbyDB?.ConfirmVerifyInfo(userAccount, code, ip);
        }

        public static void CollectLog(string logger, JObject obj)
        {
            mWinLobbyDB?.CollectLog(logger, obj);
        }

        public static string? PatchDB()
        {
            string? errMsg = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception("mWinLobbyDB is null");
                string? error = mWinLobbyDB.PatchDB();
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static JObject UserLevelInfo(int userPermission)
        {
            JObject obj = new JObject();
            try
            {
                AgentInfo? agentInfo = mWinLobbyDB?.Agent();
                if (agentInfo == null) throw new Exception("agentInfo is null");

                JObject objUserLevels = JObject.Parse(agentInfo.UserLevels);
                string key = userPermission.ToString();
                if (objUserLevels.ContainsKey(key))
                {
                    JObject? objLevelInfo = objUserLevels[key]?.Value<JObject>();
                    if (objLevelInfo == null) throw new Exception("objLevelInfo is null");
                    foreach (var item in objLevelInfo)
                    {
                        obj[item.Key] = item.Value;
                    }
                }
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("UserLevelInfo got {0}", ex.Message));
            }
            return obj;
        }

        public static List<UniPlayInfo>? QueryUniPlay(string name, string domain)
        {
            List<UniPlayInfo>? uniPlayInfos = null;
            try
            {
                if (mWinLobbyDB == null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                uniPlayInfos = mWinLobbyDB.QueryUniPlay(name, domain);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("QueryUniPlay got {0}", ex.Message));
            }
            return uniPlayInfos;
        }
    }
}

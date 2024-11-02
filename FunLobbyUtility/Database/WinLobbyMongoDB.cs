
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FunLobbyUtils.Database.Schema;

namespace FunLobbyUtils.Database
{
    public class WinLobbyMongoDB : WinLobbyDB
    {
        static readonly int mTokenExpiredTimeSpan = 24 * 60 * 60; // 1 day

        IMongoDatabase? mLobbyDatas;
        IMongoCollection<LoginInfo>? mLoginInfos;
        IMongoCollection<AgentInfo>? mAgentInfos;
        IMongoCollection<GMInfo>? mLobbyGMInfos;
        IMongoCollection<GMAction>? mGMActions;
        IMongoCollection<UserInfo>? mLobbyUserInfos;
        IMongoCollection<UserAction>? mUserActions;
        IMongoCollection<UserAward>? mUserAwards;
        IMongoCollection<GiftInfo>? mUserGifts;
        IMongoCollection<UserFreeScore>? mUserFreeScores;
        IMongoCollection<VerifyInfo>? mVerifyInfos;
        IMongoCollection<UniPlayInfo>? mUniPlayInfos;
        IMongoCollection<SMSSender>? mSMSSenders;
        IMongoCollection<BonusAward>? mBonusAwards;

        IMongoDatabase? mWinLobby;
        IMongoCollection<ScoreInfo>? mUserScores;
        IMongoCollection<MachineLog>? mMachineLogs;
        IMongoCollection<MachineScore>? mMachineScores;
        IMongoCollection<BillInfo>? mGMBills;
        IMongoCollection<WainZhu>? mWainZhus;
        IMongoCollection<UnknownOutScore>? mUnknownOutScores;

        IMongoDatabase? mCollectInfo;
        IMongoCollection<UnparsedLog>? mUnparsedLog;
        IMongoCollection<UserProfile>? mUserProfile;

        string? mDomain = null;
        int mPort = 0;
        DBCacheCLI? mDBCacheCLI = null;
        AgentInfo? mAgent = null;

        public class UpdateScoresInfo
        {
            public enum TARGET
            {
                Unknown = 0,
                User = 1,
                GM = 2
            };
            public TARGET Target { get; private set; }
            public string Account { get; protected set; }

            public UpdateScoresInfo(string account, TARGET target)
            {
                this.Account = account;
                this.Target = target;
            }
        }
        public class UpdateUserScoresInfo : UpdateScoresInfo
        {
            public long InCoins { get; protected set; }
            public long OutCoins { get; protected set; }
            public long InTrialCoins { get; protected set; }
            public long OutTrialCoins { get; protected set; }
            public long InGuoZhaoCoins { get; protected set; }
            public long OutGuoZhaoCoins { get; protected set; }

            public UpdateUserScoresInfo(string account,
                long inCoins, long outCoins, long inTrialCoins, long outTrialCoins,
                long inGuoZhaoCoins, long outGuoZhaoCoins)
                : base(account, UpdateScoresInfo.TARGET.User)
            {
                this.InCoins = inCoins;
                this.OutCoins = outCoins;
                this.InTrialCoins = inTrialCoins;
                this.OutTrialCoins = outTrialCoins;
                this.InGuoZhaoCoins = inGuoZhaoCoins;
                this.OutGuoZhaoCoins = outGuoZhaoCoins;
            }
        }
        public class UpdateGMScoresInfo : UpdateScoresInfo
        {
            public long InCoins { get; protected set; }
            public long OutCoins { get; protected set; }
            public long InTrialCoins { get; protected set; }
            public long OutTrialCoins { get; protected set; }

            public UpdateGMScoresInfo(string account,
                long inCoins, long outCoins, long inTrialCoins, long outTrialCoins)
                : base(account, UpdateScoresInfo.TARGET.GM)
            {
                this.InCoins = inCoins;
                this.OutCoins = outCoins;
                this.InTrialCoins = inTrialCoins;
                this.OutTrialCoins = outTrialCoins;
            }
        }

        Mutex mUpdateScoresMutex = new Mutex();
        Queue<UpdateScoresInfo> mQueueUpdateScoresInfo = new Queue<UpdateScoresInfo>();

        public string DBType() { return typeof(WinLobbyMongoDB).ToString(); }
        public string Domain() { return mDomain; }
        public int Port() { return mPort; }
        public AgentInfo Agent() { return mAgent; }
        public string AgentAccount() { return mAgent?.Account; }

        public WinLobbyMongoDB(string domain, int port, DBCacheCLI? dbCacheCLI = null)
        {
            mDomain = domain.ToLower();
            mPort = port;

            try
            {
                if (mDomain == "localhost" ||
                    mDomain == "127.0.0.1")
                {
                    if (Config.Instance != null)
                    {
                        string errMsg = LaunchMongoDB();
                        if (errMsg != null) throw new Exception(errMsg);
                    }
                }
                // retry max 10 times
                string? error = null;
                for (int i = 0; i < 10; i++)
                {
                    error = BuildConnection();
                    System.Threading.Thread.Sleep(100);
                    if (error == null) break;
                }
                if (error != null) throw new Exception(error);
                // get agent info
                mAgent = QueryAgent();
                mDBCacheCLI = dbCacheCLI;

                Task.Run(() =>
                {
                    while (true)
                    {
                        UpdateScoresInfo scoresInfo = PopUpdateScoresInfo();
                        // if no updateScoresInfo in queue, sleep & skip... 
                        if (scoresInfo == null)
                        {
                            Thread.Sleep(1);
                            continue;
                        }

                        switch (scoresInfo.Target)
                        {
                            case UpdateScoresInfo.TARGET.User:
                                {
                                    try
                                    {
                                        UpdateUserScoresInfo updateScoresInfo = scoresInfo as UpdateUserScoresInfo;
                                        FilterDefinition<UserInfo> findFilter = Builders<UserInfo>.Filter.Eq("Account", updateScoresInfo.Account);
                                        List<UserInfo> userInfos = mLobbyUserInfos.Find(findFilter).ToList();
                                        if (userInfos.Count == 0) throw new Exception(string.Format("User {0} doesn't exist", updateScoresInfo.Account));
                                        UserInfo userInfo = userInfos[0];

                                        // update scores of userInfo
                                        List<UpdateDefinition<UserInfo>> updates = new List<UpdateDefinition<UserInfo>>();
                                        updates.Add(Builders<UserInfo>.Update.Set("LastUpdateDate", DateTime.UtcNow));
                                        if (updateScoresInfo.InCoins > 0) updates.Add(Builders<UserInfo>.Update.Set("InCoins", userInfo.InCoins + updateScoresInfo.InCoins));
                                        if (updateScoresInfo.OutCoins > 0) updates.Add(Builders<UserInfo>.Update.Set("OutCoins", userInfo.OutCoins + updateScoresInfo.OutCoins));
                                        if (updateScoresInfo.InTrialCoins > 0) updates.Add(Builders<UserInfo>.Update.Set("InTrialCoins", userInfo.InTrialCoins + updateScoresInfo.InTrialCoins));
                                        if (updateScoresInfo.OutTrialCoins > 0) updates.Add(Builders<UserInfo>.Update.Set("OutTrialCoins", userInfo.OutTrialCoins + updateScoresInfo.OutTrialCoins));
                                        if (updateScoresInfo.InGuoZhaoCoins > 0) updates.Add(Builders<UserInfo>.Update.Set("InGuoZhaoCoins", userInfo.InGuoZhaoCoins + updateScoresInfo.InGuoZhaoCoins));
                                        if (updateScoresInfo.OutGuoZhaoCoins > 0) updates.Add(Builders<UserInfo>.Update.Set("OutGuoZhaoCoins", userInfo.OutGuoZhaoCoins + updateScoresInfo.OutGuoZhaoCoins));

                                        UpdateResult ret = mLobbyUserInfos.UpdateMany(findFilter, Builders<UserInfo>.Update.Combine(updates));
                                        // update cache
                                        if (this.mDBCacheCLI != null)
                                        {
                                            // update userInfo in cache
                                            FilterDefinition<UserInfo> updateFilter = Builders<UserInfo>.Filter.Eq("_id", new ObjectId(userInfo.GetObjectId()));
                                            userInfos = mLobbyUserInfos.Find(updateFilter).ToList();
                                            string key = string.Format("UserInfo:{0}", userInfos[0].Account);
                                            string content = JsonConvert.SerializeObject(userInfos[0]);
                                            this.mDBCacheCLI.Set(key, content);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.StoreMsg(string.Format("UpdateScoresInfo.SCORE_TARGET.User got {0}", ex.Message));
                                    }
                                }
                                break;
                            case UpdateScoresInfo.TARGET.GM:
                                {
                                    try
                                    {
                                        UpdateGMScoresInfo updateScoresInfo = scoresInfo as UpdateGMScoresInfo;
                                        FilterDefinition<GMInfo> findFilter = Builders<GMInfo>.Filter.Eq("Account", updateScoresInfo.Account);
                                        List<GMInfo> gmInfos = mLobbyGMInfos.Find(findFilter).ToList(); ;
                                        if (gmInfos.Count == 0) throw new Exception(string.Format("GM {0} doesn't exist", updateScoresInfo.Account));
                                        GMInfo gmInfo = gmInfos[0];

                                        // update scores of gmInfo
                                        List<UpdateDefinition<GMInfo>> updates = new List<UpdateDefinition<GMInfo>>();
                                        updates.Add(Builders<GMInfo>.Update.Set("LastUpdateDate", DateTime.UtcNow));
                                        if (updateScoresInfo.InCoins > 0) updates.Add(Builders<GMInfo>.Update.Set("InCoins", gmInfo.InCoins + updateScoresInfo.InCoins));
                                        if (updateScoresInfo.OutCoins > 0) updates.Add(Builders<GMInfo>.Update.Set("OutCoins", gmInfo.OutCoins + updateScoresInfo.OutCoins));
                                        if (updateScoresInfo.InTrialCoins > 0) updates.Add(Builders<GMInfo>.Update.Set("InTrialCoins", gmInfo.InTrialCoins + updateScoresInfo.InTrialCoins));
                                        if (updateScoresInfo.OutTrialCoins > 0) updates.Add(Builders<GMInfo>.Update.Set("OutTrialCoins", gmInfo.OutTrialCoins + updateScoresInfo.OutTrialCoins));

                                        UpdateResult ret = mLobbyGMInfos.UpdateMany(findFilter, Builders<GMInfo>.Update.Combine(updates));
                                        // update cache
                                        if (this.mDBCacheCLI != null)
                                        {
                                            // update gmInfo in cache
                                            FilterDefinition<GMInfo> updateFilter = Builders<GMInfo>.Filter.Eq("_id", new ObjectId(gmInfo.GetObjectId()));
                                            gmInfos = mLobbyGMInfos.Find(updateFilter).ToList();
                                            string key = string.Format("GMInfo:{0}", gmInfos[0].Account);
                                            string content = JsonConvert.SerializeObject(gmInfos[0]);
                                            this.mDBCacheCLI.Set(key, content);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.StoreMsg(string.Format("UpdateScoresInfo.SCORE_TARGET.GM got {0}", ex.Message));
                                    }
                                }
                                break;
                        }
                    }
                });
            }
            catch(Exception ex)
            {
                string errMsg = this.BuildErrMsg("WinLobbyMongoDB", ex.Message);
                Log.StoreMsg(errMsg);
            }
        }

        protected void PushUpdateScoresInfo(UpdateScoresInfo updateScoresInfo)
        {
            if (updateScoresInfo != null)
            {
                // store action in queue
                mUpdateScoresMutex.WaitOne();
                mQueueUpdateScoresInfo.Enqueue(updateScoresInfo);
                mUpdateScoresMutex.ReleaseMutex();
            }
        }

        protected UpdateScoresInfo PopUpdateScoresInfo()
        {
            UpdateScoresInfo? updateScoresInfo = null;
            mUpdateScoresMutex.WaitOne();
            if (mQueueUpdateScoresInfo.Count > 0)
            {
                updateScoresInfo = mQueueUpdateScoresInfo.Dequeue();
            }
            mUpdateScoresMutex.ReleaseMutex();
            return updateScoresInfo;
        }

        protected void TestCheck()
        {
            List<FilterDefinition<UserAction>> addFilters = new List<FilterDefinition<UserAction>>();
            addFilters.Add(Builders<UserAction>.Filter.Eq("UserId", "60e998fe2f513f19bcdbd8ee"));
            addFilters.Add(Builders<UserAction>.Filter.Eq("Action", "加分"));
            FilterDefinition<UserAction> filterAdd = Builders<UserAction>.Filter.And(addFilters);
            List<UserAction> addScores = mUserActions.Find(filterAdd).ToList();
            long totalAdd = 0;
            for (int i = 0; i < addScores.Count; i++)
            {
                JObject obj = JObject.Parse(addScores[i].Note);
                totalAdd += obj["Coins"].Value<int>();
            }

            List<FilterDefinition<UserAction>> subFilters = new List<FilterDefinition<UserAction>>();
            subFilters.Add(Builders<UserAction>.Filter.Eq("UserId", "60e998fe2f513f19bcdbd8ee"));
            subFilters.Add(Builders<UserAction>.Filter.Eq("Action", "減分"));
            FilterDefinition<UserAction> filterSub = Builders<UserAction>.Filter.And(subFilters);
            List<UserAction> subScores = mUserActions.Find(filterSub).ToList();
            long totalSub = 0;
            for (int i = 0; i < subScores.Count; i++)
            {
                JObject obj = JObject.Parse(subScores[i].Note);
                totalSub += obj["Coins"].Value<int>();
            }

            long offset = totalAdd - totalSub;
        }

        protected bool IsDomainLocalhost(string domain)
        {
            List<string> localDomain = new List<string>();
            localDomain.Add("localhost");
            localDomain.Add("127.0.0.1");
            try
            {
                // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(domain);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                for (int i = 0; i < localIPs.Length; i++)
                {
                    if (localIPs[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        localDomain.Add(localIPs[i].ToString());
                    }
                }
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("IsDomainLocalhost", ex.Message);
                Log.StoreMsg(errMsg);
            }
            for (int i = 0; i < localDomain.Count; i++)
            {
                if (domain.ToLower() == localDomain[i])
                    return true;
            }
            return false;
        }

        protected string LaunchMongoDB()
        {
            string? errMsg = null;
            string currentDir = Directory.GetCurrentDirectory();
            try
            {
                char endChar = Config.Instance.MongoDBPath[Config.Instance.MongoDBPath.Length- 1];
                string mongodbPath = Config.Instance.MongoDBPath + (endChar == '\\' ? "" : "\\");
                // check db process, if not exists, launch it
                Process[] processes = Process.GetProcessesByName("mongod");
                // if no mongodb process exists, create one
                if (processes.Count() == 0)
                {
                    string exeMongoDBPath = mongodbPath + "mongod.exe";
                    if (System.IO.File.Exists(exeMongoDBPath) == false)
                    {
                        throw new Exception("mongoDB path isn't correct, execute it manually!!!");
                    }

                    // change to MongoDB's path to avoid miss finding folder "data\db"
                    Directory.SetCurrentDirectory(mongodbPath);
                    Process mongodProcess = new Process();
                    // FileName 是要執行的檔案
                    mongodProcess.StartInfo.FileName = exeMongoDBPath;
                    mongodProcess.Start();
                    int iRetryCnt = 0;
                    Log.StoreMsg("WinLobbyMongoDB(), wait for mongoDB launched");
                    do
                    {
                        if (iRetryCnt > 50)
                        {
                            Log.StoreMsg("WinLobbyMongoDB(), unable to launch mongoDB");
                            break;
                        }
                        processes = Process.GetProcessesByName("mongod");
                        System.Threading.Thread.Sleep(100);
                        iRetryCnt++;
                    } while (processes.Count() == 0);
                    Log.StoreMsg("WinLobbyMongoDB(), success to launch mongoDB");
                }

                // check db process, if not exists, launch it
                string exeMongoPath = mongodbPath + "mongo.exe";
                if (System.IO.File.Exists(exeMongoPath) == false)
                {
                    throw new Exception(string.Format("mongo path {0} isn't correct, execute it manually!!!", exeMongoPath));
                }

                Process mongoProcess = new Process();
                // FileName 是要執行的檔案
                mongoProcess.StartInfo.FileName = exeMongoPath;
                mongoProcess.StartInfo.Arguments = " --eval \"db.adminCommand({ setParameter: 1, internalQueryExecMaxBlockingSortBytes: 335544320})\"";
                mongoProcess.StartInfo.CreateNoWindow = false;
                mongoProcess.StartInfo.UseShellExecute = false;
                mongoProcess.StartInfo.RedirectStandardOutput = true;
                mongoProcess.Start();
                bool bDone = mongoProcess.WaitForExit(10 * 1000);
                if (bDone == false) throw new Exception("SettingMongoMemSize timeout");

                string content = mongoProcess.StandardOutput.ReadToEnd().Replace("\r\n", "\n");
                string[] strResults = content.Split('\n');
                for (int i = 0; i < strResults.Count(); i++)
                {
                    Console.WriteLine(strResults[i]);
                }
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("LaunchMongoDB", ex.Message);
            }
            // change back to exe path
            Directory.SetCurrentDirectory(currentDir);
            return errMsg;
        }

        protected string BuildConnection()
        {
            string? errMsg = null;
            try
            {
                MongoClient dbConnection = new MongoClient(
                    new MongoClientSettings
                    {
                        Server = new MongoServerAddress(mDomain, mPort)
                    });

                mLobbyDatas = dbConnection.GetDatabase("LobbyDatas");
                if (mLobbyDatas != null)
                {
                    mLoginInfos = mLobbyDatas.GetCollection<LoginInfo>("Logins");
                    mAgentInfos = mLobbyDatas.GetCollection<AgentInfo>("Agents");
                    mLobbyGMInfos = mLobbyDatas.GetCollection<GMInfo>("GMInfos");
                    mGMActions = mLobbyDatas.GetCollection<GMAction>("GMActions");
                    mLobbyUserInfos = mLobbyDatas.GetCollection<UserInfo>("UserInfos");
                    mUserActions = mLobbyDatas.GetCollection<UserAction>("UserActions");
                    mUserAwards = mLobbyDatas.GetCollection<UserAward>("UserAwards");
                    mUserGifts = mLobbyDatas.GetCollection<GiftInfo>("UserGift");
                    mUserFreeScores = mLobbyDatas.GetCollection<UserFreeScore>("UserFreeScores");
                    mVerifyInfos = mLobbyDatas.GetCollection<VerifyInfo>("VerifyInfos");
                    mUniPlayInfos = mLobbyDatas.GetCollection<UniPlayInfo>("UniPlayInfos");
                    mSMSSenders = mLobbyDatas.GetCollection<SMSSender>("SMSSenders");
                    mBonusAwards = mLobbyDatas.GetCollection<BonusAward>("BonusAwards");
                }

                mWinLobby = dbConnection.GetDatabase("WinLobby");
                if (mWinLobby != null)
                {
                    mUserScores = mWinLobby.GetCollection<ScoreInfo>("Scores");
                    mMachineLogs = mWinLobby.GetCollection<MachineLog>("MachineLogs");
                    mMachineScores = mWinLobby.GetCollection<MachineScore>("MachineScores");
                    mGMBills = mWinLobby.GetCollection<BillInfo>("GMBills");
                    mWainZhus = mWinLobby.GetCollection<WainZhu>("WainZhu");
                    mUnknownOutScores = mWinLobby.GetCollection<UnknownOutScore>("UnknownOutScore");
                }

                mCollectInfo = dbConnection.GetDatabase("CollectLog");
                if (mCollectInfo != null)
                {
                    mUnparsedLog = mCollectInfo.GetCollection<UnparsedLog>("UnparsedLog");
                    mUserProfile = mCollectInfo.GetCollection<UserProfile>("UserProfile");
                }
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("BuildConnection", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return errMsg;
        }

        public void Dispose()
        {
        }

        public void CollectLog(string logger, JObject objContent)
        {
            try
            {
                if (mUserProfile == null) throw new Exception("null mUserProfile");
                if (objContent == null) throw new Exception("null objContent");

                if (logger.Contains("SendVerifyCode"))
                {
                    UserProfile profile = new UserProfile();
                    profile.ApiUrl = logger;
                    profile.Content = JsonConvert.SerializeObject(objContent);
                    if (objContent.ContainsKey("Name")) profile.Name = objContent["Name"].Value<string>();
                    if (objContent.ContainsKey("PhoneNo")) profile.PhoneNo = objContent["PhoneNo"].Value<string>();
                    if (objContent.ContainsKey("Address")) profile.Address = objContent["Address"].Value<string>();
                    if (objContent.ContainsKey("Birthday")) profile.Birthday = Convert.ToDateTime(objContent["Birthday"]);
                    mUserProfile.InsertOne(profile);
                }
                else
                {
                    throw new Exception("unknown logger");
                }
            }
            catch(Exception ex)
            {
                if (mUnparsedLog != null)
                {
                    UnparsedLog log = new UnparsedLog();
                    log.ApiUrl = logger;
                    log.Content = JsonConvert.SerializeObject(objContent);
                    log.Error = ex.Message;
                    mUnparsedLog.InsertOne(log);
                }
            }
        }

        public Dictionary<string, List<JObject>> CheckInScoreCheating()
        {
            Dictionary<string, List<JObject>>? inScoreCheatings = null;
            try
            {
                FilterDefinition<UserAction> filter = Builders<UserAction>.Filter.Eq("Action", UserAction.DecreaseScore);
                List<UserAction> tmpList = mUserActions.Find(filter).ToList();

                Dictionary<string, List<JObject>> cheatings = new Dictionary<string, List<JObject>>();
                for (int i = 0; i < tmpList.Count; i++)
                {
                    DateTime createTime1 = tmpList[i].CreateTime;
                    for (int j = i+1; j < tmpList.Count; j++)
                    {
                        if (i == j) continue;
                        if (tmpList[i].UserId != tmpList[j].UserId) continue;
                        if (tmpList[i].Lobby == tmpList[j].Lobby &&
                            tmpList[i].Machine == tmpList[j].Machine) continue;

                        DateTime createTime2 = tmpList[j].CreateTime;
                        TimeSpan ts = createTime1 - createTime2;
                        if (ts.TotalMilliseconds > -500 &&
                            ts.TotalMilliseconds < 500)
                        {
                            JObject obj = new JObject();
                            obj["Action1"] = JsonConvert.SerializeObject(tmpList[i]);
                            obj["Action2"] = JsonConvert.SerializeObject(tmpList[j]);

                            JObject objNote = JObject.Parse(tmpList[i].Note);
                            string account = objNote["From"].Value<string>();
                            if (cheatings.ContainsKey(account) == false) cheatings[account] = new List<JObject>();
                            cheatings[account].Add(obj);
                        }
                    }
                }
                inScoreCheatings = cheatings;
            }
            catch (Exception ex)
            {

            }
            return inScoreCheatings;
        }

        public string PatchDB()
        {
            string? errMsg = null;
            try
            {
                //if (mAgentInfos != null && mAgentInfos.Count() == 0)
                //{
                //    MongoCollection<AgentInfo> oldAgentInfos = mWinLobby.GetCollection<AgentInfo>("Agents");
                //    MongoCursor<AgentInfo> collect = oldAgentInfos.FindAll();
                //    List<AgentInfo> dataList = collect.ToList<AgentInfo>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        AgentInfo agentInfo = dataList[i];
                //        if (agentInfo.SMSInfo == null ||
                //            agentInfo.SMSInfo.Length == 0)
                //        {
                //            JObject obj = new JObject();
                //            obj["UID"] = "";
                //            obj["PWD"] = "";
                //            agentInfo.SMSInfo = JsonConvert.SerializeObject(obj);
                //        }

                //        mAgentInfos.Save(dataList[i]);
                //    }
                //    //oldAgentInfos.Drop();
                //}

                // patch Dachenyi C阿嘉 multi log of in:18500 out:26400
                {
                    const int inScore = 18300;//18500;//
                    const int outScore = 25600;//26400;//
                    const string userAccount = "A174";//"C阿嘉";//
                    DateTime beginTime = new DateTime(2022, 12, 6);
                    DateTime endTime = new DateTime(2022, 12, 7);

                    List<FilterDefinition<MachineLog>> logFilters = new List<FilterDefinition<MachineLog>>();
                    logFilters.Add(Builders<MachineLog>.Filter.Eq("Action", "結算"));
                    logFilters.Add(Builders<MachineLog>.Filter.Eq("LobbyName", "1:6閉店鋼珠一館"));
                    logFilters.Add(Builders<MachineLog>.Filter.Eq("ArduinoId", 17));
                    logFilters.Add(Builders<MachineLog>.Filter.Eq("MachineId", 0));
                    logFilters.Add(Builders<MachineLog>.Filter.Gte("CreateTime", beginTime));
                    logFilters.Add(Builders<MachineLog>.Filter.Lt("CreateTime", endTime));
                    FilterDefinition<MachineLog> filter = Builders<MachineLog>.Filter.And(logFilters);

                    List<MachineLog> tmpList = mMachineLogs.Find(filter).ToList();
                    List<MachineLog> targetList = new List<MachineLog>();
                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        MachineLog machineLog = tmpList[i];
                        JObject log = JObject.Parse(machineLog.Log);
                        string player = log["player"].Value<string>();
                        if (player != userAccount) continue;

                        int inValue = log["in"].Value<int>();
                        int outValue = log["out"].Value<int>();
                        if (inValue == inScore && outValue == outScore) targetList.Add(machineLog);
                    }

                    targetList.Sort((a, b) =>
                    {
                        if (a.CreateTime.Ticks < b.CreateTime.Ticks)
                            return -1;
                        else
                            return 1;
                    });

                    for (int i = 1; i < targetList.Count; i++)
                    {
                        FilterDefinition<MachineLog> filter2 = Builders<MachineLog>.Filter.Eq("_id", targetList[i]._id);
                        mMachineLogs.DeleteOne(filter2);
                    }
                }

                //DateTime beginTime = DateTime.Now;
                //DateTime endTime = new DateTime(2022, 6, 15);
                //if (endTime.Ticks > beginTime.Ticks)
                //{
                //    IMongoQuery filter = Query.And(
                //            Query.EQ("Verified", true),
                //            Query.EQ("Validate", true));
                //    MongoCursor<UserInfo> collect = mLobbyUserInfos
                //                                    .Find(filter);
                //    List<UserInfo> tmpList = collect.ToList();
                //    for (int i = 0; i < tmpList.Count; i++)
                //    {
                //        UserInfo userInfo = tmpList[i];
                //        if (userInfo.MaxDailyReceivedGolds < 10000)
                //        {
                //            JObject objParams = new JObject();
                //            objParams["MaxDailyReceivedGolds"] = 10000;
                //            this.UpdateUser(userInfo.Account, objParams);
                //        }
                //    }
                //}

                //if (mLoginInfos != null && mLoginInfos.Count() == 0)
                //{
                //    MongoCollection<LoginInfo> oldLoginInfos = mWinLobby.GetCollection<LoginInfo>("Logins");
                //    MongoCursor<LoginInfo> collect = oldLoginInfos.FindAll();
                //    List<LoginInfo> dataList = collect.ToList<LoginInfo>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        mLoginInfos.Save(dataList[i]);
                //    }
                //    //oldLoginInfos.Drop();
                //}
                //if (mLobbyGMInfos != null && mLobbyGMInfos.Count() == 0)
                //{
                //    MongoCollection<GMInfo> oldGMInfos = mWinLobby.GetCollection<GMInfo>("GameMgrs");
                //    MongoCursor<GMInfo> collect = oldGMInfos.FindAll();
                //    List<GMInfo> dataList = collect.ToList<GMInfo>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        mLobbyGMInfos.Save(dataList[i]);
                //    }
                //    //oldGMInfos.Drop();
                //}
                //if (mGMActions != null && mGMActions.Count() == 0)
                //{
                //    MongoCollection<GMAction> oldGMActions = mWinLobby.GetCollection<GMAction>("GMActions");
                //    MongoCursor<GMAction> collect = oldGMActions.FindAll();
                //    List<GMAction> dataList = collect.ToList<GMAction>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        mGMActions.Save(dataList[i]);
                //    }
                //    //oldGMActions.Drop();
                //}
                //if (mLobbyUserInfos != null && mLobbyUserInfos.Count() == 0)
                //{
                //    MongoCollection<UserInfo> oldUserInfos = mWinLobby.GetCollection<UserInfo>("Users");
                //    MongoCursor<UserInfo> collect = oldUserInfos.FindAll();
                //    List<UserInfo> dataList = collect.ToList<UserInfo>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        mLobbyUserInfos.Save(dataList[i]);
                //    }
                //    //oldUserInfos.Drop();
                //}
                //if (mUserActions != null && mUserActions.Count() == 0)
                //{
                //    MongoCollection<UserAction> oldUserInfos = mWinLobby.GetCollection<UserAction>("Actions");
                //    MongoCursor<UserAction> collect = oldUserInfos.FindAll();
                //    List<UserAction> dataList = collect.ToList<UserAction>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        mUserActions.Save(dataList[i]);
                //    }
                //    //oldUserInfos.Drop();
                //}
                //if (mUserAwards != null && mUserAwards.Count() == 0)
                //{
                //    MongoCollection<UserAward> oldUserAwards = mWinLobby.GetCollection<UserAward>("Awards");
                //    MongoCursor<UserAward> collect = oldUserAwards.FindAll();
                //    List<UserAward> dataList = collect.ToList<UserAward>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        mUserAwards.Save(dataList[i]);
                //    }
                //    //oldUserAwards.Drop();
                //}
                //if (mUserGifts != null && mUserGifts.Count() == 0)
                //{
                //    MongoCollection<GiftInfo> oldUserGifts = mWinLobby.GetCollection<GiftInfo>("UserGift");
                //    MongoCursor<GiftInfo> collect = oldUserGifts.FindAll();
                //    List<GiftInfo> dataList = collect.ToList<GiftInfo>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        mUserGifts.Save(dataList[i]);
                //    }
                //    //oldUserGifts.Drop();
                //}

                //if (mGMActions != null)
                //{
                //    DateTime t1 = DateTime.UtcNow;

                //    while (true)
                //    {
                //        MongoCursor<GMAction> addCollect = mGMActions.Find(Query.EQ("Action", "增加籌碼"));
                //        List<GMAction> addList = addCollect.ToList<GMAction>();
                //        long dataCnt = addList.Count();
                //        if (dataCnt > 0)
                //        {
                //            for (int i = 0; i < dataCnt; i++)
                //            {
                //                GMAction action = addList[i];
                //                action.Action = GMAction.AddUserCoins;
                //                action.Note = action.Note.Replace("籌碼", "點數");
                //                WriteConcernResult ret = mGMActions.Save(action);
                //                if (!ret.Ok)
                //                {
                //                    Log.StoreMsg(string.Format("WinLobbyMongoDB updates add-\"籌碼\" to \"點數\" failed...id:{0}", action.GetObjectId()));
                //                }
                //                System.Threading.Thread.Sleep(1);
                //            }
                //        }
                //        else
                //        {
                //            break;
                //        }
                //    }

                //    while (true)
                //    {
                //        MongoCursor<GMAction> subCollect = mGMActions.Find(Query.EQ("Action", "減少籌碼"));
                //        List<GMAction> subList = subCollect.ToList<GMAction>();
                //        long dataCnt = subList.Count();
                //        if (dataCnt > 0)
                //        {
                //            for (int i = 0; i < dataCnt; i++)
                //            {
                //                GMAction action = subList[i];
                //                action.Action = GMAction.SubUserCoins;
                //                action.Note = action.Note.Replace("籌碼", "點數");
                //                WriteConcernResult ret = mGMActions.Save(action);
                //                if (!ret.Ok)
                //                {
                //                    Log.StoreMsg(string.Format("WinLobbyMongoDB updates sub-\"籌碼\" to \"點數\" failed...id:{0}", action.GetObjectId()));
                //                }
                //                System.Threading.Thread.Sleep(1);
                //            }
                //        }
                //        else
                //        {
                //            break;
                //        }
                //    }

                //    TimeSpan ts = DateTime.UtcNow - t1;
                //    Console.WriteLine("replace key words costs {0} seconds", ts.TotalSeconds);
                //}

                //if (mLobbyUserInfos != null)
                //{
                //    MongoCursor<UserInfo> userCollect = mLobbyUserInfos.FindAll();
                //    List<UserInfo> userList = userCollect.ToList<UserInfo>();
                //    long dataCnt = userList.Count();
                //    for (int i = 0; i < dataCnt; i++)
                //    {
                //        if (userList[i].GM != null &&
                //            userList[i].AgentId != null &&
                //            userList[i].GM.Length > 0 &&
                //            userList[i].AgentId.Length > 0)
                //        {
                //            continue;
                //        }
                //        // update userInfo
                //        userList[i].AgentId = userList[i].AgentId != null ? userList[i].AgentId : "WinLobby";
                //        userList[i].GM = userList[i].GM != null ? userList[i].GM : "WinLobby";
                //        WriteConcernResult ret = mLobbyUserInfos.Save(userList[i]);
                //        if (!ret.Ok)
                //        {
                //            Log.StoreMsg(string.Format("WinLobbyMongoDB updates GM of user-\"{0}\" failed...id:{0}", userList[i].Account));
                //        }
                //    }
                //}

                //// patch lobby, deviceId & machineId in ScoreInfo
                //if (mUserScores != null)
                //{
                //    MongoCursor<ScoreInfo> collect = mUserScores.FindAll();
                //    List<ScoreInfo> dataList = collect.ToList<ScoreInfo>();
                //    for (int i = 0; i < dataList.Count; i++)
                //    {
                //        bool bUpdated = false;
                //        ScoreInfo scoreInfo = dataList[i];
                //        try
                //        {
                //            int index;
                //            string machineName = dataList[i].Machine;

                //            if (scoreInfo.Lobby.Length == 0)
                //            {
                //                index = machineName.LastIndexOf("at");
                //                scoreInfo.Lobby = machineName.Substring(index + 2).Trim();
                //                bUpdated = true;
                //            }

                //            int deviceId = scoreInfo.ArduinoId;
                //            int machineId = scoreInfo.MachineId;
                //            index = machineName.LastIndexOf('(');
                //            machineName = machineName.Substring(index + 1);
                //            index = machineName.IndexOf(')');
                //            machineName = machineName.Substring(0, index);
                //            string[] strParamms = machineName.Split('_');
                //            if (strParamms.Length == 2)
                //            {
                //                deviceId = int.Parse(strParamms[0]);
                //                machineId = int.Parse(strParamms[1]);
                //                if (scoreInfo.ArduinoId != deviceId - 1 ||
                //                    scoreInfo.MachineId != machineId - 1)
                //                {
                //                    scoreInfo.ArduinoId = deviceId - 1;
                //                    scoreInfo.MachineId = machineId - 1;
                //                    bUpdated = true;
                //                }
                //            }
                //        }
                //        catch (Exception ex)
                //        {
                //        }

                //        if (bUpdated)
                //        {
                //            WriteConcernResult ret = mUserScores.Save(scoreInfo);
                //            if (!ret.Ok) Log.StoreMsg(string.Format("WinLobbyMongoDB updates lobby, deviceId & machineId in ScoreInfo:{0} failed...", scoreInfo.GetObjectId()));
                //        }
                //    }
                //}

                //// remove duplicated MachineLog.Summarize in MachineLog
                //if (mMachineLogs != null)
                //{
                //    List<MachineLog> list = new List<MachineLog>();
                //    while (true)
                //    {
                //        list.Clear();
                //        bool bNoErr = true;
                //        string prePlayer = null;
                //        DateTime preCreateDate = new DateTime(0);
                //        MongoCursor<MachineLog> machineCollect = mMachineLogs.Find(Query.EQ("Action", MachineLog.Summarize));
                //        List<MachineLog> machineLogs = machineCollect.ToList<MachineLog>();
                //        long dataCnt = machineLogs.Count();
                //        for (int i = 0; i < dataCnt; i++)
                //        {
                //            MachineLog log = machineLogs[i];
                //            JObject objLog = JObject.Parse(log.Log);
                //            try
                //            {
                //                DateTime createTime = log.CreateTime;
                //                string player = objLog.ContainsKey("player") ? objLog["player"].Value<string>() : null;
                //                if (player != prePlayer ||
                //                    createTime != preCreateDate)
                //                {
                //                    prePlayer = player;
                //                    preCreateDate = createTime;

                //                    if (list.Count > 1)
                //                    {
                //                        bNoErr = false;
                //                        for (int j = 1; j < list.Count; j++)
                //                        {
                //                            MachineLog machineLog = list[j];
                //                            IMongoQuery filter = Query.And(Query.EQ("_id", machineLog._id));
                //                            mMachineLogs.Remove(filter);
                //                        }
                //                        break;
                //                    }
                //                }
                //                else
                //                {
                //                    MachineLog preLog = machineLogs[i - 1];
                //                    MachineLog curLog = machineLogs[i];
                //                    list.Add(log);
                //                }
                //            }
                //            catch (Exception ex)
                //            {

                //            }
                //        }
                //        if (bNoErr) break;
                //    }
                //    if (list.Count > 1)
                //    {
                //        for (int j = 1; j < list.Count; j++)
                //        {
                //            MachineLog machineLog = list[j];
                //            IMongoQuery filter = Query.And(Query.EQ("_id", machineLog._id));
                //            mMachineLogs.Remove(filter);
                //        }
                //    }
                //}

                //if (mMachineLogs != null)
                //{
                //    List<MachineLog> list = new List<MachineLog>();
                //    MongoCursor<MachineLog> machineCollect = mMachineLogs.Find(Query.EQ("Action", MachineLog.Summarize));
                //    List<MachineLog> machineLogs = machineCollect.ToList<MachineLog>();
                //    long dataCnt = machineLogs.Count();
                //    for (int i = 0; i < dataCnt; i++)
                //    {
                //        MachineLog log = machineLogs[i];
                //        JObject objLog = JObject.Parse(log.Log);

                //        string player = objLog.ContainsKey("player") ? objLog["player"].Value<string>() : null;
                //        if (player == "A95")
                //        {
                //            list.Add(log);
                //        }
                //    }
                //}

                //// correct MachineLog.Summarize in MachineLog
                //if (mMachineLogs != null)
                //{
                //    const int iCorrectId = 2;
                //    MongoCursor<MachineLog> machineCollect = mMachineLogs.Find(Query.EQ("Action", MachineLog.Summarize));
                //    List<MachineLog> machineLogs = machineCollect.ToList<MachineLog>();
                //    long dataCnt = machineLogs.Count();
                //    for (int i = 0; i < dataCnt; i++)
                //    {
                //        MachineLog log = machineLogs[i];
                //        JObject objLog = JObject.Parse(log.Log);
                //        int correctId = objLog.ContainsKey("corrected") ? objLog["corrected"].Value<int>() : 0;
                //        // skip if data already corrected
                //        if (correctId == iCorrectId) continue;
                //        // 
                //        try
                //        {
                //            string machineName = objLog.ContainsKey("machine") ? objLog["machine"].Value<string>() : null;
                //            int index = machineName.LastIndexOf('(');
                //            if (index < 0) throw new Exception("error parsing name");

                //            string subStr = machineName.Substring(index + 1);
                //            index = subStr.IndexOf(')');
                //            subStr = subStr.Substring(0, index);
                //            string[] strParams = subStr.Split('_');
                //            log.ArduinoId = int.Parse(strParams[0]) - 1;
                //            log.MachineId = int.Parse(strParams[1]) - 1;
                //        }
                //        catch (Exception ex)
                //        {
                //            if (objLog.ContainsKey("arduinoId")) log.ArduinoId = int.Parse(objLog["arduinoId"].Value<string>());
                //            if (objLog.ContainsKey("machineId")) log.MachineId = int.Parse(objLog["machineId"].Value<string>());
                //        }
                //        objLog["corrected"] = iCorrectId;
                //        log.Log = JsonConvert.SerializeObject(objLog);
                //        WriteConcernResult ret = mMachineLogs.Save(log);
                //        if (!ret.Ok)
                //        {
                //            Log.StoreMsg(string.Format("WinLobbyMongoDB updates log of machine-\"{0}\" failed...", log.GetObjectId()));
                //        }
                //    }
                //}

                //if (mMachineLogs != null)
                //{
                //    DateTime sTime = new DateTime(0);
                //    DateTime eTime = DateTime.Now;
                //    IMongoQuery filter = Query.And(
                //        Query.EQ("Action", MachineLog.Summarize),
                //        Query.GTE("CreateTime", sTime),
                //        Query.LT("CreateTime", eTime)
                //    );
                //    MongoCursor<MachineLog> logCollect = mMachineLogs.Find(filter)
                //                                        .SetSortOrder(SortBy.Descending("CreateTime"));
                //    List<MachineLog> machineLogs = new List<MachineLog>();
                //    machineLogs.AddRange(logCollect.ToList());
                //    // remove wrong log
                //    for (int i = machineLogs.Count-1; i >= 0; i--)
                //    {
                //        //bool bDataModified = false;
                //        MachineLog machineLog = machineLogs[i];
                //        int deviceId = machineLog.ArduinoId;
                //        int machineId = machineLog.MachineId;
                //        if (deviceId < 0 || machineId < 0)
                //        {
                //            filter = Query.And(
                //                Query.EQ("_id", machineLog._id)
                //            );
                //            mMachineLogs.Remove(filter);
                //            System.Threading.Thread.Sleep(1);
                //        }

                //        //JObject obj = JObject.Parse(machineLog.Log);

                //        //if (bDataModified)
                //        //{
                //        //    mMachineLogs.Save(machineLog);
                //        //    System.Threading.Thread.Sleep(1);
                //        //}
                //    }

                //    if (machineLogs.Count > 0 && mUserActions != null)
                //    {
                //        sTime = machineLogs[0].CreateTime;
                //        filter = Query.And(
                //           Query.EQ("Action", UserAction.SummarizeScore),
                //           Query.GT("CreateTime", sTime),
                //           Query.LT("CreateTime", eTime)
                //        );
                //        MongoCursor<UserAction> actionCollect = mUserActions.Find(filter)
                //                                            .SetSortOrder(SortBy.Descending("CreateTime"));
                //        List<UserAction> userActions = new List<UserAction>();
                //        userActions.AddRange(actionCollect.ToList());
                //        userActions.Reverse();

                //        for (int i = 0; i < userActions.Count; i++)
                //        {
                //            UserAction userAction = userActions[i];
                //            JObject obj = JObject.Parse(userAction.Note);

                //            int deviceId = -1;
                //            int machineId = -1;
                //            try
                //            {
                //                string machineName = obj["Machine"].Value<string>();
                //                int beginIndex = machineName.LastIndexOf('(');
                //                int endIndex = machineName.LastIndexOf(')');
                //                string tmp = machineName.Substring(beginIndex+1, endIndex - (beginIndex+1));
                //                string[] strParams = tmp.Split('_');
                //                deviceId = int.Parse(strParams[0]);
                //                machineId = int.Parse(strParams[1]);
                //            }
                //            catch(Exception ex)
                //            {

                //            }

                //            string lobbyName = obj["LobbyName"].Value<string>();
                //            string machine = obj["Machine"].Value<string>();
                //            long iTotalInScore = obj["TotalInScore"].Value<long>();
                //            long iTotalOutScore = obj["TotalOutScore"].Value<long>();
                //            JObject logObj = new JObject();
                //            logObj["player"] = obj["User"].Value<string>();
                //            logObj["action"] = MachineLog.Summarize;
                //            logObj["lobby"] = lobbyName;
                //            logObj["machine"] = machine;
                //            logObj["trial"] = obj.ContainsKey("Trial") ? obj["Trial"].Value<bool>() : false;
                //            logObj["arduinoId"] = deviceId;
                //            logObj["machineId"] = machineId;
                //            logObj["in"] = iTotalInScore;
                //            logObj["out"] = iTotalOutScore;
                //            logObj["total"] = iTotalInScore - iTotalOutScore;

                //            MachineLog machineLog = new MachineLog();
                //            machineLog.Action = MachineLog.Summarize;
                //            machineLog.LobbyName = lobbyName;
                //            machineLog.ArduinoId = deviceId;
                //            machineLog.MachineId = machineId;
                //            machineLog.Log = JsonConvert.SerializeObject(logObj);
                //            machineLog.CreateTime = userAction.CreateTime;

                //            WriteConcernResult ret = mMachineLogs.Insert(machineLog);
                //            System.Threading.Thread.Sleep(1);
                //        }
                //    }
                //}

                //int iNoFoundLogCnt = 0;
                //if (mUserActions != null)
                //{
                //    MongoCursor<MachineLog> machineCollect = mMachineLogs.Find(Query.EQ("Action", MachineLog.Summarize));
                //    List<MachineLog> machineLogs = machineCollect.ToList<MachineLog>();

                //    MongoCursor<UserAction> collect = mUserActions.Find(Query.EQ("Action", UserAction.SummarizeScore));
                //    List<UserAction> userActions = collect.ToList<UserAction>();
                //    for (int i = 0; i < userActions.Count; i++)
                //    {
                //        UserAction userAction = userActions[i];
                //        JObject scoreObj = JObject.Parse(userAction.Note);
                //        // skip if already data is corrected
                //        if (scoreObj.ContainsKey("ArduinoId") &&
                //            scoreObj.ContainsKey("MachineId")) continue;

                //        string user = scoreObj["User"].Value<string>();
                //        string userId = scoreObj["UserId"].Value<string>();
                //        string lobbyName = scoreObj["LobbyName"].Value<string>();
                //        string machine = scoreObj["Machine"].Value<string>();
                //        long iTotalInScore = scoreObj["TotalInScore"].Value<long>();
                //        long iTotalOutScore = scoreObj["TotalOutScore"].Value<long>();
                //        DateTime sTime = scoreObj["BeginTime"].Value<DateTime>();
                //        DateTime eTime = scoreObj["EndTime"].Value<DateTime>();

                //        int index = machine.LastIndexOf('(');
                //        string strTmp = machine.Substring(index + 1);
                //        index = strTmp.IndexOf(')');
                //        strTmp = strTmp.Substring(0, index);
                //        string[] strParams = strTmp.Split('_');
                //        int deviceId = int.Parse(strParams[0]) - 1;
                //        int machineId = int.Parse(strParams[1]) - 1;
                //        scoreObj["ArduinoId"] = deviceId;
                //        scoreObj["MachineId"] = machineId;

                //        // locate machineLog
                //        List<MachineLog> logs = new List<MachineLog>();
                //        for (int j = 0; j < machineLogs.Count; j++)
                //        {
                //            JObject objLog = JObject.Parse(machineLogs[j].Log);
                //            if (objLog["player"].Value<string>() == user &&
                //                objLog["machine"].Value<string>() == machine &&
                //                objLog["in"].Value<int>() == iTotalInScore &&
                //                objLog["out"].Value<int>() == iTotalOutScore)
                //            {
                //                DateTime time1 = userAction.CreateTime.ToLocalTime();
                //                DateTime time2 = machineLogs[j].CreateTime.ToLocalTime();
                //                TimeSpan ts = time1 - time2;
                //                if (ts.TotalSeconds < 2 &&
                //                    ts.TotalSeconds > -2)
                //                {
                //                    logs.Add(machineLogs[j]);
                //                }
                //            }
                //        }
                //        MachineLog machineLog = null;
                //        if (logs.Count > 0)
                //        {
                //            machineLog = logs[0];
                //            //// remove extra log
                //            //if (logs.Count > 1)
                //            //{
                //            //    for (int j = 1; j < logs.Count; j++)
                //            //    {
                //            //        IMongoQuery filter2 = Query.And(
                //            //            Query.EQ("_id", logs[j]._id)
                //            //        );
                //            //        mMachineLogs.Remove(filter2);
                //            //        System.Threading.Thread.Sleep(1);
                //            //    }
                //            //    machineCollect = mMachineLogs.Find(Query.EQ("Action", MachineLog.Summarize));
                //            //    machineLogs = machineCollect.ToList<MachineLog>();
                //            //}
                //        }
                //        //else
                //        //{
                //        //    for (int j = 0; j < machineLogs.Count; j++)
                //        //    {
                //        //        JObject objLog = JObject.Parse(machineLogs[j].Log);
                //        //        if (objLog["player"].Value<string>() != user)
                //        //            continue;
                //        //        if (objLog["machine"].Value<string>() != machine)
                //        //            continue;
                //        //        if (objLog["in"].Value<int>() != iTotalInScore)
                //        //            continue;
                //        //        if (objLog["out"].Value<int>() != iTotalOutScore)
                //        //            continue;
                //        //    }
                //        //}

                //        // recal iTotalInScore & iTotalOutScore
                //        sTime = sTime.ToUniversalTime();
                //        eTime = eTime.ToUniversalTime();
                //        IMongoQuery filter = Query.And(
                //            Query.EQ("UserId", userId),
                //            Query.GTE("CreateTime", sTime),
                //            Query.LT("CreateTime", eTime)
                //        );
                //        MongoCursor<ScoreInfo> scoreCollect = mUserScores.Find(filter)
                //                                            .SetSortOrder(SortBy.Descending("CreateTime"));

                //        long iTotalInScore2 = 0;
                //        long iTotalOutScore2 = 0;
                //        List<ScoreInfo> scoreList = scoreCollect.ToList();
                //        for (int j = 0; j < scoreList.Count; j++)
                //        {
                //            ScoreInfo scoreInfo = scoreList[j];
                //            if (scoreInfo.ArduinoId == deviceId &&
                //                scoreInfo.MachineId == machineId)
                //            {
                //                iTotalInScore2 += scoreInfo.TotalInScore;
                //                iTotalOutScore2 += scoreInfo.TotalOutScore;
                //            }
                //        }

                //        // update note content
                //        scoreObj["TotalInScore"] = iTotalInScore2;
                //        scoreObj["TotalOutScore"] = iTotalOutScore2;
                //        userAction.Note = JsonConvert.SerializeObject(scoreObj);
                //        mUserActions.Save(userAction);

                //        if (machineLog != null)
                //        {
                //            JObject objLog = JObject.Parse(machineLog.Log);
                //            objLog["arduinoId"] = machineLog.ArduinoId;
                //            objLog["machineId"] = machineLog.MachineId;
                //            objLog["in"] = iTotalInScore2;
                //            objLog["out"] = iTotalOutScore2;
                //            objLog["total"] = iTotalInScore2 - iTotalOutScore2;
                //            machineLog.Log = JsonConvert.SerializeObject(objLog);
                //            mMachineLogs.Save(machineLog);
                //        }
                //        else
                //        {
                //            // maybe the bug of recording wrong JObject to machineLog, so no data in MachineLog
                //            iNoFoundLogCnt++;
                //        }
                //        System.Threading.Thread.Sleep(0);
                //    }
                //}
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public DateTime GetTimeFromServer()
        {
            // ref from https://stackoverflow.com/questions/14149346/what-value-should-i-pass-into-timezoneinfo-findsystemtimezonebyidstring
            const string ETimeZone_Taipei = "Taipei Standard Time";
            //const string ETimeZone_Tokyo = "Tokyo Standard Time";
            //const string ETimeZone_Singapore = "Singapore Standard Time";

            DateTime utcTime = DateTime.UtcNow;
            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(ETimeZone_Taipei);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, localTimeZone);
            return localTime;
        }

        public AgentInfo QueryAgent()
        {
            AgentInfo? agentInfo = null;
            try
            {
                if (mAgentInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                FilterDefinition<AgentInfo> filter = Builders<AgentInfo>.Filter.Eq("Validate", true);
                IFindFluent<AgentInfo, AgentInfo> collect = mAgentInfos.Find(filter);
                List<AgentInfo> agentInfos = collect.ToList();
                // if no agentInfo, delay for a while and try finding again
                if (agentInfos.Count == 0)
                {
                    Thread.Sleep(1000);
                    collect = mAgentInfos.Find(filter);
                    agentInfos = collect.ToList();
                }

                if (agentInfos.Count == 0)
                {
                    agentInfo = new AgentInfo("WinLobby", "WinLobby");
                    mAgentInfos.InsertOne(agentInfo);
                }
                else
                {
                    // should be only one agent in one db
                    agentInfo = agentInfos[0];
                    // remove others
                    for (int i = agentInfos.Count - 1; i >= 1; i--)
                    {
                        AgentInfo tmpInfo = agentInfos[i];
                        FilterDefinition<AgentInfo> filter2 = Builders<AgentInfo>.Filter.Eq("_id", tmpInfo._id);
                        mAgentInfos.DeleteOne(filter2);
                    }
                }

                // make sure property SMSInfo & UserLevels exists
                Dictionary<string, string> dicUpdate = new Dictionary<string, string>();
                if (agentInfo.SMSInfo == null || agentInfo.SMSInfo.Length == 0)
                {
                    JObject obj = new JObject();
                    obj["UID"] = "";
                    obj["PWD"] = "";
                    obj["Template"] = "{0}";
                    // update SMSInfo in agentInfo
                    agentInfo.SMSInfo = JsonConvert.SerializeObject(obj);
                    dicUpdate["SMSInfo"] = agentInfo.SMSInfo;
                }
                JObject objSMSInfo = JObject.Parse(agentInfo.SMSInfo);
                if (objSMSInfo.ContainsKey("Template") == false)
                {
                    // assign Template
                    objSMSInfo["Template"] = "{0}";
                    // update SMSInfo in agentInfo
                    agentInfo.SMSInfo = JsonConvert.SerializeObject(objSMSInfo);
                    dicUpdate["SMSInfo"] = agentInfo.SMSInfo;
                }
                if (objSMSInfo.ContainsKey("Providers") == false)
                {
                    JObject obj = new JObject();
                    obj["uid"] = objSMSInfo["UID"].Value<string>();
                    obj["pwd"] = objSMSInfo["PWD"].Value<string>();
                    // add sms setting to array
                    JObject objProvider = new JObject();
                    objProvider["every8d"] = obj;
                    // assign Providers
                    objSMSInfo["Providers"] = objProvider;
                    // update SMSInfo in agentInfo
                    agentInfo.SMSInfo = JsonConvert.SerializeObject(objSMSInfo);
                    dicUpdate["SMSInfo"] = agentInfo.SMSInfo;
                }
                if (agentInfo.SetAfterVerifyUser == null || agentInfo.SetAfterVerifyUser.Length == 0)
                {
                    JObject obj = new JObject();
                    obj["Verified"] = true;
                    obj["GuoZhaoEnabled"] = true;
                    // update SMSInfo in agentInfo
                    agentInfo.SetAfterVerifyUser = JsonConvert.SerializeObject(obj);
                    dicUpdate["SetAfterVerifyUser"] = agentInfo.SetAfterVerifyUser;
                }
                if (agentInfo.UserLevels == null || agentInfo.UserLevels.Length == 0)
                {
                    JObject objUserLevels = new JObject();
                    JObject objLevel0 = new JObject();
                    objLevel0["level"] = "銀卡";
                    objLevel0["maxPay"] = 2000;
                    objLevel0["bonusRate"] = 0;
                    objUserLevels[UserInfo.Permission_VIP0.ToString()] = objLevel0;
                    JObject objLevel1 = new JObject();
                    objLevel1["level"] = "金卡";
                    objLevel1["maxPay"] = 5000;
                    objLevel0["bonusRate"] = 0;
                    objUserLevels[UserInfo.Permission_VIP1.ToString()] = objLevel1;
                    JObject objLevel2 = new JObject();
                    objLevel2["level"] = "鉑金卡";
                    objLevel2["maxPay"] = 10000;
                    objLevel0["bonusRate"] = 0;
                    objUserLevels[UserInfo.Permission_VIP2.ToString()] = objLevel2;
                    // update UserLevels in agentInfo
                    agentInfo.UserLevels = JsonConvert.SerializeObject(objUserLevels);
                    dicUpdate["UserLevels"] = agentInfo.UserLevels;
                }
                else
                {
                    Dictionary<string, JObject> dicUserLevels = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(agentInfo.UserLevels);
                    foreach (var item in dicUserLevels)
                    {
                        JObject obj = item.Value;
                        if (obj.ContainsKey("bonusRate") == false)
                        {
                            obj["bonusRate"] = 0;
                            // update UserLevels in agentInfo
                            agentInfo.UserLevels = JsonConvert.SerializeObject(dicUserLevels);
                            dicUpdate["UserLevels"] = agentInfo.UserLevels;
                        }
                    }
                }
                if (dicUpdate.Count > 0)
                {
                    FilterDefinition<AgentInfo> filter2 = Builders<AgentInfo>.Filter.Eq("_id", new ObjectId(agentInfo.GetObjectId()));
                    // update scores of userInfo
                    List<UpdateDefinition<AgentInfo>> updates = new List<UpdateDefinition<AgentInfo>>();
                    foreach (var item in dicUpdate)
                    {
                        updates.Add(Builders<AgentInfo>.Update.Set(item.Key, item.Value));
                    }
                    UpdateResult ret = mAgentInfos.UpdateOne(filter2, Builders<AgentInfo>.Update.Combine(updates));
                }
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("QueryAgent", ex.Message);
                Log.StoreMsg(errMsg);
            }
            mAgent = agentInfo;
            return agentInfo;
        }

        public JObject SaveAgent(AgentInfo agentInfo)
        {
            JObject resObj = new JObject();
            try
            {
                if (mAgentInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (agentInfo == null) throw new Exception("agentInfo為空值");

                FilterDefinition<AgentInfo> filter = Builders<AgentInfo>.Filter.Eq("_id", agentInfo._id);
                List<AgentInfo> tgtAgentInfos = mAgentInfos.Find(filter).ToList();
                AgentInfo tgtAgentInfo = tgtAgentInfos.Count > 0 ? tgtAgentInfos[0] : null;
                if (tgtAgentInfo == null) throw new Exception("agentInfo為空值");

                List<UpdateDefinition<AgentInfo>> updates = new List<UpdateDefinition<AgentInfo>>();
                if (tgtAgentInfo.Domain != agentInfo.Domain) updates.Add(Builders<AgentInfo>.Update.Set("Domain", agentInfo.Domain));
                if (tgtAgentInfo.ScoreRatioToNTD != agentInfo.ScoreRatioToNTD) updates.Add(Builders<AgentInfo>.Update.Set("ScoreRatioToNTD", agentInfo.ScoreRatioToNTD));
                if (tgtAgentInfo.MultiLogin != agentInfo.MultiLogin) updates.Add(Builders<AgentInfo>.Update.Set("MultiLogin", agentInfo.MultiLogin));
                if (tgtAgentInfo.UserNoHeartbeatAckKickout != agentInfo.UserNoHeartbeatAckKickout) updates.Add(Builders<AgentInfo>.Update.Set("UserNoHeartbeatAckKickout", agentInfo.UserNoHeartbeatAckKickout));
                if (tgtAgentInfo.FinalizeTimeOut != agentInfo.FinalizeTimeOut) updates.Add(Builders<AgentInfo>.Update.Set("FinalizeTimeOut", agentInfo.FinalizeTimeOut));
                if (tgtAgentInfo.PeriodMaxGuoZhaoCnt != agentInfo.MaxDailyGuoZhaoCnt) updates.Add(Builders<AgentInfo>.Update.Set("PeriodMaxGuoZhaoCnt", agentInfo.PeriodMaxGuoZhaoCnt));
                if (tgtAgentInfo.GuoZhaoPeriods != agentInfo.GuoZhaoPeriods) updates.Add(Builders<AgentInfo>.Update.Set("GuoZhaoPeriods", agentInfo.GuoZhaoPeriods));
                if (tgtAgentInfo.InScoreBonusBase != agentInfo.InScoreBonusBase) updates.Add(Builders<AgentInfo>.Update.Set("InScoreBonusBase", agentInfo.InScoreBonusBase));
                if (tgtAgentInfo.InScoreBonusFormal != agentInfo.InScoreBonusFormal) updates.Add(Builders<AgentInfo>.Update.Set("InScoreBonusFormal", agentInfo.InScoreBonusFormal));
                if (tgtAgentInfo.InScoreBonusTrial != agentInfo.InScoreBonusTrial) updates.Add(Builders<AgentInfo>.Update.Set("InScoreBonusTrial", agentInfo.InScoreBonusTrial));
                if (tgtAgentInfo.CanExchangeTrialToFormal != agentInfo.CanExchangeTrialToFormal) updates.Add(Builders<AgentInfo>.Update.Set("CanExchangeTrialToFormal", agentInfo.CanExchangeTrialToFormal));
                if (tgtAgentInfo.CanExchangeFormalToTrial != agentInfo.CanExchangeFormalToTrial) updates.Add(Builders<AgentInfo>.Update.Set("CanExchangeFormalToTrial", agentInfo.CanExchangeFormalToTrial));
                if (tgtAgentInfo.ExchangeRatio != agentInfo.ExchangeRatio) updates.Add(Builders<AgentInfo>.Update.Set("ExchangeRatio", agentInfo.ExchangeRatio));
                if (tgtAgentInfo.ExchangeMinLeft != agentInfo.ExchangeMinLeft) updates.Add(Builders<AgentInfo>.Update.Set("ExchangeMinLeft", agentInfo.ExchangeMinLeft));
                if (tgtAgentInfo.ExchangeReceiverRate != agentInfo.ExchangeReceiverRate) updates.Add(Builders<AgentInfo>.Update.Set("ExchangeReceiverRate", agentInfo.ExchangeReceiverRate));
                if (tgtAgentInfo.GiftTaxRate != agentInfo.GiftTaxRate) updates.Add(Builders<AgentInfo>.Update.Set("GiftTaxRate", agentInfo.GiftTaxRate));
                if (tgtAgentInfo.MaxPlayerScore != agentInfo.MaxPlayerScore) updates.Add(Builders<AgentInfo>.Update.Set("MaxPlayerScore", agentInfo.MaxPlayerScore));
                if (tgtAgentInfo.DailyAward != agentInfo.DailyAward) updates.Add(Builders<AgentInfo>.Update.Set("DailyAward", agentInfo.DailyAward));
                if (tgtAgentInfo.AwardType != agentInfo.AwardType) updates.Add(Builders<AgentInfo>.Update.Set("AwardType", agentInfo.AwardType));
                if (tgtAgentInfo.MaxDailyAward != agentInfo.MaxDailyAward) updates.Add(Builders<AgentInfo>.Update.Set("MaxDailyAward", agentInfo.MaxDailyAward));
                if (tgtAgentInfo.AwardDuration != agentInfo.AwardDuration) updates.Add(Builders<AgentInfo>.Update.Set("AwardDuration", agentInfo.AwardDuration));
                if (tgtAgentInfo.StopAwardAt != agentInfo.StopAwardAt) updates.Add(Builders<AgentInfo>.Update.Set("StopAwardAt", agentInfo.StopAwardAt));
                if (tgtAgentInfo.VerifyUserInfo != agentInfo.VerifyUserInfo) updates.Add(Builders<AgentInfo>.Update.Set("VerifyUserInfo", agentInfo.VerifyUserInfo));
                if (tgtAgentInfo.MachineStopGuoZhaoTEMP != agentInfo.MachineStopGuoZhaoTEMP) updates.Add(Builders<AgentInfo>.Update.Set("MachineStopGuoZhaoTEMP", agentInfo.MachineStopGuoZhaoTEMP));
                if (tgtAgentInfo.WainZhuMinCnt != agentInfo.WainZhuMinCnt) updates.Add(Builders<AgentInfo>.Update.Set("WainZhuMinCnt", agentInfo.WainZhuMinCnt));
                if (tgtAgentInfo.WainZhuMaxCnt != agentInfo.WainZhuMaxCnt) updates.Add(Builders<AgentInfo>.Update.Set("WainZhuMaxCnt", agentInfo.WainZhuMaxCnt));
                if (tgtAgentInfo.MinExchangeBetReward != agentInfo.MinExchangeBetReward) updates.Add(Builders<AgentInfo>.Update.Set("MinExchangeBetReward", agentInfo.MinExchangeBetReward));
                if (tgtAgentInfo.MaxExchangeBetReward != agentInfo.MaxExchangeBetReward) updates.Add(Builders<AgentInfo>.Update.Set("MaxExchangeBetReward", agentInfo.MaxExchangeBetReward));
                if (tgtAgentInfo.EnableUserInfoSortIndex != agentInfo.EnableUserInfoSortIndex) updates.Add(Builders<AgentInfo>.Update.Set("EnableUserInfoSortIndex", agentInfo.EnableUserInfoSortIndex));
                if (tgtAgentInfo.FrontEndVersion != agentInfo.FrontEndVersion) updates.Add(Builders<AgentInfo>.Update.Set("FrontEndVersion", agentInfo.FrontEndVersion));
                if (tgtAgentInfo.ShowBetReward != agentInfo.ShowBetReward) updates.Add(Builders<AgentInfo>.Update.Set("ShowBetReward", agentInfo.ShowBetReward));
                if (tgtAgentInfo.ShowPlayerLevel != agentInfo.ShowPlayerLevel) updates.Add(Builders<AgentInfo>.Update.Set("ShowPlayerLevel", agentInfo.ShowPlayerLevel));
                if (tgtAgentInfo.UserLevels != agentInfo.UserLevels) updates.Add(Builders<AgentInfo>.Update.Set("UserLevels", agentInfo.UserLevels));
                if (tgtAgentInfo.DefUserAwardEnable != agentInfo.DefUserAwardEnable) updates.Add(Builders<AgentInfo>.Update.Set("DefUserAwardEnable", agentInfo.DefUserAwardEnable));
                
                if (tgtAgentInfo.SMSInfo == null ||
                    tgtAgentInfo.SMSInfo.Length == 0)
                {
                    JObject obj = new JObject();
                    obj["UID"] = "";
                    obj["PWD"] = "";
                    updates.Add(Builders<AgentInfo>.Update.Set("SMSInfo", JsonConvert.SerializeObject(obj)));
                }
                if (tgtAgentInfo.SetAfterVerifyUser == null ||
                    tgtAgentInfo.SetAfterVerifyUser.Length == 0)
                {
                    JObject obj = new JObject();
                    obj["Verified"] = true;
                    obj["GuoZhaoEnabled"] = true;
                    updates.Add(Builders<AgentInfo>.Update.Set("SetAfterVerifyUser", JsonConvert.SerializeObject(obj)));
                }
                //
                if (updates.Count > 0)
                {
                    UpdateResult ret = mAgentInfos.UpdateOne(filter, Builders<AgentInfo>.Update.Combine(updates));
                }
                // requery agentInfo from db
                List<AgentInfo> agentInfos = mAgentInfos.Find(filter).ToList();
                agentInfo = agentInfos[0];
                if (agentInfo.Account == mAgent.Account) mAgent = agentInfo;
                string strContent = JsonConvert.SerializeObject(agentInfo);
                resObj["status"] = "success";
                resObj["agentInfo"] = JObject.Parse(strContent);
            }
            catch (Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = BuildErrMsg("SaveAgent", ex.Message);
            }
            return resObj;
        }

        public JObject CreateUser(string gmAccount, string userAccount, string password,
            string nickName, string phoneNo, DateTime birthday, string note,
            bool bGiftEnabled, int maxDailyReceivedGolds, string avatarInfo)
        {
            JObject resObj = new JObject();
            try
            {
                if (mLobbyGMInfos == null || mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (userAccount == null || password == null) throw new Exception("帳號或密碼不正確");
                for (int i = 0; i < phoneNo.Length; i++)
                {
                    if (phoneNo[i] != '0' && phoneNo[i] != '1' && phoneNo[i] != '2' && phoneNo[i] != '3' && phoneNo[i] != '4' &&
                        phoneNo[i] != '5' && phoneNo[i] != '6' && phoneNo[i] != '7' && phoneNo[i] != '8' && phoneNo[i] != '9')
                    {
                        throw new Exception("電話僅接受數字");
                    }
                }

                List<FilterDefinition<GMInfo>> gmFilters = new List<FilterDefinition<GMInfo>>();
                gmFilters.Add(Builders<GMInfo>.Filter.Eq("Account", gmAccount));
                gmFilters.Add(Builders<GMInfo>.Filter.Eq("Validate", true));
                FilterDefinition<GMInfo> findFilter = Builders<GMInfo>.Filter.And(gmFilters);
                List<GMInfo> gmInfos = mLobbyGMInfos.Find(findFilter).ToList();
                if (gmInfos.Count == 0) throw new Exception(string.Format("無效的GM帳號:{0}", gmAccount));
                GMInfo gmInfo = gmInfos[0];
                if ((gmInfo.Permission & GMInfo.PERMISSION.CreateUserInfo) == 0x0)
                {
                    throw new Exception(string.Format("管理者:{0}不具備建立用戶權限", gmAccount));
                }

                List<UserInfo> userInfos = new List<UserInfo>();
                int sortIndex = 0;
                AgentInfo agentInfo = this.mAgent;
                if (agentInfo.EnableUserInfoSortIndex)
                {
                    FilterDefinition<UserInfo> filterUser = Builders<UserInfo>.Filter.Empty;
                    List<UserInfo> results = mLobbyUserInfos.Find(filterUser).ToList();
                    foreach (UserInfo info in results)
                    {
                        if (info.Account == userAccount) userInfos.Add(info);
                        else sortIndex = sortIndex > info.SortIndex ? sortIndex : info.SortIndex;
                    }
                    sortIndex++;
                }
                else
                {
                    List<FilterDefinition<UserInfo>> userFilters = new List<FilterDefinition<UserInfo>>();
                    userFilters.Add(Builders<UserInfo>.Filter.Eq("Account", userAccount));
                    FilterDefinition<UserInfo> filterUser = Builders<UserInfo>.Filter.And(userFilters);
                    userInfos = mLobbyUserInfos.Find(filterUser).ToList();
                }

                bool bUserAwardEnable = false;
                if (mAgent != null)
                    bUserAwardEnable = mAgent.DefUserAwardEnable != null ? mAgent.DefUserAwardEnable : true;
                UserInfo userInfo = userInfos.Count > 0 ? userInfos[0] : null;
                if (userInfo != null)
                {
                    if (userInfo.Validate) throw new Exception(string.Format("用戶{0}已存在", userAccount));
                    JObject obj = new JObject();
                    obj["AgentId"] = mAgent.Account;
                    obj["GM"] = gmAccount;
                    obj["Password"] = password;
                    obj["NickName"] = nickName;
                    obj["LoginErrCnt"] = 0;
                    obj["PhoneNo"] = phoneNo;
                    obj["Birthday"] = birthday;
                    obj["Permission"] = UserInfo.Permission_VIP0;
                    obj["Note"] = note;
                    obj["InCoins"] = 0;
                    obj["OutCoins"] = 0;
                    obj["InTrialCoins"] = 0;
                    obj["OutTrialCoins"] = 0;
                    obj["InGuoZhaoCoins"] = 0;
                    obj["OutGuoZhaoCoins"] = 0;
                    obj["GiftEnabled"] = bGiftEnabled;
                    obj["UserAwardEnable"] = bUserAwardEnable;
                    obj["MaxDailyReceivedGolds"] = maxDailyReceivedGolds;
                    obj["LastUpdateDate"] = DateTime.Now;
                    obj["Validate"] = true;
                    obj["Verified"] = false;
                    obj["GuoZhaoEnabled"] = false;
                    obj["Blocked"] = false;
                    obj["AvatarInfo"] = avatarInfo;
                    obj["Token"] = "";
                    obj["TokenTime"] = Utils.zeroDateTime();
                    obj["LoginAwardDate"] = Utils.zeroDateTime();
                    obj["PaiedAward"] = 0;
                    obj["DailyReceivedGoldsDate"] = Utils.zeroDateTime();
                    obj["DailyReceivedGolds"] = 0;
                    obj["MaxDailyReceivedGolds"] = 0;
                    obj["GuoZhaoCnt"] = 0;
                    obj["GuoZhaoInfo"] = "";
                    obj["LastGuoZhaoDate"] = Utils.zeroDateTime();
                    obj["BindedMachines"] = "";
                    obj["BetReward"] = 0;
                    obj["SortIndex"] = sortIndex;
                    string errMsg = this.UpdateUser(userAccount, obj);
                    if (errMsg != null) throw new Exception(errMsg);
                    // since UpdateUser won't update CreateDate, so modify it here...
                    List<UpdateDefinition<UserInfo>> updates = new List<UpdateDefinition<UserInfo>>();
                    updates.Add(Builders<UserInfo>.Update.Set("CreateDate", DateTime.UtcNow));
                    List<FilterDefinition<UserInfo>> userFilters = new List<FilterDefinition<UserInfo>>();
                    userFilters.Add(Builders<UserInfo>.Filter.Eq("Account", userAccount));
                    FilterDefinition<UserInfo> filterUser = Builders<UserInfo>.Filter.And(userFilters);
                    UpdateResult ret = mLobbyUserInfos.UpdateMany(filterUser, Builders<UserInfo>.Update.Combine(updates));
                }
                else
                {
                    userInfo = new UserInfo();
                    userInfo.AgentId = mAgent.Account;
                    userInfo.GM = gmAccount;
                    userInfo.Account = userAccount;
                    userInfo.Password = password;
                    userInfo.NickName = nickName;
                    userInfo.PhoneNo = phoneNo;
                    userInfo.Birthday = birthday;
                    userInfo.Note = note;
                    userInfo.GiftEnabled = bGiftEnabled;
                    userInfo.UserAwardEnable = bUserAwardEnable;
                    userInfo.MaxDailyReceivedGolds = maxDailyReceivedGolds;
                    userInfo.CreateDate = DateTime.Now;
                    userInfo.LastUpdateDate = DateTime.Now;
                    userInfo.Validate = true;
                    userInfo.AvatarInfo = avatarInfo;
                    userInfo.SortIndex = sortIndex;
                    mLobbyUserInfos.InsertOne(userInfo);
                }

                string strContent = JsonConvert.SerializeObject(userInfo);
                resObj["status"] = "success";
                resObj["userInfo"] = JObject.Parse(strContent);
            }
            catch(Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = BuildErrMsg("CreateUser", ex.Message);
            }
            return resObj;
        }

        public string UpdateUser(string account, JObject objParams)
        {
            string? errMsg = null;
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (account == null) throw new Exception("account為空值");
                if (objParams == null) throw new Exception("objParams為空值");

                List<FilterDefinition<UserInfo>> userFilters = new List<FilterDefinition<UserInfo>>();
                userFilters.Add(Builders<UserInfo>.Filter.Eq("Account", account));
                FilterDefinition<UserInfo> findFilter = Builders<UserInfo>.Filter.And(userFilters);
                List<UserInfo> userInfos = mLobbyUserInfos.Find(findFilter).ToList();
                UserInfo userInfo = userInfos.Count > 0 ? userInfos[0] : null;
                if (userInfo == null) throw new Exception("查詢的用戶不存在");
                // if userInfo.Password == userInfo.PhoneNo, it maybe GM reset password,
                // so clear LoginErrCnt 
                if (userInfo.Password == userInfo.PhoneNo) objParams["LoginErrCnt"] = 0;

                List<UpdateDefinition<UserInfo>> updates = new List<UpdateDefinition<UserInfo>>();

                // update all data of userInfo but formal, trial, guoZhao in/out coins
                updates.Add(Builders<UserInfo>.Update.Set("LastUpdateDate", DateTime.UtcNow));
                if (objParams.ContainsKey("Password")) updates.Add(Builders<UserInfo>.Update.Set("Password", objParams["Password"].Value<string>()));
                if (objParams.ContainsKey("Permission") && userInfo.Permission != UserInfo.Permission_Engineer) updates.Add(Builders<UserInfo>.Update.Set("Permission", objParams["Permission"].Value<int>()));
                if (objParams.ContainsKey("NickName")) updates.Add(Builders<UserInfo>.Update.Set("NickName", objParams["NickName"].Value<string>()));
                if (objParams.ContainsKey("Birthday")) updates.Add(Builders<UserInfo>.Update.Set("Birthday", Convert.ToDateTime(objParams["Birthday"])));
                if (objParams.ContainsKey("InCoins")) updates.Add(Builders<UserInfo>.Update.Set("InCoins", objParams["InCoins"].Value<Int64>()));
                if (objParams.ContainsKey("OutCoins")) updates.Add(Builders<UserInfo>.Update.Set("OutCoins", objParams["OutCoins"].Value<Int64>()));
                if (objParams.ContainsKey("InTrialCoins")) updates.Add(Builders<UserInfo>.Update.Set("InTrialCoins", objParams["InTrialCoins"].Value<Int64>()));
                if (objParams.ContainsKey("OutTrialCoins")) updates.Add(Builders<UserInfo>.Update.Set("OutTrialCoins", objParams["OutTrialCoins"].Value<Int64>()));
                if (objParams.ContainsKey("InGuoZhaoCoins")) updates.Add(Builders<UserInfo>.Update.Set("InGuoZhaoCoins", objParams["InGuoZhaoCoins"].Value<Int64>()));
                if (objParams.ContainsKey("OutGuoZhaoCoins")) updates.Add(Builders<UserInfo>.Update.Set("OutGuoZhaoCoins", objParams["OutGuoZhaoCoins"].Value<Int64>()));
                if (objParams.ContainsKey("LoginErrCnt")) updates.Add(Builders<UserInfo>.Update.Set("LoginErrCnt", objParams["LoginErrCnt"].Value<int>()));
                if (objParams.ContainsKey("PhoneNo")) updates.Add(Builders<UserInfo>.Update.Set("PhoneNo", objParams["PhoneNo"].Value<string>()));
                if (objParams.ContainsKey("Note")) updates.Add(Builders<UserInfo>.Update.Set("Note", objParams["Note"].Value<string>()));
                if (objParams.ContainsKey("GiftEnabled")) updates.Add(Builders<UserInfo>.Update.Set("GiftEnabled", objParams["GiftEnabled"].Value<bool>()));
                if (objParams.ContainsKey("UserAwardEnable")) updates.Add(Builders<UserInfo>.Update.Set("UserAwardEnable", objParams["UserAwardEnable"].Value<bool>()));
                if (objParams.ContainsKey("Validate")) updates.Add(Builders<UserInfo>.Update.Set("Validate", objParams["Validate"].Value<bool>()));
                if (objParams.ContainsKey("Verified")) updates.Add(Builders<UserInfo>.Update.Set("Verified", objParams["Verified"].Value<bool>()));
                if (objParams.ContainsKey("GuoZhaoEnabled")) updates.Add(Builders<UserInfo>.Update.Set("GuoZhaoEnabled", objParams["GuoZhaoEnabled"].Value<bool>()));
                if (objParams.ContainsKey("Blocked")) updates.Add(Builders<UserInfo>.Update.Set("Blocked", objParams["Blocked"].Value<bool>()));
                if (objParams.ContainsKey("UniPlayInfo")) updates.Add(Builders<UserInfo>.Update.Set("UniPlayInfo", objParams["UniPlayInfo"].Value<string>()));
                if (objParams.ContainsKey("AvatarInfo")) updates.Add(Builders<UserInfo>.Update.Set("AvatarInfo", objParams["AvatarInfo"].Value<string>()));
                if (objParams.ContainsKey("Token"))
                {
                    string token = objParams["Token"].Value<string>();
                    if (token == null) token = "";
                    updates.Add(Builders<UserInfo>.Update.Set("Token", token));
                    updates.Add(Builders<UserInfo>.Update.Set("TokenTime", token.Length > 0 ? DateTime.Now : Utils.zeroDateTime()));
                }
                if (objParams.ContainsKey("LoginAwardDate")) updates.Add(Builders<UserInfo>.Update.Set("LoginAwardDate", Convert.ToDateTime(objParams["LoginAwardDate"])));
                if (objParams.ContainsKey("PaiedAward")) updates.Add(Builders<UserInfo>.Update.Set("PaiedAward", objParams["PaiedAward"].Value<int>()));
                if (objParams.ContainsKey("DailyReceivedGoldsDate")) updates.Add(Builders<UserInfo>.Update.Set("DailyReceivedGoldsDate", Convert.ToDateTime(objParams["DailyReceivedGoldsDate"])));
                if (objParams.ContainsKey("DailyReceivedGolds")) updates.Add(Builders<UserInfo>.Update.Set("DailyReceivedGolds", objParams["DailyReceivedGolds"].Value<int>()));
                if (objParams.ContainsKey("MaxDailyReceivedGolds")) updates.Add(Builders<UserInfo>.Update.Set("MaxDailyReceivedGolds", objParams["MaxDailyReceivedGolds"].Value<int>()));
                if (objParams.ContainsKey("GuoZhaoCnt")) updates.Add(Builders<UserInfo>.Update.Set("GuoZhaoCnt", objParams["GuoZhaoCnt"].Value<int>()));
                if (objParams.ContainsKey("LastGuoZhaoDate")) updates.Add(Builders<UserInfo>.Update.Set("LastGuoZhaoDate", Convert.ToDateTime(objParams["LastGuoZhaoDate"])));
                if (objParams.ContainsKey("GuoZhaoInfo"))
                {
                    string strGuoZhaoInfo = objParams["GuoZhaoInfo"].Value<string>();
                    if (strGuoZhaoInfo.Length > 0)
                    {
                        // make sure to convert "ArduinoId" to "DeviceId"
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
                    updates.Add(Builders<UserInfo>.Update.Set("GuoZhaoInfo", strGuoZhaoInfo));
                }
                if (objParams.ContainsKey("BindedMachines"))
                {
                    string strBindedMachines = objParams["BindedMachines"].Value<string>();
                    if (strBindedMachines.Length > 0)
                    {
                        // remove duplicated data
                        List<UserInfo.CBindedMachine> bindedMachines = JsonConvert.DeserializeObject<List<UserInfo.CBindedMachine>>(strBindedMachines);
                        for (int i = bindedMachines.Count-1; i >= 0; i--)
                        {
                            UserInfo.CBindedMachine curMachine = bindedMachines[i];
                            for (int j = i-1; j >= 0; j--)
                            {
                                UserInfo.CBindedMachine bindedMachine = bindedMachines[j];
                                if (bindedMachine.Lobby == curMachine.Lobby &&
                                    bindedMachine.DeviceId == curMachine.DeviceId &&
                                    bindedMachine.MachineId == curMachine.MachineId)
                                {
                                    // remove curMachineInfo
                                    bindedMachines.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        strBindedMachines = JsonConvert.SerializeObject(bindedMachines);
                    }
                    updates.Add(Builders<UserInfo>.Update.Set("BindedMachines", strBindedMachines));
                }
                if (objParams.ContainsKey("BetReward")) updates.Add(Builders<UserInfo>.Update.Set("BetReward", objParams["BetReward"].Value<float>()));
                if (objParams.ContainsKey("WainZhuRecords"))
                {
                    BsonArray wainZhuRecords = new BsonArray();
                    JArray wainZhus = objParams["WainZhuRecords"] as JArray;
                    for (int i = 0; i < wainZhus.Count; i++)
                    {
                        JObject objWainZhu = wainZhus[i].Value<JObject>();
                        string context = JsonConvert.SerializeObject(objWainZhu);
                        wainZhuRecords.Add(BsonDocument.Parse(context));
                    }
                    updates.Add(Builders<UserInfo>.Update.Set("WainZhuRecords", wainZhuRecords));
                }
                if (objParams.ContainsKey("SortIndex")) updates.Add(Builders<UserInfo>.Update.Set("SortIndex", objParams["SortIndex"].Value<int>()));
                UpdateResult ret = mLobbyUserInfos.UpdateMany(findFilter, Builders<UserInfo>.Update.Combine(updates));
                // update cache
                if (this.mDBCacheCLI != null)
                {
                    // refresh userInfo
                    FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.Eq("_id", new ObjectId(userInfo.GetObjectId()));
                    userInfos = mLobbyUserInfos.Find(filter).ToList();
                    string key = string.Format("UserInfo:{0}", userInfos[0].Account);
                    string content = JsonConvert.SerializeObject(userInfos[0]);
                    this.mDBCacheCLI.Set(key, content);
                }
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("UpdateUser", ex.Message);
            }
            return errMsg;
        }

        public JObject VerifyUser(string token)
        {
            JObject resObj = new JObject();
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserInfo userInfo = FindUserByToken(token);
                if (userInfo == null) throw new Exception("無效的Token");
                // reset LoginErrCnt if needed
                if (userInfo.LoginErrCnt != 0)
                {
                    userInfo.LoginErrCnt = 0;

                    JObject objParams = new JObject();
                    objParams["LoginErrCnt"] = 0;
                    string error = this.UpdateUser(userInfo.Account, objParams);
                    if (error != null) throw new Exception(error);
                }
                string content = JsonConvert.SerializeObject(userInfo);
                resObj["status"] = "success";
                resObj["userInfo"] = JObject.Parse(content);
            }
            catch(Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = BuildErrMsg("VerifyUser", ex.Message);
            }
            return resObj;
        }

        public UserInfo QueryUser(string account, string id, string token)
        {
            UserInfo? userInfo = null;
            if (account != null) userInfo = FindUserByAccount(account);
            if (userInfo == null && id != null) userInfo = FindUserById(id);
            if (userInfo == null && token != null) userInfo = FindUserByToken(token);
            if (userInfo != null)
            {
                userInfo.BetReward = Math.Round(userInfo.BetReward, 2);

                List<WainZhu> records = GetWainZhuRecords(userInfo.Account);
                userInfo.WainZhuRecords = records;
            }
            return userInfo;
        }

        public UserInfo QueryUserByTel(string PhoneNo)
        {
            UserInfo? userInfo = null;
            List<UserInfo> userInfos = FindUsersByTel(PhoneNo);
            if (userInfos != null && userInfos.Count > 0)
            {
                userInfo = userInfos[0];
            }

            if (userInfo != null)
            {
                List<WainZhu> records = GetWainZhuRecords(userInfo.Account);
                userInfo.WainZhuRecords = records;
            }
            return userInfo;
        }

        public JObject AwardUser(string account, string reason, string awardType, int award)
        {
            JObject resObj = new JObject();
            try
            {
                if (mUserAwards == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserInfo userInfo = FindUserByAccount(account);
                if (userInfo == null) throw new Exception(string.Format("用戶{0}不存在", account));
                if (award <= 0) throw new Exception(string.Format("錯誤的點數{0}", award));

                JObject objScores = new JObject();
                JObject objParams = new JObject();
                JObject objNote = new JObject();
                switch (awardType)
                {
                    case UserAward.Coins:
                        {
                            // update userInfo
                            objScores["InCoins"] = award;
                            objParams["PaiedAward"] = userInfo.PaiedAward + award;
                            objParams["LoginAwardDate"] = DateTime.Now;

                            objNote["AwardType"] = awardType;
                            objNote["InCoins"] = userInfo.InCoins;
                            objNote["PaiedAward"] = userInfo.PaiedAward;
                            objNote["LoginAwardDate"] = userInfo.LoginAwardDate;
                        }
                        break;
                    case UserAward.TrialCoins:
                        {
                            // update userInfo
                            objScores["InTrialCoins"] = award;
                            objParams["PaiedAward"] = userInfo.PaiedAward + award;
                            objParams["LoginAwardDate"] = DateTime.Now;

                            objNote["AwardType"] = awardType;
                            objNote["InTrialCoins"] = userInfo.InTrialCoins;
                            objNote["PaiedAward"] = userInfo.PaiedAward;
                            objNote["LoginAwardDate"] = userInfo.LoginAwardDate;
                        }
                        break;
                    default:
                        throw new Exception(string.Format("unknown awardType {0} in AwardUser()", awardType));
                }
                string error1 = this.UpdateUserScores(account, objScores);
                string error2 = this.UpdateUser(account, objParams);
                if (error1 != null || error2 != null) throw new Exception(string.Format("error1:{0}, error2:{1}", error1, error2));

                UserAward userAward = new UserAward();
                userAward.Account = userInfo.Account;
                userAward.Reason = reason;
                userAward.AwardType = awardType;
                userAward.Award = award;
                userAward.Note = JsonConvert.SerializeObject(objNote);
                userAward.CreateTime = DateTime.Now.ToUniversalTime();
                mUserAwards.InsertOne(userAward);

                resObj["status"] = "success";
                resObj["award"] = award;
                resObj["coinType"] = awardType;
                resObj["reason"] = reason;
            }
            catch (Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = BuildErrMsg("AwardUser", ex.Message);
            }
            return resObj;
        }

        public List<UserAward> QueryAwards(string targetUser, List<string> awardTypes, DateTime sTime, DateTime eTime)
        {
            List<UserAward> list = new List<UserAward>();
            try
            {
                if (mUserAwards == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                List<FilterDefinition<UserAward>> filters = new List<FilterDefinition<UserAward>>();
                filters.Add(Builders<UserAward>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<UserAward>.Filter.Lt("CreateTime", eTime));
                FilterDefinition<UserAward> findFilter = Builders<UserAward>.Filter.And(filters);
                IFindFluent<UserAward, UserAward> collect = mUserAwards.Find(findFilter);
                List<UserAward> tmpList = collect.ToList();
                // if set filter, collect only matched type
                if (awardTypes != null && awardTypes.Count > 0)
                {
                    for (int i = 0; i < tmpList.Count; i++)
                    {
                        if (awardTypes.Contains(tmpList[i].AwardType) == false)
                            continue;
                        list.Add(tmpList[i]);
                    }
                }
                else
                {
                    list = tmpList;
                }
                list.Reverse();
                list.Sort(delegate (UserAward x, UserAward y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1; 
                });
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("QueryAwards", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return list;
        }

        public List<UserInfo> GetValidateUserInfos(string gmAccount)
        {
            List<UserInfo>? list = null;
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                List<FilterDefinition<UserInfo>> filters = new List<FilterDefinition<UserInfo>>();
                filters.Add(Builders<UserInfo>.Filter.Eq("Validate", true));
                if (gmAccount != null && gmAccount.Length > 0) filters.Add(Builders<UserInfo>.Filter.Eq("GM", gmAccount));
                FilterDefinition<UserInfo> findFilter = Builders<UserInfo>.Filter.And(filters);
                SortDefinition<UserInfo> sortDefinition;
                if (this.mAgent.EnableUserInfoSortIndex)
                {
                    sortDefinition  = Builders<UserInfo>.Sort.Combine(
                        Builders<UserInfo>.Sort.Ascending("SortIndex"),
                        Builders<UserInfo>.Sort.Ascending("Account")
                    );
                }
                else
                {
                    sortDefinition = Builders<UserInfo>.Sort.Combine(
                        Builders<UserInfo>.Sort.Ascending("Account")
                    );
                }
                IFindFluent<UserInfo, UserInfo> collect = mLobbyUserInfos.Find(findFilter).Sort(sortDefinition);
                list = collect.ToList();
                // don't update DBCache to avoid massive UserInfo will cause heavy CPU loading
                //if (this.mDBCacheCLI != null)
                //{
                //    foreach (UserInfo userInfo in list)
                //    {
                //        string key = string.Format("UserInfo:{0}", userInfo.Account);
                //        string content = JsonConvert.SerializeObject(userInfo);
                //        this.mDBCacheCLI.Set(key, content);
                //    }
                //}
            }
            catch(Exception ex)
            {
                list = null;
                string errMsg = BuildErrMsg("GetValidateUserInfos", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return list;
        }

        public string AddLoginInfo(string agentId, string appId, string token, string lobbyName)
        {
            string? errMsg = null;
            try
            {
                if (mLoginInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                FilterDefinition<LoginInfo> findFilter = Builders<LoginInfo>.Filter.Eq("AppId", appId);
                List<LoginInfo> loginInfos = mLoginInfos.Find(findFilter).ToList();
                LoginInfo loginInfo = loginInfos.Count > 0 ? loginInfos[0] : null;
                if (loginInfo == null)
                {
                    loginInfo = new LoginInfo();
                    loginInfo.AppId = appId;
                    loginInfo.Token = token;
                    loginInfo.LobbyName = lobbyName;
                    loginInfo.CreateDate = DateTime.Now.ToUniversalTime();
                    mLoginInfos.InsertOne(loginInfo);
                }
                else
                {
                    List<UpdateDefinition<LoginInfo>> updates = new List<UpdateDefinition<LoginInfo>>();
                    updates.Add(Builders<LoginInfo>.Update.Set("AppId", appId));
                    updates.Add(Builders<LoginInfo>.Update.Set("Token", token));
                    updates.Add(Builders<LoginInfo>.Update.Set("LobbyName", lobbyName));
                    updates.Add(Builders<LoginInfo>.Update.Set("CreateDate", DateTime.Now.ToUniversalTime()));
                    mLoginInfos.UpdateOne(findFilter, Builders<LoginInfo>.Update.Combine(updates));
                }
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("AddLoginInfo", ex.Message);
            }
            return errMsg;
        }

        public JObject CheckLoginInfo(string agent, string appId)
        {
            JObject resObj = new JObject();
            try
            {
                if (mLoginInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                FilterDefinition<LoginInfo> findFilter = Builders<LoginInfo>.Filter.Eq("AppId", appId);
                List<LoginInfo> loginInfos = mLoginInfos.Find(findFilter).ToList();
                LoginInfo loginInfo = loginInfos.Count > 0 ? loginInfos[0] : null;
                if (loginInfo != null)
                {
                    mLoginInfos.DeleteMany(findFilter);
                    resObj["status"] = "success";
                    resObj["agent"] = agent;
                    resObj["appId"] = appId;
                    resObj["token"] = loginInfo.Token;
                    resObj["lobbyName"] = loginInfo.LobbyName;
                }
                else
                {
                    throw new Exception("未發現符合的登入資料");
                }
            }
            catch(Exception ex)
            {
                resObj["status"] = "failed";
                resObj["agent"] = agent;
                resObj["appId"] = appId;
                resObj["error"] = BuildErrMsg("CheckLoginInfo", ex.Message);
            }
            return resObj;
        }

        public string UpdateUserScores(string account, JObject objScores)
        {
            string? errMsg = null;
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (account == null) throw new Exception("account is null");
                if (objScores == null) throw new Exception("objScores is null");

                long inCoins = 0;
                long outCoins = 0;
                long inTrialCoins = 0;
                long outTrialCoins = 0;
                long inGuoZhaoCoins = 0;
                long outGuoZhaoCoins = 0;
                if (objScores.ContainsKey("InCoins"))
                {
                    long scores = objScores["InCoins"].Value<long>();
                    if (scores < 0) throw new Exception("InCoins must be larger than 0");
                    inCoins = scores;
                }
                if (objScores.ContainsKey("OutCoins"))
                {
                    long scores = objScores["OutCoins"].Value<long>();
                    if (scores < 0) throw new Exception("OutCoins must be larger than 0");
                    outCoins = scores;
                }
                if (objScores.ContainsKey("InTrialCoins"))
                {
                    long scores = objScores["InTrialCoins"].Value<long>();
                    if (scores < 0) throw new Exception("InTrialCoins must be larger than 0");
                    inTrialCoins = scores;
                }
                if (objScores.ContainsKey("OutTrialCoins"))
                {
                    long scores = objScores["OutTrialCoins"].Value<long>();
                    if (scores < 0) throw new Exception("OutTrialCoins must be larger than 0");
                    outTrialCoins = scores;
                }
                if (objScores.ContainsKey("InGuoZhaoCoins"))
                {
                    long scores = objScores["InGuoZhaoCoins"].Value<long>();
                    if (scores < 0) throw new Exception("InGuoZhaoCoins must be larger than 0");
                    inGuoZhaoCoins = scores;
                }
                if (objScores.ContainsKey("OutGuoZhaoCoins"))
                {
                    long scores = objScores["OutGuoZhaoCoins"].Value<long>();
                    if (scores < 0) throw new Exception("OutGuoZhaoCoins must be larger than 0");
                    outGuoZhaoCoins = scores;
                }
                // don't update db here to avoid updating conflict
                UpdateScoresInfo userScoresInfo = new UpdateUserScoresInfo(
                    account, inCoins, outCoins, inTrialCoins, outTrialCoins, inGuoZhaoCoins, outGuoZhaoCoins);
                this.PushUpdateScoresInfo(userScoresInfo);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("UpdateUserScores", ex.Message);
            }
            return errMsg;
        }

        public string RecordUserAction(string userId, string lobby, string machine, string actionLog, string note)
        {
            string? errMsg = null;
            try
            {
                if (mUserActions == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserAction userAction = new UserAction();
                userAction.UserId = userId;
                userAction.Lobby = lobby;
                userAction.Machine = machine;
                userAction.Action = actionLog;
                userAction.Note = note;
                userAction.CreateTime = DateTime.Now;

                mUserActions.InsertOne(userAction);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("RecordUserAction", ex.Message);
            }
            return errMsg;
        }

        public List<UserAction> GetUserActions(string account, List<string> actionFilter, int requiredCount)
        {
            DateTime beginDT = new DateTime(0);
            DateTime endDT = DateTime.UtcNow;
            List<UserAction> tmpList = GetUserActions(account, actionFilter, beginDT, endDT);

            List<UserAction> collectList = new List<UserAction>();
            int dataCnt = requiredCount < tmpList.Count ? requiredCount : tmpList.Count;
            for (int i = 0; i < dataCnt; i++) collectList.Add(tmpList[i]);
            return collectList;
        }

        public List<UserAction> GetUserActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime)
        {
            List<UserAction> collectList = new List<UserAction>();
            try
            {
                if (mUserActions == null || mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserInfo userInfo = null;
                if (account != null)
                {
                    userInfo = FindUserByAccount(account);
                    if (userInfo == null) throw new Exception(string.Format("{0}:{1}", TextConverter.Convert("無效的用戶"), account));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                List<FilterDefinition<UserAction>> filters = new List<FilterDefinition<UserAction>>();
                filters.Add(Builders<UserAction>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<UserAction>.Filter.Lt("CreateTime", eTime));
                if (userInfo != null) filters.Add(Builders<UserAction>.Filter.Eq("UserId", userInfo.GetObjectId()));
                if (actionFilter != null && actionFilter.Count > 0)
                {
                    List<FilterDefinition<UserAction>> queryActions = new List<FilterDefinition<UserAction>>();
                    for (int i = 0; i < actionFilter.Count; i++)
                    {
                        queryActions.Add(Builders<UserAction>.Filter.Eq("Action", actionFilter[i]));
                    }
                    filters.Add(Builders<UserAction>.Filter.Or(queryActions));
                }
                FilterDefinition<UserAction> filter = Builders<UserAction>.Filter.And(filters);
                IFindFluent<UserAction, UserAction> collect = mUserActions.Find(filter);
                collectList = collect.ToList();
                collectList.Sort(delegate (UserAction x, UserAction y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1;
                });
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetUserActions", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return collectList;
        }

        public string UserSendGift(string account, string target, JObject objGift)
        {
            string? errMsg = null;
            try
            {
                if (mUserGifts == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                GiftInfo giftInfo = new GiftInfo(objGift);
                giftInfo.User = account;
                giftInfo.Target = target;
                giftInfo.CreateTime = DateTime.Now;

                mUserGifts.InsertOne(giftInfo);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("UserSendGift", ex.Message);
            }
            return errMsg;
        }

        public List<GiftInfo> GetUserGiftRecords(string account, string target, DateTime sTime, DateTime eTime)
        {
            List<GiftInfo> list = new List<GiftInfo>();
            try
            {
                if (mUserGifts == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                if (account == null && target == null)
                {
                    List<FilterDefinition<GiftInfo>> filters = new List<FilterDefinition<GiftInfo>>();
                    filters.Add(Builders<GiftInfo>.Filter.Gte("CreateTime", sTime));
                    filters.Add(Builders<GiftInfo>.Filter.Lt("CreateTime", eTime));
                    FilterDefinition<GiftInfo> filter = Builders<GiftInfo>.Filter.And(filters);
                    IFindFluent<GiftInfo, GiftInfo> collect = mUserGifts.Find(filter);
                    List<GiftInfo> list1 = collect.ToList();
                    list1.Reverse();
                    list.AddRange(list1);
                }
                else
                {
                    if (account != null)
                    {
                        List<FilterDefinition<GiftInfo>> filters = new List<FilterDefinition<GiftInfo>>();
                        filters.Add(Builders<GiftInfo>.Filter.Eq("User", account));
                        filters.Add(Builders<GiftInfo>.Filter.Gte("CreateTime", sTime));
                        filters.Add(Builders<GiftInfo>.Filter.Lt("CreateTime", eTime));
                        FilterDefinition<GiftInfo> filter = Builders<GiftInfo>.Filter.And(filters);
                        IFindFluent<GiftInfo, GiftInfo> collect = mUserGifts.Find(filter);
                        List<GiftInfo> list1 = collect.ToList();
                        list1.Reverse();
                        list.AddRange(list1);
                    }
                
                    if (target != null)
                    {
                        List<FilterDefinition<GiftInfo>> filters = new List<FilterDefinition<GiftInfo>>();
                        filters.Add(Builders<GiftInfo>.Filter.Eq("Target", target));
                        filters.Add(Builders<GiftInfo>.Filter.Gte("CreateTime", sTime));
                        filters.Add(Builders<GiftInfo>.Filter.Lt("CreateTime", eTime));
                        FilterDefinition<GiftInfo> filter = Builders<GiftInfo>.Filter.And(filters);
                        IFindFluent<GiftInfo, GiftInfo> collect = mUserGifts.Find(filter);
                        List<GiftInfo> list2 = collect.ToList();
                        list2.Reverse();
                        list.AddRange(list2);
                    }
                }

                // if records are from 2 parts. do sort
                list.Sort((a, b) =>
                {
                    TimeSpan ts = a.CreateTime - b.CreateTime;
                    return ts.TotalMilliseconds < 0 ? 1 : -1;
                });
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("GetUserGiftRecords", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return list;
        }

        public string? RecordUserFreeScore(string account, SCORE_TYPE scoreType, int scores, string note)
        {
            string? errMsg = null;
            try
            {
                if (mUserFreeScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (scoreType != SCORE_TYPE.Formal) throw new Exception("scoreType不為正式幣");
                if (scores <= 0) throw new Exception("scores必須大於0");

                UserFreeScore userFreeScore = new UserFreeScore();
                userFreeScore.User = account;
                userFreeScore.ScoreType = scoreType;
                userFreeScore.Scores = scores;
                userFreeScore.Note = note;
                userFreeScore.CreateTime = DateTime.Now;

                mUserFreeScores.InsertOne(userFreeScore);
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("UserSendGift", ex.Message);
            }
            return errMsg;
        }

        public List<UserFreeScore> GetUserFreeScores(string account, DateTime sTime, DateTime eTime)
        {
            List<UserFreeScore> list = new List<UserFreeScore>();
            try
            {
                if (mUserFreeScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                List<FilterDefinition<UserFreeScore>> filters = new List<FilterDefinition<UserFreeScore>>();
                filters.Add(Builders<UserFreeScore>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<UserFreeScore>.Filter.Lt("CreateTime", eTime));
                if (account != null) filters.Add(Builders<UserFreeScore>.Filter.Eq("User", account));
                FilterDefinition<UserFreeScore> filter = Builders<UserFreeScore>.Filter.And(filters);
                IFindFluent<UserFreeScore, UserFreeScore> collect = mUserFreeScores.Find(filter);
                List<UserFreeScore> list1 = collect.ToList();
                list1.Reverse();
                list.AddRange(list1);
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("GetUserGiftRecords", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return list;
        }

        public string? RecordBonusAward(string winType, float totalBet, float scoreInterval,
            string awardInfo, string urlTransferPoints, string strTransferResponse)
        {
            string? errMsg = null;
            try
            {
                if (mBonusAwards == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                BonusAward bonusAward = new BonusAward();
                bonusAward.WinType = winType;
                bonusAward.TotalBet = totalBet;
                bonusAward.ScoreInterval = scoreInterval;
                bonusAward.AwardInfo = awardInfo;
                bonusAward.ApiTransferPoints = urlTransferPoints;
                bonusAward.ApiTransferResponse = strTransferResponse;

                mBonusAwards.InsertOne(bonusAward);
            }
            catch(Exception ex)
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
                if (mUserScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserInfo userInfo = FindUserByAccount(account);
                if (userInfo == null) throw new Exception(string.Format("{0}不存在", account));

                ScoreInfo scoreInfo = new ScoreInfo();
                scoreInfo.UserId = userInfo.GetObjectId();
                scoreInfo.Lobby = lobby;
                scoreInfo.Machine = machine;
                scoreInfo.ArduinoId = deviceId;
                scoreInfo.MachineId = machineId;
                scoreInfo.TotalInScore = totalInScore;
                scoreInfo.TotalOutScore = totalOutScore;
                scoreInfo.CreateTime = DateTime.Now;

                mUserScores.InsertOne(scoreInfo);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("RecordScore", ex.Message);
            }
            return errMsg;
        }

        public string SummarizeScore(string account, string lobby, string machine, int deviceId, int machineId, SCORE_TYPE scoreType, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                if (mUserScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserInfo userInfo = FindUserByAccount(account);
                if (userInfo == null) throw new Exception(string.Format("{0}不存在", account));

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                List<FilterDefinition<ScoreInfo>> filters = new List<FilterDefinition<ScoreInfo>>();
                filters.Add(Builders<ScoreInfo>.Filter.Eq("UserId", userInfo.GetObjectId()));
                filters.Add(Builders<ScoreInfo>.Filter.Eq("Lobby", lobby));
                filters.Add(Builders<ScoreInfo>.Filter.Eq("ArduinoId", deviceId));
                filters.Add(Builders<ScoreInfo>.Filter.Eq("MachineId", machineId));
                filters.Add(Builders<ScoreInfo>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<ScoreInfo>.Filter.Lt("CreateTime", eTime));
                FilterDefinition<ScoreInfo> filter = Builders<ScoreInfo>.Filter.And(filters);
                IFindFluent<ScoreInfo, ScoreInfo> collect = mUserScores.Find(filter);
                List<ScoreInfo> list = collect.ToList();
                long iTotalInScore = 0;
                long iTotalOutScore = 0;
                foreach (ScoreInfo scoreInfo in list)
                {
                    iTotalInScore += scoreInfo.TotalInScore;
                    iTotalOutScore += scoreInfo.TotalOutScore;
                }

                // record to user action
                JObject scoreObj = new JObject();
                scoreObj["User"] = userInfo.Account;
                scoreObj["UserId"] = userInfo.GetObjectId();
                scoreObj["LobbyName"] = lobby;
                scoreObj["Machine"] = machine;
                scoreObj["ScoreType"] = (int)scoreType;
                scoreObj["ArduinoId"] = deviceId;
                scoreObj["DeviceId"] = deviceId;
                scoreObj["MachineId"] = machineId;
                scoreObj["TotalInScore"] = iTotalInScore;
                scoreObj["TotalOutScore"] = iTotalOutScore;
                scoreObj["BeginTime"] = sTime;
                scoreObj["EndTime"] = eTime;
                RecordUserAction(userInfo.GetObjectId(), lobby, machine, UserAction.SummarizeScore, JsonConvert.SerializeObject(scoreObj));

                List<FilterDefinition<MachineLog>> filters2 = new List<FilterDefinition<MachineLog>>();
                filters2.Add(Builders<MachineLog>.Filter.Eq("Action", MachineLog.Lock));
                filters2.Add(Builders<MachineLog>.Filter.Eq("LobbyName", lobby));
                filters2.Add(Builders<MachineLog>.Filter.Eq("ArduinoId", deviceId));
                filters2.Add(Builders<MachineLog>.Filter.Eq("MachineId", machineId));
                filters2.Add(Builders<MachineLog>.Filter.Gte("CreateTime", sTime));
                filters2.Add(Builders<MachineLog>.Filter.Lt("CreateTime", eTime));
                FilterDefinition<MachineLog> filter2 = Builders<MachineLog>.Filter.And(filters2);
                IFindFluent<MachineLog, MachineLog> collect2 = mMachineLogs.Find(filter2);
                List<MachineLog> list2 = collect2.ToList();
                JArray arrLock = new JArray();
                for (int i = 0; i < list2.Count; i++)
                {
                    MachineLog machineLog = list2[i];
                    JObject obj = JObject.Parse(machineLog.Log);
                    string player = obj.ContainsKey("player") ? obj["player"].Value<string>() : null;
                    if (player == account) arrLock.Add(obj);
                }

                JObject logObj = new JObject();
                logObj["player"] = userInfo.Account;
                logObj["permission"] = userInfo.Permission;
                logObj["action"] = MachineLog.Summarize;
                logObj["lobby"] = lobby;
                logObj["machine"] = machine;
                logObj["scoreType"] = (int)scoreType;
                logObj["arduinoId"] = deviceId;
                logObj["machineId"] = machineId;
                logObj["in"] = iTotalInScore;
                logObj["out"] = iTotalOutScore;
                logObj["total"] = iTotalInScore - iTotalOutScore;
                logObj["lockLogs"] = arrLock;
                RecordMachineLog(lobby, deviceId, machineId, MachineLog.Summarize, logObj, account);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("SummarizeScore", ex.Message);
            }
            return errMsg;
        }

        public string SummarizeUniPlay(string account, string uniPlayDomain, string uniPlayAccount, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                if (mUserScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserInfo userInfo = FindUserByAccount(account);
                if (userInfo == null) throw new Exception(string.Format("{0}不存在", account));

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                // summarize total out
                List<FilterDefinition<UserAction>> outFilters = new List<FilterDefinition<UserAction>>();
                outFilters.Add(Builders<UserAction>.Filter.Eq("Action", UserAction.TransferUniPlayScore));
                outFilters.Add(Builders<UserAction>.Filter.Eq("UserId", userInfo.GetObjectId()));
                outFilters.Add(Builders<UserAction>.Filter.Eq("Lobby", uniPlayDomain));
                outFilters.Add(Builders<UserAction>.Filter.Gte("CreateTime", sTime));
                outFilters.Add(Builders<UserAction>.Filter.Lt("CreateTime", eTime));
                FilterDefinition<UserAction> outFilter = Builders<UserAction>.Filter.And(outFilters);
                IFindFluent<UserAction, UserAction> outCollect = mUserActions.Find(outFilter);
                List<UserAction> outList = outCollect.ToList();
                long iTotalTransferedScore = 0;
                foreach (UserAction userAction in outList)
                {
                    JObject obj = JObject.Parse(userAction.Note);
                    iTotalTransferedScore += obj["Scores"].Value<int>();
                }
                // summarize total in
                List<FilterDefinition<UserAction>> inFilters = new List<FilterDefinition<UserAction>>();
                inFilters.Add(Builders<UserAction>.Filter.Eq("Action", UserAction.RetriveUniPlayScore));
                inFilters.Add(Builders<UserAction>.Filter.Eq("UserId", userInfo.GetObjectId()));
                inFilters.Add(Builders<UserAction>.Filter.Eq("Lobby", uniPlayDomain));
                inFilters.Add(Builders<UserAction>.Filter.Gte("CreateTime", sTime));
                inFilters.Add(Builders<UserAction>.Filter.Lt("CreateTime", eTime));
                FilterDefinition<UserAction> inFilter = Builders<UserAction>.Filter.And(inFilters);
                IFindFluent<UserAction, UserAction> inCollect = mUserActions.Find(inFilter);
                List<UserAction> inList = inCollect.ToList();
                long iTotalRetrivedScore = 0;
                foreach (UserAction userAction in inList)
                {
                    JObject obj = JObject.Parse(userAction.Note);
                    iTotalRetrivedScore += obj["Scores"].Value<int>();
                }

                AgentInfo agentInfo = this.Agent();
                float uniPlayExchangeRatio = 1.0f;
                int profitPayRatio = 0;
                List<UniPlayInfo> uniPlays = this.QueryUniPlay(null, uniPlayDomain);
                if (uniPlays != null && uniPlays.Count > 0)
                {
                    UniPlayInfo uniPlay = uniPlays[0];
                    uniPlayExchangeRatio = uniPlay.ExchangeRatioToNTD;
                    profitPayRatio = uniPlay.ProfitPayRatio;
                }
                // record to user action
                JObject scoreObj = new JObject();
                scoreObj["User"] = userInfo.Account;
                scoreObj["UserId"] = userInfo.GetObjectId();
                scoreObj["UserPermission"] = userInfo.Permission;
                scoreObj["GM"] = userInfo.GM;
                scoreObj["Domain"] = uniPlayDomain;
                scoreObj["UniPlayExchangeRatio"] = uniPlayExchangeRatio;
                scoreObj["AgentInfoScoreRatio"] = agentInfo.ScoreRatioToNTD;
                scoreObj["ProfitPayRatio"] = profitPayRatio;
                scoreObj["Account"] = uniPlayAccount;
                scoreObj["TotalTransfered"] = iTotalTransferedScore;
                scoreObj["TotalRetrived"] = iTotalRetrivedScore;
                scoreObj["BeginTime"] = sTime;
                scoreObj["EndTime"] = eTime;
                RecordUserAction(userInfo.GetObjectId(), uniPlayDomain, "", UserAction.SummarizeUniPlay, JsonConvert.SerializeObject(scoreObj));
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("SummarizeScore", ex.Message);
            }
            return errMsg;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public JObject CreateGM(string gmAccount, string account, string password, string nickName,
            int permission, string phoneNo, string prefix, float benefitRatio, string note)
        {
            JObject resObj = new JObject();
            try
            {
                if (mLobbyGMInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (account == null || password == null) throw new Exception("帳號或密碼不正確");
                for (int i = 0; i < phoneNo.Length; i++)
                {
                    if (phoneNo[i] != '0' && phoneNo[i] != '1' && phoneNo[i] != '2' && phoneNo[i] != '3' && phoneNo[i] != '4' &&
                        phoneNo[i] != '5' && phoneNo[i] != '6' && phoneNo[i] != '7' && phoneNo[i] != '8' && phoneNo[i] != '9')
                    {
                        throw new Exception("電話僅接受數字");
                    }
                }

                // make sure prefix is always upper case
                prefix = prefix.ToUpper();
                // to check is there any gm owns specified prefix
                GMInfo gmPrfix = null;
                if (prefix != null && prefix.Length > 0)
                {
                    FilterDefinition<GMInfo> prefixFilter = Builders<GMInfo>.Filter.Eq("Prefix", prefix);
                    List<GMInfo> prefixGMInfos = mLobbyGMInfos.Find(prefixFilter).ToList();
                    gmPrfix = prefixGMInfos.Count > 0 ? prefixGMInfos[0] : null;
                }
                // if gmPrfix exists, return error msg
                if (gmPrfix != null && gmPrfix.Validate)
                {
                    throw new Exception(string.Format("前綴字與GM:{0}衝突", gmPrfix.Account));
                }

                List<int> maxPermission = new List<int>();
                string gmParent = null;
                FilterDefinition<GMInfo> filter = Builders<GMInfo>.Filter.Eq("Account", gmAccount);
                List<GMInfo> gmInfos = mLobbyGMInfos.Find(filter).ToList();
                GMInfo gmInfo = gmInfos.Count > 0 ? gmInfos[0] : null;
                if (gmInfo != null)
                {
                    if ((gmInfo.Permission & GMInfo.PERMISSION.CreateGMInfo) == 0x0)
                    {
                        throw new Exception(string.Format("管理者:{0}不具備建立GM權限", gmAccount));
                    }

                    gmParent = gmInfo.GM.Length > 0 ? string.Format("{0};{1}", gmInfo.GM, gmInfo.Account) : gmInfo.Account;
                    if (gmParent == null) throw new Exception(string.Format("無效的GM:{0}", gmAccount));
                    // check permissions
                    List<int> permissionParent = GMInfo.PERMISSION.ToList(gmInfo.Permission);
                    List<int> permissionNewGM = GMInfo.PERMISSION.ToList(permission);
                    foreach (int code in permissionNewGM)
                    {
                        if (permissionParent.Contains(code))
                            continue;
                        throw new Exception(string.Format("指定給新GM:{0}的權限超過建立者:{1}權限", account, gmAccount));
                    }

                    if (benefitRatio < 0) throw new Exception(string.Format("無效的分紅配比:{0}", benefitRatio));
                    if (gmInfo != null && gmInfo.BenefitRatio < benefitRatio) throw new Exception(string.Format("超出允許的分紅配比:{0}/{1}", benefitRatio, gmInfo.BenefitRatio));
                }
                else if (account == "WinLobby")
                {
                    gmParent = "";
                    // assign permissions
                    maxPermission.Add(GMInfo.PERMISSION.InfiniteScore);
                    maxPermission.Add(GMInfo.PERMISSION.CreateUserInfo);
                    maxPermission.Add(GMInfo.PERMISSION.ModifyUserInfo);
                    maxPermission.Add(GMInfo.PERMISSION.DeleteUserInfo);
                    maxPermission.Add(GMInfo.PERMISSION.AddUserScore);
                    maxPermission.Add(GMInfo.PERMISSION.SubUserScore);
                    maxPermission.Add(GMInfo.PERMISSION.CreateGMInfo);
                    maxPermission.Add(GMInfo.PERMISSION.ModifyGMInfo);
                    maxPermission.Add(GMInfo.PERMISSION.DeleteGMInfo);
                    maxPermission.Add(GMInfo.PERMISSION.AddGMScore);
                    maxPermission.Add(GMInfo.PERMISSION.SubGMScore);
                    maxPermission.Add(GMInfo.PERMISSION.MainSetting);
                    maxPermission.Add(GMInfo.PERMISSION.ViewReport);
                    // rebuild permission
                    for (int i = 0; i < maxPermission.Count; i++)
                    {
                        permission |= maxPermission[i];
                    }
                }
                else
                {
                    throw new Exception(string.Format("無效的父GM:{0}", gmAccount));
                }

                FilterDefinition<GMInfo> newFilter = Builders<GMInfo>.Filter.Eq("Account", account);
                List<GMInfo> newGMInfos = mLobbyGMInfos.Find(newFilter).ToList();
                GMInfo newGMInfo = newGMInfos.Count > 0 ? newGMInfos[0] : null;
                if (newGMInfo != null)
                {
                    if (newGMInfo.Validate) throw new Exception(string.Format("管理者:{0}已存在", newGMInfo));

                    JObject objParams = new JObject();
                    objParams["LastUpdateDate"] = DateTime.UtcNow;
                    objParams["Password"] = password;
                    objParams["NickName"] = nickName;
                    objParams["AgentId"] = mAgent.Account;
                    objParams["GM"] = gmParent;
                    objParams["Permission"] = permission;
                    objParams["PhoneNo"] = phoneNo;
                    objParams["Prefix"] = prefix;
                    objParams["BenefitRatio"] = benefitRatio;
                    objParams["Note"] = note;
                    objParams["InCoins"] = 0;
                    objParams["OutCoins"] = 0;
                    objParams["InTrialCoins"] = 0;
                    objParams["OutTrialCoins"] = 0;
                    objParams["LastUpdateDate"] = DateTime.Now;
                    objParams["Validate"] = true;
                    objParams["Token"] = "";
                    objParams["TokenTime"] = Utils.zeroDateTime();
                    this.UpdateGM(account, objParams);
                    objParams["CreateDate"] = DateTime.Now;
                    // since UpdateGM won't update CreateDate, so modify it here...
                    List<UpdateDefinition<GMInfo>> updates = new List<UpdateDefinition<GMInfo>>();
                    updates.Add(Builders<GMInfo>.Update.Set("CreateDate", DateTime.UtcNow));
                    UpdateResult ret = mLobbyGMInfos.UpdateMany(filter, Builders<GMInfo>.Update.Combine(updates));
                }
                else
                {
                    newGMInfo = new GMInfo();
                    newGMInfo.Account = account;
                    newGMInfo.Password = password;
                    newGMInfo.NickName = nickName;
                    newGMInfo.AgentId = mAgent.Account;
                    newGMInfo.GM = gmParent;
                    newGMInfo.Permission = permission;
                    newGMInfo.PhoneNo = phoneNo;
                    newGMInfo.Prefix = prefix;
                    newGMInfo.BenefitRatio = benefitRatio;
                    newGMInfo.Note = note;
                    newGMInfo.CreateDate = DateTime.Now;
                    newGMInfo.LastUpdateDate = DateTime.Now;
                    newGMInfo.Validate = true;
                    mLobbyGMInfos.InsertOne(newGMInfo);
                }

                string strContent = JsonConvert.SerializeObject(newGMInfo);
                resObj["status"] = "success";
                resObj["gmInfo"] = JObject.Parse(strContent);
            }
            catch(Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = BuildErrMsg("CreateGM", ex.Message);
            }
            return resObj;
        }

        public GMInfo FindGMByAccount(string account)
        {
            GMInfo? gmInfo = null;
            try
            {
                if (mLobbyGMInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (account == null) throw new Exception("null account");

                if (this.mDBCacheCLI != null)
                {
                    try
                    {
                        string key = string.Format("GMInfo:{0}", account);
                        JObject obj = this.mDBCacheCLI.GetJObject(key);
                        if (obj != null) gmInfo = GMInfo.FromJson(obj);
                    }
                    catch (Exception ex) { }
                }
                // if not in cache, query gm from mongo db
                if (gmInfo == null)
                {
                    FilterDefinition<GMInfo> filter = Builders<GMInfo>.Filter.Eq("Account", account);
                    List<GMInfo> gmInfos = mLobbyGMInfos.Find(filter).ToList();
                    gmInfo = gmInfos.Count > 0 ? gmInfos[0] : null;
                }
                if (gmInfo == null || gmInfo.Validate == false) throw new Exception(String.Format("GM:{0} doesn't exist", account));
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
                else
                {
                    // store in db cache
                    if (this.mDBCacheCLI != null)
                    {
                        string key = string.Format("GMInfo:{0}", gmInfo.Account);
                        string content = JsonConvert.SerializeObject(gmInfo);
                        this.mDBCacheCLI.Set(key, content);
                    }
                }
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("FindGM", ex.Message);
                Log.StoreMsg(errMsg);
                gmInfo = null;
            }
            return gmInfo;
        }

        public GMInfo FindGMById(string gmId)
        {
            GMInfo? gmInfo = null;
            try
            {
                if (mLobbyGMInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (gmId == null) throw new Exception("null gmId");

                FilterDefinition<GMInfo> filter = Builders<GMInfo>.Filter.Eq("_id", new ObjectId(gmId));
                List<GMInfo> gmIngos = mLobbyGMInfos.Find(filter).ToList();
                gmInfo = gmIngos.Count > 0 ? gmIngos[0] : null;
                if (gmInfo == null || gmInfo.Validate == false) throw new Exception("not found GM with token");
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
                else
                {
                    // store in db cache
                    if (this.mDBCacheCLI != null)
                    {
                        string key = string.Format("GMInfo:{0}", gmInfo.Account);
                        string content = JsonConvert.SerializeObject(gmInfo);
                        this.mDBCacheCLI.Set(key, content);
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("FindGM", ex.Message);
                Log.StoreMsg(errMsg);
                gmInfo = null;
            }
            return gmInfo;
        }

        public GMInfo FindGMByToken(string token)
        {
            GMInfo? gmInfo = null;
            try
            {
                if (mLobbyGMInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (token == null) throw new Exception("null token");

                FilterDefinition<GMInfo> filter = Builders<GMInfo>.Filter.Eq("Token", token);
                List<GMInfo> gmIngos = mLobbyGMInfos.Find(filter).ToList();
                gmInfo = gmIngos.Count > 0 ? gmIngos[0] : null;
                if (gmInfo != null)
                {
                    // check if token time expired...
                    TimeSpan timeSpan = DateTime.Now - gmInfo.TokenTime;
                    if (timeSpan.TotalSeconds > mTokenExpiredTimeSpan)
                    {
                        gmInfo.Token = "";
                        gmInfo.TokenTime = Utils.zeroDateTime();
                        JObject objParams = new JObject();
                        objParams["Token"] = gmInfo.Token;
                        this.UpdateGM(gmInfo.Account, objParams);
                        gmInfo = null;
                    }
                }
                if (gmInfo == null || gmInfo.Validate == false) throw new Exception("not found GM with token");
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
                else
                {
                    // store in db cache
                    if (this.mDBCacheCLI != null)
                    {
                        string key = string.Format("GMInfo:{0}", gmInfo.Account);
                        string content = JsonConvert.SerializeObject(gmInfo);
                        this.mDBCacheCLI.Set(key, content);
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("FindGM", ex.Message);
                Log.StoreMsg(errMsg);
                gmInfo = null;
            }
            return gmInfo;
        }

        public string UpdateGM(string account, JObject objParams)
        {
            string? errMsg = null;
            try
            {
                if (mLobbyGMInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                if (account == null) throw new Exception("account為空值");
                if (objParams == null) throw new Exception("objParams為空值");

                FilterDefinition<GMInfo> findFilter = Builders<GMInfo>.Filter.Eq("Account", account);
                List<GMInfo> gmInfos = mLobbyGMInfos.Find(findFilter).ToList();
                GMInfo gmInfo = gmInfos.Count > 0 ? gmInfos[0] : null;
                if (gmInfo == null) throw new Exception("gmInfo為空值");

                // update all data of gmInfo but formal, trial in/out coins
                List<UpdateDefinition<GMInfo>> updates = new List<UpdateDefinition<GMInfo>>();
                updates.Add(Builders<GMInfo>.Update.Set("LastUpdateDate", DateTime.UtcNow));
                if (objParams.ContainsKey("Password")) updates.Add(Builders<GMInfo>.Update.Set("Password", objParams["Password"].Value<string>()));
                if (objParams.ContainsKey("Permission")) updates.Add(Builders<GMInfo>.Update.Set("Permission", objParams["Permission"].Value<int>()));
                if (objParams.ContainsKey("NickName")) updates.Add(Builders<GMInfo>.Update.Set("NickName", objParams["NickName"].Value<string>()));
                if (objParams.ContainsKey("AgentId")) updates.Add(Builders<GMInfo>.Update.Set("AgenrId", objParams["AgenrId"].Value<string>()));
                if (objParams.ContainsKey("GM")) updates.Add(Builders<GMInfo>.Update.Set("GM", objParams["GM"].Value<string>()));
                if (objParams.ContainsKey("Title")) updates.Add(Builders<GMInfo>.Update.Set("Title", objParams["Title"].Value<string>()));
                if (objParams.ContainsKey("PhoneNo")) updates.Add(Builders<GMInfo>.Update.Set("PhoneNo", objParams["PhoneNo"].Value<string>()));
                if (objParams.ContainsKey("Prefix")) updates.Add(Builders<GMInfo>.Update.Set("Prefix", objParams["Prefix"].Value<string>()));
                if (objParams.ContainsKey("Note")) updates.Add(Builders<GMInfo>.Update.Set("Note", objParams["Note"].Value<string>()));
                if (objParams.ContainsKey("BenefitRatio")) updates.Add(Builders<GMInfo>.Update.Set("BenefitRatio", objParams["BenefitRatio"].Value<float>()));
                if (objParams.ContainsKey("InCoins")) updates.Add(Builders<GMInfo>.Update.Set("InCoins", objParams["InCoins"].Value<Int64>()));
                if (objParams.ContainsKey("OutCoins")) updates.Add(Builders<GMInfo>.Update.Set("OutCoins", objParams["OutCoins"].Value<Int64>()));
                if (objParams.ContainsKey("InTrialCoins")) updates.Add(Builders<GMInfo>.Update.Set("InTrialCoins", objParams["InTrialCoins"].Value<Int64>()));
                if (objParams.ContainsKey("OutTrialCoins")) updates.Add(Builders<GMInfo>.Update.Set("OutTrialCoins", objParams["OutTrialCoins"].Value<Int64>()));
                if (objParams.ContainsKey("LoginErrCnt")) updates.Add(Builders<GMInfo>.Update.Set("LoginErrCnt", objParams["LoginErrCnt"].Value<int>()));
                if (objParams.ContainsKey("Validate")) updates.Add(Builders<GMInfo>.Update.Set("Validate", objParams["Validate"].Value<bool>()));
                if (objParams.ContainsKey("Token"))
                {
                    string token = objParams["Token"].Value<string>();
                    if (token == null) token = "";
                    updates.Add(Builders<GMInfo>.Update.Set("Token", token));
                    updates.Add(Builders<GMInfo>.Update.Set("TokenTime", token.Length > 0 ? DateTime.Now : Utils.zeroDateTime()));
                }

                UpdateResult ret = mLobbyGMInfos.UpdateMany(findFilter, Builders<GMInfo>.Update.Combine(updates));
                // update cache
                if (this.mDBCacheCLI != null)
                {
                    // refresh userInfo
                    FilterDefinition<GMInfo> filter = Builders<GMInfo>.Filter.Eq("_id", new ObjectId(gmInfo.GetObjectId()));
                    gmInfos = mLobbyGMInfos.Find(filter).ToList();
                    string key = string.Format("GMInfo:{0}", gmInfos[0].Account);
                    string content = JsonConvert.SerializeObject(gmInfos[0]);
                    this.mDBCacheCLI.Set(key, content);
                }
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("SaveGM", ex.Message);
            }
            return errMsg;
        }

        public GMInfo QueryGM(string account, string id, string token)
        {
            GMInfo? gmInfo = null;
            if (account != null) gmInfo = FindGMByAccount(account);
            else if (gmInfo == null && id != null) gmInfo = FindGMById(id);
            else if (gmInfo == null && token != null) gmInfo = FindGMByToken(token);
            return gmInfo;
        }

        public List<GMInfo> GetValidateGMInfos(string gmAccount)
        {
            List<GMInfo>? list = null;
            try
            {
                if (mLobbyGMInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                FilterDefinition<GMInfo> filter = Builders<GMInfo>.Filter.Eq("Validate", true);
                IFindFluent<GMInfo, GMInfo> collect = mLobbyGMInfos.Find(filter).Sort("{Account:1}");
                list = collect.ToList();
                if (gmAccount != null && gmAccount.Length > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        GMInfo gmInfo = list[i];
                        string[] gmParents = gmInfo.GM.Split(';');
                        if (gmInfo.Account != gmAccount &&
                            gmParents.Contains(gmAccount) == false)
                        {
                            list.RemoveAt(i);
                        }
                    }
                }
                foreach (GMInfo gmInfo in list)
                {
                    int gmPermission = GMInfo.PERMISSION.ConvertPermission(gmInfo.Permission);
                    if (gmPermission != gmInfo.Permission)
                    {
                        switch (gmInfo.Permission)
                        {
                            case 0x80: gmInfo.Title = "(老闆)Root"; break;
                            case 0x20: gmInfo.Title = "(經理)Manager"; break;
                            case 0x4: gmInfo.Title = "(領班)Leader"; break;
                            case 0x1: gmInfo.Title = "(員工)Employee"; break;
                        }
                        gmInfo.Permission = gmPermission;

                        JObject objParams = new JObject();
                        objParams["Title"] = gmInfo.Title;
                        objParams["Permission"] = gmInfo.Permission;
                        this.UpdateGM(gmInfo.Account, objParams);
                    }
                    // don't update DBCache to avoid massive GMInfo will cause heavy CPU loading
                    //else
                    //{
                    //    // store in db cache
                    //    if (this.mDBCacheCLI != null)
                    //    {
                    //        string key = string.Format("GMInfo:{0}", gmInfo.Account);
                    //        string content = JsonConvert.SerializeObject(gmInfo);
                    //        this.mDBCacheCLI.Set(key, content);
                    //    }
                    //}
                }
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetValidateGMInfos", ex.Message);
                Log.StoreMsg(errMsg);
                list = null;
            }
            return list;
        }

        public string UpdateGMScores(string account, JObject objScores)
        {
            string? errMsg = null;
            try
            {
                if (mLobbyGMInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (account == null) throw new Exception("account is null");
                if (objScores == null) throw new Exception("objScores is null");

                long inCoins = 0;
                long outCoins = 0;
                long inTrialCoins = 0;
                long outTrialCoins = 0;
                if (objScores.ContainsKey("InCoins"))
                {
                    long scores = objScores["InCoins"].Value<long>();
                    if (scores < 0) throw new Exception("InCoins must be larger than 0");
                    inCoins = scores;
                }
                if (objScores.ContainsKey("OutCoins"))
                {
                    long scores = objScores["OutCoins"].Value<long>();
                    if (scores < 0) throw new Exception("OutCoins must be larger than 0");
                    outCoins = scores;
                }
                if (objScores.ContainsKey("InTrialCoins"))
                {
                    long scores = objScores["InTrialCoins"].Value<long>();
                    if (scores < 0) throw new Exception("InTrialCoins must be larger than 0");
                    inTrialCoins = scores;
                }
                if (objScores.ContainsKey("OutTrialCoins"))
                {
                    long scores = objScores["OutTrialCoins"].Value<long>();
                    if (scores < 0) throw new Exception("OutTrialCoins must be larger than 0");
                    outTrialCoins = scores;
                }
                // don't update db here to avoid updating conflict
                UpdateScoresInfo gmScoresInfo = new UpdateGMScoresInfo(
                    account, inCoins, outCoins, inTrialCoins, outTrialCoins);
                this.PushUpdateScoresInfo(gmScoresInfo);
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("UpdateGMScores", ex.Message);
            }
            return errMsg;
        }

        public List<GMAction> GetGMActions(string account, List<string> actionFilter, int requiredCount)
        {
            DateTime beginDT = new DateTime(0);
            DateTime endDT = DateTime.UtcNow;
            List<GMAction> tmpList = GetGMActions(account, actionFilter, beginDT, endDT);

            List<GMAction> collectList = new List<GMAction>();
            int dataCnt = requiredCount < tmpList.Count ? requiredCount : tmpList.Count;
            for (int i = 0; i < dataCnt; i++) collectList.Add(tmpList[i]);
            return collectList;
        }

        public List<GMAction> GetGMActions(string account, List<string> actionFilter, DateTime sTime, DateTime eTime)
        {
            List<GMAction> collectList = new List<GMAction>();
            try
            {
                if (mGMActions == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = sTime;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();

                List<FilterDefinition<GMAction>> filters = new List<FilterDefinition<GMAction>>();
                filters.Add(Builders<GMAction>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<GMAction>.Filter.Lt("CreateTime", eTime));
                if (account != null) filters.Add(Builders<GMAction>.Filter.Eq("Account", account));
                if (actionFilter != null && actionFilter.Count > 0)
                {
                    List<FilterDefinition<GMAction>> queryActions = new List<FilterDefinition<GMAction>>();
                    for (int i = 0; i < actionFilter.Count; i++)
                    {
                        queryActions.Add(Builders<GMAction>.Filter.Eq("Action", actionFilter[i]));
                    }
                    filters.Add(Builders<GMAction>.Filter.Or(queryActions));
                }
                FilterDefinition<GMAction> filter = Builders<GMAction>.Filter.And(filters);
                IFindFluent<GMAction, GMAction> collect = mGMActions.Find(filter);
                collectList = collect.ToList();
                collectList.Sort(delegate (GMAction x, GMAction y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1;
                });
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetGMActions", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return collectList;
        }

        public string RecordGMAction(string account, string actionLog, string note)
        {
            string? errMsg = null;
            try
            {
                if (mGMActions == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                GMAction gmAction = new GMAction();
                gmAction.Account = account;
                gmAction.Action = actionLog;
                gmAction.Note = note;
                gmAction.CreateTime = DateTime.Now;

                mGMActions.InsertOne(gmAction);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("RecordGMAction", ex.Message);
            }
            return errMsg;
        }

        public string AddMachineScore(string lobbyName, int deviceId, int machineId, long inCount, long outCount, string userAccount)
        {
            string? errMsg = null;
            try
            {
                MachineScore machineScore = new MachineScore();
                machineScore.LobbyName = lobbyName;
                machineScore.ArduinoId = deviceId;
                machineScore.MachineId = machineId;
                machineScore.UserAccount = userAccount;
                machineScore.In = inCount;
                machineScore.Out = outCount;
                machineScore.CreateTime = DateTime.Now;
                machineScore.UpdateTime = DateTime.Now;
                mMachineScores.InsertOne(machineScore);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("AddMachineScore", ex.Message);
            }
            return errMsg;
        }

        public List<MachineScore> GetMachineScore(DateTime sTime, DateTime eTime, string lobbyName, int deviceId, int machineId, string userAccount)
        {
            List<MachineScore>? machineScores = null;
            try
            {
                if (mMachineScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                List<FilterDefinition<MachineScore>> filters = new List<FilterDefinition< MachineScore>>();
                filters.Add(Builders<MachineScore>.Filter.Gte("UpdateTime", sTime));
                filters.Add(Builders<MachineScore>.Filter.Lt("UpdateTime", eTime));
                if (lobbyName != null) filters.Add(Builders<MachineScore>.Filter.Eq("LobbyName", lobbyName));
                if (deviceId >= 0) filters.Add(Builders<MachineScore>.Filter.Eq("ArduinoId", deviceId));
                if (machineId >= 0) filters.Add(Builders<MachineScore>.Filter.Eq("MachineId", machineId));
                if (userAccount != null && userAccount.Length > 0) filters.Add(Builders<MachineScore>.Filter.Eq("UserAccount", userAccount));
                FilterDefinition<MachineScore> filter = Builders<MachineScore>.Filter.And(filters);

                IFindFluent<MachineScore, MachineScore> scoreCollect = mMachineScores.Find(filter);
                machineScores = new List<MachineScore>();
                machineScores.AddRange(scoreCollect.ToList());
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetMachineScore", ex.Message);
                Log.StoreMsg(errMsg);
                machineScores = null;
            }
            return machineScores;
        }

        public string ResetMachineScore(string lobbyName, int deviceId, int machineId)
        {
            string? errMsg = null;
            try
            {
                if (mMachineScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                DateTime sTime = new DateTime(0);
                DateTime eTime = DateTime.Now;
                List<FilterDefinition<MachineScore>> filters = new List<FilterDefinition<MachineScore>>();
                filters.Add(Builders<MachineScore>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<MachineScore>.Filter.Lt("CreateTime", eTime));
                if (lobbyName != null) filters.Add(Builders<MachineScore>.Filter.Eq("LobbyName", lobbyName));
                if (deviceId >= 0) filters.Add(Builders<MachineScore>.Filter.Eq("ArduinoId", deviceId));
                if (machineId >= 0) filters.Add(Builders<MachineScore>.Filter.Eq("MachineId", machineId));
                FilterDefinition<MachineScore> filter = Builders<MachineScore>.Filter.And(filters);
                IFindFluent<MachineScore, MachineScore> scoreCollect = mMachineScores.Find(filter);
                List<string> errMsgs = new List<string>();
                List<MachineScore> machineScores = new List<MachineScore>();
                machineScores.AddRange(scoreCollect.ToList());
                for (int i = 0; i < machineScores.Count; i++)
                {
                    MachineScore machineScore = machineScores[i];

                    FilterDefinition<MachineScore> updateFilter = Builders<MachineScore>.Filter.Eq("_id", new ObjectId(machineScore.GetObjectId()));
                    List<UpdateDefinition<MachineScore>> updates = new List<UpdateDefinition<MachineScore>>();
                    updates.Add(Builders<MachineScore>.Update.Set("In", 0));
                    updates.Add(Builders<MachineScore>.Update.Set("Out", 0));
                    updates.Add(Builders<MachineScore>.Update.Set("UpdateTime", DateTime.Now));

                    UpdateResult ret = mMachineScores.UpdateOne(updateFilter, Builders<MachineScore>.Update.Combine(updates));
                    System.Threading.Thread.Sleep(1);
                }

                if (errMsgs.Count > 0) throw new Exception(JsonConvert.SerializeObject(errMsgs));
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("ResetMachineScore", ex.Message);
            }
            return errMsg;
        }

        public string RecordMachineLog(string lobby, int deviceId, int machineId, string action, JObject logObj, string userAccount)
        {
            string? errMsg = null;
            try
            {
                if (mMachineLogs == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                MachineLog machineLog = new MachineLog();
                machineLog.Action = action;
                machineLog.LobbyName = lobby;
                machineLog.ArduinoId = deviceId;
                machineLog.MachineId = machineId;
                machineLog.Log = JsonConvert.SerializeObject(logObj);
                machineLog.UserAccount = userAccount;
                machineLog.CreateTime = DateTime.Now;

                mMachineLogs.InsertOne(machineLog);

                switch (action)
                {
                    case MachineLog.InScore:
                        {
                            int inCount = logObj.ContainsKey("scoreCnt") ? logObj["scoreCnt"].Value<int>() : 0;
                            this.AddMachineScore(lobby, deviceId, machineId, inCount, 0, userAccount);
                        }
                        break;
                    case MachineLog.OutScore:
                        {
                            int outCount = logObj.ContainsKey("scoreCnt") ? logObj["scoreCnt"].Value<int>() : 0;
                            this.AddMachineScore(lobby, deviceId, machineId, 0, outCount, userAccount);
                        }
                        break;
                }
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("RecordMachineLog", ex.Message);
            }
            return errMsg;
        }

        public List<MachineLog> GetMachineLog(DateTime sTime, DateTime eTime, List<string> actionLogs, string userAccount)
        {
            List<MachineLog> newLogs = new List<MachineLog>();
            try
            {
                if (mMachineLogs == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                List<FilterDefinition<MachineLog>> conditions = new List<FilterDefinition<MachineLog>>();
                conditions.Add(Builders<MachineLog>.Filter.Gte("CreateTime", sTime));
                conditions.Add(Builders<MachineLog>.Filter.Lt("CreateTime", eTime));
                if (actionLogs != null && actionLogs.Count > 0)
                {
                    List<FilterDefinition<MachineLog>> queryActions = new List<FilterDefinition<MachineLog>>();
                    for (int i = 0; i < actionLogs.Count; i++)
                    {
                        queryActions.Add(Builders<MachineLog>.Filter.Eq("Action", actionLogs[i]));
                    }
                    conditions.Add(Builders<MachineLog>.Filter.Or(queryActions));
                }
                if (userAccount != null && userAccount.Length > 0) conditions.Add(Builders<MachineLog>.Filter.Eq("UserAccount", userAccount));
                FilterDefinition<MachineLog> filter = Builders<MachineLog>.Filter.And(conditions);
                IFindFluent<MachineLog, MachineLog> logCollect = mMachineLogs.Find(filter);
                newLogs = logCollect.ToList();
                newLogs.Reverse();
                newLogs.Sort(delegate (MachineLog x, MachineLog y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1;
                });
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetMachineLog", ex.Message);
                Log.StoreMsg(errMsg);
                newLogs = null;
            }
            return newLogs;
        }

        public JObject GetUnbillingAddSubCoinsRecords(string gm)
        {
            JObject resObj = new JObject();
            try
            {
                if (mGMActions == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                List<FilterDefinition<GMAction>> billFilters = new List<FilterDefinition<GMAction>>();
                billFilters.Add(Builders<GMAction>.Filter.Eq("Account", gm));
                billFilters.Add(Builders<GMAction>.Filter.Eq("Action", GMAction.CalBill));
                FilterDefinition<GMAction> billFilter = Builders<GMAction>.Filter.And(billFilters);
                IFindFluent<GMAction, GMAction> billCollect = mGMActions.Find(billFilter);
                List<GMAction> calBillActions = billCollect.ToList();
                calBillActions.Reverse();
                calBillActions.Sort(delegate (GMAction x, GMAction y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1;
                });
                DateTime sTime = calBillActions.Count > 0 ? calBillActions[0].CreateTime : Utils.zeroDateTime();
                DateTime eTime = DateTime.Now;

                List<FilterDefinition<GMAction>> filters = new List<FilterDefinition<GMAction>>();
                filters.Add(Builders<GMAction>.Filter.Eq("Account", gm));
                filters.Add(Builders<GMAction>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<GMAction>.Filter.Lt("CreateTime", eTime));

                List<FilterDefinition<GMAction>> addSubfilters = new List<FilterDefinition<GMAction>>();
                addSubfilters.Add(Builders<GMAction>.Filter.Eq("Action", GMAction.AddUserCoins));
                addSubfilters.Add(Builders<GMAction>.Filter.Eq("Action", GMAction.SubUserCoins));
                FilterDefinition<GMAction> addSubfilter = Builders<GMAction>.Filter.Or(addSubfilters);
                filters.Add(addSubfilter);

                FilterDefinition<GMAction> addFilter = Builders<GMAction>.Filter.And(filters);
                IFindFluent<GMAction, GMAction> collect = mGMActions.Find(addFilter);
                List<GMAction> actions = collect.ToList();

                Dictionary<string, JArray> records = new Dictionary<string, JArray>();
                for (int i = 0; i < actions.Count; i++)
                {
                    JObject objNote = JObject.Parse(actions[i].Note);
                    string vipAccount = null;
                    if (objNote.ContainsKey("Account")) vipAccount = objNote["Account"].Value<string>();
                    else if (objNote.ContainsKey("vipAccount")) vipAccount = objNote["vipAccount"].Value<string>();

                    if (vipAccount != null)
                    {
                        string coins = null;
                        if (objNote.ContainsKey("Coins")) coins = objNote["Coins"].Value<string>();
                        else if (objNote.ContainsKey("coins")) coins = objNote["coins"].Value<string>();
                        // skip this data
                        if (coins == null)
                            continue;
                        JObject objRecord = new JObject();
                        objRecord["gm"] = actions[i].Account;
                        objRecord["account"] = vipAccount;
                        objRecord["action"] = actions[i].Action;
                        objRecord["coins"] = int.Parse(coins);
                        objRecord["date"] = actions[i].CreateTime;
                        // if new list, create & add to dic
                        if (records.ContainsKey(vipAccount) == false) records[vipAccount] = new JArray();
                        records[vipAccount].Add(objRecord);
                    }
                }
                JArray arrRecord = new JArray();
                foreach (var item in records)
                {
                    arrRecord.Add(item.Value);
                }

                resObj["status"] = "success";
                resObj["preTime"] = sTime;
                resObj["records"] = arrRecord;
            }
            catch(Exception ex)
            {
                resObj["status"] = "failed";
                resObj["error"] = BuildErrMsg("GetUnbillingAddSubCoinsRecords", ex.Message);
            }
            return resObj;
        }

        public string RecordBill(string account, int totalCoins, string note)
        {
            string? errMsg = null;
            try
            {
                if (mGMBills == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                BillInfo billInfo = new BillInfo();
                billInfo.GM = account;
                billInfo.Note = note;
                billInfo.CreateTime = DateTime.Now;

                mGMBills.InsertOne(billInfo);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("RecordBill", ex.Message);
            }
            return errMsg;
        }

        public List<BillInfo> GetBills(string account, bool bFiltGM, DateTime sTime, DateTime eTime)
        {
            List<BillInfo> billInfoList = new List<BillInfo>();
            try
            {
                if (mUserActions == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                List<FilterDefinition<BillInfo>> filters = new List<FilterDefinition<BillInfo>>();
                filters.Add(Builders<BillInfo>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<BillInfo>.Filter.Lt("CreateTime", eTime));
                FilterDefinition<BillInfo> filter = Builders<BillInfo>.Filter.And(filters);
                IFindFluent<BillInfo, BillInfo> collect = mGMBills.Find(filter);
                List<BillInfo> recordList = collect.ToList();

                if (bFiltGM)
                {
                    Dictionary<string, bool> dicGMIsSameFamily = new Dictionary<string, bool>();
                    dicGMIsSameFamily[account] = true;
                    for (int i = recordList.Count-1; i >= 0 ; i--)
                    {
                        string gmAccount = recordList[i].GM;
                        if (dicGMIsSameFamily.ContainsKey(gmAccount) == false)
                        {
                            GMInfo gmInfo = FindGMByAccount(gmAccount);
                            if (gmInfo != null)
                            {
                                string[] parentGMs = gmInfo.GM.Split(';');
                                dicGMIsSameFamily[gmAccount] = parentGMs.Contains<string>(account) ? true : false;
                            }
                            else
                            {
                                dicGMIsSameFamily[gmAccount] = false;
                            }
                        }
                        // add to billInfoList
                        if (dicGMIsSameFamily[gmAccount]) billInfoList.Add(recordList[i]);
                    }
                }
                else
                {
                    billInfoList = recordList;
                    billInfoList.Reverse();
                }
                billInfoList.Sort(delegate (BillInfo x, BillInfo y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1;
                });
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetBills", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return billInfoList;
        }

        // ===================================================================================================================
        UserInfo FindUserByAccount(string account)
        {
            UserInfo? userInfo = null;
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (account == null) throw new Exception("null account");

                if (this.mDBCacheCLI != null)
                {
                    try
                    {
                        string key = string.Format("UserInfo:{0}", account);
                        JObject obj = this.mDBCacheCLI.GetJObject(key);
                        if (obj != null) userInfo = UserInfo.FromJson(obj);
                    }
                    catch (Exception ex) {}
                }
                if (userInfo == null)
                {
                    // query from mongo db
                    FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.Eq("Account", account);
                    List<UserInfo> userInfos = mLobbyUserInfos.Find(filter).ToList();
                    userInfo = userInfos.Count > 0 ? userInfos[0] : null;
                    // store in db cache
                    if (this.mDBCacheCLI != null && userInfo != null)
                    {
                        string key = string.Format("UserInfo:{0}", userInfo.Account);
                        string content = JsonConvert.SerializeObject(userInfo);
                        this.mDBCacheCLI.Set(key, content);
                    }
                }
                if (userInfo != null && userInfo.Validate == false) userInfo = null;
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("FindUser", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return userInfo;
        }

        UserInfo FindUserById(string userId)
        {
            UserInfo? userInfo = null;
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (userId == null) throw new Exception("null userId");
                // query from mongo db
                FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.Eq("_id", new ObjectId(userId));
                List<UserInfo> userInfos = mLobbyUserInfos.Find(filter).ToList();
                userInfo = userInfos.Count > 0 ? userInfos[0] : null;
                // store in db cache
                if (this.mDBCacheCLI != null && userInfo != null)
                {
                    string key = string.Format("UserInfo:{0}", userInfo.Account);
                    string content = JsonConvert.SerializeObject(userInfo);
                    this.mDBCacheCLI.Set(key, content);
                }
                if (userInfo != null && userInfo.Validate == false) userInfo = null;
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("FindUser", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return userInfo;
        }

        UserInfo FindUserByToken(string token)
        {
            UserInfo? userInfo = null;
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (token == null) throw new Exception("null token");
                // query from mongo db
                FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.Eq("Token", token);
                List<UserInfo> userInfos = mLobbyUserInfos.Find(filter).ToList();
                userInfo = userInfos.Count > 0 ? userInfos[0] : null;
                if (userInfo != null)
                {
                    // check if token time expired...
                    TimeSpan timeSpan = DateTime.Now - userInfo.TokenTime;
                    if (timeSpan.TotalSeconds > mTokenExpiredTimeSpan)
                    {
                        userInfo.Token = "";
                        userInfo.TokenTime = Utils.zeroDateTime();
                        // update db
                        JObject objParams = new JObject();
                        objParams["Token"] = userInfo.Token;
                        objParams["TokenTime"] = userInfo.TokenTime;
                        this.UpdateUser(userInfo.Account, objParams);
                        // clear
                        userInfo = null;
                    }
                    else
                    {
                        // store in db cache
                        if (this.mDBCacheCLI != null)
                        {
                            string key = string.Format("UserInfo:{0}", userInfo.Account);
                            string content = JsonConvert.SerializeObject(userInfo);
                            this.mDBCacheCLI.Set(key, content);
                        }
                    }
                }
                if (userInfo != null && userInfo.Validate == false) userInfo = null;
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("FindUser", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return userInfo;
        }

        List<UserInfo> FindUsersByTel(string PhoneNo)
        {
            List<UserInfo>? userInfos = null;
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                if (PhoneNo == null) throw new Exception("PhoneNo is null");

                List<FilterDefinition<UserInfo>> filters = new List<FilterDefinition<UserInfo>>();
                filters.Add(Builders<UserInfo>.Filter.Eq("PhoneNo", PhoneNo));
                filters.Add(Builders<UserInfo>.Filter.Eq("Validate", true));
                FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.And(filters);
                IFindFluent<UserInfo, UserInfo> collect = mLobbyUserInfos.Find(filter).Sort("{Account:1}");

                userInfos = collect.ToList();
                // store in db cache
                if (this.mDBCacheCLI != null)
                {
                    foreach(UserInfo userInfo in userInfos)
                    {
                        string key = string.Format("UserInfo:{0}", userInfo.Account);
                        string content = JsonConvert.SerializeObject(userInfo);
                        this.mDBCacheCLI.Set(key, content);
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("FindUserByTel", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return userInfos;
        }
        
        public JArray GetAddSubCoinsRecords(string account, List<string> queriedActions, DateTime sTime, DateTime eTime)
        {
            JArray? arrRecord = null;
            try
            {
                if (mGMActions == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                List<GMAction> actions = new List<GMAction>();
                for (int i = 0; i < queriedActions.Count; i++)
                {
                    List<FilterDefinition<GMAction>> filters = new List<FilterDefinition<GMAction>>();
                    filters.Add(Builders<GMAction>.Filter.Eq("Action", queriedActions[i]));
                    filters.Add(Builders<GMAction>.Filter.Gte("CreateTime", sTime));
                    filters.Add(Builders<GMAction>.Filter.Lt("CreateTime", eTime));
                    FilterDefinition<GMAction> filter = Builders<GMAction>.Filter.And(filters);
                    IFindFluent<GMAction, GMAction> addCollect = mGMActions.Find(filter);
                    actions.AddRange(addCollect.ToList());
                }

                // check matched VIP account
                if (account != null)
                {
                    List<GMAction> newList = new List<GMAction>();
                    for (int i = 0; i < actions.Count; i++)
                    {
                        JObject objNote = JObject.Parse(actions[i].Note);
                        string vipAccount = null;
                        if (objNote.ContainsKey("Account")) vipAccount = objNote["Account"].Value<string>();
                        else if (objNote.ContainsKey("vipAccount")) vipAccount = objNote["vipAccount"].Value<string>();
                        // add to list
                        if (vipAccount == account)
                        {
                            newList.Add(actions[i]);
                        }
                    }
                    actions = newList;
                }

                Dictionary<string, JArray> records = new Dictionary<string, JArray>();
                for (int i = 0; i < actions.Count; i++)
                {
                    JObject objNote = JObject.Parse(actions[i].Note);
                    string targetAccount = null;
                    if (objNote.ContainsKey("Account")) targetAccount = objNote["Account"].Value<string>();
                    else if (objNote.ContainsKey("vipAccount")) targetAccount = objNote["vipAccount"].Value<string>();

                    if (targetAccount != null)
                    {
                        string coins = null;
                        if (objNote.ContainsKey("Coins")) coins = objNote["Coins"].Value<string>();
                        else if (objNote.ContainsKey("coins")) coins = objNote["coins"].Value<string>();
                        // skip this data
                        if (coins == null)
                            continue;
                        string reason = objNote.ContainsKey("Reason") ? objNote["Reason"].Value<string>() : null;
                        if (reason == null || reason == "一般下分" || reason == "NormalScore")
                        {
                            reason = Config.ScoringUsual;
                        }

                        JObject objRecord = new JObject();
                        objRecord["gm"] = actions[i].Account;
                        objRecord["account"] = targetAccount;
                        objRecord["action"] = actions[i].Action;
                        objRecord["coins"] = int.Parse(coins);
                        objRecord["reason"] = reason;
                        objRecord["date"] = actions[i].CreateTime;
                        // if new list, create & add to dic
                        if (records.ContainsKey(targetAccount) == false) records[targetAccount] = new JArray();
                        records[targetAccount].Add(objRecord);
                    }
                }
                arrRecord = new JArray();
                foreach (var item in records)
                {
                    arrRecord.Add(item.Value);
                }
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetAddSubCoinsRecords", ex.Message);
                Log.StoreMsg(errMsg);
                arrRecord = null;
            }
            return arrRecord;
        }

        public string RecordWainZhu(string account, int scoreCnt, int minCnt, int maxCnt)
        {
            string? errMsg = null;
            try
            {
                if (mWainZhus == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UserInfo userInfo = FindUserByAccount(account);
                if (userInfo == null) throw new Exception(string.Format("帳號{0}不存在", account));

                if (minCnt != maxCnt &&
                    scoreCnt >= minCnt &&
                    scoreCnt <= maxCnt)
                {
                    WainZhu wainZhu = new WainZhu();
                    wainZhu.User = account;
                    wainZhu.ScoreCnt = scoreCnt;
                    wainZhu.CreateTime = DateTime.Now;

                    mWainZhus.InsertOne(wainZhu);
                }
                else
                {
                    throw new Exception(string.Format("萬株次數:{0}，不符範圍{1}~{2}", scoreCnt, minCnt, maxCnt));
                }
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("RecordWainZhu", ex.Message);
            }
            return errMsg;
        }

        public List<WainZhu> GetWainZhuRecords(string account)
        {
            List<WainZhu>? recordList = null;
            try
            {
                if (mWainZhus == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                FilterDefinition<WainZhu> filter = Builders<WainZhu>.Filter.Empty;
                if (account != null && account.Length > 0)
                {
                    filter = Builders<WainZhu>.Filter.Eq("User", account);
                }
                IFindFluent<WainZhu, WainZhu> collect = mWainZhus.Find(filter);
                recordList = collect.ToList();
                recordList.Reverse();
                recordList.Sort(delegate (WainZhu x, WainZhu y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1;
                });
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetWainZhuRecords", ex.Message);
                Log.StoreMsg(errMsg);
                recordList = null;
            }
            return recordList;
        }

        public string ClearWainZhuRecords()
        {
            string? errMsg = null;
            try
            {
                if (mWainZhus == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                DeleteResult ret = mWainZhus.DeleteMany(Builders<WainZhu>.Filter.Empty);
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("ClearWainZhuRecords", ex.Message);
            }
            return errMsg;
        }

        public string RecordUnknownOutScore(string lobbyName, string machineName, int deviceId, int machineId, int singleOutScore, long outScoreCnt)
        {
            string? errMsg = null;
            try
            {
                if (mUnknownOutScores == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                UnknownOutScore unknownOutScore = new UnknownOutScore();
                unknownOutScore.LobbyName = lobbyName != null ? lobbyName : "不明/unKnow";
                unknownOutScore.MachineName = machineName != null ? machineName : "不明/unKnow";
                unknownOutScore.ArduinoId = deviceId;
                unknownOutScore.MachineId = machineId;
                unknownOutScore.SingleOutScore = singleOutScore;
                unknownOutScore.OutScoreCnt = outScoreCnt;
                unknownOutScore.CreateTime = DateTime.Now;

                mUnknownOutScores.InsertOne(unknownOutScore);
            }
            catch (Exception ex)
            {
                errMsg = BuildErrMsg("RecordUnknownOutScore", ex.Message);
            }

            return errMsg;
        }

        public List<UnknownOutScore> GetUnknownOutScore(DateTime sTime, DateTime eTime)
        {
            List<UnknownOutScore>? unknowOutScoreList = null;
            try
            {
                if (mUserActions == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                List<FilterDefinition<UnknownOutScore>> filters = new List<FilterDefinition<UnknownOutScore>>();
                filters.Add(Builders<UnknownOutScore>.Filter.Gte("CreateTime", sTime));
                filters.Add(Builders<UnknownOutScore>.Filter.Lt("CreateTime", eTime));
                FilterDefinition<UnknownOutScore> filter = Builders<UnknownOutScore>.Filter.And(filters);
                IFindFluent<UnknownOutScore, UnknownOutScore> collect = mUnknownOutScores.Find(filter);
                unknowOutScoreList = collect.ToList();
                unknowOutScoreList.Reverse();
                unknowOutScoreList.Sort(delegate (UnknownOutScore x, UnknownOutScore y)
                {
                    return x.CreateTime.Ticks < y.CreateTime.Ticks ? 1 : -1;
                });
            }
            catch(Exception ex)
            {
                string errMsg = BuildErrMsg("GetUnknownOutScore", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return unknowOutScoreList;
        }

        public string RemoveDataInCollection(string collection, DateTime sTime, DateTime eTime)
        {
            string? errMsg = null;
            try
            {
                TimeSpan duration = eTime - sTime;
                if (duration.TotalSeconds < 0)
                {
                    DateTime tmp = sTime;
                    sTime = eTime;
                    eTime = tmp;
                }

                sTime = sTime.ToUniversalTime();
                eTime = eTime.ToUniversalTime();
                DeleteResult ret = null;
                switch (collection)
                {
                    case "Agents":
                        {
                            List<FilterDefinition<AgentInfo>> filters = new List<FilterDefinition<AgentInfo>>();
                            filters.Add(Builders<AgentInfo>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<AgentInfo>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<AgentInfo> filter = Builders<AgentInfo>.Filter.And(filters);
                            ret = mAgentInfos.DeleteMany(Builders<AgentInfo>.Filter.And(filter));
                        }
                        break;
                    case "Logins":
                        {
                            List<FilterDefinition<LoginInfo>> filters = new List<FilterDefinition<LoginInfo>>();
                            filters.Add(Builders<LoginInfo>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<LoginInfo>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<LoginInfo> filter = Builders<LoginInfo>.Filter.And(filters);
                            ret = mLoginInfos.DeleteMany(filter);
                        }
                        break;
                    case "Users":
                        {
                            List<FilterDefinition<UserInfo>> filters = new List<FilterDefinition<UserInfo>>();
                            filters.Add(Builders<UserInfo>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<UserInfo>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.And(filters);
                            ret = mLobbyUserInfos.DeleteMany(filter);
                        }
                        break;
                    case "Actions":
                        {
                            List<FilterDefinition<UserAction>> filters = new List<FilterDefinition<UserAction>>();
                            filters.Add(Builders<UserAction>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<UserAction>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<UserAction> filter = Builders<UserAction>.Filter.And(filters);
                            ret = mUserActions.DeleteMany(filter);
                        }
                        break;
                    case "Scores":
                        {
                            List<FilterDefinition<ScoreInfo>> filters = new List<FilterDefinition<ScoreInfo>>();
                            filters.Add(Builders<ScoreInfo>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<ScoreInfo>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<ScoreInfo> filter = Builders<ScoreInfo>.Filter.And(filters);
                            ret = mUserScores.DeleteMany(filter);
                        }
                        break;
                    case "GameMgrs":
                        {
                            List<FilterDefinition<GMInfo>> filters = new List<FilterDefinition<GMInfo>>();
                            filters.Add(Builders<GMInfo>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<GMInfo>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<GMInfo> filter = Builders<GMInfo>.Filter.And(filters);
                            ret = mLobbyGMInfos.DeleteMany(filter);
                        }
                        break;
                    case "GMActions":
                        {
                            List<FilterDefinition<GMAction>> filters = new List<FilterDefinition<GMAction>>();
                            filters.Add(Builders<GMAction>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<GMAction>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<GMAction> filter = Builders<GMAction>.Filter.And(filters);
                            ret = mGMActions.DeleteMany(filter);
                        }
                        break;
                    case "MachineLogs":
                        {
                            List<FilterDefinition<MachineLog>> filters = new List<FilterDefinition<MachineLog>>();
                            filters.Add(Builders<MachineLog>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<MachineLog>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<MachineLog> filter = Builders<MachineLog>.Filter.And(filters);
                            ret = mMachineLogs.DeleteMany(filter);
                        }
                        break;
                    case "GMBills":
                        {
                            List<FilterDefinition<BillInfo>> filters = new List<FilterDefinition<BillInfo>>();
                            filters.Add(Builders<BillInfo>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<BillInfo>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<BillInfo> filter = Builders<BillInfo>.Filter.And(filters);
                            ret = mGMBills.DeleteMany(filter);
                        }
                        break;
                    case "UserGift":
                        {
                            List<FilterDefinition<GiftInfo>> filters = new List<FilterDefinition<GiftInfo>>();
                            filters.Add(Builders<GiftInfo>.Filter.Gte("CreateTime", sTime));
                            filters.Add(Builders<GiftInfo>.Filter.Lt("CreateTime", eTime));
                            FilterDefinition<GiftInfo> filter = Builders<GiftInfo>.Filter.And(filters);
                            ret = mUserGifts.DeleteMany(filter);
                        }
                        break;
                }
            }
            catch(Exception ex)
            {
                errMsg = BuildErrMsg("RemoveDataInCollection", string.Format("資料表{0}移除{1}到{2}的資料錯誤",
                    collection, sTime.ToShortDateString(), eTime.ToShortDateString(), ex.Message));
            }
            return errMsg;
        }

        public string VerifySMSSender(string ip, int maxCnt)
        {
            string? errMsg = null;
            try
            {
                if (ip == null) throw new Exception("null IP");
                if (ip.Contains("localhost") == false &&
                    ip.Contains("127.0.0.1") == false &&
                    ip.Contains("::1") == false)
                {
                    FilterDefinition <SMSSender> filter = Builders<SMSSender>.Filter.Eq("IP", ip);
                    IFindFluent<SMSSender, SMSSender> collect = mSMSSenders.Find(filter);
                    // check if multi userInfo use same phoneNo
                    List<SMSSender> list = collect.ToList();
                    if (list.Count > 0)
                    {
                        List<UpdateDefinition<SMSSender>> updates = new List<UpdateDefinition<SMSSender>>();
                        if (maxCnt > 0)
                        {
                            SMSSender smsSender = list[0];
                            if (smsSender.SnedCnt > maxCnt)
                            {
                                TimeSpan ts = DateTime.Now - smsSender.LastUpdateDate;
                                if (ts.TotalHours < 24) throw new Exception("VerifySMSSender too many times");
                                // clear SnedCnt
                                smsSender.SnedCnt = 0;
                            }

                            updates.Add(Builders<SMSSender>.Update.Set("SnedCnt", smsSender.SnedCnt + 1));
                            updates.Add(Builders<SMSSender>.Update.Set("LastUpdateDate", DateTime.UtcNow));
                        }
                        else
                        {
                            updates.Add(Builders<SMSSender>.Update.Set("SnedCnt", 0));
                            updates.Add(Builders<SMSSender>.Update.Set("LastUpdateDate", Utils.zeroDateTime()));
                        }
                        mSMSSenders.UpdateMany(filter, Builders<SMSSender>.Update.Combine(updates));
                    }
                    else
                    {
                        SMSSender smsSender = new SMSSender();
                        smsSender.IP = ip;
                        smsSender.SnedCnt = 1;
                        mSMSSenders.InsertOne(smsSender);
                    }
                }
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public VerifyInfo CreateVerifyInfo(string userAccount, string phoneNo)
        {
            VerifyInfo? verifyInfo = null;
            try
            {
                UserInfo userInfo = FindUserByAccount(userAccount);
                if (userInfo == null) throw new Exception(string.Format("user {0} doesn't exist", userAccount));
                if (userInfo.Verified) throw new Exception(string.Format("user {0} had been verified", userAccount));
                // check if any other verified userInfo already uses specified phoneNo, then reject it
                if (mLobbyUserInfos != null)
                {
                    List<FilterDefinition<UserInfo>> filters = new List<FilterDefinition<UserInfo>>();
                    filters.Add(Builders<UserInfo>.Filter.Ne("Account", userAccount));
                    filters.Add(Builders<UserInfo>.Filter.Eq("PhoneNo", phoneNo));
                    filters.Add(Builders<UserInfo>.Filter.Eq("Validate", true));
                    FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.And(filters);
                    IFindFluent<UserInfo, UserInfo> collect = mLobbyUserInfos.Find(filter);
                    // check if multi userInfo use same phoneNo
                    List<UserInfo> userInfos = collect.ToList();
                    // check if any left userInfo is already verified, reject CreateVerifyInfo
                    for (int i = 0; i < userInfos.Count; i++)
                    {
                        if (userInfos[i].Verified) throw new Exception("PhoneNo already be used");
                    }
                }

                if (mVerifyInfos != null)
                {
                    string code = VerifyInfo.GenerateCode();
                    // check if user already require verifyCode
                    FilterDefinition<VerifyInfo> findFilter = Builders<VerifyInfo>.Filter.Eq("Account", userAccount);
                    IFindFluent<VerifyInfo, VerifyInfo> collect = mVerifyInfos.Find(findFilter);
                    List<VerifyInfo> verifyInfos = new List<VerifyInfo>();
                    // should be only one if exists
                    verifyInfos.AddRange(collect.ToList());
                    if (verifyInfos.Count > 0)
                    {
                        // remove others
                        for (int i = 1; i < verifyInfos.Count; i++)
                        {
                            FilterDefinition<VerifyInfo> delFilter = Builders<VerifyInfo>.Filter.Eq("_id", verifyInfos[i]._id);
                            mVerifyInfos.DeleteOne(delFilter);
                        }

                        verifyInfo = verifyInfos[0];

                        List<UpdateDefinition<VerifyInfo>> updates = new List<UpdateDefinition<VerifyInfo>>();
                        DateTime curTime = DateTime.UtcNow;
                        DateTime lastUpdateTime = verifyInfo.UpdateTime;
                        if (curTime.Year != lastUpdateTime.Year ||
                            curTime.DayOfYear != lastUpdateTime.DayOfYear)
                        {
                            // reset daily verify cnt
                            updates.Add(Builders<VerifyInfo>.Update.Set("OneDayUpdateCnt", 0));
                        }
                        // if found user had applied verifying, refresh its verifyCode & CreateTime
                        TimeSpan ts = curTime - verifyInfo.UpdateTime;
                        verifyInfo.PhoneNo = phoneNo;
                        if (ts.TotalMinutes > 10) updates.Add(Builders<VerifyInfo>.Update.Set("Code", code));
                        updates.Add(Builders<VerifyInfo>.Update.Set("OneDayUpdateCnt", verifyInfo.OneDayUpdateCnt + 1));
                        updates.Add(Builders<VerifyInfo>.Update.Set("UpdateTime", DateTime.UtcNow));
                        FilterDefinition<VerifyInfo> filter = Builders<VerifyInfo>.Filter.Eq("_id", verifyInfo._id);
                        mVerifyInfos.UpdateMany(filter, Builders<VerifyInfo>.Update.Combine(updates));
                    }
                    else
                    {
                        verifyInfo = new VerifyInfo();
                        verifyInfo.Account = userAccount;
                        verifyInfo.PhoneNo = phoneNo;
                        verifyInfo.Code = code;
                        mVerifyInfos.InsertOne(verifyInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                verifyInfo = null;
            }
            return verifyInfo;
        }

        public VerifyInfo ConfirmVerifyInfo(string userAccount, string code, string ip)
        {
            VerifyInfo? verifyInfo = null;
            try
            {
                // check if user already require verifyCode
                List<FilterDefinition<VerifyInfo>> filters = new List<FilterDefinition<VerifyInfo>>();
                filters.Add(Builders<VerifyInfo>.Filter.Eq("Account", userAccount));
                filters.Add(Builders<VerifyInfo>.Filter.Eq("Code", code));
                FilterDefinition<VerifyInfo> filter = Builders<VerifyInfo>.Filter.And(filters);
                IFindFluent<VerifyInfo, VerifyInfo> collect = mVerifyInfos.Find(filter);
                if (collect.CountDocuments() == 0) throw new Exception("verifyInfo doesn't exist");

                List<VerifyInfo> verifyInfos = new List<VerifyInfo>();
                verifyInfos.AddRange(collect.ToList());
                for (int i = 0; i < verifyInfos.Count; i++)
                {
                    VerifyInfo tmpInfo = verifyInfos[i];
                    TimeSpan ts = DateTime.UtcNow - tmpInfo.UpdateTime;
                    if (ts.TotalMinutes <= 10)
                    {
                        verifyInfo = tmpInfo;
                        break;
                    }
                }
                if (verifyInfo == null) throw new Exception(string.Format("no verifyInfo exists for {0}", userAccount));
                UserInfo usrInfo = FindUserByAccount(userAccount);
                if (usrInfo == null || !usrInfo.Validate) throw new Exception(string.Format("user {0} doesn't exist", userAccount));
                // update userInfo
                JObject objParams = new JObject();
                objParams["PhoneNo"] = verifyInfo.PhoneNo;
                objParams["Verified"] = true;
                objParams["GuoZhaoEnabled"] = true;
                // check setting in AgentInfo
                AgentInfo agentInfo = this.Agent();
                if (agentInfo.SetAfterVerifyUser != null&&
                    agentInfo.SetAfterVerifyUser.Length > 0)
                {
                    try
                    {
                        JObject objAfterVerifyUser = JObject.Parse(agentInfo.SetAfterVerifyUser);
                        if (objAfterVerifyUser.ContainsKey("Verified")) objParams["Verified"] = objAfterVerifyUser["Verified"].Value<bool>();
                        if (objAfterVerifyUser.ContainsKey("GuoZhaoEnabled")) objParams["GuoZhaoEnabled"] = objAfterVerifyUser["GuoZhaoEnabled"].Value<bool>();
                    }
                    catch(Exception ex) {}
                }

                string error = this.UpdateUser(usrInfo.Account, objParams);
                if (error != null) throw new Exception(error);
                // reset verifying cnt for ip
                if (ip != null && ip.Length > 0)
                {
                    FilterDefinition<SMSSender> filter2 = Builders<SMSSender>.Filter.Eq("IP", ip);
                    IFindFluent<SMSSender, SMSSender> collect2 = mSMSSenders.Find(filter2);
                    // check if multi userInfo use same phoneNo
                    List<SMSSender> list = collect2.ToList();
                    if (list.Count > 0)
                    {
                        List<UpdateDefinition<SMSSender>> updates = new List<UpdateDefinition<SMSSender>>();
                        updates.Add(Builders<SMSSender>.Update.Set("SnedCnt", 0));
                        updates.Add(Builders<SMSSender>.Update.Set("LastUpdateDate", Utils.zeroDateTime()));
                        mSMSSenders.UpdateMany(filter2, Builders<SMSSender>.Update.Combine(updates));
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                verifyInfo = null;
            }
            return verifyInfo;
        }

        public string CreateUniPlay(string name, string domain, string urlLogo, string desc,
            int profitPayRatio, float exchangeRatioToNTD, bool bAppliable, bool bAcceptable)
        {
            string? errMsg = null;
            try
            {
                try
                {
                    if (name == null) throw new Exception("null name");
                    if (domain == null) throw new Exception("domain name");
                    if (urlLogo == null) throw new Exception("urlLogo name");
                    if (desc == null) throw new Exception("desc name");
                    if (profitPayRatio < 0 || profitPayRatio > 100) throw new Exception("invalid profitPayRatio");
                    if (exchangeRatioToNTD < 0) throw new Exception("invalid exchangeRatioToNTD");

                    List<FilterDefinition<UniPlayInfo>> uniPlayFilters = new List<FilterDefinition<UniPlayInfo>>();
                    uniPlayFilters.Add(Builders<UniPlayInfo>.Filter.Eq("Name", name));
                    uniPlayFilters.Add(Builders<UniPlayInfo>.Filter.Eq("Domain", domain));
                    FilterDefinition<UniPlayInfo> filterUniPlay = Builders<UniPlayInfo>.Filter.Or(uniPlayFilters);
                    List<UniPlayInfo> uniPlayInfos = mUniPlayInfos.Find(filterUniPlay).ToList();

                    UniPlayInfo uniPlayInfo = uniPlayInfos.Count > 0 ? uniPlayInfos[0] : null;
                    if (uniPlayInfo != null)
                    {
                        if (uniPlayInfo.Validate) throw new Exception(string.Format("聯盟館{0}/{1}已存在", name, domain));

                        JObject obj = new JObject();
                        obj["Name"] = name;
                        obj["Domain"] = domain;
                        obj["UrlLogo"] = urlLogo;
                        obj["Desc"] = desc;
                        obj["ProfitPayRatio"] = profitPayRatio;
                        obj["ExchangeRatioToNTD"] = exchangeRatioToNTD;
                        obj["Appliable"] = bAppliable;
                        obj["Acceptable"] = bAcceptable;
                        obj["Validate"] = true;
                        string error = this.UpdateUniPlay(domain, obj);
                        if (error != null) throw new Exception(error);
                        // since UpdateUniPlay won't update CreateDate, so modify it here...
                        List<UpdateDefinition<UniPlayInfo>> updates = new List<UpdateDefinition<UniPlayInfo>>();
                        updates.Add(Builders<UniPlayInfo>.Update.Set("CreateDate", DateTime.UtcNow));
                        UpdateResult ret = mUniPlayInfos.UpdateMany(filterUniPlay, Builders<UniPlayInfo>.Update.Combine(updates));
                    }
                    else
                    {
                        uniPlayInfo = new UniPlayInfo();
                        uniPlayInfo.Name = name;
                        uniPlayInfo.Domain = domain;
                        uniPlayInfo.UrlLogo = urlLogo;
                        uniPlayInfo.Desc = desc;
                        uniPlayInfo.Desc = desc;
                        uniPlayInfo.Desc = desc;
                        uniPlayInfo.ProfitPayRatio = profitPayRatio;
                        uniPlayInfo.ExchangeRatioToNTD = exchangeRatioToNTD;
                        mUniPlayInfos.InsertOne(uniPlayInfo);
                    }
                }
                catch (Exception ex)
                {
                    errMsg = ex.Message;
                }
                return errMsg;
            }
            catch(Exception ex)
            {

            }
            return errMsg;
        }

        public string UpdateUniPlay(string domain, JObject objParams)
        {
            string? errMsg = null;
            try
            {
                if (mUniPlayInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }
                if (domain == null) throw new Exception("domain為空值");
                if (objParams == null) throw new Exception("objParams為空值");

                List<FilterDefinition<UniPlayInfo>> uniPlayFilters = new List<FilterDefinition<UniPlayInfo>>();
                uniPlayFilters.Add(Builders<UniPlayInfo>.Filter.Eq("Domain", domain));
                FilterDefinition<UniPlayInfo> findFilter = Builders<UniPlayInfo>.Filter.And(uniPlayFilters);
                List<UniPlayInfo> uniPlayInfos = mUniPlayInfos.Find(findFilter).ToList();
                UniPlayInfo uniPlayInfo = uniPlayInfos.Count > 0 ? uniPlayInfos[0] : null;
                if (uniPlayInfo == null) throw new Exception("查詢的UniPlayInfo不存在");

                List<UpdateDefinition<UniPlayInfo>> updates = new List<UpdateDefinition<UniPlayInfo>>();
                // update all data of uniPlayInfo
                if (objParams.ContainsKey("Name")) updates.Add(Builders<UniPlayInfo>.Update.Set("Name", objParams["Name"].Value<string>()));
                if (objParams.ContainsKey("Domain")) updates.Add(Builders<UniPlayInfo>.Update.Set("Domain", objParams["Domain"].Value<string>()));
                if (objParams.ContainsKey("UrlLogo")) updates.Add(Builders<UniPlayInfo>.Update.Set("UrlLogo", objParams["UrlLogo"].Value<string>()));
                if (objParams.ContainsKey("Desc")) updates.Add(Builders<UniPlayInfo>.Update.Set("Desc", objParams["Desc"].Value<string>()));
                if (objParams.ContainsKey("ProfitPayRatio")) updates.Add(Builders<UniPlayInfo>.Update.Set("ProfitPayRatio", objParams["ProfitPayRatio"].Value<int>()));
                if (objParams.ContainsKey("ExchangeRatioToNTD")) updates.Add(Builders<UniPlayInfo>.Update.Set("ExchangeRatioToNTD", objParams["ExchangeRatioToNTD"].Value<float>()));
                if (objParams.ContainsKey("Appliable")) updates.Add(Builders<UniPlayInfo>.Update.Set("Appliable", objParams["Appliable"].Value<bool>()));
                if (objParams.ContainsKey("Acceptable")) updates.Add(Builders<UniPlayInfo>.Update.Set("Acceptable", objParams["Acceptable"].Value<bool>()));
                if (objParams.ContainsKey("Validate")) updates.Add(Builders<UniPlayInfo>.Update.Set("Validate", objParams["Validate"].Value<bool>()));
                UpdateResult ret = mUniPlayInfos.UpdateMany(findFilter, Builders<UniPlayInfo>.Update.Combine(updates));
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public List<UniPlayInfo> QueryUniPlay(string name, string domain)
        {
            List<UniPlayInfo>? uniPlayInfos = null;
            try
            {
                List<FilterDefinition<UniPlayInfo>> filters = new List<FilterDefinition<UniPlayInfo>>();
                filters.Add(Builders<UniPlayInfo>.Filter.Eq("Validate", true));
                if (name != null || domain != null)
                {
                    List<FilterDefinition<UniPlayInfo>> queryKeys = new List<FilterDefinition<UniPlayInfo>>();
                    if (name != null) queryKeys.Add(Builders<UniPlayInfo>.Filter.Eq("Name", name));
                    if (domain != null) queryKeys.Add(Builders<UniPlayInfo>.Filter.Eq("Domain", domain));
                    filters.Add(Builders<UniPlayInfo>.Filter.Or(queryKeys));
                }
                FilterDefinition<UniPlayInfo> filter = Builders<UniPlayInfo>.Filter.And(filters);
                uniPlayInfos = mUniPlayInfos.Find(filter).ToList();
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("QueryUniPlay got {0}", ex.Message));
            }
            return uniPlayInfos;
        }

        public void BuildUserInfoSortIndex(string sortKey, bool bIncludeInvalidUser = false)
        {
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                List<FilterDefinition<UserInfo>> filters = new List<FilterDefinition<UserInfo>>();
                if (bIncludeInvalidUser)
                    filters.Add(Builders<UserInfo>.Filter.Ne("Account", ""));
                else
                    filters.Add(Builders<UserInfo>.Filter.Eq("Validate", true));
                FilterDefinition<UserInfo> findFilter = Builders<UserInfo>.Filter.And(filters);
                SortDefinition<UserInfo> sortDefinition = Builders<UserInfo>.Sort.Combine(Builders<UserInfo>.Sort.Ascending(sortKey));
                IFindFluent<UserInfo, UserInfo> collect = mLobbyUserInfos.Find(findFilter).Sort(sortDefinition);
                List<UserInfo> list = collect.ToList();
                int index = 0;
                foreach (UserInfo userInfo in list)
                {
                    index++;
                    JObject obj = new JObject();
                    obj["SortIndex"] = index;
                    this.UpdateUser(userInfo.Account, obj);
                    // delay for a while
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("BuildUserInfoSortIndex", ex.Message);
                Log.StoreMsg(errMsg);
            }
        }

        public void ClearUserInfoSortIndex()
        {
            try
            {
                if (mLobbyUserInfos == null)
                {
                    if (this.BuildConnection() != null) throw new Exception(TextConverter.Convert("資料庫未連線"));
                }

                List<FilterDefinition<UserInfo>> filters = new List<FilterDefinition<UserInfo>>();
                filters.Add(Builders<UserInfo>.Filter.Ne("Account", ""));
                FilterDefinition<UserInfo> findFilter = Builders<UserInfo>.Filter.And(filters);
                SortDefinition<UserInfo> sortDefinition = Builders<UserInfo>.Sort.Combine(Builders<UserInfo>.Sort.Ascending("CreateDate"));
                IFindFluent<UserInfo, UserInfo> collect = mLobbyUserInfos.Find(findFilter).Sort(sortDefinition);
                List<UserInfo> list = collect.ToList();
                foreach (UserInfo userInfo in list)
                {
                    JObject obj = new JObject();
                    obj["SortIndex"] = 0;
                    this.UpdateUser(userInfo.Account, obj);
                    // delay for a while
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                string errMsg = BuildErrMsg("BuildUserInfoSortIndex", ex.Message);
                Log.StoreMsg(errMsg);
            }
        }

        public void Test()
        {
            //int deviceId = 1;
            //int machineId = 1;
            //SCORE_TYPE scoreType = SCORE_TYPE.GuoZhao;
            //DateTime sTime = DateTime.Parse("2020-04-27T18:33:00.9540716Z");
            //DateTime eTime = DateTime.Parse("2020-04-28T02:45:57.4145609+08:00");
            //SummarizeScore("S0001", "遊戲大樂門", "齊天大聖-1(1_1) at 遊戲大樂門", deviceId, machineId, scoreType, sTime, eTime);
            
            //List<FilterDefinition<BsonDocument>> queries = new List<FilterDefinition<BsonDocument>>();
            //FilterDefinition<UserInfo> filter = Builders<UserInfo>.Filter.Regex("Account", new BsonRegularExpression("S", "i"));
            //mLobbyUserInfos.Find( filter).FirstOrDefaultAsync();
        }

        string BuildErrMsg(string funcName, string errMsg)
        {
            return string.Format("{0} got {1}", funcName, errMsg);
            //return errMsg;
        }
    }
}

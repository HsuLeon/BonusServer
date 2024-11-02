
using BonusServer.Models;
using BonusServer.Services.QueueInfo;
using BonusServer.Services.RuleTrigger;
using FunLobbyUtils;
using FunLobbyUtils.Database.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace BonusServer.Services
{
    public class BonusAgent
    {
        static string mRecordsPath = "C:/SignalR/BonusServer/BonusRecords.json";
        static List<string> mSubDomains = new List<string>();
        static Dictionary<string, string> mTokenOfDomains = new Dictionary<string, string>();
        static Mutex mMutexNotifyData = new Mutex();
        static Dictionary<string, BonusRecords> mBonusRecords = new Dictionary<string, BonusRecords>();
        static string? mJoinWinToken = null;

        static QueueInterface? mQueue = null;

        static public DateTime LaunchTime { get; set; }
        static public BonusRule? RuleTrigger { get; private set; }
        static public string? WebSite { get; set; }
        static public string? BonusServerDomain { get; set; }
        static public int BonusServerPort { get; set; }
        static public string? UpperDomain { get; set; }
        static public List<string> SubDomains { get { return mSubDomains; } }
        static public string? APITransferPoints { get; set; }
        static public float CollectSubScale { get; set; }

        public static void InitQueues(string server, string userName, string password)
        {
            try
            {
                mQueue = new CustomQInfo(); //new RabbitMQInfo(); //
                mQueue.Start(server, userName, password);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("BonusAgent InitQueues got {0}", ex.Message));
            }
        }

        public static void InitTriggers(ConfigSetting.BETWIN_RULE rule, string triggerWinA, string triggerWinB, string triggerWinCR)
        {
            switch (rule)
            {
                case ConfigSetting.BETWIN_RULE.Rule_0:
                    RuleTrigger = new BonusRule_0();
                    break;
                case ConfigSetting.BETWIN_RULE.Rule_1:
                    RuleTrigger = new BonusRule_1();
                    break;
                default: throw new Exception(string.Format("unknown rule: {0}", rule));
            }
            RuleTrigger.ParseSettings(BonusRule.WIN_TYPE.WinA, triggerWinA);
            RuleTrigger.ParseSettings(BonusRule.WIN_TYPE.WinB, triggerWinB);
            RuleTrigger.ParseSettings(BonusRule.WIN_TYPE.WinCR, triggerWinCR);
        }

        public static void RestoreRecords(string jsonPath)
        {
            mRecordsPath = jsonPath;
            BonusAgent.ImportRecords(mRecordsPath);
        }

        public static void OnChannelSpinA(JObject obj)
        {
            try
            {
                if (BonusAgent.RuleTrigger == null) throw new Exception("null RuleTrigger");
                string? errMsg = BonusAgent.RuleTrigger.TryWinSpinA(obj);
                if (errMsg != null) throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                // if obj is from sub domain, reply to its domain for sub domain handles it.
                if (obj.ContainsKey("from"))
                {
                    Task.Run(() =>
                    {
                        string? urlFrom = obj["from"]?.Value<string>();
                        JObject? data = obj["data"]?.Value<JObject>();
                        if (urlFrom != null && data != null)
                        {
                            string? token = BonusAgent.GetTokenOfDomain(urlFrom);
                            if (token != null)
                            {
                                string url = string.Format("{0}/bonus/upper/replyToWinA", urlFrom);
                                Utils.HttpPost(url, data, token);
                            }
                        }
                    });
                }
                else
                {
                    // discard...
                }
            }
        }

        public static void OnChannelSpinB(JObject obj)
        {
            try
            {
                if (BonusAgent.RuleTrigger == null) throw new Exception("null RuleTrigger");
                string? errMsg = BonusAgent.RuleTrigger?.TryWinSpinB(obj);
                if (errMsg != null) throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                // discard...
            }
        }

        public static void OnChannelSpinCR(JObject obj)
        {
            try
            {
                if (BonusAgent.RuleTrigger == null) throw new Exception("null RuleTrigger");
                string? errMsg = BonusAgent.RuleTrigger?.TryWinSpinCR(obj);
                if (errMsg != null) throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                // if obj is from sub domain, reply to its domain for sub domain handles it.
                if (obj.ContainsKey("from"))
                {
                    Task.Run(() =>
                    {
                        string? urlFrom = obj["from"]?.Value<string>();
                        JObject? data = obj["data"]?.Value<JObject>();
                        if (urlFrom != null && data != null)
                        {
                            string? token = BonusAgent.GetTokenOfDomain(urlFrom);
                            if (token != null)
                            {
                                string url = string.Format("{0}/bonus/upper/replyToWinCR", urlFrom);
                                Utils.HttpPost(url, data, token);
                            }
                        }
                    });
                }
                else
                {
                    // discard...
                }
            }
        }

        public static void StartHeartbeat()
        {
            const int timeInterval = 15 * 1000;
            Task.Run(() =>
            {
                bool bLoop = true;
                while (bLoop)
                {
                    AgentInfo? agentInfo = DBAgent.Agent();
                    if (agentInfo == null) agentInfo = DBAgent.QueryAgent();

                    Thread.Sleep(timeInterval);
                    // check queue heartbeat
                    mQueue?.FireHeartbeat();
                    // fire API heartbeat
                    WakeupAPI();
                    // join upper server
                    JoinWin();
                }
            });
        }

        public static string? AddSubDomain(string domain, string password)
        {
            string? errMsg = null;
            try
            {
                if (domain == null || domain.Length == 0) throw new Exception("null domain");
                if (mSubDomains.Contains(domain)) return null;
                // check allowed sun domain list
                bool bAllowed = false;
                ConfigSetting? configSetting = Config.Instance as ConfigSetting;
                if (configSetting == null) throw new Exception("null ConfigSetting");
                List<ConfigSetting.BonusLogin> subDomains = configSetting.SubDomains;
                foreach (ConfigSetting.BonusLogin info in subDomains)
                {
                    string domainAllowed = info.Domain;
                    string domainPassword = info.Password;

                    if (domainAllowed.IndexOf(domain) >= 0 ||
                        domain.IndexOf(domainAllowed) >= 0)
                    {
                        if (domainPassword != password) throw new Exception("invalid password");
                        bAllowed = true;
                        break;
                    }
                }
                if (bAllowed == false) throw new Exception("unknown domain");
                mSubDomains.Add(domain);
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static bool IsUpperDomain(string domain)
        {
            if (BonusAgent.UpperDomain != null &&
                BonusAgent.UpperDomain.IndexOf(domain) >= 0)
                return true;
            return false;
        }

        public static bool IsSubDomain(string domain)
        {
            foreach (var item in mSubDomains)
            {
                if (item.IndexOf(domain) >= 0) return true;
            }
            return false;
        }

        public static void AddNotifyData(string? bonusWebSite, NotifyData.WinSpin winSpinType,
            string message, int scores, string? machineName, string? userAccout)
        {
            if (winSpinType == NotifyData.WinSpin.None ||
                bonusWebSite == null ||
                message == null) return;

            mMutexNotifyData.WaitOne();
            try
            {
                if (mBonusRecords.ContainsKey(bonusWebSite) == false) mBonusRecords[bonusWebSite] = new BonusRecords();
                BonusRecords bonusRecords = mBonusRecords[bonusWebSite];
                List<NotifyData>? notifyWinList = null;
                switch (winSpinType)
                {
                    case NotifyData.WinSpin.A:
                        {
                            // assign notify WinAList
                            notifyWinList = bonusRecords.WinAList;
                        }
                        break;
                    case NotifyData.WinSpin.B:
                        {
                            // assign notify WinBList
                            notifyWinList = bonusRecords.WinBList;
                        }
                        break;
                    case NotifyData.WinSpin.CR:
                        {
                            // assign notify WinCRList
                            notifyWinList = bonusRecords.WinCRList;
                        }
                        break;
                    default:
                        throw new Exception(string.Format("unknown winSpinType: {0}", winSpinType.ToString()));
                }

                NotifyData notifyData = new NotifyData();
                notifyData.WinSpinType = winSpinType;
                notifyData.WebSite = bonusWebSite;
                notifyData.Message = message;
                notifyData.Scores = scores;
                notifyData.MachineName = machineName != null ? machineName : "";
                notifyData.UserAccount = userAccout != null ? userAccout : "神秘爺";
                notifyData.CreateTime = DateTime.Now;
                string content = JsonConvert.SerializeObject(notifyData);
                JObject objNotifyData = JObject.Parse(content);
                foreach (string domain in mSubDomains)
                {
                    string? token = BonusAgent.GetTokenOfDomain(domain);
                    string urlNotify = string.Format("{0}/system/notify", domain);
                    Utils.HttpPost(urlNotify, objNotifyData, token);
                }
                // add to tail of list
                notifyWinList.Add(notifyData);
                // keep last 10 records, remove first element if needed
                if (notifyWinList.Count > 10) notifyWinList.RemoveAt(0);
                BonusAgent.ExportRecords();
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("AddNotifyData got {0}", ex.Message));
            }
            mMutexNotifyData.ReleaseMutex();
        }

        public static string? GetTokenOfDomain(string domain)
        {
            return mTokenOfDomains.ContainsKey(domain) ? mTokenOfDomains[domain] : null;
        }

        public static void RefreshToken(string domain, string token)
        {
            if (domain == null ||  token == null) return;
            if (BonusAgent.IsSubDomain(domain) == false) return;
            mTokenOfDomains[domain] = token;
        }

        public static void CollectBonus(CollectData data)
        {
            try
            {
                CSpinCollection.BETWIN_TYPE bonusType = (CSpinCollection.BETWIN_TYPE)data.BonusType;
                switch (bonusType)
                {
                    case CSpinCollection.BETWIN_TYPE.Slot:
                        RuleTrigger?.Collect(BonusRule.WIN_TYPE.WinA, data);
                        RuleTrigger?.Collect(BonusRule.WIN_TYPE.WinB, data);
                        break;
                    case CSpinCollection.BETWIN_TYPE.Cr:
                    case CSpinCollection.BETWIN_TYPE.ChinaCr:
                        RuleTrigger?.Collect(BonusRule.WIN_TYPE.WinCR, data);
                        break;
                    default: throw new Exception(string.Format("invalid bonusType: {0}", bonusType));
                }
                // notify upper bonus server to do collecting
                if (!string.IsNullOrEmpty(BonusAgent.UpperDomain))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string url = string.Format("{0}/bonus/lower/collect", BonusAgent.UpperDomain);
                        string? strContent = JsonConvert.SerializeObject(data);
                        StringContent? content = new StringContent(strContent, Encoding.UTF8, "application/json");
                        if (mJoinWinToken != null) client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", mJoinWinToken));
                        Task<HttpResponseMessage> task = client.PostAsync(url, content);
                        task.Wait();
                        HttpResponseMessage response = task.Result;
                        if (!response.IsSuccessStatusCode) throw new Exception($"Failed to collect bonus: {response.ReasonPhrase}");
                        Task<string> task2 = response.Content.ReadAsStringAsync();
                        task2.Wait();
                        string result = task2.Result;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("CollectBonus got {0}", ex.Message));
            }
        }

        public static string? RushUpperWinSpinA(JObject objData)
        {
            string? errMsg = null;
            try
            {
                if (objData == null) throw new Exception("null objData");
                if (BonusAgent.UpperDomain == null ||
                    BonusAgent.UpperDomain.Length == 0)
                {
                    throw new Exception("no upper domain");
                }

                JObject obj = new JObject();
                obj["from"] = string.Format("{0}:{1}", BonusAgent.BonusServerDomain, BonusAgent.BonusServerPort);
                obj["data"] = objData;
                // fire API to check upper server
                using (HttpClient client = new HttpClient())
                {
                    string url = string.Format("{0}/bonus/lower/rushToWinA", BonusAgent.UpperDomain);
                    string? strContent = JsonConvert.SerializeObject(obj);
                    StringContent? content = new StringContent(strContent, Encoding.UTF8, "application/json");
                    if (mJoinWinToken != null) client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", mJoinWinToken));
                    Task<HttpResponseMessage> task = client.PostAsync(url, content);
                    task.Wait();
                    HttpResponseMessage response = task.Result;
                    if (!response.IsSuccessStatusCode) throw new Exception($"Failed to RushUpperWinSpinA: {response.ReasonPhrase}");
                    Task<string> task2 = response.Content.ReadAsStringAsync();
                    task2.Wait();
                    string result = task2.Result;
                    JObject objRes = JObject.Parse(result);
                    string? status = objRes["status"]?.Value<string>();
                    if (status != "success")
                    {
                        string? error = objRes["error"]?.Value<string>();
                        throw new Exception(error != null ? errMsg : "unknow error");
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                //Log.StoreMsg(string.Format("RushUpperWinSpinA got {0}", errMsg));
            }
            return errMsg;
        }

        public static string? PushToWinSpinA(JObject obj)
        {
            string? errMsg = null;
            try
            {
                if (BonusAgent.RuleTrigger != null &&
                    BonusAgent.RuleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinA) == false)
                {
                    throw new Exception("winA not enabled");
                }

                string message = JsonConvert.SerializeObject(obj);
                string? error = mQueue?.Publish("SpinA", message);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("PushToWinSpinA got {0}", errMsg));
            }
            return errMsg;
        }

        public static string? PushToWinSpinB(JObject obj)
        {
            string? errMsg = null;
            try
            {
                if (BonusAgent.RuleTrigger != null &&
                    BonusAgent.RuleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinB) == false)
                {
                    throw new Exception("winB not enabled");
                }
                string message = JsonConvert.SerializeObject(obj);
                string? error = mQueue?.Publish("SpinB", message);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("PushToWinSpinB got {0}", errMsg));
            }
            return errMsg;
        }


        public static string? RushUpperWinSpinCR(JObject objData)
        {
            string? errMsg = null;
            try
            {
                if (objData == null) throw new Exception("null objData");
                if (BonusAgent.UpperDomain == null ||
                    BonusAgent.UpperDomain.Length == 0)
                {
                    throw new Exception("no upper domain");
                }

                JObject obj = new JObject();
                obj["from"] = string.Format("{0}:{1}", BonusAgent.BonusServerDomain, BonusAgent.BonusServerPort);
                obj["data"] = objData;
                // fire API to check upper server
                using (HttpClient client = new HttpClient())
                {
                    string url = string.Format("{0}/bonus/lower/rushToWinCR", BonusAgent.UpperDomain);
                    string? strContent = JsonConvert.SerializeObject(obj);
                    StringContent? content = new StringContent(strContent, Encoding.UTF8, "application/json");
                    if (mJoinWinToken != null) client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", mJoinWinToken));
                    Task<HttpResponseMessage> task = client.PostAsync(url, content);
                    task.Wait();
                    HttpResponseMessage response = task.Result;
                    if (!response.IsSuccessStatusCode) throw new Exception($"Failed to RushUpperWinSpinCR: {response.ReasonPhrase}");
                    Task<string> task2 = response.Content.ReadAsStringAsync();
                    task2.Wait();
                    string result = task2.Result;
                    JObject objRes = JObject.Parse(result);
                    string? status = objRes["status"]?.Value<string>();
                    if (status != "success")
                    {
                        string? error = objRes["error"]?.Value<string>();
                        throw new Exception(error != null ? errMsg : "unknow error");
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                //Log.StoreMsg(string.Format("RushUpperWinSpinCR got {0}", errMsg));
            }
            return errMsg;
        }

        public static string? PushToWinSpinCR(JObject obj)
        {
            string? errMsg = null;
            try
            {
                if (BonusAgent.RuleTrigger != null &&
                    BonusAgent.RuleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinCR) == false)
                {
                    throw new Exception("winCR not enabled");
                }
                string message = JsonConvert.SerializeObject(obj);
                string? error = mQueue?.Publish("SpinB", message);
                if (error != null) throw new Exception(error);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Log.StoreMsg(string.Format("PushToWinSpinCR got {0}", errMsg));
            }
            return errMsg;
        }

        public static Dictionary<string, BonusRecords> GetNotify()
        {
            Dictionary<string, BonusRecords> dicNotify = new Dictionary<string, BonusRecords>();
            mMutexNotifyData.WaitOne();
            try
            {
                foreach (var item in mBonusRecords)
                {
                    BonusRecords bonusRecords = item.Value;
                    dicNotify[item.Key] = bonusRecords.Clone();
                }
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("GetNotify got {0}", ex.Message));
            }
            mMutexNotifyData.ReleaseMutex();
            return dicNotify;
        }

        public static void ExportRecords()
        {
            try
            {
                Task.Run(() =>
                {
                    // export cache records
                    JObject obj = new JObject();
                    obj["bonusRecords"] = JsonConvert.SerializeObject(mBonusRecords);
                    obj["collectedA"] = JsonConvert.SerializeObject(RuleTrigger.WinCollection_A);
                    obj["collectedB"] = JsonConvert.SerializeObject(RuleTrigger.WinCollection_B);
                    obj["collectedCR"] = JsonConvert.SerializeObject(RuleTrigger.WinCollection_CR);
                    File.WriteAllText(mRecordsPath, JsonConvert.SerializeObject(obj));
                });
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("ExportRecords got {0}", ex.Message));
            }
        }

        static void ImportRecords(string jsonPath)
        {
            try
            {
                string content = File.ReadAllText(jsonPath);
                JObject obj = JObject.Parse(content);

                if (obj.ContainsKey("bonusRecords"))
                {
                    string bonusRecords = obj["bonusRecords"].Value<string>();
                    mBonusRecords = JsonConvert.DeserializeObject<Dictionary<string, BonusRecords>>(bonusRecords);
                }

                if (obj.ContainsKey("collectedA"))
                {
                    string strCollectedA = obj["collectedA"].Value<string>();
                    CollectData collectedA = JsonConvert.DeserializeObject<CollectData>(strCollectedA);
                    RuleTrigger.WinCollection_A.TotalBet = collectedA.TotalBet;
                    RuleTrigger.WinCollection_A.TotalWin = collectedA.TotalWin;
                    RuleTrigger.WinCollection_A.WinA = collectedA.WinA;
                    RuleTrigger.WinCollection_A.WinB = collectedA.WinB;
                    RuleTrigger.WinCollection_A.BonusType = collectedA.BonusType;
                }

                if (obj.ContainsKey("collectedB"))
                {
                    string strCollectedB = obj["collectedB"].Value<string>();
                    CollectData collectedB = JsonConvert.DeserializeObject<CollectData>(strCollectedB);
                    RuleTrigger.WinCollection_B.TotalBet = collectedB.TotalBet;
                    RuleTrigger.WinCollection_B.TotalWin = collectedB.TotalWin;
                    RuleTrigger.WinCollection_B.WinA = collectedB.WinA;
                    RuleTrigger.WinCollection_B.WinB = collectedB.WinB;
                    RuleTrigger.WinCollection_B.BonusType = collectedB.BonusType;
                }

                if (obj.ContainsKey("collectedCR"))
                {
                    string strCollectedCR = obj["collectedCR"].Value<string>();
                    CollectData collectedCR = JsonConvert.DeserializeObject<CollectData>(strCollectedCR);
                    RuleTrigger.WinCollection_CR.TotalBet = collectedCR.TotalBet;
                    RuleTrigger.WinCollection_CR.TotalWin = collectedCR.TotalWin;
                    RuleTrigger.WinCollection_CR.WinA = collectedCR.WinA;
                    RuleTrigger.WinCollection_CR.WinB = collectedCR.WinB;
                    RuleTrigger.WinCollection_CR.BonusType = collectedCR.BonusType;
                }
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("ImportRecords got {0}", ex.Message));
            }
        }

        static void WakeupAPI()
        {
            try
            {
                if (BonusAgent.BonusServerDomain == null) throw new Exception("BonusServerDomain is null");

                string urlWakeup = string.Format("{0}:{1}/wakeup/heartbeat", BonusAgent.BonusServerDomain, BonusAgent.BonusServerPort);
                string? response = Utils.HttpGet(urlWakeup);
                if (response == null) throw new Exception(string.Format("SelfCallingAPI {0} no response", urlWakeup));
            }
            catch (Exception ex)
            {
                Log.StoreMsg(ex.Message);
            }
        }

        static void JoinWin()
        {
            if (mJoinWinToken != null ||
                BonusAgent.UpperDomain == null ||
                BonusAgent.UpperDomain.Length == 0)
                return;

            try
            {
                ConfigSetting? configSetting = Config.Instance as ConfigSetting;
                if (configSetting == null) throw new Exception("configSetting is null");
                if (configSetting.BonusServerDomain == null) throw new Exception("configSetting.BonusServerDomain is null");
                if (configSetting.BonusServerPassword == null) throw new Exception("configSetting.BonusServerPassword is null");

                JoinWinData joinWinData = new JoinWinData();
                joinWinData.BonusServerDomain = configSetting.BonusServerDomain;
                joinWinData.BonusServerPort = configSetting.BonusServerPort;
                joinWinData.Password = configSetting.BonusServerPassword;
                string postData = JsonConvert.SerializeObject(joinWinData);

                string url = string.Format("{0}/bonus/lower/joinWin", BonusAgent.UpperDomain);
                string? response = Utils.HttpPost(url, postData, "application/json");
                if (response == null) throw new Exception("joinWin failed");
                mJoinWinToken = response;
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("JoinWin got {0}", ex.Message));
                mJoinWinToken = null;
            }
        }
    }
}

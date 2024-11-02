using BonusServer.Services.RuleTrigger;
using FunLobbyUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BonusServer
{
    public class ConfigSetting : FunLobbyUtils.Config
    {
        public enum BETWIN_RULE
        {
            Rule_0,
            Rule_1
        }

        public class BonusLogin
        {
            public string Domain { get; set; }
            public string Password { get; set; }
        }

        public ConfigSetting(string configFileName, bool bEncodeContent = true) : base(configFileName, bEncodeContent)
        {
            this.storeConfig();
        }

        public void Reload()
        {
            mConfigJson = getJson();
        }

        public Dictionary<string, string> getContent()
        {
            string content = JsonConvert.SerializeObject(mConfigJson);
            JObject? obj = JsonConvert.DeserializeObject<JObject>(content);
            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            if (obj != null)
            {
                foreach (var item in obj)
                {
                    dicSettings[item.Key] = item.Value != null ? item.Value.ToString() : "";
                }
            }
            return dicSettings;
        }

        override protected JObject getJson()
        {
            int errCode = 0;
            JObject? configJson = null;
            try
            {
                configJson = base.getJson();

                // assign default value if it doesn't exist
                if (configJson.ContainsKey("databases") == false) configJson["databases"] = new JArray();
                JArray? databases = configJson["databases"] as JArray;
                if (databases == null) databases = new JArray();
                if (databases.Count == 0)
                {
                    JObject defaultDB = new JObject();
                    defaultDB["agent"] = "WinLobby";
                    defaultDB["type"] = "mongodb";
                    defaultDB["domain"] = "localhost";
                    defaultDB["port"] = 27017;

                    databases.Add(defaultDB);
                    configJson["databases"] = databases;
                }

                errCode = 100;

                if (configJson.ContainsKey("WebSite") == false) configJson["WebSite"] = "彩金";
                if (configJson.ContainsKey("BonusServerDomain") == false) configJson["BonusServerDomain"] = "http://localhost";
                if (configJson.ContainsKey("BonusServerPort") == false) configJson["BonusServerPort"] = 80;
                if (configJson.ContainsKey("BonusServerPassword") == false) configJson["BonusServerPassword"] = "localhost";
                if (configJson.ContainsKey("UpperDomain") == false) configJson["UpperDomain"] = "";
                if (configJson.ContainsKey("SubDomains") == false) configJson["SubDomains"] = new JArray();
                JArray? subDomains = configJson["SubDomains"] as JArray;
                if (subDomains == null) subDomains = new JArray();
                if (subDomains.Count == 0)
                {
                    JObject obj = new JObject();
                    obj["domain"] = "localhost";
                    obj["password"] = "localhost";
                    subDomains.Add(obj);
                    configJson["SubDomains"] = subDomains;
                }
                if (configJson.ContainsKey("TransferScores") == false) configJson["TransferScores"] = "http://slot888.tw/APIs/TransferPoints.ashx";
                if (configJson.ContainsKey("CollectSubScale") == false) configJson["CollectSubScale"] = 1.0f;
                if (configJson.ContainsKey("RabbitMQServer") == false) configJson["RabbitMQServer"] = "127.0.0.1";
                if (configJson.ContainsKey("RabbitMQUserName") == false) configJson["RabbitMQUserName"] = "guest";
                if (configJson.ContainsKey("RabbitMQPassword") == false) configJson["RabbitMQPassword"] = "guest";
                if (configJson.ContainsKey("BetWinRule") == false) configJson["BetWinRule"] = (int)BETWIN_RULE.Rule_0;

                errCode = 200;

                BETWIN_RULE rule = (BETWIN_RULE)(configJson["BetWinRule"].Value<int>());
                BonusRule? bonusRule = null;
                switch (rule)
                {
                    case BETWIN_RULE.Rule_0: bonusRule = new BonusRule_0(); break;
                    case BETWIN_RULE.Rule_1: bonusRule = new BonusRule_1(); break;
                    default: throw new Exception(string.Format("unknown rule {0}", rule.ToString()));
                }

                errCode = 300;
                try
                {
                    string? strContent = configJson["ConditionWinA"]?.Value<string>();
                    if (strContent == null) throw new Exception("null ConditionWinA");
                    bonusRule.ParseSettings(BonusRule.WIN_TYPE.WinA, strContent);
                }
                catch (Exception ex) { }

                errCode = 400;
                try
                {
                    string? strContent = configJson["ConditionWinB"]?.Value<string>();
                    if (strContent == null) throw new Exception("null ConditionWinB");
                    bonusRule.ParseSettings(BonusRule.WIN_TYPE.WinB, strContent);
                }
                catch (Exception ex) { }

                errCode = 500;
                try
                {
                    string? strContent = configJson["ConditionWinCR"]?.Value<string>();
                    if (strContent == null) throw new Exception("null ConditionWinCR");
                    bonusRule.ParseSettings(BonusRule.WIN_TYPE.WinCR, strContent);
                }
                catch (Exception ex) { }

                errCode = 600;
                // assign conditions to rule
                configJson["ConditionWinA"] = JsonConvert.SerializeObject(bonusRule.Condition_A.Settings());
                configJson["ConditionWinB"] = JsonConvert.SerializeObject(bonusRule.Condition_B.Settings());
                configJson["ConditionWinCR"] = JsonConvert.SerializeObject(bonusRule.Condition_CR.Settings());
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("getJson got code:{0}, msg:{1}", errCode, ex.Message);
                throw new Exception(errMsg);
            }

            return configJson;
        }

        public string DBHost
        {
            get
            {
                try
                {
                    JArray? databases = mConfigJson["databases"] as JArray;
                    if (databases == null ||
                        databases.Count == 0)
                    {
                        throw new Exception("null databases");
                    }

                    JObject? obj = databases[0].Value<JObject>();
                    if (obj == null) throw new Exception("null obj in databases[0]");
                    string? domain = obj["domain"]?.Value<string>();
                    if (domain == null) throw new Exception("null domain");
                    return domain;
                }
                catch(Exception ex)
                {
                    Log.StoreMsg(ex.Message);
                    return "127.0.0.1";
                }
            }
        }
        public int DBPort
        {
            get
            {
                try
                {
                    JArray? databases = mConfigJson["databases"] as JArray;
                    if (databases == null ||
                        databases.Count == 0)
                    {
                        throw new Exception("null databases");
                    }

                    JObject? obj = databases[0].Value<JObject>();
                    if (obj == null) throw new Exception("null obj in databases[0]");
                    string? port = obj["port"]?.Value<string>();
                    if (port == null) throw new Exception("null port");
                    return int.Parse(port);
                }
                catch(Exception ex)
                {
                    Log.StoreMsg(ex.Message);
                    return 27017;
                }
            }
        }
        public JArray? DataBases { get { return mConfigJson["databases"]?.Value<JArray>(); } }

        public string? WebSite
        {
            get { return mConfigJson["WebSite"]?.Value<string>(); }
        }

        public string? RabbitMQServer
        {
            get { return mConfigJson["RabbitMQServer"]?.Value<string>(); }
            set { mConfigJson["RabbitMQServer"] = value; }
        }
        public string? RabbitMQUserName
        {
            get { return mConfigJson["RabbitMQUserName"]?.Value<string>(); }
            set { mConfigJson["RabbitMQUserName"] = value; }
        }
        public string? RabbitMQPassword
        {
            get { return mConfigJson["RabbitMQPassword"]?.Value<string>(); }
            set { mConfigJson["RabbitMQPassword"] = value; }
        }
        public string? BonusServerDomain
        {
            get { return mConfigJson["BonusServerDomain"]?.Value<string>(); }
            set { mConfigJson["BonusServerDomain"] = value; }
        }
        public int BonusServerPort
        {
            get { return mConfigJson["BonusServerPort"].Value<int>(); }
            set { mConfigJson["BonusServerPort"] = value; }
        }
        public string? BonusServerPassword
        {
            get { return mConfigJson["BonusServerPassword"]?.Value<string>(); }
            set { mConfigJson["BonusServerPassword"] = value; }
        }
        public string? UpperDomain
        {
            get { return mConfigJson["UpperDomain"]?.Value<string>(); }
            set { mConfigJson["UpperDomain"] = value; }
        }
        public List<BonusLogin> SubDomains
        {
            get
            {
                List<BonusLogin> list = new List<BonusLogin>();
                JArray? subDomains = mConfigJson["SubDomains"]?.Value<JArray>();
                if (subDomains != null &&
                    subDomains.Count > 0)
                {
                    for (int i = 0; i < subDomains.Count; i++)
                    {
                        JObject? obj = subDomains[i].Value<JObject>();
                        if (obj == null) continue;
                        string? domain = obj["domain"]?.Value<string>();
                        string? password = obj["password"]?.Value<string>();

                        BonusLogin info = new BonusLogin();
                        info.Domain = domain != null ? domain : "";
                        info.Password = password != null ? password : "";
                        list.Add(info);
                    }
                }
                return list;
            }
            set
            {
                JArray subDomains = new JArray();
                for (int i = 0; i < value.Count; i++)
                {
                    BonusLogin info = value[i];
                    JObject obj = new JObject();
                    obj["domain"] = info.Domain;
                    obj["password"] = info.Password;
                    subDomains.Add(obj);
                }
                mConfigJson["SubDomains"] = subDomains;
            }
        }
        public string? APITransferPoints
        {
            get { return mConfigJson["TransferScores"]?.Value<string>(); }
            set { mConfigJson["TransferScores"] = value; }
        }
        public float CollectSubScale
        {
            get { return mConfigJson["CollectSubScale"].Value<float>(); }
            set { mConfigJson["CollectSubScale"] = value; }
        }
        public BETWIN_RULE BetWinRule
        {
            get { return (BETWIN_RULE)mConfigJson["BetWinRule"].Value<int>(); }
            set { mConfigJson["BetWinRule"] = (int)value; }
        }
        public string? ConditionWinA
        {
            get { return mConfigJson["ConditionWinA"]?.Value<string>(); }
            set { mConfigJson["ConditionWinA"] = value; }
        }
        public string? ConditionWinB
        {
            get { return mConfigJson["ConditionWinB"]?.Value<string>(); }
            set { mConfigJson["ConditionWinB"] = value; }
        }
        public string? ConditionWinCR
        {
            get { return mConfigJson["ConditionWinCR"]?.Value<string>(); }
            set { mConfigJson["ConditionWinCR"] = value; }
        }
    }
}

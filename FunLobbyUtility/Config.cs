using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils
{ 
    public class Config
    {
        public enum LangType
        {
            zh_TW = 0,
            en_US = 1,
            UnknownLangType = 99,
        }

        public const int gatePort = 8080;// default port
        public const string Config_Malaysia = "馬來西亞";
        public const string Config_BAISHENG38 = "基隆百勝38號";
        public const string Config_FUHAO = "嘉義富豪娛樂城";
        public const string Config_Dashi = "大溪";
        public const string Config_Wanhua = "萬華";
        public const string Config_CHINGGE = "台南慶哥";
        public const string Config_DaCianYi = "台南大千逸";
        public const string Config_YANJIUN = "台南小鋼珠彥均";
        public const string Config_JINCHANG = "高雄錦昌";
        public const string Config_Taichung_Shiger = "台中溪哥";
        public const string Config_Wanhua_YU = "萬華余老闆";
        
        public const string Config_Common = "通用";

        public const string ScoringUsual = "一般上分";
        public const string ScoringGuoZhao = "過招";
        public const string ScoringUnlockGuoZhao = "解招";
        public const string UniPlay = "聯合營運";
        public const string UniBonus = "聯合彩金";
        public const string BookMachine = "機台訂位";

        public bool EncodeContent { get; private set; }
        public string Customer { get; private set; }

        static Config mInstance = null;
        string mConfigFileName = null;
        protected JObject mConfigJson = null;

        public static Config Instance { get { return mInstance; } }
        public string Protocol { get { return mConfigJson["Protocol"].Value<string>(); } }
        public string MongoDBPath { get { return mConfigJson["MongoDBPath"].Value<string>(); } }

        public LangType LanguageType
        {
            get { return (LangType)mConfigJson["LanguageType"].Value<int>(); }
            set { mConfigJson["LanguageType"] = (int)value; }
        }

        public bool ExportLog
        {
            get { return mConfigJson["ExportLog"].Value<bool>(); }
            set { mConfigJson["ExportLog"] = value; }
        }

        protected Config(string configFileName, bool bEncodeContent)
        {
            mInstance = this;
#if DEBUG
            this.EncodeContent = false;
#else
            this.EncodeContent = bEncodeContent;
#endif
            this.Customer = Config_Common;
            mConfigFileName = configFileName != null ? configFileName : "config";
            mConfigJson = getJson();
        }

        public void AssignCustomer(string customer)
        {
            switch (customer)
            {
                case Config_Malaysia: this.Customer = Config_Malaysia; break;
                case Config_BAISHENG38: this.Customer = Config_BAISHENG38; break;
                case Config_FUHAO: this.Customer = Config_FUHAO; break;
                case Config_CHINGGE: this.Customer = Config_CHINGGE; break;
                case Config_DaCianYi: this.Customer = Config_DaCianYi; break;
                case Config_YANJIUN: this.Customer = Config_YANJIUN; break;
                case Config_Dashi: this.Customer = Config_Dashi; break;
                case Config_Wanhua: this.Customer = Config_Wanhua; break;
                case Config_Taichung_Shiger: this.Customer = Config_Taichung_Shiger; break;
                case Config_Wanhua_YU: this.Customer = Config_Wanhua_YU; break;
                case Config_JINCHANG: this.Customer = Config_JINCHANG; break;
                default: this.Customer = Config_Common; break;
            }
            // store customer
            mConfigJson["Customer"] = this.Customer;
            // set extra properties for customer
            AddCommonProperties(mConfigJson);
        }

        virtual protected void AddCommonProperties(JObject objConfig)
        {
            // check if Customer is Config_Common
            if (objConfig.ContainsKey("Customer"))
            {
                string customer = objConfig["Customer"].Value<string>();
                if (customer != null && customer.Length > 0) this.Customer = customer;
            }
            if (objConfig.ContainsKey("LanguageType") == false) objConfig["LanguageType"] = (int)LangType.zh_TW;
        }

        virtual protected void RemoveCommonProperties(JObject objConfig)
        {
        }

        virtual protected JObject getJson()
        {
            int errCode = 0;
            try
            {
                JObject configJson = null;
                //config的內容請不要複製貼上 請透過程式產生
                //先取得加密的檔案，解密之後會再給房間編碼，再加密之後傳到QRCode
                if (System.IO.File.Exists(mConfigFileName) == true)
                {
                    errCode = 111;
                    System.IO.File.SetAttributes(mConfigFileName, System.IO.FileAttributes.Normal);
                    // 
                    System.IO.FileStream fs = new System.IO.FileStream(mConfigFileName, System.IO.FileMode.Open);
                    byte[] byteArray = new byte[fs.Length];
                    fs.Read(byteArray, 0, byteArray.Length);
                    fs.Close();

                    try
                    {
                        errCode = 222;
                        byte[] convertedBytes;
                        convertedBytes = Utils.XORByteArray(byteArray);
                        string decData = System.Text.Encoding.UTF8.GetString(convertedBytes);
                        configJson = JObject.Parse(decData);
                        errCode = 223;
                        if (this.EncodeContent)
                        {
                            // to make file read-only
                            System.IO.File.SetAttributes(mConfigFileName, System.IO.FileAttributes.ReadOnly);
                        }
                    }
                    catch (Exception ex)
                    {
                        // if failed at release build, try using big5ByteArray to decode again
                        if (configJson == null)
                        {
                            errCode = 224;
                            string decData = System.Text.Encoding.UTF8.GetString(byteArray);
                            configJson = JObject.Parse(decData);
                            errCode = 225;
                        }
                    }
                }

                errCode = 333;
                if (configJson == null) configJson = new JObject();
                if (configJson.ContainsKey("Protocol") == false) configJson["Protocol"] = "http";
                if (configJson.ContainsKey("MongoDBPath") == false)
                {
                    List<string> mongoDBPath = new List<string>();
                    mongoDBPath.Add("C:\\Program Files\\MongoDB\\Server\\3.6\\bin\\");
                    mongoDBPath.Add("C:\\Program Files\\MongoDB\\Server\\4.4\\bin\\");
                    string dbPath = null;
                    foreach (string path in mongoDBPath)
                    {
                        if (Directory.Exists(path) == false) continue;
                        dbPath = path;
                        break;
                    }
                    if (dbPath == null) dbPath = "C:\\Program Files\\MongoDB\\Server\\3.6\\bin\\";
                    configJson["MongoDBPath"] = dbPath;
                }
                if (configJson.ContainsKey("ExportLog") == false) configJson["ExportLog"] = true;

                errCode = 444;
                //set common properties, which are always in coding
                AddCommonProperties(configJson);

                errCode = 555;
                mConfigJson = configJson;
            }
            catch(Exception ex)
            {
                string errMsg = string.Format("getJson got code:{0}, msg:{1}", errCode, ex.Message);
                throw new Exception(errMsg);
            }
            return mConfigJson;
        }

        virtual public void storeConfig(JObject configJson = null)
        {
            configJson = configJson != null ? configJson : mConfigJson;
            string decData = JsonConvert.SerializeObject(configJson);
            JObject tmpConfig = JObject.Parse(decData);
            RemoveCommonProperties(tmpConfig);
            decData = tmpConfig.ToString();
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(decData);
            if (this.EncodeContent) byteArray = Utils.XORByteArray(byteArray);
            string configPath = Path.GetDirectoryName(mConfigFileName);
            string[] strPath = configPath.Split("\\");
            string tmpPath = "";
            for (int i = 0; i < strPath.Length; i++)
            {
                tmpPath += strPath[i];
                if (Directory.Exists(tmpPath) == false) Directory.CreateDirectory(tmpPath);
                tmpPath += "\\";
            }
            if (File.Exists(mConfigFileName))
            {
                System.IO.File.SetAttributes(mConfigFileName, System.IO.FileAttributes.Normal);
                // remove old file first.
                File.Delete(mConfigFileName);
                System.Threading.Thread.Sleep(10);
            }
            System.IO.FileStream fs = File.Open(mConfigFileName, FileMode.CreateNew);
            fs.Write(byteArray, 0, byteArray.Count());
            fs.Close();
            // enable read-only
            if (this.EncodeContent) System.IO.File.SetAttributes(mConfigFileName, System.IO.FileAttributes.ReadOnly);
        }

        public string DecryptAES256(string txt)
        {
            int iCharOffset = 72;
            char[] charArr = txt.ToCharArray();
            for (int i = 0; i < charArr.Length; i++)
            {
                int value = (int)(charArr[i]) - iCharOffset;
                charArr[i] = (char)value;
            }

            string strRestored = new string(charArr);
            return strRestored;
        }

        public string EncryptAES256(string txt)
        {
            int iCharOffset = 72;
            char[] charArr = txt.ToCharArray();
            for (int i = 0; i < charArr.Length; i++)
            {
                int value = (int)(charArr[i]) + iCharOffset;
                charArr[i] = (char)value;
            }
            string strRestored = new string(charArr);
            return strRestored;
        }
    }
}

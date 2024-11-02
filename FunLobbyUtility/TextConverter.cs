using Newtonsoft.Json;

namespace FunLobbyUtils
{
    public class TextConverter
    {
        Dictionary<Config.LangType, Dictionary<string, string>> mTextBanks = new Dictionary<Config.LangType, Dictionary<string, string>>();

        static TextConverter mInstance = null;
        public static TextConverter Instance
        {
            get
            {
                if (mInstance == null) mInstance = new TextConverter();
                return mInstance;
            }
        }

        protected TextConverter()
        {
            this.LoadText(Config.Instance.LanguageType);
        }

        public static string Convert(string text)
        {
            if (text != null && text.Length > 0)
            {
                Config.LangType langType = Config.Instance.LanguageType;
                Dictionary<string, string> textBank = TextConverter.Instance.mTextBanks[langType];
                if (textBank.ContainsKey(text)) return textBank[text];
            }
            return text;
        }

        void LoadText(Config.LangType langType)
        {
            try
            {
                if (mTextBanks.ContainsKey(langType) == false) mTextBanks[langType] = new Dictionary<string, string>();

                //config的內容請不要複製貼上 請透過程式產生
                //先取得加密的檔案，解密之後會再給房間編碼，再加密之後傳到QRCode
                string textFileName;
                switch (langType)
                {
                    case Config.LangType.zh_TW:
                        textFileName = "zh_TW.json";
                        break;
                    case Config.LangType.en_US:
                        textFileName = "en_US.json";
                        break;
                    default:
                        throw new Exception(string.Format("unknown LangType: {0}", langType.ToString()));
                }

                if (System.IO.File.Exists(textFileName) == false) throw new Exception(string.Format("file {0} doesn't exist", textFileName));

                System.IO.FileStream fs = new System.IO.FileStream(textFileName, System.IO.FileMode.Open);
                byte[] byteArray = new byte[fs.Length];
                fs.Read(byteArray, 0, byteArray.Length);
                fs.Close();
                string textData = System.Text.Encoding.UTF8.GetString(byteArray);
                mTextBanks[langType] = JsonConvert.DeserializeObject<Dictionary<string, string>>(textData);
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("TextConverter.LoadText got {0}", ex.Message));
            }
        }
    }
}

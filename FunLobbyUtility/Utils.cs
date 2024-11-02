using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace FunLobbyUtils
{
    public class Utils
    {
        public enum ENCRYPT
        {
            MD5,
            AES256
        }

        public class LogData
        {
            Dictionary<string, string> mParams = new Dictionary<string, string>();

            public LogData(string logger)
            {
                mParams["Logger"] = logger;
            }

            public void AddParam(string key, string content)
            {
                mParams[key] = content;
            }

            public JObject ToJson()
            {
                JObject obj = new JObject();
                foreach (var item in mParams)
                {
                    obj[item.Key] = item.Value;
                }
                return obj;
            }
        }

        static public readonly byte[] dataMask = new byte[] { 0x35, 0x57, 0x27, 0x95, 0xc6, 0xa4, 0xda, 0xb4, 0xa8, 0x9c, 0xcd, 0x39, 0x1f, 0xe5 };
        static public readonly char[] mRandChar = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string machineLogKey(string lobbyName, int deviceId, int machineId)
        {
            string key = string.Format("{0}_{1} at {2}", deviceId, machineId, lobbyName);
            return key;
        }

        public static int StringToHex(string strValue)
        {
            // make all character to lower case
            strValue = strValue.ToLower();
            int iValue = 0;
            for (int j = 0; j < strValue.Length; j++)
            {
                switch (strValue[j])
                {
                    case '0':
                        iValue += 0;
                        break;
                    case '1':
                        iValue += 1;
                        break;
                    case '2':
                        iValue += 2;
                        break;
                    case '3':
                        iValue += 3;
                        break;
                    case '4':
                        iValue += 4;
                        break;
                    case '5':
                        iValue += 5;
                        break;
                    case '6':
                        iValue += 6;
                        break;
                    case '7':
                        iValue += 7;
                        break;
                    case '8':
                        iValue += 8;
                        break;
                    case '9':
                        iValue += 9;
                        break;
                    default:
                        {
                            // base on 'a' to get value
                            int value = (strValue[j] - 'a' + 10) % 16;
                            iValue += value;
                        }
                        break;
                }

                if (j + 1 < strValue.Length)
                    iValue = iValue * 16;
            }
            return iValue;
        }

        public static byte[] XORByteArray(byte[] byteArray)
        {
            byte[] outByteArray = new byte[byteArray.Length];
            // do xor
            for (int i = 0; i < byteArray.Length; i++)
            {
                int index = i % dataMask.Count();
                outByteArray[i] = (byte)(byteArray[i] ^ dataMask[index]);
            }
            return outByteArray;
        }

        public static string generateRandom(int strLength)
        {
            DateTime now = DateTime.Now;
            Random rand = new Random((int)(now.Ticks % 100000000));
            string strToken = "";
            for (int i = 0; i < strLength; i++)
            {
                strToken += mRandChar[rand.Next() % mRandChar.Length];
            }
            return strToken;
        }

        public static string Encrypt(string contentString, string encryptKey, string encryptIV, ENCRYPT mode = ENCRYPT.MD5)
        {
            string outStr = null;
            try
            {
                //密碼轉譯一定都是用byte[] 所以把string都換成byte[]
                byte[] byteContentString = Encoding.UTF8.GetBytes(contentString);
                byte[] byteEncryptKey = Encoding.UTF8.GetBytes(encryptKey);
                byte[] byteEncryptIV = Encoding.UTF8.GetBytes(encryptIV);
                ICryptoTransform cTransform;
                switch (mode)
                {
                    case ENCRYPT.MD5:
                        {
                            //加解密函數的key通常都會有固定的長度 而使用者輸入的key長度不定 因此用hash過後的值當做key
                            MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
                            byte[] byteMD5EncryptKey = mD5CryptoServiceProvider.ComputeHash(byteEncryptKey);
                            byte[] byteMD5EncryptIV = mD5CryptoServiceProvider.ComputeHash(byteEncryptIV);
                            //產生加密實體 如果要用其他不同的加解密演算法就改這裡(ex:3DES)
                            RijndaelManaged RDEL = new RijndaelManaged();
                            cTransform = RDEL.CreateEncryptor(byteMD5EncryptKey, byteMD5EncryptIV);
                        }
                        break;
                    case ENCRYPT.AES256:
                        {
                            RijndaelManaged RDEL = new RijndaelManaged();
                            RDEL.Key = byteEncryptKey;
                            RDEL.IV = byteEncryptIV;
                            RDEL.Mode = CipherMode.CBC;
                            RDEL.Padding = PaddingMode.Zeros;
                            cTransform = RDEL.CreateEncryptor();
                        }
                        break;
                    default:
                        throw new Exception(string.Format("unknown encrypt mode {0}", mode));
                }
                //output就是加密過後的結果
                byte[] output = cTransform.TransformFinalBlock(byteContentString, 0, byteContentString.Length);
                outStr = Convert.ToBase64String(output, 0, output.Length);
            }
            catch(Exception ex)
            {
                Log.StoreMsg(string.Format("Utils.Encrypt got {0}", ex.Message));
            }
            return outStr;
        }

        public static string Decrypt(string contentString, string encryptKey, string encryptIV, ENCRYPT mode = ENCRYPT.MD5)
        {
            string outStr = null;
            try
            {
                //密碼轉譯一定都是用byte[] 所以把string都換成byte[]
                byte[] byteContentString = Convert.FromBase64String(contentString);
                byte[] byteEncryptKey = Encoding.UTF8.GetBytes(encryptKey);
                byte[] byteEncryptIV = Encoding.UTF8.GetBytes(encryptIV);

                ICryptoTransform cTransform;
                switch (mode)
                {
                    case ENCRYPT.MD5:
                        {
                            //加解密函數的key通常都會有固定的長度 而使用者輸入的key長度不定 因此用hash過後的值當做key
                            MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
                            byte[] byteMD5EncryptKey = mD5CryptoServiceProvider.ComputeHash(byteEncryptKey);
                            byte[] byteMD5EncryptIV = mD5CryptoServiceProvider.ComputeHash(byteEncryptIV);
                            //產生解密實體 如果要用其他不同的加解密演算法就改這裡(ex:3DES)
                            RijndaelManaged RDEL = new RijndaelManaged();
                            cTransform = RDEL.CreateDecryptor(byteMD5EncryptKey, byteMD5EncryptIV);
                        }
                        break;
                    case ENCRYPT.AES256:
                        {
                            RijndaelManaged RDEL = new RijndaelManaged();
                            RDEL.Key = byteEncryptKey;
                            RDEL.IV = byteEncryptIV;
                            RDEL.Mode = CipherMode.CBC;
                            RDEL.Padding = PaddingMode.Zeros;
                            cTransform = RDEL.CreateDecryptor();
                        }
                        break;
                    default:
                        throw new Exception(string.Format("unknown encode {0}", mode));
                }
                //output就是解密過後的結果
                byte[] output = cTransform.TransformFinalBlock(byteContentString, 0, byteContentString.Length);
                outStr = Encoding.UTF8.GetString(output);
                int index = outStr.IndexOf('\0');
                if (index >= 0) outStr = outStr.Substring(0, index);
            }
            catch (Exception ex)
            {
                Log.StoreMsg(string.Format("Utils.Decrypt got {0}", ex.Message));
            }
            return outStr;
        }

        public static string SHA1_Encrypt(string input)
        {
            SHA1 sha = SHA1.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha.ComputeHash(inputBytes);
            // 將 byte 陣列轉換為十六進位字串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            string returnStr = sb.ToString();
            return returnStr.ToUpper();
        }

        public static string SHA256_Encrypt(string Content)
        {
            SHA256 sha = SHA256.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(Content);
            byte[] hashBytes = sha.ComputeHash(inputBytes);
            // 將 byte 陣列轉換為十六進位字串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            string returnStr = sb.ToString();
            return returnStr.ToUpper();
        }

        public static string HMACSHA256_Encrypt(string Content, string Key)
        {
            byte[] ByteString = Encoding.ASCII.GetBytes(Content);
            byte[] ByteKeyString = Encoding.ASCII.GetBytes(Key);
            HMACSHA256 sha = new HMACSHA256(ByteKeyString);
            byte[] hashBytes = sha.ComputeHash(ByteString);
            string returnStr = BitConverter.ToString(hashBytes).Replace("-", "");
            return returnStr.ToUpper();
        }

        public static string dateTimeToString(DateTime dateTime)
        {
            // use UTC timezone
            dateTime = dateTime.ToUniversalTime();
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            string strDateTime = string.Format("{0}/{1}/{2} {3}:{4}:{5}",
                year,
                month < 10 ? "0" + month.ToString() : month.ToString(),
                day < 10 ? "0" + day.ToString() : day.ToString(),
                hour < 10 ? "0" + hour.ToString() : hour.ToString(),
                minute < 10 ? "0" + minute.ToString() : minute.ToString(),
                second < 10 ? "0" + second.ToString() : second.ToString());
            return strDateTime;
        }

        public static DateTime stringToDateTime(string strDateTime)
        {
            DateTime dt = Convert.ToDateTime(strDateTime);
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return dt;
        }

        public static DateTime zeroDateTime()
        {
            DateTime dt = new DateTime(0);
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return dt;
        }

        public static DateTime ConvertLocalTime(DateTime dateTime)
        {
            return dateTime.ToLocalTime();
        }

        public static string? VerCode()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        public static string? HttpGet(string url, string? strData = null, string? token = null)
        {
            string? result;
            try
            {
                string apiUrl = url;
                if (strData != null) apiUrl += string.Format("?{0}", strData);
                using (HttpClient client = new HttpClient())
                {
                    if (token != null) client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", token));
                    Task<HttpResponseMessage> task1 = client.GetAsync(apiUrl);
                    task1.Wait();
                    HttpResponseMessage response = task1.Result;
                    if (!response.IsSuccessStatusCode) throw new Exception($"HTTP GET request failed: {response.ReasonPhrase}");
                    Task<string> task2 = response.Content.ReadAsStringAsync();
                    task2.Wait();
                    result = task2.Result;
                }
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                result = null;
            }
            return result;
        }

        public static string? HttpPost(string url, JObject objData, string? token = null)
        {
            string? result;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (token != null) client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", token));
                    string? strContent = JsonConvert.SerializeObject(objData);
                    StringContent? content = new StringContent(strContent, Encoding.UTF8, "application/json");
                    Task<HttpResponseMessage> task1 = client.PostAsync(url, content);
                    task1.Wait();
                    HttpResponseMessage response = task1.Result;
                    if (!response.IsSuccessStatusCode) throw new Exception($"HTTP POST request failed: {response.ReasonPhrase}");
                    Task<string> task2 = response.Content.ReadAsStringAsync();
                    task2.Wait();
                    result = task2.Result;
                }
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                //if (token != null) request.Headers.Add("Authorization", string.Format("Bearer {0}", token));
                //request.Method = "POST";
                //request.ContentType = "application/json";
                //string postData = objData != null ? JsonConvert.SerializeObject(objData) : "{}";
                //byte[] bs = System.Text.Encoding.UTF8.GetBytes(postData);
                //request.ContentLength = bs.Length;
                //request.GetRequestStream().Write(bs, 0, bs.Length);
                ////取得 WebResponse 的物件 然後把回傳的資料讀出
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //StreamReader sr = new StreamReader(response.GetResponseStream());
                //result = sr.ReadToEnd();
                //sr.Close();
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                result = null;
            }
            return result;
        }

        public static string? HttpPost(string url, Dictionary<string, object> dicData, string? token = null)
        {
            string? result;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (token != null) client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", token));
                    List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                    foreach (var item in dicData)
                    {
                        postData.Add(new KeyValuePair<string, string>(item.Key, item.Value.ToString()));
                    }
                    HttpContent content = new FormUrlEncodedContent(postData);
                    Task<HttpResponseMessage> task1 = client.PostAsync(url, content);
                    task1.Wait();
                    HttpResponseMessage response = task1.Result;
                    if (!response.IsSuccessStatusCode) throw new Exception($"HTTP POST request failed: {response.ReasonPhrase}");
                    Task<string> task2 = response.Content.ReadAsStringAsync();
                    task2.Wait();
                    result = task2.Result;
                }
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                //if (token != null) request.Headers.Add("Authorization", string.Format("Bearer {0}", token));
                //request.Method = "POST";
                //request.ContentType = "application/x-www-form-urlencoded";
                //string postData = "";
                //foreach (var item in dicData)
                //{
                //    if (postData.Length > 0) postData += '&';
                //    postData += string.Format("{0}={1}", item.Key, item.Value);
                //}
                //byte[] bs = System.Text.Encoding.UTF8.GetBytes(postData);
                //request.ContentLength = bs.Length;
                //request.GetRequestStream().Write(bs, 0, bs.Length);
                ////取得 WebResponse 的物件 然後把回傳的資料讀出
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //StreamReader sr = new StreamReader(response.GetResponseStream());
                //result = sr.ReadToEnd();
                //sr.Close();
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                result = null;
            }
            return result;
        }

        public static string? HttpPost(string url, string postData, string contentType, string? token = null)
        {
            string? result = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (token != null) client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", token));
                    HttpContent content = new StringContent(postData, Encoding.UTF8, contentType);
                    Task<HttpResponseMessage> task1 = client.PostAsync(url, content);
                    task1.Wait();
                    HttpResponseMessage response = task1.Result;
                    if (!response.IsSuccessStatusCode) throw new Exception($"HTTP POST request failed: {response.ReasonPhrase}");
                    Task<string> task2 = response.Content.ReadAsStringAsync();
                    task2.Wait();
                    result = task2.Result;
                }
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                //if (token != null) request.Headers.Add("Authorization", string.Format("Bearer {0}", token));
                //request.Method = "POST";
                //request.ContentType = contentType;
                //postData = postData != null ? postData : "";
                //byte[] bs = System.Text.Encoding.UTF8.GetBytes(postData);
                //request.ContentLength = bs.Length;
                //request.GetRequestStream().Write(bs, 0, bs.Length);
                ////取得 WebResponse 的物件 然後把回傳的資料讀出
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //StreamReader sr = new StreamReader(response.GetResponseStream());
                //result = sr.ReadToEnd();
                //sr.Close();
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                result = null;
            }
            return result;
        }

        public static UdpClient? SendPacket_UDP(byte[] bytes, string hostName, int port)
        {
            UdpClient? udpClient = null;
            try
            {
                Task task = new Task(new Action(() =>
                {
                    udpClient = new UdpClient();
                    int iSent = udpClient.Send(bytes, bytes.Length, hostName, port);
                    if (iSent != bytes.Length) throw new Exception(string.Format("Send doesn't complete, {0}/{1}", iSent, bytes.Length));
                }));
                task.Start();
                bool bRet = task.Wait(100);
                if (!bRet) throw new Exception("timeout");
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("SendPacket_UDP got {0}", ex.Message);
                Log.StoreMsg(errMsg);
                udpClient = null;
            }
            return udpClient;
        }

        public static void SendPacket_UDP(string content, string hostName, int port)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            SendPacket_UDP(bytes, hostName, port);
        }

        public static void SendPacket_UDP(byte[] bytes, IPEndPoint endPoint)
        {
            SendPacket_UDP(bytes, endPoint.Address.ToString(), endPoint.Port);
        }

        public static void SendPacket_UDP(string content, IPEndPoint endPoint)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            SendPacket_UDP(bytes, endPoint.Address.ToString(), endPoint.Port);
        }

        public static byte[]? ReceivePacket_UDP(UdpClient udpClient, ref IPEndPoint remotePoint)
        {
            byte[]? received = null;
            try
            {
                if (udpClient != null &&
                    udpClient.Client != null &&
                    udpClient.Client.Connected)
                {
                    IPEndPoint? resPoint = null;
                    Task task = new Task(new Action(() =>
                    {
                        resPoint = new IPEndPoint(IPAddress.Parse("1.1.1.1"), 1);
                        received = udpClient.Receive(ref resPoint);
                    }));
                    task.Start();
                    bool bRet = task.Wait(100);
                    if (!bRet) throw new Exception("timeout");
                    remotePoint = resPoint;
                }
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("ReceivePacket_UDP got {0}", ex.Message);
                Log.StoreMsg(errMsg);
            }
            return received;
        }

        public static string? SendPacket_TCP(byte[] bytes, TcpClient tcpClient)
        {
            string? errMsg = null;
            try
            {
                NetworkStream ns = tcpClient.GetStream();
                if (!ns.CanWrite) throw new Exception("CanWrite is false");

                ns.Write(bytes, 0, bytes.Length);
                ns.Flush();
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public static string? SendPacket_TCP(string content, TcpClient tcpClient)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            return SendPacket_TCP(bytes, tcpClient);
        }

        public static string? SendSMS_every8d(string userID, string password, string phoneNo, string message)
        {
            string? errMsg = null;
            try
            {
                const string getCreditUrl = "http://api.every8d.com/API21/HTTP/getCredit.ashx";
                const string sendSMSUrl = "http://api.every8d.com/API21/HTTP/sendSMS.ashx";

                Dictionary<string, object> postData1 = new Dictionary<string, object>();
                postData1["ID"] = userID;
                postData1["PWD"] = password;
                string? resultString = HttpPost(getCreditUrl, postData1);
                if (resultString.StartsWith("-")) throw new Exception(string.Format("Error credit:{0}", resultString));
                // check if credit is not enough
                double credit = Convert.ToDouble(resultString);
                if (credit <= 0) throw new Exception("creadit isn't enough");

                Dictionary<string, object> postData2 = new Dictionary<string, object>();
                postData2["UID"] = userID;
                postData2["PWD"] = password;
                postData2["MSG"] = message;
                postData2["DEST"] = phoneNo;
                resultString = HttpPost(sendSMSUrl, postData2);
                if (resultString.StartsWith("-")) throw new Exception(string.Format("Error sending:{0}", resultString));

                string[] split = resultString.Split(',');
                credit = Convert.ToDouble(split[0]);
            }
            catch(Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }
        
        public static string? SendSMS_bulk360(string userID, string password, string phoneNo, string message)
        {
            string? errMsg = null;
            try
            {
                const string sendSMSUrl = "https://sms.360.my/gw/bulk360/v3_0/send.php";
                string strParams = string.Format("user={0}&pass={1}&to={2}&text={3}", userID, password, phoneNo, message);
                string? resultString = HttpPost(sendSMSUrl, strParams, "application/x-www-form-urlencoded");
                if (resultString.Contains("OK") == false) throw new Exception(string.Format("Error sending:{0}", resultString));
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }
        public static string? SendSMS_bulk360WhatsApp(string userID, string password, string phoneNo, string message)
        {
            string? errMsg = null;
            try
            {//與SendSMS_bulk360傳送SMS只差在to=whatsapp:但怕以後忘記還是先拆成兩個Function來執行
                const string sendSMSUrl = "https://sms.360.my/gw/bulk360/v3_0/send.php";
                string strParams = string.Format("user={0}&pass={1}&to=whatsapp:{2}&text={3}", userID, password, phoneNo, message);
                string? resultString = HttpPost(sendSMSUrl, strParams, "application/x-www-form-urlencoded");
                if (resultString.Contains("OK") == false) throw new Exception(string.Format("Error sending:{0}", resultString));
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }
        public static string SendSMS_twsms(string userID, string password, string phoneNo, string message)
        {
            string errMsg = null;
            try
            {
                const string sendSMSUrl = "http://api.twsms.com/json/sms_send.php";
                string strParams = string.Format("username={0}&password={1}&mobile={2}&message={3}", userID, password, phoneNo, message);
                string? resultString = HttpPost(sendSMSUrl, strParams, "application/x-www-form-urlencoded");
                if (resultString.Contains("Success") == false) throw new Exception(string.Format("Error sending:{0}", resultString));
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }
        

        public static string? SendSMS_movider(string userID, string password, string phoneNo, string message)
        {
            string? errMsg = null;
            try
            {
                const string sendSMSUrl = "https://api.movider.co/v1/sms";
                //string strParams = string.Format("un={0}&pwd={1}&dstno={2}&msg={3}&type=1", userID, password, phoneNo, message);
                string strParams = string.Format("api_key={0}&api_secret={1}&to={2}&text={3}", userID, password, phoneNo, message);
                string? resultString = HttpPost(sendSMSUrl, strParams, "application/x-www-form-urlencoded");
                if (resultString.StartsWith("400") == false) throw new Exception(string.Format("Error sending:{0}", resultString));
                
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }
        
        public static string? SendSMS_isms(string userID, string password, string phoneNo, string message)
        {
            string? errMsg = null;
            try
            {
                const string sendSMSUrl = "https://isms.com.my/isms_send.php";

                string agreedterm = "YES";
                string strParams = string.Format("un={0}&pwd={1}&dstno={2}&msg={3}&type=1&agreedterm={4}", userID, password, phoneNo, message, agreedterm);
                //string resultString = HttpGet(sendSMSUrl, strParams);
                //if (resultString.StartsWith("2000") == false) throw new Exception(string.Format("Error sending:{0}", resultString));
                // Create a new 'Uri' object with the specified string.
                Uri myUri = new Uri(string.Format("{0}?{1}", sendSMSUrl, strParams));
                // Create a new request to the above mentioned URL.
                WebRequest myWebRequest = WebRequest.Create(myUri);
                // Assign the response object of 'WebRequest' to a 'WebResponse' variable.
                WebResponse myWebResponse = myWebRequest.GetResponse();
                StreamReader reader = new StreamReader(myWebResponse.GetResponseStream());
                string resultString = reader.ReadToEnd();
                reader.Close();
                if (resultString.StartsWith("2000") == false) throw new Exception(string.Format("Error sending:{0}", resultString));
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }
        
        public static void SendLog(LogData logData)
        {
            const string url = "http://slot888.tw/APIs/SendLog.ashx";//"http://localhost:20000/APIs/SendLog.ashx";//
            JObject obj = new JObject();
            obj["log"] = JsonConvert.SerializeObject(logData.ToJson());
            HttpPost(url, obj);
        }
    }

    public class Notification
    {
        public enum Status
        {
            onUndefined = 0,
            onError = 2,
            onConnectSuccess = 11,
            onConnectFailed = 12,
            //onReconnectSuccess = 13,
            //onReconnectFailed = 14,
            onGetConfig = 15,
            onNotifyJoin = 20, // some connection join group
            onNotifyLeave = 21, // sone connection leave group
            onAction = 22,
            onResponse = 23,
            onGroupInfo = 30, // acquire all connection info in group
            onSendHeartbeatTriggered = 40, //發送心跳包
            onSendHeartbeatSuccess = 41,//發送心跳包成功
            onSendHeartbeatFault = 42,//發送心跳包失敗
            onConnectionStatus = 43,//遙控端心跳包 但表client端還活著
        }
    }

    public class WinLobby
    {
        public const string Gate = "Gate";
        public const string Guard = "WinLobbyGuard";
        public const string Client = "WinLobbyClient";
        public const string Manager = "WinLobbyManager";
        public const string WebManager = "WebManager";
        public const string All = "WinLobbyAll";
    }

    public class LobbyType
    {
        public const string WinLobby = "WinLobby";
        public const string Baija = "Baija";
    }

}
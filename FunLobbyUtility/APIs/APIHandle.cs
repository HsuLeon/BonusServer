
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils
{
    public class APIHandle
    {
        public delegate void ProceedResponse(JObject? resObj);

        public enum MethodType
        {
            UNDEFINED,
            GET,
            POST,
            UPLOADBINARY,
            DOWNLOADBINARY
        }
        string? mAPIDomain;
        int mAPIPort;
        string? mAPICommand = "";
        string mParams = "";
        byte[]? mMediaData = null;
        MethodType mMethodType = MethodType.GET;
        ProceedResponse? mProceedResponse = null;

        public APIHandle(string? domain, int port, string? apiCommand, MethodType methodType)
        {
            mAPIDomain = domain;
            mAPIPort = port;
            mAPICommand = apiCommand;
            mMethodType = methodType;
        }

        public void Dispose()
        {
            mMediaData = null;
        }

        public void AddParam(string param, object value)
        {
            if (mParams.Length > 0)
            {
                mParams += "&";
            }
            mParams += string.Format("{0}={1}", param, value);
        }

        public void AddMediaData(byte[] mediaData)
        {
            mMediaData = mediaData;
        }

        protected JObject? ProceedAPIGet()
        {
            JObject? reponseObject = null;
            if (mProceedResponse != null) mProceedResponse(reponseObject);
            return reponseObject;
        }

        protected JObject ProceedAPIPost()
        {
            //int time1 = Environment.TickCount;
            string apiUrl = string.Format("{0}://{1}:{2}/APIs/DB/{3}.ashx", Config.Instance.Protocol, mAPIDomain, mAPIPort, mAPICommand);
            WebRequest request = WebRequest.Create(apiUrl);
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = ".NET Framework Example Client";
            request.Method = "POST";

            string? jsonString = null;
            try
            {
                if (mParams.Length > 0)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(mParams);
                    request.ContentLength = byteArray.Length;
                    request.ContentType = "application/x-www-form-urlencoded";
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }
                else
                {
                    request.ContentLength = 0;
                    request.ContentType = "application/x-www-form-urlencoded";
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Close();
                }

                WebResponse response = request.GetResponse();
                //to parse response data
                StreamReader tokenStreamReader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("UTF-8"));
                jsonString = tokenStreamReader.ReadToEnd();
                tokenStreamReader.Close();
                // close connection, to avoid time out when mass connecting
                response.Close();
            }
            catch(Exception ex)
            {
                if (ex.Message.Contains("逾時") == false &&
                    ex.Message.ToLower().Contains("timeout") == false)
                {
                    throw new Exception(ex.Message);
                }

                // delay for a while
                Random random = new Random();
                int delayIn = random.Next(300, 1000);
                System.Threading.Thread.Sleep(delayIn);
                // try again
                if (mParams.Length > 0)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(mParams);
                    request.ContentLength = byteArray.Length;
                    request.ContentType = "application/x-www-form-urlencoded";
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }
                else
                {
                    request.ContentLength = 0;
                    request.ContentType = "application/x-www-form-urlencoded";
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Close();
                }

                WebResponse response = request.GetResponse();
                //to parse response data
                StreamReader tokenStreamReader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("UTF-8"));
                jsonString = tokenStreamReader.ReadToEnd();
                tokenStreamReader.Close();
                // close connection, to avoid time out when mass connecting
                response.Close();
            }
            if (jsonString == null) throw new Exception("server response null");

            JObject reponseObject = JObject.Parse(jsonString);
            if (mProceedResponse != null) mProceedResponse(reponseObject);

            return reponseObject;
        }

        protected JObject ProceedUploadBinary()
        {
            if (mMediaData == null) return null;

            //int time1 = Environment.TickCount;
            string apiUrl = string.Format("{0}://{1}:{2}/APIs/DB/{3}.ashx", Config.Instance.Protocol, mAPIDomain, mAPIPort, mAPICommand);
            WebRequest request = WebRequest.Create(apiUrl);
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = ".NET Framework Example Client";
            request.Method = "POST";
            request.Timeout = 10000;

            WebHeaderCollection WH = new WebHeaderCollection();
            if (mParams.Length > 0)
            {
                string[] stringSeparators = new string[] { "&" };
                string[] result = mParams.Split(stringSeparators, StringSplitOptions.None);
                for (int i = 0; i < result.Length; i++)
                {
                    stringSeparators = new string[] { "=" };
                    string[] subResult = result[i].Split(stringSeparators, StringSplitOptions.None);
                    if (subResult.Length == 2) WH.Add(subResult[0], subResult[1]);
                }
            }
            request.Headers = WH;
            request.ContentLength = mMediaData.Length;
            request.ContentType = "application/octet-stream";// "application/x-www-form-urlencoded"; //"multipart/form-data";//

            Stream dataStream = request.GetRequestStream(); //add data to request stream
            dataStream.Write(mMediaData, 0, mMediaData.Length);// (buffer, 0, buffer.Length);
            dataStream.Close();

            //int time2 = Environment.TickCount;

            WebResponse response = request.GetResponse();
            //int time3 = Environment.TickCount;

            Encoding myEncoding = Encoding.GetEncoding("UTF-8");
            //to parse response data
            StreamReader tokenStreamReader = new StreamReader(response.GetResponseStream(), myEncoding);
            string jsonString = tokenStreamReader.ReadToEnd();
            tokenStreamReader.Close();
            // close connection, to avoid time out when mass connecting
            response.Close();
            JObject reponseObject = JObject.Parse(jsonString);
            //int time4 = Environment.TickCount;
            //// to check operating duration
            //int deltaTime1 = time2 - time1;
            //int deltaTime2 = time3 - time2;
            //int deltaTime3 = time4 - time3;

            if (mProceedResponse != null) mProceedResponse(reponseObject);

            return reponseObject;
        }

        protected JObject ProceedDownloadBinary()
        {
            //int time1 = Environment.TickCount;
            string apiUrl = string.Format("{0}://{1}:{2}/APIs/DB/{3}.ashx", Config.Instance.Protocol, mAPIDomain, mAPIPort, mAPICommand);
            WebRequest request = WebRequest.Create(apiUrl);
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = ".NET Framework Example Client";
            request.Method = "POST";
            request.Timeout = 10000;

            if (mParams.Length > 0)
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(mParams);
                request.ContentLength = byteArray.Length;
                request.ContentType = "application/x-www-form-urlencoded";
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
            }
            //int time2 = Environment.TickCount;

            WebResponse response = request.GetResponse();
            //int time3 = Environment.TickCount;

            JObject reponseObject = new JObject();
            reponseObject["status"] = response.Headers["status"];
            if (reponseObject["status"].Value<string>() == "success")
            {
                string resMessage = response.Headers["message"].ToString();
                string[] stringSeparators = new string[] { "," };
                string[] resParams = resMessage.Split(stringSeparators, StringSplitOptions.None);
                JObject msgObj = new JObject();
                for (int i = 0; i < resParams.Length; i++)
                {
                    string[] subSeparators = new string[] { ":" };
                    string[] subParams = resParams[i].Split(subSeparators, StringSplitOptions.None);
                    msgObj[subParams[0]] = subParams[1];
                }
                    
                int iTotalSize = msgObj["totalSize"].Value<int>();
                int iReadSize = msgObj["readSize"].Value<int>();

                //fill response data to rawData
                Stream resStream = response.GetResponseStream();
                byte[] rawData = new byte[iTotalSize];
                int iCur = 0;
                int iLength = (int)iReadSize;
                while (iCur < iLength)
                {
                    iCur += resStream.Read(rawData, iCur, iLength - iCur);
                }
                resStream.Close();

                // if not download total media content, keep download them...
                while (iCur < iTotalSize)
                {
                    WebRequest continueRequest = WebRequest.Create(mAPICommand);
                    continueRequest.Credentials = CredentialCache.DefaultCredentials;
                    ((HttpWebRequest)continueRequest).UserAgent = ".NET Framework Example Client";
                    continueRequest.Method = "POST";
                    continueRequest.Timeout = 10000;

                    string continueParams = mParams;
                    continueParams += "&";
                    continueParams += string.Format("{0}={1}", "ReadPos", iCur);

                    byte[] byteArray = Encoding.UTF8.GetBytes(continueParams);
                    continueRequest.ContentLength = byteArray.Length;
                    continueRequest.ContentType = "application/x-www-form-urlencoded";
                    Stream dataStream = continueRequest.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    WebResponse continueResponse = continueRequest.GetResponse();
                    if (continueResponse.Headers["status"].ToString() == "success")
                    {
                        stringSeparators = new string[] { "," };
                        resParams = continueResponse.Headers["message"].ToString().Split(stringSeparators, StringSplitOptions.None);
                        msgObj = new JObject();
                        for (int i = 0; i < resParams.Length; i++)
                        {
                            string[] subSeparators = new string[] { ":" };
                            string[] subParams = resParams[i].Split(subSeparators, StringSplitOptions.None);
                            msgObj[subParams[0]] = subParams[1];
                        }

                        //to parse response data
                        Stream continueResStream = continueResponse.GetResponseStream();
                        iLength += msgObj["readSize"].Value<int>();
                        while (iCur < iLength)
                        {
                            iCur += continueResStream.Read(rawData, iCur, iLength - iCur);
                        }
                        continueResStream.Close();
                    }
                    else
                    {
                        break;
                    }
                }

                JObject resObj = new JObject();
                resObj["RawData"] = rawData;
                    
                reponseObject["message"] = "size:" + resStream.Length;
                reponseObject["content"] = resObj;
            }
            else
            {
                JObject errObj = (JObject)response.Headers["message"];
                reponseObject["message"] = errObj;
            }
            // close connection, to avoid time out when mass connecting
            response.Close();
            //int time4 = Environment.TickCount;
            //// to check operating duration
            //int deltaTime1 = time2 - time1;
            //int deltaTime2 = time3 - time2;
            //int deltaTime3 = time4 - time3;
            if (mProceedResponse != null) mProceedResponse(reponseObject);

            return reponseObject;
        }

        public JObject? Fire(ProceedResponse? proceedResponse = null)
        {
            mProceedResponse = proceedResponse;

            switch (mMethodType)
            {
                case MethodType.GET: return ProceedAPIGet();
                case MethodType.POST: return ProceedAPIPost();
                case MethodType.UPLOADBINARY: return ProceedUploadBinary();
                case MethodType.DOWNLOADBINARY: return ProceedDownloadBinary();
                default: return null;
            }
        }
    }
}

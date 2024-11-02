using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FunLobbyUtils
{
    /// <summary>
    /// CommunicationBase給客戶端和主機端共用，可傳送接收訊息
    /// </summary>
    public class CommunicationBase
    {
        //public const bool usePacket = false;
        //public static readonly byte[] Head = new byte[] { 0x0, 0x9, 0x7, 0x8 };
        //public static readonly byte[] Tail = new byte[] { 0xb, 0xa, 0xc, 0xa };
        class myType
        {
            public byte b { get; private set; }
            public int i { get; private set; }

            public myType(byte b, int i)
            {
                this.b = b;
                this.i = i;
            }
        }

        public static byte[] EncodeString(string src)
        {
            // do encode here...
            byte[] dest = Encoding.UTF8.GetBytes(src);
            return dest;
        }

        public static string DecodeString(byte[] src, int length = -1)
        {
            // do decode here...
            string dest = Encoding.UTF8.GetString(src, 0, length >= 0 ? length : src.Length);
            return dest;
        }

        //public static byte[] BuildPacket(byte[] content)
        //{
        //    // cal length of byte[]
        //    byte[] lengthBytes = BitConverter.GetBytes(content.Length);
        //    if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        //    List<byte[]> data = new List<byte[]>();
        //    data.Add(Head);
        //    data.Add(lengthBytes);
        //    data.Add(content);
        //    data.Add(Tail);

        //    byte[] byteContent = new byte[Head.Length + lengthBytes.Length + content.Length + Tail.Length];
        //    int offset = 0;
        //    for (int i = 0; i < data.Count; i++)
        //    {
        //        System.Buffer.BlockCopy(data[i], 0, byteContent, offset, data[i].Length);
        //        offset += data[i].Length;
        //    }
        //    return byteContent;
        //}

        //public static byte[] ParsePacket(ref byte[] content)
        //{
        //    // skip if content's length is less than head content & sizeof(int)
        //    if (usePacket == false ||
        //        content == null ||
        //        content.Length < Head.Length + sizeof(int))
        //        return null;

        //    // check head
        //    for (int i = 0; i < content.Length - 4; i++)
        //    {
        //        if (content[i] == Head[0] &&
        //            content[i + 1] == Head[1] &&
        //            content[i + 2] == Head[2] &&
        //            content[i + 3] == Head[3])
        //        {
        //            byte[] lengthBytes = new byte[sizeof(int)];
        //            System.Buffer.BlockCopy(content, i + Head.Length, lengthBytes, 0, lengthBytes.Length);
        //            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        //            int contentLength = BitConverter.ToInt32(lengthBytes, 0);
        //            if (contentLength >= 0)
        //            {
        //                // check left length is enough for whole packet or not
        //                if (content.Length - i >= Head.Length + lengthBytes.Length + contentLength + Tail.Length)
        //                {
        //                    byte[] tailBytes = new byte[Tail.Length];
        //                    System.Buffer.BlockCopy(content, i + Head.Length + lengthBytes.Length + contentLength, tailBytes, 0, tailBytes.Length);
        //                    // check tail
        //                    bool tailMatched = true;
        //                    for (int j = 0; j < Tail.Length; j++)
        //                    {
        //                        if (tailBytes[j] != Tail[j])
        //                        {
        //                            tailMatched = false;
        //                            break;
        //                        }
        //                    }
        //                    if (tailMatched)
        //                    {
        //                        byte[] result = new byte[contentLength];
        //                        System.Buffer.BlockCopy(content, i + Head.Length + lengthBytes.Length, result, 0, result.Length);

        //                        int leftLength = content.Length - (i + Head.Length + lengthBytes.Length + result.Length + Tail.Length);
        //                        if (leftLength > 0)
        //                        {
        //                            byte[] newContent = new byte[leftLength];
        //                            System.Buffer.BlockCopy(content, content.Length - newContent.Length, newContent, 0, newContent.Length);
        //                            content = newContent;
        //                        }
        //                        else
        //                        {
        //                            content = null;
        //                        }
        //                        //
        //                        return result;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return null;
        //}

        static string ParsePacket(ref string content)
        {
            if (/*usePacket == true ||*/
                content == null ||
                content.Length == 0)
                return null;

            bool bCheck = false;
            int cnt = 0;
            int index = 0;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '{')
                {
                    cnt++;
                    if (bCheck == false)
                    {
                        bCheck = true;
                        index = i;
                    }
                }
                else if (bCheck && content[i] == '}')
                {
                    cnt--;

                    if (cnt == 0)
                    {
                        int length = i + 1 - index;
                        string strResult = content.Substring(index, length);
                        content = content.Substring(i + 1);
                        return strResult;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 接收訊息
        /// </summary>
        /// <param name="tmpTcpClient">TcpClient</param>
        /// <returns>接收到的訊息</returns>
        public static List<string> ParsePackets(string strReceived, ref byte[] bufUnparsed)
        {
            List<string> packets = new List<string>();
            // if got strReceived, process packet
            if (strReceived.Length > 0)
            {
                string strUnparsed = bufUnparsed != null ? DecodeString(bufUnparsed) : null;
                strUnparsed = strUnparsed != null ? strUnparsed + strReceived : strReceived;
                while (true)
                {
                    string packetStr = ParsePacket(ref strUnparsed);
                    if (packetStr == null) break;
                    //
                    packets.Add(packetStr);
                }
                bufUnparsed = strUnparsed != null ? EncodeString(strUnparsed) : null;
            }

            return packets;
        }

        /// <summary>
        /// 傳送訊息
        /// </summary>
        /// <param name="msg">要傳送的訊息</param>
        /// <param name="tmpTcpClient">TcpClient</param>
        public static void SendMsg(TcpClient tmpTcpClient, string msg)
        {
            if (tmpTcpClient == null ||
                tmpTcpClient.Connected == false) return;

            //ns is a NetworkStream class parameter
            //logger.Output(">> sendind data to client ...", LogLevel.LL_INFO);
            string error = Utils.SendPacket_TCP(msg, tmpTcpClient);
            if (error != null) Console.WriteLine(string.Format("SendMsg got error: {0}", error));
        }

        public static void SendMsg_WS(TcpClient tmpTcpClient, string msg, int maxLength = 65536)
        {
            if (tmpTcpClient == null ||
                tmpTcpClient.Connected == false) return;

            //ns is a NetworkStream class parameter
            //logger.Output(">> sendind data to client ...", LogLevel.LL_INFO);
            try
            {
                NetworkStream ns = tmpTcpClient.GetStream();
                if (ns.CanWrite)
                {
                    int totalPackets = msg.Length / maxLength + ((msg.Length % maxLength > 0) ? 1 : 0);

                    if (totalPackets == 1)
                    {
                        byte[] buf = Encoding.UTF8.GetBytes(msg);
                        int frameSize = 64;

                        List<byte[]> parts = buf.Select((b, i) => new { b, i })
                                        .GroupBy(x => x.i / (frameSize - 1))
                                        .Select(x => x.Select(y => y.b).ToArray())
                                        .ToList();

                        for (int i = 0; i < parts.Count; i++)
                        {
                            byte cmd = 0;
                            if (i == 0) cmd |= 1;
                            if (i == parts.Count - 1) cmd |= 0x80;

                            ns.WriteByte(cmd);
                            ns.WriteByte((byte)parts[i].Length);
                            ns.Write(parts[i], 0, parts[i].Length);
                        }

                        ns.Flush();
                    }
                    else
                    {
                        JObject obj = new JObject();
                        obj["id"] = Utils.generateRandom(20);
                        obj["total"] = totalPackets;
                        for (int index = 0; index < totalPackets; index++)
                        {
                            obj["index"] = index;
                            if (msg.Length > maxLength)
                            {
                                int length = msg.Length;
                                obj["content"] = msg.Substring(0, maxLength);
                                msg = msg.Substring(maxLength);
                            }
                            else
                            {
                                obj["content"] = msg;
                                msg = "";
                            }

                            string content = JsonConvert.SerializeObject(obj);
                            byte[] buf = Encoding.UTF8.GetBytes(content);
                            int frameSize = 64;

                            List<byte[]> parts = buf.Select((b, i) => new { b, i })
                                            .GroupBy(x => x.i / (frameSize - 1))
                                            .Select(x => x.Select(y => y.b).ToArray())
                                            .ToList();

                            for (int i = 0; i < parts.Count; i++)
                            {
                                byte cmd = 0;
                                if (i == 0) cmd |= 1;
                                if (i == parts.Count - 1) cmd |= 0x80;

                                ns.WriteByte(cmd);
                                ns.WriteByte((byte)parts[i].Length);
                                ns.Write(parts[i], 0, parts[i].Length);
                            }

                            ns.Flush();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("SendMsg_WS >> {0}", ex.Message));
            }
        }
    }
}
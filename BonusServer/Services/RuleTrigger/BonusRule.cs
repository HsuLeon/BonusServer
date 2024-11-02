using BonusServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FunLobbyUtils;

namespace BonusServer.Services.RuleTrigger
{
    public class BonusRule
    {
        public enum RULEID
        {
            Rule_None = 0,
            Rule_0 = 1,
            Rule_1 = 2
        }

        public enum WIN_TYPE
        {
            WinA,
            WinB,
            WinCR
        }

        public class TriggeringCondition
        {
            // 每注換算獎金的分母
            public float ScoreInterval { get; set; }
            public DateTime WinBonusBeginUtcTime { get; set; }
            public int MinPay { get; set; }

            public TriggeringCondition()
            {
                this.ScoreInterval = 3000;
                this.WinBonusBeginUtcTime = Utils.zeroDateTime();
                this.MinPay = 1;
            }

            public virtual JObject Settings()
            {
                throw new Exception("call Settings in base class");
            }
        }
        public RULEID RuleId { get; protected set; }

        public CollectData WinCollection_A { get; protected set; }
        public CollectData WinCollection_B { get; protected set; }
        public CollectData WinCollection_CR { get; protected set; }

        public TriggeringCondition? Condition_A { get; protected set; }
        public TriggeringCondition? Condition_B { get; protected set; }
        public TriggeringCondition? Condition_CR { get; protected set; }

        public BonusRule()
        {
            this.RuleId = RULEID.Rule_None;

            this.WinCollection_A = new CollectData();
            this.WinCollection_B = new CollectData();
            this.WinCollection_CR = new CollectData();

            this.Condition_A = null;
            this.Condition_B = null;
            this.Condition_CR = null;
        }

        public virtual void ParseSettings(WIN_TYPE winType, string content)
        {
            throw new Exception("ParseSettings in base class");
        }

        public virtual void Collect(WIN_TYPE winType, CollectData data)
        {
            throw new Exception("Collect in base class");
        }

        public bool IsWinSpinEnabled(WIN_TYPE winType)
        {
            switch (winType)
            {
                case WIN_TYPE.WinA:
                    return this.Condition_A?.WinBonusBeginUtcTime.Ticks > 0 ? true : false;
                case WIN_TYPE.WinB:
                    return this.Condition_B?.WinBonusBeginUtcTime.Ticks > 0 ? true : false;
                case WIN_TYPE.WinCR:
                    return this.Condition_CR?.WinBonusBeginUtcTime.Ticks > 0 ? true : false;
                default:
                    return false;
            }
        }

        public string? TryWinSpinA(JObject objData)
        {
            string? errMsg = null;
            try
            {
                // skip if not WinSpinA not enabled
                if (this.IsWinSpinEnabled(WIN_TYPE.WinA) == false)
                {
                    throw new Exception("WinSpinA not enabled");
                }
                // skip if random check failed
                if (this.RandomWin(WIN_TYPE.WinA) == false)
                {
                    throw new Exception("RandomWinA failed");
                }

                JObject? subData = objData;
                while (subData != null)
                {
                    if (subData.ContainsKey("data") == false) break;
                    // replace to inner data
                    objData = subData;
                    // check next
                    subData = objData["data"]?.Value<JObject>();
                }
                if (objData != null &&
                    objData.ContainsKey("account") &&
                    objData.ContainsKey("urlTransferPoints") &&
                    objData.ContainsKey("spinData"))
                {
                    //check if win bonus or not
                    string? account = objData["account"]?.Value<string>();
                    string? urlTransferPoints = objData["urlTransferPoints"]?.Value<string>();
                    string? strSpinData = objData["spinData"]?.Value<string>();
                    SpinData? spinData = strSpinData != null ? JsonConvert.DeserializeObject<SpinData>(strSpinData) : null;
                    string? error = this.VerifyWinA(urlTransferPoints, account, spinData);
                    if (error != null) throw new Exception(error);
                }
                else
                {
                    throw new Exception("invalid objData");
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public string? TryWinSpinB(JObject objData)
        {
            string? errMsg = null;
            try
            {
                // skip if not WinSpinB not enabled
                if (this.IsWinSpinEnabled(WIN_TYPE.WinB) == false)
                {
                    throw new Exception("WinSpinB not enabled");
                }
                // skip if random check failed
                if (this.RandomWin(WIN_TYPE.WinB) == false)
                {
                    throw new Exception("RandomWinB failed");
                }

                JObject? subData = objData;
                while (subData != null)
                {
                    if (subData.ContainsKey("data") == false) break;
                    // replace to inner data
                    objData = subData;
                    // check next
                    subData = objData["data"]?.Value<JObject>();
                }
                if (objData != null &&
                    objData.ContainsKey("account") &&
                    objData.ContainsKey("urlTransferPoints") &&
                    objData.ContainsKey("spinData"))
                {
                    //check if win bonus or not
                    string? account = objData["account"]?.Value<string>();
                    string? urlTransferPoints = objData["urlTransferPoints"]?.Value<string>();
                    string? strSpinData = objData["spinData"]?.Value<string>();
                    SpinData? spinData = strSpinData != null ? JsonConvert.DeserializeObject<SpinData>(strSpinData) : null;
                    string? error = this.VerifyWinB(urlTransferPoints, account, spinData);
                    if (error != null) throw new Exception(error);
                }
                else
                {
                    throw new Exception("invalid objData");
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        public string? TryWinSpinCR(JObject objData)
        {
            string? errMsg = null;
            try
            {
                // skip if not WinSpinCR not enabled
                if (this.IsWinSpinEnabled(WIN_TYPE.WinCR) == false)
                {
                    throw new Exception("WinSpinCR not enabled");
                }
                // skip if random check failed
                if (this.RandomWin(WIN_TYPE.WinCR) == false)
                {
                    throw new Exception("RandomWinCR failed");
                }

                JObject? subData = objData;
                while (subData != null)
                {
                    if (subData.ContainsKey("data") == false) break;
                    // replace to inner data
                    objData = subData;
                    // check next
                    subData = objData["data"]?.Value<JObject>();
                }
                if (objData != null &&
                    objData.ContainsKey("account") &&
                    objData.ContainsKey("urlTransferPoints") &&
                    objData.ContainsKey("spinData"))
                {
                    //check if win bonus or not
                    string? account = objData["account"]?.Value<string>();
                    string? urlTransferPoints = objData["urlTransferPoints"]?.Value<string>();
                    string? strSpinData = objData["spinData"]?.Value<string>();
                    SpinData? spinData = strSpinData != null ? JsonConvert.DeserializeObject<SpinData>(strSpinData) : null;
                    string? error = this.VerifyWinCR(urlTransferPoints, account, spinData); ;
                    if (error != null) throw new Exception(error);
                }
                else
                {
                    throw new Exception("invalid objData");
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        protected string? VerifyWinA(string? urlTransferPoints, string? account, SpinData? spinData)
        {
            string? errMsg = null;
            try
            {
                if (urlTransferPoints == null) throw new Exception("WinSpinA got null urlTransferPoints");
                if (account == null) throw new Exception("WinSpinA got null account");
                if (spinData == null) throw new Exception("WinSpinCR got null spinData");
                if (this.Condition_A == null) throw new Exception("WinSpinCR got null mConditionA");
                int scores = (int)(WinCollection_A.TotalBet / Condition_A.ScoreInterval);
                if (scores < Condition_A.MinPay) throw new Exception("collected scores not enough");
                // skip if request timing is early than WinBonusBeginDT
                DateTime spinUtcTime = new DateTime(spinData.UtcTicks, DateTimeKind.Utc);
                TimeSpan ts = spinUtcTime - this.Condition_A.WinBonusBeginUtcTime;
                if (ts.TotalMilliseconds < 0) throw new Exception("WinSpinA got before than WinBonusBeginDT");

                // win scores for A
                JObject objBonus = new JObject();
                objBonus["agent"] = "WinLobby";
                objBonus["userid"] = account;
                objBonus["points"] = scores;
                objBonus["award"] = false;
                objBonus["reason"] = Config.UniBonus;
                string? strResponse = Utils.HttpPost(urlTransferPoints, objBonus);
                if (strResponse == null) strResponse = string.Format("{0} returns null", urlTransferPoints);
                // make record here...
                DBAgent.RecordBonusAward(BonusRule.WIN_TYPE.WinA.ToString(),
                    WinCollection_A.TotalBet, Condition_A.ScoreInterval,
                    objBonus, urlTransferPoints, strResponse);
                // cal left totalBet
                float leftTotalBet = WinCollection_A.TotalBet;
                while (leftTotalBet > Condition_A.ScoreInterval) leftTotalBet -= Condition_A.ScoreInterval;
                // reset...
                WinCollection_A.Reset();
                // assign TotalBet by leftTotalBet
                WinCollection_A.TotalBet = leftTotalBet;
                // disable winA
                this.Condition_A.WinBonusBeginUtcTime = Utils.zeroDateTime();

                // broadcast notification to all subDomain
                DateTime winTime = new DateTime(spinData.UtcTicks, DateTimeKind.Utc);
                string msg = GenerateWinMessage(NotifyData.WinSpin.A, spinData, account, winTime, scores);
                BonusAgent.AddNotifyData(BonusAgent.WebSite, NotifyData.WinSpin.A, msg, scores, spinData.MachineName, account);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        protected string? VerifyWinB(string? urlTransferPoints, string? account, SpinData? spinData)
        {
            string? errMsg = null;
            try
            {
                if (urlTransferPoints == null) throw new Exception("WinSpinB got null urlTransferPoints");
                if (account == null) throw new Exception("WinSpinB got null account");
                if (spinData == null) throw new Exception("WinSpinB got null spinData");
                if (this.Condition_B == null) throw new Exception("WinSpinB got null mConditionB");
                int scores = (int)(WinCollection_B.TotalBet / Condition_B.ScoreInterval);
                if (scores < Condition_B.MinPay) throw new Exception("collected scores not enough");
                // skip if request timing is early than WinBonusBeginDT
                DateTime spinUtcTime = new DateTime(spinData.UtcTicks, DateTimeKind.Utc);
                TimeSpan ts = spinUtcTime - this.Condition_B.WinBonusBeginUtcTime;
                if (ts.TotalMilliseconds < 0) throw new Exception("WinSpinB got before than WinBonusBeginDT");

                // win scores for B
                JObject objBonus = new JObject();
                objBonus["agent"] = "WinLobby";
                objBonus["userid"] = account;
                objBonus["points"] = scores;
                objBonus["award"] = false;
                objBonus["reason"] = Config.UniBonus;
                string? strResponse = Utils.HttpPost(urlTransferPoints, objBonus);
                if (strResponse == null) strResponse = string.Format("{0} returns null", urlTransferPoints);
                // make record here...
                DBAgent.RecordBonusAward(BonusRule.WIN_TYPE.WinB.ToString(),
                    WinCollection_B.TotalBet, Condition_B.ScoreInterval,
                    objBonus, urlTransferPoints, strResponse);
                // cal left totalBet
                float leftTotalBet = WinCollection_B.TotalBet;
                while (leftTotalBet > Condition_B.ScoreInterval) leftTotalBet -= Condition_B.ScoreInterval;
                // reset...
                WinCollection_B.Reset();
                // assign TotalBet by leftTotalBet
                WinCollection_B.TotalBet = leftTotalBet;
                // disable winB
                this.Condition_B.WinBonusBeginUtcTime = Utils.zeroDateTime();

                // broadcast notification to all subDomain
                DateTime winTime = new DateTime(spinData.UtcTicks, DateTimeKind.Utc);
                string msg = GenerateWinMessage(NotifyData.WinSpin.B, spinData, account, winTime, scores);
                BonusAgent.AddNotifyData(BonusAgent.WebSite, NotifyData.WinSpin.B, msg, scores, spinData.MachineName, account);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        protected string? VerifyWinCR(string? urlTransferPoints, string? account, SpinData? spinData)
        {
            string? errMsg = null;
            try
            {
                if (urlTransferPoints == null) throw new Exception("WinSpinCR got null urlTransferPoints");
                if (account == null) throw new Exception("WinSpinCR got null account");
                if (spinData == null) throw new Exception("WinSpinCR got null spinData");
                if (this.Condition_CR == null) throw new Exception("WinSpinCR got null mConditionCR");
                int scores = (int)(WinCollection_CR.TotalBet / Condition_CR.ScoreInterval);
                if (scores < Condition_CR.MinPay) throw new Exception("collected scores not enough");
                // skip if request timing is early than WinBonusBeginDT
                DateTime spinUtcTime = new DateTime(spinData.UtcTicks, DateTimeKind.Utc);
                TimeSpan ts = spinUtcTime - this.Condition_CR.WinBonusBeginUtcTime;
                if (ts.TotalMilliseconds < 0) throw new Exception("WinSpinCR got before than WinBonusBeginDT");

                // win scores for cr
                JObject objBonus = new JObject();
                objBonus["agent"] = "WinLobby";
                objBonus["userid"] = account;
                objBonus["points"] = scores;
                objBonus["award"] = false;
                objBonus["reason"] = Config.UniBonus;
                string? strResponse = Utils.HttpPost(urlTransferPoints, objBonus);
                if (strResponse == null) strResponse = string.Format("{0} returns null", urlTransferPoints);
                // make record here...
                DBAgent.RecordBonusAward(BonusRule.WIN_TYPE.WinCR.ToString(),
                    WinCollection_CR.TotalBet, Condition_CR.ScoreInterval,
                    objBonus, urlTransferPoints, strResponse);
                // cal left totalBet
                float leftTotalBet = WinCollection_CR.TotalBet;
                while (leftTotalBet > Condition_CR.ScoreInterval) leftTotalBet -= Condition_CR.ScoreInterval;
                // reset...
                WinCollection_CR.Reset();
                // assign TotalBet by leftTotalBet
                WinCollection_CR.TotalBet = leftTotalBet;
                // disable winCR
                this.Condition_CR.WinBonusBeginUtcTime = Utils.zeroDateTime();

                // broadcast notification to all subDomain
                DateTime winTime = new DateTime(spinData.UtcTicks, DateTimeKind.Utc);
                string msg = GenerateWinMessage(NotifyData.WinSpin.CR, spinData, account, winTime.ToLocalTime(), scores);
                BonusAgent.AddNotifyData(BonusAgent.WebSite, NotifyData.WinSpin.CR, msg, scores, spinData.MachineName, account);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }
            return errMsg;
        }

        protected virtual bool RandomWin(WIN_TYPE winType)
        {
            throw new Exception("RandomWin in base class");
        }

        protected string GenerateWinMessage(NotifyData.WinSpin winSpin, SpinData spinData, string account, DateTime winTime, int scores)
        {
            List<string> congratulations = new List<string> { "恭喜", "哇勒", "天啊", "狂賀", "大賀喜", "了不起", "殺很大", "出奇蹟啦", "給你放煙火" };
            Random rand = new Random();
            string congrats = congratulations[rand.Next(congratulations.Count)];
            string bonusKind = "彩金";
            switch (winSpin)
            {
                case NotifyData.WinSpin.A:
                    bonusKind = "彩金A";
                    break;
                case NotifyData.WinSpin.B:
                    bonusKind = "彩金B";
                    break;
                case NotifyData.WinSpin.CR:
                    bonusKind = "彩金CR";
                    break;
            }

            string strFormat = account.Length > 0 ?
                "{0}!!!{1}的玩家{2}於{3}贏得{4}的{5}共{6}分" :
                "{0}!!!{1}的遊戲{2}於{3}贏得{4}的{5}共{6}分";
            string msg = string.Format(strFormat,
                congrats,
                spinData.WebSite,
                account.Length > 0 ? account : spinData.MachineName,
                winTime.ToLocalTime(),
                BonusAgent.WebSite,
                bonusKind,
                scores);
            return msg;
        }
    }
}

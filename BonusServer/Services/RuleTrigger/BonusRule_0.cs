using BonusServer.Models;
using FunLobbyUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BonusServer.Services.RuleTrigger
{
    public class BonusRule_0 : BonusRule
    {
        class TriggeringCondition_0 : BonusRule.TriggeringCondition
        {
            // 啟動開獎條件1: 總得分 / 總下注
            public float WinOverBet { get; set; }
            // 啟動開獎條件2: 贏分次數
            public float WinCount { get; set; }

            public TriggeringCondition_0() : base()
            {
                WinOverBet = 0.9f;
                WinCount = 100;
            }

            public override JObject Settings()
            {
                JObject obj = new JObject();
                obj["ScoreInterval"] = this.ScoreInterval;
                obj["WinOverBet"] = this.WinOverBet;
                obj["WinCount"] = this.WinCount;
                obj["MinPay"] = this.MinPay;
                return obj;
            }
        }

        public BonusRule_0() : base()
        {
            this.RuleId = RULEID.Rule_0;

            this.Condition_A = new TriggeringCondition_0();
            this.Condition_B = new TriggeringCondition_0();
            this.Condition_CR = new TriggeringCondition_0();
        }

        public override void ParseSettings(WIN_TYPE winType, string content)
        {
            try
            {
                if (content == null) throw new Exception("ParseSettings got null content");

                TriggeringCondition_0? src = JsonConvert.DeserializeObject<TriggeringCondition_0>(content);
                switch (winType)
                {
                    case WIN_TYPE.WinA:
                        {
                            TriggeringCondition_0? condition = this.Condition_A as TriggeringCondition_0;
                            if (condition == null) throw new Exception("ParseSettings got null condition for A");
                            condition.ScoreInterval = src.ScoreInterval;
                            condition.WinOverBet = src.WinOverBet;
                            condition.WinCount = src.WinCount;
                            condition.MinPay = src.MinPay;
                        }
                        break;
                    case WIN_TYPE.WinB:
                        {
                            TriggeringCondition_0? condition = this.Condition_B as TriggeringCondition_0;
                            if (condition == null) throw new Exception("ParseSettings got null condition for B");
                            condition.ScoreInterval = src.ScoreInterval;
                            condition.WinOverBet = src.WinOverBet;
                            condition.WinCount = src.WinCount;
                            condition.MinPay = src.MinPay;
                        }
                        break;
                    case WIN_TYPE.WinCR:
                        {
                            TriggeringCondition_0? condition = this.Condition_CR as TriggeringCondition_0;
                            if (condition == null) throw new Exception("ParseSettings got null condition for CR");
                            condition.ScoreInterval = src.ScoreInterval;
                            condition.WinOverBet = src.WinOverBet;
                            condition.WinCount = src.WinCount;
                            condition.MinPay = src.MinPay;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.StoreMsg(ex.Message);
            }
        }

        public override void Collect(WIN_TYPE winType, CollectData data)
        {
            switch (winType)
            {
                case WIN_TYPE.WinA:
                    {
                        WinCollection_A.TotalBet += data.TotalBet;
                        WinCollection_A.TotalWin += data.TotalWin;
                        WinCollection_A.WinA += data.WinA;
                        TriggeringCondition_0? condition = this.Condition_A as TriggeringCondition_0;
                        if (condition != null &&
                            condition.WinBonusBeginUtcTime.Ticks == 0)
                        {
                            // cal and check if trigger BetWin of WinA
                            if (condition.WinCount > WinCollection_A.WinA &&
                                condition.WinOverBet > WinCollection_A.TotalWin / WinCollection_A.TotalBet)
                            {
                                condition.WinBonusBeginUtcTime = DateTime.UtcNow;
                            }
                            else
                            {
                                condition.WinBonusBeginUtcTime = Utils.zeroDateTime();
                            }
                        }
                    }
                    break;
                case WIN_TYPE.WinB:
                    {
                        WinCollection_B.TotalBet += data.TotalBet;
                        WinCollection_B.TotalWin += data.TotalWin;
                        WinCollection_B.WinB += data.WinB;
                        TriggeringCondition_0? condition = this.Condition_B as TriggeringCondition_0;
                        if (condition != null &&
                            condition.WinBonusBeginUtcTime.Ticks == 0)
                        {
                            // cal and check if trigger BetWin of WinB
                            if (condition.WinCount > WinCollection_B.WinB &&
                                condition.WinOverBet > WinCollection_B.TotalWin / WinCollection_B.TotalBet)
                            {
                                condition.WinBonusBeginUtcTime = DateTime.UtcNow;
                            }
                            else
                            {
                                condition.WinBonusBeginUtcTime = Utils.zeroDateTime();
                            }
                        }
                    }
                    break;
                case WIN_TYPE.WinCR:
                    {
                        WinCollection_CR.TotalBet += data.TotalBet;
                        WinCollection_CR.TotalWin += data.TotalWin;
                        WinCollection_CR.WinB += data.WinB;
                        TriggeringCondition_0? condition = this.Condition_CR as TriggeringCondition_0;
                        if (condition != null &&
                            condition.WinBonusBeginUtcTime.Ticks == 0)
                        {
                            // cal and check if trigger BetWin of WinB
                            if (condition.WinCount > WinCollection_CR.WinB &&
                                condition.WinOverBet > WinCollection_CR.TotalWin / WinCollection_CR.TotalBet)
                            {
                                condition.WinBonusBeginUtcTime = DateTime.UtcNow;
                            }
                            else
                            {
                                condition.WinBonusBeginUtcTime = Utils.zeroDateTime();
                            }
                        }
                    }
                    break;
            }
        }

        protected override bool RandomWin(WIN_TYPE winType)
        {
            // always pass
            return true;
        }
    }
}

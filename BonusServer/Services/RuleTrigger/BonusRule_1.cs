using BonusServer.Models;
using FunLobbyUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BonusServer.Services.RuleTrigger
{
    public class BonusRule_1 : BonusRule
    {
        class TriggeringCondition_1 : BonusRule.TriggeringCondition
        {
            // 中獎率
            float mWinRatio;
            public float WinRatio
            {
                get {  return mWinRatio; }
                set
                {
                    if (value > 0.0f)
                    {
                        mWinRatio = value;
                        // cal WinDivided
                        int curValue = 10;
                        while (mWinRatio * curValue < 1.0f)
                        {
                            curValue *= 10;
                        }
                        this.RandomNum = (int)(mWinRatio * curValue);
                        this.RandomDivided = curValue;
                    }
                }
            }
            public int RandomNum { get; protected set; }
            public int RandomDivided { get; protected set; }
            // 啟動開獎條件2: 贏分次數
            public float MinTotalBet { get; set; }

            public TriggeringCondition_1() : base()
            {
                this.WinRatio = 0.001f;
                this.MinTotalBet = 0;
            }

            public override JObject Settings()
            {
                JObject obj = new JObject();
                obj["ScoreInterval"] = this.ScoreInterval;
                obj["WinRatio"] = this.WinRatio;
                obj["MinTotalBet"] = this.MinTotalBet;
                obj["MinPay"] = this.MinPay;
                return obj;
            }
        }

        public BonusRule_1() : base()
        {
            this.RuleId = RULEID.Rule_1;

            this.Condition_A = new TriggeringCondition_1();
            this.Condition_B = new TriggeringCondition_1();
            this.Condition_CR = new TriggeringCondition_1();
        }

        public override void ParseSettings(WIN_TYPE winType, string content)
        {
            try
            {
                if (content == null) throw new Exception("ParseSettings got null content");

                TriggeringCondition_1? src = JsonConvert.DeserializeObject<TriggeringCondition_1>(content);
                switch (winType)
                {
                    case WIN_TYPE.WinA:
                        {
                            TriggeringCondition_1? condition = this.Condition_A as TriggeringCondition_1;
                            if (condition == null) throw new Exception("ParseSettings got null condition for A");
                            condition.ScoreInterval = src.ScoreInterval;
                            condition.WinRatio = src.WinRatio;
                            condition.MinTotalBet = src.MinTotalBet;
                            condition.MinPay = src.MinPay;
                        }
                        break;
                    case WIN_TYPE.WinB:
                        {
                            TriggeringCondition_1? condition = this.Condition_B as TriggeringCondition_1;
                            if (condition == null) throw new Exception("ParseSettings got null condition for B");
                            condition.ScoreInterval = src.ScoreInterval;
                            condition.WinRatio = src.WinRatio;
                            condition.MinTotalBet = src.MinTotalBet;
                            condition.MinPay = src.MinPay;
                        }
                        break;
                    case WIN_TYPE.WinCR:
                        {
                            TriggeringCondition_1? condition = this.Condition_CR as TriggeringCondition_1;
                            if (condition == null) throw new Exception("ParseSettings got null condition for CR");
                            condition.ScoreInterval = src.ScoreInterval;
                            condition.WinRatio = src.WinRatio;
                            condition.MinTotalBet = src.MinTotalBet;
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
                        WinCollection_A.WinA += data.WinA;
                        TriggeringCondition_1? condition = this.Condition_A as TriggeringCondition_1;
                        // trigger win as long as WinBonusBeginDT is 0
                        if (condition != null &&
                            condition.WinBonusBeginUtcTime.Ticks == 0)
                        {
                            if (WinCollection_A.WinA > 0 &&
                                WinCollection_A.TotalBet >= condition.MinTotalBet)
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
                        WinCollection_B.WinB += data.WinB;
                        TriggeringCondition_1? condition = this.Condition_B as TriggeringCondition_1;
                        // trigger win as long as WinBonusBeginDT is 0
                        if (condition != null &&
                            condition.WinBonusBeginUtcTime.Ticks == 0)
                        {
                            if (WinCollection_B.WinB > 0 &&
                                WinCollection_B.TotalBet >= condition.MinTotalBet)
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
                        WinCollection_CR.WinB += data.WinB;
                        TriggeringCondition_1? condition = this.Condition_CR as TriggeringCondition_1;
                        // trigger win as long as WinBonusBeginDT is 0
                        if (condition != null &&
                            condition.WinBonusBeginUtcTime.Ticks == 0)
                        {
                            if (WinCollection_CR.WinB > 0 &&
                                WinCollection_CR.TotalBet >= condition.MinTotalBet)
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
            TriggeringCondition_1? condition = null;
            switch (winType)
            {
                case WIN_TYPE.WinA:
                    condition = this.Condition_A as TriggeringCondition_1;
                    break;
                case WIN_TYPE.WinB:
                    condition = this.Condition_B as TriggeringCondition_1;
                    break;
                case WIN_TYPE.WinCR:
                    condition = this.Condition_CR as TriggeringCondition_1;
                    break;
            }

            if (condition != null)
            {
                // random value to get win
                Random rand = new Random();
                int num = rand.Next(0, condition.RandomDivided);
                if (num <= condition.RandomNum) return true;
            }
            return false;
        }
    }
}

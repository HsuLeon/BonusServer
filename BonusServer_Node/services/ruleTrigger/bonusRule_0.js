
import Utils from '../../utils/utils.js';
import Log from '../../utils/log.js';
import {TriggeringCondition, BonusRule} from './bonusRule.js';

class TriggeringCondition_0 extends TriggeringCondition
{
    // 啟動開獎條件1: 總得分 / 總下注
    WinOverBet;
    // 啟動開獎條件2: 贏分次數
    WinCount;

    constructor()
    {
        super();
        this.WinOverBet = 0.9;
        this.WinCount = 100;
    }

    Settings()
    {
        const obj = {
            ScoreInterval: this.ScoreInterval,
            WinOverBet: this.WinOverBet,
            WinCount: this.WinCount,
            MinPay: this.MinPay
        }
        return obj;
    }
}

export default class BonusRule_0 extends BonusRule
{
    constructor()
    {
        super(BonusRule.RULEID.Rule_0);

        this._Condition_A = new TriggeringCondition_0();
        this._Condition_B = new TriggeringCondition_0();
        this._Condition_CR = new TriggeringCondition_0();
    }

    ParseSettings(winType, content)
    {
        try
        {
            if (content == null) throw new Error("ParseSettings got null content");

            const src = JSON.parse(content);
            switch (winType)
            {
                case BonusRule.WIN_TYPE.WinA:
                    {
                        if (!this._Condition_A) throw new Error("ParseSettings got null condition for A");
                        this._Condition_A.ScoreInterval = src.ScoreInterval;
                        this._Condition_A.WinOverBet = src.WinOverBet;
                        this._Condition_A.WinCount = src.WinCount;
                        this._Condition_A.MinPay = src.MinPay;
                    }
                    break;
                case BonusRule.WIN_TYPE.WinB:
                    {
                        if (!this._Condition_B) throw new Error("ParseSettings got null condition for B");
                        this._Condition_B.ScoreInterval = src.ScoreInterval;
                        this._Condition_B.WinOverBet = src.WinOverBet;
                        this._Condition_B.WinCount = src.WinCount;
                        this._Condition_B.MinPay = src.MinPay;
                    }
                    break;
                case BonusRule.WIN_TYPE.WinCR:
                    {
                        if (!this._Condition_CR) throw new Error("ParseSettings got null condition for CR");
                        this._Condition_CR.ScoreInterval = src.ScoreInterval;
                        this._Condition_CR.WinOverBet = src.WinOverBet;
                        this._Condition_CR.WinCount = src.WinCount;
                        this._Condition_CR.MinPay = src.MinPay;
                    }
                    break;
            }
        }
        catch (err)
        {
            Log.storeMsg(err.message ? err.message : err);
        }
    }

    Collect(winType, collectData)
    {
        switch (winType)
        {
            case BonusRule.WIN_TYPE.WinA:
                {
                    this._WinCollection_A.TotalBet += collectData.TotalBet;
                    this._WinCollection_A.TotalWin += collectData.TotalWin;
                    this._WinCollection_A.WinA += collectData.WinA;
                    const condition = this._Condition_A;
                    if (condition && condition.WinBonusBeginUtcTime.getTime() == 0)
                    {
                        const curWinOverBet = this._WinCollection_A.TotalWin / this._WinCollection_A.TotalBet;
                        // cal and check if trigger BetWin of WinA
                        if (this._WinCollection_A.WinA > condition.WinCount &&
                            curWinOverBet < condition.WinOverBet)
                        {
                            condition.WinBonusBeginUtcTime = new Date();
                        }
                        else
                        {
                            condition.WinBonusBeginUtcTime = Utils.zeroDateTime();
                        }
                    }
                }
                break;
            case BonusRule.WIN_TYPE.WinB:
                {
                    this._WinCollection_B.TotalBet += collectData.TotalBet;
                    this._WinCollection_B.TotalWin += collectData.TotalWin;
                    this._WinCollection_B.WinB += collectData.WinB;
                    const condition = this._Condition_B;
                    if (condition && condition.WinBonusBeginUtcTime.getTime() == 0)
                    {
                        const curWinOverBet = this._WinCollection_B.TotalWin / this._WinCollection_B.TotalBet;
                        // cal and check if trigger BetWin of WinB
                        if (this._WinCollection_B.WinB > condition.WinCount &&
                            curWinOverBet < condition.WinOverBet)
                        {
                            condition.WinBonusBeginUtcTime = new Date();
                        }
                        else
                        {
                            condition.WinBonusBeginUtcTime = Utils.zeroDateTime();
                        }
                    }
                }
                break;
            case BonusRule.WIN_TYPE.WinCR:
                {
                    this._WinCollection_CR.TotalBet += collectData.TotalBet;
                    this._WinCollection_CR.TotalWin += collectData.TotalWin;
                    this._WinCollection_CR.WinB += collectData.WinB;
                    const condition = this._Condition_CR;
                    if (condition && condition.WinBonusBeginUtcTime.getTime() == 0)
                    {
                        const curWinOverBet = this._WinCollection_CR.TotalWin / this._WinCollection_CR.TotalBet;
                        // cal and check if trigger BetWin of WinB
                        if (this._WinCollection_CR.WinB > condition.WinCount &&
                            curWinOverBet < condition.WinOverBet)
                        {
                            condition.WinBonusBeginUtcTime = new Date();
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

    RandomWin(winType)
    {
        // always pass
        return true;
    }
}
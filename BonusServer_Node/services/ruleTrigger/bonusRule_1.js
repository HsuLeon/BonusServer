
import Utils from '../../utils/utils.js';
import Log from '../../utils/log.js';
import {TriggeringCondition, BonusRule} from './bonusRule.js';

class TriggeringCondition_1 extends TriggeringCondition
{
    // 中獎率
    #mWinRatio;
    #mRandomNum;
    #mRandomDivided;

    // 啟動開獎條件2: 贏分次數
    MinTotalBet;

    WinRatio()
    {
        return this.#mWinRatio;
    }

    setWinRatio(value)
    {
        if (value > 0.0)
        {
            this.#mWinRatio = value;
            // cal WinDivided
            let curValue = 10;
            while (this.#mWinRatio * curValue < 1.0)
            {
                curValue *= 10;
            }
            this.#mRandomNum = Math.floor(this.#mWinRatio * curValue);
            this.#mRandomDivided = curValue;
        }
    }

    RandomNum()
    {
        return this.#mRandomNum;
    }
    
    RandomDivided()
    {
        return this.#mRandomDivided;
    }

    constructor()
    {
        super();        
        this.#mWinRatio = 0.001;
        this.MinTotalBet = 0;
    }

    Settings()
    {
        const obj = {
            ScoreInterval: this.ScoreInterval,
            WinRatio: this.WinRatio,
            MinTotalBet: this.MinTotalBet,
            MinPay: this.MinPay
        }
        return obj;
    }
}

export default class BonusRule_1 extends BonusRule
{
    constructor()
    {
        super(BonusRule.RULEID.Rule_1);

        this._Condition_A = new TriggeringCondition_1();
        this._Condition_B = new TriggeringCondition_1();
        this._Condition_CR = new TriggeringCondition_1();
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
                        this._Condition_A.setWinRatio(src.WinRatio);
                        this._Condition_A.MinTotalBet = src.MinTotalBet;
                        this._Condition_A.MinPay = src.MinPay;
                    }
                    break;
                case BonusRule.WIN_TYPE.WinB:
                    {
                        if (!this._Condition_B) throw new Error("ParseSettings got null condition for B");
                        this._Condition_B.ScoreInterval = src.ScoreInterval;
                        this._Condition_B.setWinRatio(src.WinRatio);
                        this._Condition_B.MinTotalBet = src.MinTotalBet;
                        this._Condition_B.MinPay = src.MinPay;
                    }
                    break;
                case BonusRule.WIN_TYPE.WinCR:
                    {
                        if (!this._) throw new Error("ParseSettings got null condition for CR");
                        this._Condition_CR.ScoreInterval = src.ScoreInterval;
                        this._Condition_CR.setWinRatio(src.WinRatio);
                        this._Condition_CR.MinTotalBet = src.MinTotalBet;
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
                    this._WinCollection_A.WinA += collectData.WinA;
                    const condition = this._Condition_A;
                    // trigger win as long as WinBonusBeginDT is 0
                    if (condition && condition.WinBonusBeginUtcTime.getTime() == 0)
                    {
                        if (this._WinCollection_A.WinA > 0 &&
                            this._WinCollection_A.TotalBet >= condition.MinTotalBet)
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
                    this._WinCollection_B.WinB += collectData.WinB;
                    const condition = this._Condition_B;
                    // trigger win as long as WinBonusBeginDT is 0
                    if (condition && condition.WinBonusBeginUtcTime.getTime() == 0)
                    {
                        if (this._WinCollection_B.WinB > 0 &&
                            this._WinCollection_B.TotalBet >= condition.MinTotalBet)
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
                    this._WinCollection_CR.WinB += collectData.WinB;
                    const condition = this._Condition_CR;
                    // trigger win as long as WinBonusBeginDT is 0
                    if (condition && condition.WinBonusBeginUtcTime.getTime() == 0)
                    {
                        if (this._WinCollection_CR.WinB > 0 &&
                            this._WinCollection_CR.TotalBet >= condition.MinTotalBet)
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
        let condition = null;
        switch (winType)
        {
            case BonusRule.WIN_TYPE.WinA:
                condition = this._Condition_A;
                break;
            case BonusRule.WIN_TYPE.WinB:
                condition = this._Condition_B;
                break;
            case BonusRule.WIN_TYPE.WinCR:
                condition = this._Condition_CR;
                break;
        }

        if (condition)
        {
            const randomDivided = condition.RandomDivided();
            const randomNum = condition.RandomNum();
            // random value to get win
            const num = Math.floor(Math.random() * randomDivided);
            if (num <= randomNum) return true;
        }
        return false;
    }
}
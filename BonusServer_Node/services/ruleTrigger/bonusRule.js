
import axios from "axios";
import Utils from "../../utils/utils.js";
import Config from "../../utils/config.js";
import DBAgent from "../dbAgent.js";
import BonusAgent from "../bonusAgent.js";
import NotifyData from "../../models/notifyData.js";
import CollectData from "../../models/collectData.js";

export class TriggeringCondition
{
    // 每注換算獎金的分母
    ScoreInterval;
    WinBonusBeginUtcTime;
    MinPay;

    constructor()
    {
        this.ScoreInterval = 3000;
        this.WinBonusBeginUtcTime = Utils.zeroDateTime();
        this.MinPay = 1;
    }

    Settings()
    {
        throw new Error("call Settings in base class");
    }
}

export class BonusRule
{
    static RULEID = {
        Rule_None: 0,
        Rule_0: 1,
        Rule_1: 2
    }

    static WIN_TYPE = {
        WinA: 'WinA',
        WinB: 'WinB',
        WinCR: 'WinCR'
    }
    
    RuleId;
    _WinCollection_A;
    _WinCollection_B;
    _WinCollection_CR;
    _Condition_A;
    _Condition_B;
    _Condition_CR;
    
    constructor(ruleId)
    {
        this.RuleId = ruleId ? ruleId : RULEID.Rule_None;

        this._WinCollection_A = new CollectData();
        this._WinCollection_B = new CollectData();
        this._WinCollection_CR = new CollectData();

        this._Condition_A = null;
        this._Condition_B = null;
        this._Condition_CR = null;
    }

    WinCollection_A()
    {
        return this._WinCollection_A;
    }

    SetWinCollection_A(totalBet, totalWin, winA, winB, bonusType)
    {
        this._WinCollection_A.TotalBet = totalBet;
        this._WinCollection_A.TotalWin = totalWin;
        this._WinCollection_A.WinA = winA;
        this._WinCollection_A.WinB = winB;
        this._WinCollection_A.BonusType = bonusType;
    }

    WinCollection_B()
    {
        return this._WinCollection_B;
    }

    SetWinCollection_B(totalBet, totalWin, winA, winB, bonusType)
    {
        this._WinCollection_B.TotalBet = totalBet;
        this._WinCollection_B.TotalWin = totalWin;
        this._WinCollection_B.WinA = winA;
        this._WinCollection_B.WinB = winB;
        this._WinCollection_B.BonusType = bonusType;
    }

    WinCollection_CR()
    {
        return this._WinCollection_CR;
    }

    SetWinCollection_CR(totalBet, totalWin, winA, winB, bonusType)
    {
        this._WinCollection_CR.TotalBet = totalBet;
        this._WinCollection_CR.TotalWin = totalWin;
        this._WinCollection_CR.WinA = winA;
        this._WinCollection_CR.WinB = winB;
        this._WinCollection_CR.BonusType = bonusType;
    }

    Condition_A()
    {
        return this._Condition_A;
    }

    Condition_B()
    {
        return this._Condition_B;
    }

    Condition_CR()
    {
        return this._Condition_CR;
    }

    ParseSettings(winType, content)
    {
        throw new Error("ParseSettings in base class");
    }

    Collect(winType, data)
    {
        throw new Error("Collect in base class");
    }

    IsWinSpinEnabled(winType)
    {
        switch (winType)
        {
            case BonusRule.WIN_TYPE.WinA:
                return this._Condition_A && this._Condition_A.WinBonusBeginUtcTime.getTime() > 0 ? true : false;
            case BonusRule.WIN_TYPE.WinB:
                return this._Condition_B && this._Condition_B.WinBonusBeginUtcTime.getTime() > 0 ? true : false;
            case BonusRule.WIN_TYPE.WinCR:
                return this._Condition_CR && this._Condition_CR.WinBonusBeginUtcTime.getTime() > 0 ? true : false;
            default:
                return false;
        }
    }

    async TryWinSpinA(objData)
    {
        let errMsg = null;
        try
        {
            // skip if not WinSpinA not enabled
            if (this.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinA) == false)
            {
                throw new Error("WinSpinA not enabled");
            }
            // skip if random check failed
            if (this.RandomWin(BonusRule.WIN_TYPE.WinA) == false)
            {
                throw new Error("RandomWinA failed");
            }

            let subData = objData;
            while (subData)
            {
                if (subData.hasOwnProperty("data") == false) break;
                // replace to inner data
                objData = subData;
                // check next
                subData = objData["data"];
            }
            if (objData &&
                objData.hasOwnProperty("account") &&
                objData.hasOwnProperty("urlTransferPoints") &&
                objData.hasOwnProperty("spinData"))
            {
                //check if win bonus or not
                const account = objData["account"];
                const urlTransferPoints = objData["urlTransferPoints"];
                const strSpinData = objData["spinData"];
                const spinData = strSpinData ? JSON.parse(strSpinData) : null;
                const error = await this.VerifyWinA(urlTransferPoints, account, spinData);
                if (error) throw new Error(error);
            }
            else
            {
                throw new Error("invalid objData");
            }
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
        }
        return errMsg;
    }

    async TryWinSpinB(objData)
    {
        let errMsg = null;
        try
        {
            // skip if not WinSpinB not enabled
            if (this.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinB) == false)
            {
                throw new Error("WinSpinB not enabled");
            }
            // skip if random check failed
            if (this.RandomWin(BonusRule.WIN_TYPE.WinB) == false)
            {
                throw new Error("RandomWinB failed");
            }

            let subData = objData;
            while (subData)
            {
                if (subData.hasOwnProperty("data") == false) break;
                // replace to inner data
                objData = subData;
                // check next
                subData = objData["data"];
            }
            if (objData &&
                objData.hasOwnProperty("account") &&
                objData.hasOwnProperty("urlTransferPoints") &&
                objData.hasOwnProperty("spinData"))
            {
                //check if win bonus or not
                const account = objData["account"];
                const urlTransferPoints = objData["urlTransferPoints"];
                const strSpinData = objData["spinData"];
                const spinData = strSpinData ? JSON.parse(strSpinData) : null;
                const error = await this.VerifyWinB(urlTransferPoints, account, spinData);
                if (error) throw new Error(error);
            }
            else
            {
                throw new Error("invalid objData");
            }
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
        }
        return errMsg;
    }

    async TryWinSpinCR(objData)
    {
        let errMsg = null;
        try
        {
            // skip if not WinSpinCR not enabled
            if (this.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinCR) == false)
            {
                throw new Error("WinSpinCR not enabled");
            }
            // skip if random check failed
            if (this.RandomWin(BonusRule.WIN_TYPE.WinCR) == false)
            {
                throw new Error("RandomWinCR failed");
            }

            let subData = objData;
            while (subData)
            {
                if (subData.hasOwnProperty("data") == false) break;
                // replace to inner data
                objData = subData;
                // check next
                subData = objData["data"];
            }
            if (objData &&
                objData.hasOwnProperty("account") &&
                objData.hasOwnProperty("urlTransferPoints") &&
                objData.hasOwnProperty("spinData"))
            {
                //check if win bonus or not
                const account = objData["account"];
                const urlTransferPoints = objData["urlTransferPoints"];
                const strSpinData = objData["spinData"];
                const spinData = strSpinData ? JSON.parse(strSpinData) : null;
                const error = await this.VerifyWinCR(urlTransferPoints, account, spinData); ;
                if (error) throw new Error(error);
            }
            else
            {
                throw new Error("invalid objData");
            }
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
        }
        return errMsg;
    }

    async VerifyWinA(urlTransferPoints, account, spinData)
    {
        let errMsg = null;
        try
        {
            if (!urlTransferPoints) throw new Error("WinSpinA got null urlTransferPoints");
            if (!account) throw new Error("WinSpinA got null account");
            if (!spinData) throw new Error("WinSpinCR got null spinData");
            if (!this._Condition_A) throw new Error("WinSpinCR got null mConditionA");
            const scores = Math.floor(this._WinCollection_A.TotalBet / this._Condition_A.ScoreInterval);
            if (scores < this._Condition_A.MinPay) throw new Error("collected scores not enough");
            // skip if request timing is early than WinBonusBeginDT
            const spinUtcTime = new Date(spinData.UtcTicks);
            const ts = spinUtcTime - this._Condition_A.WinBonusBeginUtcTime;
            if (ts.TotalMilliseconds < 0) throw new Error("WinSpinA got before than WinBonusBeginDT");

            // win scores for A
            const url = urlTransferPoints;
            const objBonus = {                
                agent: "WinLobby",
                userid: account,
                points: scores,
                award: false,
                reason: Config.UniBonus
            };
            const response = await axios.post(url, objBonus);
            if (!response) throw new Error(`API ${url} returns null`);
            // make record here...
            DBAgent.addBonusRecord(BonusRule.WIN_TYPE.WinA,
                this._WinCollection_A.TotalBet, this._Condition_A.ScoreInterval,
                objBonus, urlTransferPoints, response);
            // cal left totalBet
            let leftTotalBet = this._WinCollection_A.TotalBet;
            while (leftTotalBet > this._Condition_A.ScoreInterval) leftTotalBet -= this._Condition_A.ScoreInterval;
            // reset...
            this._WinCollection_A.Reset();
            // assign TotalBet by leftTotalBet
            this._WinCollection_A.TotalBet = leftTotalBet;
            // disable winA
            this._Condition_A.WinBonusBeginUtcTime = Utils.zeroDateTime();

            // broadcast notification to all subDomain
            const winTime = new Date(spinData.UtcTicks);
            const msg = this.GenerateWinMessage(NotifyData.WinSpin.A, spinData, account, winTime, scores);
            BonusAgent.addNotifyData(BonusAgent.webSite(), NotifyData.WinSpin.A, msg, scores, spinData.MachineName, account);
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
        }
        return errMsg;
    }

    async VerifyWinB(urlTransferPoints, account, spinData)
    {
        let errMsg = null;
        try
        {
            if (!urlTransferPoints) throw new Error("WinSpinB got null urlTransferPoints");
            if (!account) throw new Error("WinSpinB got null account");
            if (!spinData) throw new Error("WinSpinCR got null spinData");
            if (!this._Condition_B) throw new Error("WinSpinB got null mConditionB");
            const scores = Math.floor(this.WinCollection_B.TotalBet / this._Condition_B.ScoreInterval);
            if (scores < this._Condition_B.MinPay) throw new Error("collected scores not enough");
            // skip if request timing is early than WinBonusBeginDT
            const spinUtcTime = new Date(spinData.UtcTicks);
            const ts = spinUtcTime - this._Condition_B.WinBonusBeginUtcTime;
            if (ts.TotalMilliseconds < 0) throw new Error("WinSpinB got before than WinBonusBeginDT");

            // win scores for B
            const url = urlTransferPoints;
            const objBonus = {
                agent: "WinLobby",
                userid: account,
                points: scores,
                award: false,
                reason: Config.UniBonus
            };
            const response = await axios.post(url, objBonus);
            if (!response) throw new Error(`API ${url} returns null`);
            // make record here...
            DBAgent.addBonusRecord(BonusRule.WIN_TYPE.WinB,
                this._WinCollection_B.TotalBet, this._Condition_B.ScoreInterval,
                objBonus, urlTransferPoints, response);
            // cal left totalBet
            let leftTotalBet = this._WinCollection_B.TotalBet;
            while (leftTotalBet > this._Condition_B.ScoreInterval) leftTotalBet -= this._Condition_B.ScoreInterval;
            // reset...
            this._WinCollection_B.Reset();
            // assign TotalBet by leftTotalBet
            this._WinCollection_B.TotalBet = leftTotalBet;
            // disable winB
            this._Condition_B.WinBonusBeginUtcTime = Utils.zeroDateTime();

            // broadcast notification to all subDomain
            const winTime = new Date(spinData.UtcTicks);
            const msg = this.GenerateWinMessage(NotifyData.WinSpin.B, spinData, account, winTime, scores);
            BonusAgent.AddNotifyData(BonusAgent.webSite(), NotifyData.WinSpin.B, msg, scores, spinData.MachineName, account);
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
        }
        return errMsg;
    }

    async VerifyWinCR(urlTransferPoints, account, spinData)
    {
        let errMsg = null;
        try
        {
            if (!urlTransferPoints) throw new Error("WinSpinCR got null urlTransferPoints");
            if (!account) throw new Error("WinSpinCR got null account");
            if (!spinData) throw new Error("WinSpinCR got null spinData");
            if (!this._Condition_CR) throw new Error("WinSpinCR got null mConditionCR");
            const scores = Math.floor(this._WinCollection_CR.TotalBet / this._Condition_CR.ScoreInterval);
            if (scores < this._Condition_CR.MinPay) throw new Error("collected scores not enough");
            // skip if request timing is early than WinBonusBeginDT
            const spinUtcTime = new Date(spinData.UtcTicks);
            const ts = spinUtcTime - this._Condition_CR.WinBonusBeginUtcTime;
            if (ts.TotalMilliseconds < 0) throw new Error("WinSpinCR got before than WinBonusBeginDT");

            // win scores for cr
            const url = urlTransferPoints;
            const objBonus = {
                agent: "WinLobby",
                userid: account,
                points: scores,
                award: false,
                reason: Config.UniBonus
            };
            const response = await axios.post(url, objBonus);
            if (!response) throw new Error(`API ${url} returns null`);
            // make record here...
            DBAgent.addBonusRecord(BonusRule.WIN_TYPE.WinCR,
                this._WinCollection_CR.TotalBet, this._Condition_CR.ScoreInterval,
                objBonus, urlTransferPoints, response);
            // cal left totalBet
            let leftTotalBet = this._WinCollection_CR.TotalBet;
            while (leftTotalBet > this._Condition_CR.ScoreInterval) leftTotalBet -= this._Condition_CR.ScoreInterval;
            // reset...
            this._WinCollection_CR.Reset();
            // assign TotalBet by leftTotalBet
            this._WinCollection_CR.TotalBet = leftTotalBet;
            // disable winCR
            this._Condition_CR.WinBonusBeginUtcTime = Utils.zeroDateTime();

            // broadcast notification to all subDomain
            const winTime = new Date(spinData.UtcTicks);
            const msg = this.GenerateWinMessage(NotifyData.WinSpin.CR, spinData, account, winTime.ToLocalTime(), scores);
            BonusAgent.AddNotifyData(BonusAgent.webSite(), NotifyData.WinSpin.CR, msg, scores, spinData.MachineName, account);
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
        }
        return errMsg;
    }

    RandomWin(winType)
    {
        throw new Error("RandomWin in base class");
    }

    GenerateWinMessage(winSpin, spinData, account, winTime, scores)
    {
        const congratulations = ["恭喜", "哇勒", "天啊", "狂賀", "大賀喜", "了不起", "殺很大", "出奇蹟啦", "給你放煙火" ];
        const index = Math.floor(Math.random() * congratulations.length);
        const congrats = congratulations[index];
        let bonusKind = "彩金";
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

        if (account && account.length > 0) {
            return `${congrats}!!!${spinData.WebSite}的玩家${account}於${winTime}贏得${BonusAgent.webSite()}的${bonusKind}共${scores}分`;
        }
        else {
            return `${congrats}!!!${spinData.WebSite}的遊戲${spinData.MachineName}於${winTime}贏得${BonusAgent.webSite()}的${bonusKind}共${scores}分`;
        }
    }
}

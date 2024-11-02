
import Log from '../utils/log.js';

class CSpinData
{
    TotalBet;
    TotalWin;
    WinA;
    WinB;

    constructor()
    {
        this.TotalBet = 0;
        this.TotalWin = 0;
        this.WinA = 0;
        this.WinB = 0;
    }

    static FromJson(obj)
    {
        const spinData = new CSpinData();
        spinData.TotalBet = obj.TotalBet ? parseInt(obj.TotalBet) : 0;
        spinData.TotalWin = obj.TotalWin ? parseInt(obj.TotalWin) : 0;
        spinData.WinA = obj.WinA ? parseInt(obj.WinA) : 0;
        spinData.WinB = obj.WinB ? parseInt(obj.WinB) : 0;
        return spinData;
    }
}

export default class CSpinCollection
{
    static BETWIN_TYPE =
    {
        Others: 0,
        Slot: 1,
        Cr: 2,
        ChinaCr: 3
    }

    UserAccount;
    BonusType;
    SpinData;
    SyncTime;
    AbleToRushBonus;

    constructor()
    {
        this.UserAccount = null;
        this.BonusType = CSpinCollection.BETWIN_TYPE.Others;
        this.SpinData = null;
        this.SyncTime = 0;
        this.AbleToRushBonus = false;
    }

    static FromJson(obj)
    {
        let spinCollection = null;
        try
        {
            if (!obj || !obj.SpinData) throw new Error("null SpinData");
            const strContent = obj.SpinData;
            if (strContent == null) throw new Error("null strContent");
            const objSpinData = JSON.parse(strContent);

            spinCollection = new CSpinCollection();
            spinCollection.UserAccount = obj.UserAccount ? obj.UserAccount : "";
            spinCollection.BonusType = obj.BonusType ? obj.BonusType : CSpinCollection.BETWIN_TYPE.Others;
            spinCollection.SpinData = CSpinData.FromJson(objSpinData);
            spinCollection.SyncTime = obj.SyncTime ? obj.SyncTime : 0;
            spinCollection.AbleToRushBonus = obj.AbleToRushBonus ? true : false;
        }
        catch(err)
        {
            Log.storeMsg(err.message ? err.message : err);
        }
        return spinCollection;
    }
}
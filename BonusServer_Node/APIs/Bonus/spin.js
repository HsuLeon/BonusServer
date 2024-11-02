
import BonusAgent from "../../services/bonusAgent.js";
import CollectData from "../../models/collectData.js";
import CSpinCollection from "../../models/spinCollection.js";
import SpinData from "../../services/ruleTrigger/spinData.js";
import {BonusRule} from "../../services/ruleTrigger/bonusRule.js";

export default async function spin(machineName, scoreScale, spinCollection)
{
    let msg = null;
    try {
        const userAccount = spinCollection.UserAccount;
        const bonusType = spinCollection.BonusType;
        const spinData = spinCollection.SpinData;
        const totalBet = spinData.TotalBet * scoreScale;
        const totalWin = spinData.TotalWin * scoreScale;
        const winA = spinData.WinA;
        const winB = spinData.WinB;
        
        if (totalBet > 0 ||
            totalWin > 0 ||
            winA > 0 ||
            winB > 0)
        {
            const data = new CollectData(
                totalBet,
                totalWin,
                winA,
                winB,
                bonusType
            );
            BonusAgent.collectBonus(data);
        }
        // if able to rush for WinA/WinB
        if (spinCollection.AbleToRushBonus)
        {
            switch (bonusType)
            {
                case CSpinCollection.BETWIN_TYPE.Slot:
                    if (winA > 0)
                    {
                        const data = new SpinData();
                        data.Domain = BonusAgent.BonusServerDomain;
                        data.Port = BonusAgent.BonusServerPort;
                        data.WebSite = BonusAgent.WebSite;
                        data.MachineName = machineName;
                        data.UserAccount = userAccount;
                        data.Win = winA;
                        data.UtcTicks = Date.now();
                        const obj = {
                            account: userAccount,
                            urlTransferPoints: BonusAgent.apiTransferPoints(),
                            spinData: JSON.stringify(data)
                        };
                        // try upper server first
                        const errMsg = await BonusAgent.rushUpperWinSpinA(obj);
                        // if upper server doesn't accept request, check self winA
                        if (errMsg) BonusAgent.pushToWinSpinA(obj);
                    }
                    else if (winB > 0)
                    {
                        const ruleTrigger = BonusAgent.ruleTrigger();
                        if (ruleTrigger && ruleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinB))
                        {
                            const data = new SpinData();
                            data.Domain = BonusAgent.BonusServerDomain;
                            data.Port = BonusAgent.BonusServerPort;
                            data.WebSite = BonusAgent.WebSite;
                            data.MachineName = machineName;
                            data.UserAccount = userAccount;
                            data.Win = winB;
                            data.UtcTicks = Date.now();
                            const obj = {
                                account: userAccount,
                                urlTransferPoints: BonusAgent.apiTransferPoints(),
                                spinData: JSON.stringify(data)
                            };

                            BonusAgent.pushToWinSpinB(obj);
                        }
                    }
                    break;
                case CSpinCollection.BETWIN_TYPE.Cr:
                case CSpinCollection.BETWIN_TYPE.ChinaCr:
                    if (winB > 0)
                    {
                        const data = new SpinData();
                        data.Domain = BonusAgent.BonusServerDomain;
                        data.Port = BonusAgent.BonusServerPort;
                        data.WebSite = BonusAgent.WebSite;
                        data.MachineName = machineName;
                        data.UserAccount = userAccount;
                        data.Win = winB;
                        data.UtcTicks = DateTime.UtcNow.Ticks;
                        const obj = {
                            account: userAccount,
                            urlTransferPoints: BonusAgent.apiTransferPoints(),
                            spinData: JSON.stringify(data)
                        };
                        // try upper server first
                        const errMsg = await BonusAgent.rushUpperWinSpinCR(obj);
                        // if upper server doesn't accept request, check self winCR
                        if (errMsg) BonusAgent.pushToWinSpinCR(obj);
                    }
                    break;
            }
        }
    }
    catch(err) {
        msg = err.message ? err.message : err;
    }
    return msg;
}
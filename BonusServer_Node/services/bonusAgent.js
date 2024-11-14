
import axios from "axios";
import fs from "fs";
import RabbitMQManager from "../utils/rabbitMqClass.js";
import Log from "../utils/log.js";
import ConfigSetting from "../configSetting.js";
import {BonusRule} from "./ruleTrigger/bonusRule.js";
import BonusRule_0 from "./ruleTrigger/bonusRule_0.js";
import BonusRule_1 from "./ruleTrigger/bonusRule_1.js";
import NotifyData from "../models/notifyData.js";
import CSpinCollection from "../models/spinCollection.js";

export default class BonusAgent
{
    static #mRecordsPath = "C:/SignalR/BonusServer/Records.json";
    static #mSubDomains = [];
    static #mTokenOfDomains = {};
    static #mBonusRecords = {};
    static #mJoinWinToken = null;

    static #mRuleTrigger = null;
    static #mLaunchTime = new Date();
    static #mWebSite = null;
    static #mBonusServerDomain = null;
    static #mBonusServerPort = 0;
    static #mUpperDomain = null;
    static #mAPITransferPoints = null;
    static #mCollectSubScale = 1;

    constructor()
    {
    }

    static initParams(webSite, bonusServerDomain, bonusServerPort, upperDomain, apiTransferPoints, collectSubScale)
    {
        BonusAgent.#mLaunchTime = new Date();
        BonusAgent.#mWebSite = webSite;
        BonusAgent.#mBonusServerDomain = bonusServerDomain;
        BonusAgent.#mBonusServerPort = bonusServerPort;
        BonusAgent.#mUpperDomain = upperDomain;
        BonusAgent.#mAPITransferPoints = apiTransferPoints;
        BonusAgent.#mCollectSubScale = collectSubScale;
    }

    static initQueues(queueServer, userName, password)
    {
        // 獲取 RabbitMQManager 實例
        const rabbitMQManager = RabbitMQManager.getInstance();
        rabbitMQManager.createConnection(queueServer, userName, password)
        .then(async function() {
            const funcs = {
                "SpinA": BonusAgent.onChannelSpinA,
                "SpinB": BonusAgent.onChannelSpinB,
                "SpinCR": BonusAgent.onChannelSpinCR,
            }
            // 為每個隊列設置回調
            await Promise.all(
                Object.keys(funcs).map((queue) =>
                    rabbitMQManager.setQueueCallback(queue, (message) => {
                        try {
                            const func = funcs[queue];
                            const obj = JSON.parse(message);
                            func(obj);
                        }
                        catch(err) {
                            const errMsg = err.message ? err.message : err;
                            console.log(`queue got ${errMsg}`);
                        }
                    })
                )
            );
            // export records intervally
            setInterval(function() {
                BonusAgent.exportRecords();
            }, 60*1000);
            console.log("All queues are set up successfully.");
        })
        .catch(function(err) {
            console.error("RabbitMQ setup error:", err);
        });
    }

    static initTriggers(rule, conditionWinA, conditionWinB, conditionWinCR)
    {
        let ruleTrigger = null;
        switch (rule)
        {
            case ConfigSetting.BETWIN_RULE.Rule_0:
                ruleTrigger = new BonusRule_0();
                break;
            case ConfigSetting.BETWIN_RULE.Rule_1:
                ruleTrigger = new BonusRule_1();
                break;
            default: throw new Error(`unknown rule: ${rule}`);
        }
        ruleTrigger.ParseSettings(BonusRule.WIN_TYPE.WinA, conditionWinA);
        ruleTrigger.ParseSettings(BonusRule.WIN_TYPE.WinB, conditionWinB);
        ruleTrigger.ParseSettings(BonusRule.WIN_TYPE.WinCR, conditionWinCR);
        // assign to BonusAgent.#mRuleTrigger
        BonusAgent.#mRuleTrigger = ruleTrigger;
    }

    static restoreRecords(jsonPath)
    {
        // assign #mRecordsPath
        BonusAgent.#mRecordsPath = jsonPath;
        BonusAgent.importRecords(BonusAgent.#mRecordsPath);
    }

    static async onChannelSpinA(obj)
    {
        try
        {
            const ruleTrigger = BonusAgent.ruleTrigger();
            if (!ruleTrigger) throw new Exception("null ruleTrigger");
            const errMsg = await ruleTrigger.TryWinSpinA(obj);
            if (errMsg) throw new Error(errMsg);
        }
        catch (err)
        {
            // if obj is from sub domain, reply to its domain for sub domain handles it.
            if (obj.hasOwnProperty("from"))
            {
                const urlFrom = obj.from;
                const data = obj.data;
                if (urlFrom && data)
                {
                    const token = BonusAgent.getTokenOfDomain(urlFrom);
                    const url = `${urlFrom}/bonus/upper/replyToWinA`;
                    const config = {
                        headers: {
                            "Authorization": `Bearer ${token}`
                        },
                    }
                    axios.post(url, data, config);
                }
            }
            else
            {
                // discard...
            }
        }
    }

    static async onChannelSpinB(obj)
    {
        try
        {
            const ruleTrigger = BonusAgent.ruleTrigger();
            if (!ruleTrigger) throw new Exception("null ruleTrigger");
            const errMsg = await ruleTrigger.TryWinSpinB(obj);
            if (errMsg) throw new Error(errMsg);
        }
        catch (err)
        {
            // discard...
        }
    }

    static async onChannelSpinCR(obj)
    {
        try
        {
            const ruleTrigger = BonusAgent.ruleTrigger();
            if (!ruleTrigger) throw new Exception("null ruleTrigger");
            const errMsg = await ruleTrigger.TryWinSpinCR(obj);
            if (errMsg) throw new Error(errMsg);
        }
        catch (err)
        {
            // if obj is from sub domain, reply to its domain for sub domain handles it.
            if (obj.hasOwnProperty("from"))
            {
                const urlFrom = obj.from;
                const data = obj.data;
                if (urlFrom && data)
                {
                    const token = BonusAgent.getTokenOfDomain(urlFrom);
                    const url = `${urlFrom}/bonus/upper/replyToWinCR`;
                    const config = {
                        headers: {
                            "Authorization": `Bearer ${token}`
                        },
                    }
                    axios.post(url, data, config);
                }
            }
            else
            {
                // discard...
            }
        }
    }

    static addSubDomain(domain, password)
    {
        let errMsg = null;
        try
        {
            if (domain == null || domain.Length == 0) throw new Error("null domain");
            if (BonusAgent.isSubDomain(domain)) return null;
            // check allowed sun domain list
            let bAllowed = false;
            const subDomains = ConfigSetting.SubDomains();
            for (let info in subDomains)
            {
                const domainAllowed = info.Domain;
                const domainPassword = info.Password;

                if (domainAllowed.indexOf(domain) >= 0 ||
                    domain.indexOf(domainAllowed) >= 0)
                {
                    if (domainPassword != password) throw new Error("invalid password");
                    bAllowed = true;
                    break;
                }
            }
            if (bAllowed == false) throw new Error("unknown domain");
            BonusAgent.#mSubDomains.push(domain);
        }
        catch(err)
        {
            errMsg = err.message ? err.message : err;
        }
        return errMsg;
    }
    
    static isUpperDomain(domain)
    {
        const upperDomain = BonusAgent.#mUpperDomain;
        if (upperDomain != null &&
            upperDomain.indexOf(domain) >= 0)
            return true;
        return false;
    }

    static isSubDomain(domain)
    {
        const subDomains = BonusAgent.#mSubDomains;
        for (let item in subDomains)
        {
            if (item.indexOf(domain) >= 0) return true;
        }
        return false;
    }

    static addNotifyData(bonusWebSite, winSpinType, message, scores, machineName, userAccout)
    {
        if (winSpinType == NotifyData.WinSpin.None ||
            bonusWebSite == null ||
            message == null) return;

        try
        {
            const bonusRecords = BonusAgent.#mBonusRecords;
            if (bonusRecords.hasOwnProperty(bonusWebSite) == false) bonusRecords[bonusWebSite] = new BonusRecords();
            const records = bonusRecords[bonusWebSite];
            let notifyWinList = null;
            switch (winSpinType)
            {
                case NotifyData.WinSpin.A:
                    {
                        // assign notify WinAList
                        notifyWinList = records.WinAList;
                    }
                    break;
                case NotifyData.WinSpin.B:
                    {
                        // assign notify WinBList
                        notifyWinList = records.WinBList;
                    }
                    break;
                case NotifyData.WinSpin.CR:
                    {
                        // assign notify WinCRList
                        notifyWinList = records.WinCRList;
                    }
                    break;
                default:
                    throw new Error(`unknown winSpinType: ${winSpinType.toString()}`);
            }

            const notifyData = {
                WinSpinType: winSpinType,
                WebSite: bonusWebSite,
                Message: message,
                Scores: scores,
                MachineName: machineName != null ? machineName : "",
                UserAccount: userAccout != null ? userAccout : "神秘爺",
                CreateTime: Date.now
            };
            const content = JSON.stringify(notifyData);
            const objNotifyData = JSON.parse(content);
            const subDomains = BonusAgent.#mSubDomains;
            for (let domain in subDomains)
            {
                const token = BonusAgent.getTokenOfDomain(domain);
                // skip if no token
                if (!token) continue;
                const url = `${domain}/system/notify`;
                const config = {
                    headers: {
                        "Authorization": `Bearer ${token}`
                    },
                };
                axios.post(url, objNotifyData, config);
            }
            // add to tail of list
            notifyWinList.push(notifyData);
            // keep last 10 records, remove first element if needed
            if (notifyWinList.length > 10) notifyWinList.shift();
            BonusAgent.exportRecords();
        }
        catch(err)
        {
            const errMsg = err.message ? err.message : err;
            Log.storeMsg(`AddNotifyData got ${errMsg}`);
        }
    }

    static getTokenOfDomain(domain)
    {
        if (BonusAgent.#mTokenOfDomains.hasOwnProperty(domain))
            return BonusAgent.#mTokenOfDomains[domain];
        else
            return null;
    }

    static refreshToken(domain, token)
    {
        if (domain == null ||  token == null) return;
        if (BonusAgent.isSubDomain(domain) == false) return;
        mTokenOfDomains[domain] = token;
    }

    static async collectBonus(collectData)
    {
        try
        {
            const bonusType = collectData.BonusType;
            switch (bonusType)
            {
                case CSpinCollection.BETWIN_TYPE.Slot:
                    BonusAgent.#mRuleTrigger.Collect(BonusRule.WIN_TYPE.WinA, collectData);
                    BonusAgent.#mRuleTrigger.Collect(BonusRule.WIN_TYPE.WinB, collectData);
                    break;
                case CSpinCollection.BETWIN_TYPE.Cr:
                case CSpinCollection.BETWIN_TYPE.ChinaCr:
                    BonusAgent.#mRuleTrigger.Collect(BonusRule.WIN_TYPE.WinCR, collectData);
                    break;
                default: throw new Error(`invalid bonusType: ${bonusType}`);
            }
            // notify upper bonus server to do collecting
            if (BonusAgent.#mUpperDomain)
            {
                // to verify machine token
                const url = `${BonusAgent.#mUpperDomain}/bonus/lower/collect`;
                const data = collectData;
                const config = {
                    headers: {
                        "Authorization": `Bearer ${BonusAgent.#mJoinWinToken}`
                    },
                };
                const result = await axios.post(url, data, config);
                if (result.status != 200)
                {
                    throw new Error(`Failed to collect bonus: ${result.statusText}`);
                } 
            }
        }
        catch (error)
        {
            Log.storeMsg(`CollectBonus got ${error.message}`);
        }
    }

    static async rushUpperWinSpinA(objData)
    {
        let errMsg = null;
        try
        {
            if (objData == null) throw new Error("null objData");
            if (!BonusAgent.#mUpperDomain) throw new Error("no upper domain");

            const obj =  {
                from: `${BonusAgent.#mBonusServerDomain}:${BonusAgent.#mBonusServerPort}`,
                data: objData
            }
            // fire API to check upper server
            const url = `${BonusAgent.#mUpperDomain}/bonus/lower/rushToWinA`;
            const data = obj;
            const config = {
                headers: {
                    "Authorization": `Bearer ${BonusAgent.#mJoinWinToken}`
                },
            };
            const result = await axios.post(url, data, config);
            if (result.status != 200)
            {
                throw new Error(`Failed to rushUpperWinSpinA: ${result.statusText}`);
            } 
            const objRes = result.data;
            const status = objRes["status"].toLowerCase();
            if (status != "success")
            {
                const error = objRes["error"];
                throw new Error(error ? error : "unknow error");
            }
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
            //Log.storeMsg(`rushUpperWinSpinA got ${errMsg}`);
        }
        return errMsg;
    }
    
    static async pushToWinSpinA(obj)
    {
        let errMsg = null;
        try
        {
            const ruleTrigger = BonusAgent.ruleTrigger();
            if (ruleTrigger && ruleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinA) == false)
            {
                throw new Error("winA not enabled");
            }

            const message = JSON.stringify(obj);
            const error = await RabbitMQManager.getInstance().publish("SpinA", message);
            if (error) throw new Error(error);
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
            Log.storeMsg(`pushToWinSpinA got ${errMsg}`);
        }
        return errMsg;
    }

    static async pushToWinSpinB(obj)
    {
        let errMsg = null;
        try
        {
            const ruleTrigger = BonusAgent.ruleTrigger();
            if (ruleTrigger && ruleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinB) == false)
            {
                throw new Error("winB not enabled");
            }
            const message = JSON.stringify(obj);
            const error = await RabbitMQManager.getInstance().publish("SpinB", message);
            if (error) throw new Error(error);
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
            Log.storeMsg(`pushToWinSpinB got ${errMsg}`);
        }
        return errMsg;
    }

    static async rushUpperWinSpinCR(objData)
    {
        let errMsg = null;
        try
        {
            if (objData == null) throw new Error("null objData");
            if (!BonusAgent.#mUpperDomain) throw new Error("no upper domain");

            const obj = {
                from: `${BonusAgent.#mBonusServerDomain}:${BonusAgent.#mBonusServerPort}`,
                data: objData
            }
            // fire API to check upper server
            const url = `${BonusAgent.UpperDomain}/bonus/lower/rushToWinCR`;
            const data = obj;
            const config = {
                headers: {
                    "Authorization": `Bearer ${BonusAgent.#mJoinWinToken}`
                },
            };
            const result = await axios.post(url, data, config);
            if (result.status != 200)
            {
                throw new Error(`Failed to rushUpperWinSpinCR: ${result.statusText}`);
            } 
            const objRes = result.data;
            const status = objRes["status"].toLowerCase();
            if (status != "success")
            {
                const error = objRes["error"];
                throw new Error(error ? error : "unknow error");
            }
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
            //Log.storeMsg(`rushUpperWinSpinCR got ${errMsg}`);
        }
        return errMsg;
    }

    static async pushToWinSpinCR(obj)
    {
        let errMsg = null;
        try
        {
            const ruleTrigger = BonusAgent.ruleTrigger();
            if (ruleTrigger && ruleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinCR) == false)
            {
                throw new Error("winCR not enabled");
            }
            const message = JSON.stringify(obj);
            const error = await RabbitMQManager.getInstance().publish("SpinB", message);
            if (error) throw new Error(error);
        }
        catch (err)
        {
            errMsg = err.message ? err.message : err;
            Log.storeMsg(`pushToWinSpinCR got ${errMsg}`);
        }
        return errMsg;
    }
    
    static getNotify()
    {
        const dicNotify = {};
        try
        {
            const bonusRecords = BonusAgent.#mBonusRecords;
            const keys = Object.keys(bonusRecords);
            for (let i = 0 ; i < keys.length ; i++)
            {
                const key = keys[i];
                const records = bonusRecords[key];
                const content = JSON.stringify(records);
                const obj = JSON.parse(content);
                dicNotify[key] = obj;
            }
        }
        catch(error)
        {
            Log.storeMsg(`GetNotify got ${error.message}`);
        }
        return dicNotify;
    }
    
    static exportRecords()
    {
        try
        {
            const bonusRecords = JSON.stringify(BonusAgent.#mBonusRecords);
            const ruleTrigger = BonusAgent.#mRuleTrigger;
            const collectedA = JSON.stringify(ruleTrigger.WinCollection_A());
            const collectedB = JSON.stringify(ruleTrigger.WinCollection_B());
            const collectedCR = JSON.stringify(ruleTrigger.WinCollection_CR());
            // export cache records
            const obj = {
                bonusRecords: JSON.parse(bonusRecords),
                collectedA: JSON.parse(collectedA),
                collectedB: JSON.parse(collectedB),
                collectedCR: JSON.parse(collectedCR),
            }
            const content = JSON.stringify(obj, null, 4);
            fs.writeFileSync(BonusAgent.#mRecordsPath, content);
        }
        catch (error)
        {
            Log.storeMsg(`exportRecords() got ${error.message}`);
        }
    }

    static async importRecords(jsonPath)
    {
        try
        {
            const content = fs.readFileSync(jsonPath, 'utf8');
            const obj = JSON.parse(content);

            if (obj.hasOwnProperty("bonusRecords"))
            {
                const bonusRecords = JSON.stringify(obj["bonusRecords"]);
                BonusAgent.#mBonusRecords = JSON.parse(bonusRecords);
                console.log(JSON.stringify(BonusAgent.#mBonusRecords));
            }

            if (obj.hasOwnProperty("collectedA"))
            {
                const strCollected = JSON.stringify(obj["collectedA"]);
                const collected = JSON.parse(strCollected);
                if (collected.hasOwnProperty("TotalBet") &&
                    collected.hasOwnProperty("TotalWin") &&
                    collected.hasOwnProperty("WinA") &&
                    collected.hasOwnProperty("WinB") &&
                    collected.hasOwnProperty("BonusType"))
                {
                    BonusAgent.ruleTrigger().SetWinCollection_A(collected.TotalBet, collected.TotalWin, collected.WinA, collected.WinB, collected.BonusType);
                }
            }

            if (obj.hasOwnProperty("collectedB"))
            {
                const strCollected = JSON.stringify(obj["collectedB"]);
                const collected = JSON.parse(strCollected);
                if (collected.hasOwnProperty("TotalBet") &&
                    collected.hasOwnProperty("TotalWin") &&
                    collected.hasOwnProperty("WinA") &&
                    collected.hasOwnProperty("WinB") &&
                    collected.hasOwnProperty("BonusType"))
                {
                    BonusAgent.ruleTrigger().SetWinCollection_B(collected.TotalBet, collected.TotalWin, collected.WinA, collected.WinB, collected.BonusType);
                }
            }

            if (obj.hasOwnProperty("collectedCR"))
            {
                const strCollected = JSON.stringify(obj["collectedCR"]);
                const collected = JSON.parse(strCollected);
                if (collected.hasOwnProperty("TotalBet") &&
                    collected.hasOwnProperty("TotalWin") &&
                    collected.hasOwnProperty("WinA") &&
                    collected.hasOwnProperty("WinB") &&
                    collected.hasOwnProperty("BonusType"))
                {
                    BonusAgent.ruleTrigger().SetWinCollection_CR(collected.TotalBet, collected.TotalWin, collected.WinA, collected.WinB, collected.BonusType);
                }
            }
        }
        catch(error)
        {
            Log.storeMsg(`importRecords() got ${error.message}`);
        }
    }

    static async JoinWin()
    {
        if (BonusAgent.#mJoinWinToken ||
            !BonusAgent.#mUpperDomain || BonusAgent.#mUpperDomain.length == 0)
            return;
        try
        {
            const bonusServerDomain = ConfigSetting.BonusServerDomain();
            const bonusServerPassword = ConfigSetting.BonusServerPassword();

            if (!bonusServerDomain) throw new Error("configSetting.BonusServerDomain is null");
            if (!bonusServerPassword) throw new Error("configSetting.BonusServerPassword is null");

            const joinWinData = {
                BonusServerDomain: configSetting.BonusServerDomain,
                BonusServerPort: configSetting.BonusServerPort,
                Password: configSetting.BonusServerPassword
            };

            const url = `${BonusAgent.#mUpperDomain}/bonus/lower/joinWin`;
            const response = await axios.post(url, joinWinData);
            if (response == null) throw new Error("joinWin failed");
            BonusAgent.#mJoinWinToken = response;
        }
        catch(err)
        {
            const errMsg = err.message ? err.message : err;
            Log.storeMsg(`JoinWin got ${errMsg}`);
            BonusAgent.#mJoinWinToken = null;
        }
    }
    
    static launchTime()
    {
        return BonusAgent.#mLaunchTime;
    }

    static webSite()
    {
        return BonusAgent.#mWebSite;
    }

    static bonusServerDomain()
    {
        return BonusAgent.#mBonusServerDomain;
    }

    static bonusServerPort()
    {
        return BonusAgent.#mBonusServerPort;
    }

    static upperDomain()
    {
        return BonusAgent.#mUpperDomain;
    }

    static apiTransferPoints()
    {
        return BonusAgent.#mAPITransferPoints;
    }

    static collectSubScale()
    {
        return BonusAgent.#mCollectSubScale;
    }

    static ruleTrigger()
    {
        return BonusAgent.#mRuleTrigger;
    }
}
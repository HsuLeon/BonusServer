
import fs from "fs";
import Log from "./utils/log.js";

const mConfigJson = {};
let mConfigFileName = null;

export default class ConfigSetting
{
    static BETWIN_RULE = {
        Rule_0: 0,
        Rule_1: 1
    }

    constructor()
    {
    }

    // 載入配置文件
    static async loadConfig(filename)
    {
        try {
            const data = fs.readFileSync(filename, 'utf8');
            const config = JSON.parse(data);
            
            mConfigJson.Protocol = config.hasOwnProperty('Protocol') ? config.Protocol : 'http';
            mConfigJson.ExportLog = config.ExportLog ? true : false;
            mConfigJson.LanguageType = config.hasOwnProperty('LanguageType') ? parseInt(config.LanguageType) : 0;
            mConfigJson.databases = config.hasOwnProperty('databases') ? config.databases : [];
            mConfigJson.BonusServerDomain = config.hasOwnProperty('BonusServerDomain') ? config.BonusServerDomain : '127.0.0.1';
            mConfigJson.BonusServerPort = config.hasOwnProperty('BonusServerPort') ? parseInt(config.BonusServerPort) : 80;
            mConfigJson.UpperDomain = config.UpperDomain;
            mConfigJson.SubDomains = [];
            if (config.SubDomains) {
                const subDomains = config.SubDomains;
                for (let i = 0; i < subDomains.length; i++)
                {
                    const obj = subDomains[i];
                    if (!obj) continue;

                    const domain = obj.domain;
                    const password = obj.password;

                    mConfigJson.SubDomains.push({
                        Domain: domain ? domain : "",
                        Password: password ? password : "" 
                    });
                }
            }
            mConfigJson.TransferScores = config.hasOwnProperty('TransferScores') ? config.TransferScores : 'http://localhost/APIs/TransferPoints.ashx';
            mConfigJson.CollectSubScale = config.hasOwnProperty('CollectSubScale') ? parseFloat(config.CollectSubScale) : 1.0;
            mConfigJson.RabbitMQServer = config.hasOwnProperty('RabbitMQServer') ? config.RabbitMQServer : '127.0.0.1';
            mConfigJson.RabbitMQUserName = config.hasOwnProperty('RabbitMQUserName') ? config.RabbitMQUserName : 'guest';
            mConfigJson.RabbitMQPassword = config.hasOwnProperty('RabbitMQPassword') ? config.RabbitMQPassword : 'guest';
            mConfigJson.BonusServerPassword = config.hasOwnProperty('BonusServerPassword') ? config.BonusServerPassword : 'localhost';
            if (config.hasOwnProperty('BetWinRule')) {
                const ruleId = parseInt(config.BetWinRule)
                switch (ruleId) {
                    case 0:
                        mConfigJson.BetWinRule = ConfigSetting.BETWIN_RULE.Rule_0;
                        break;
                    case 1:
                    default:
                        mConfigJson.BetWinRule = ConfigSetting.BETWIN_RULE.Rule_1;
                        break;
                }
            }
            else {
                mConfigJson.BetWinRule = ConfigSetting.BETWIN_RULE.Rule_1;
            }
            mConfigJson.WebSite = config.hasOwnProperty('WebSite') ? config.WebSite : '彩金測試場';
            try {
                if (config.hasOwnProperty('ConditionWinA') == false) throw new Error("no settings for ConditionWinA");
                // confirm settings
                const obj = JSON.parse(config.ConditionWinA);
                mConfigJson.ConditionWinA = JSON.stringify(obj);
            }
            catch(err) {
                switch(mConfigJson.BetWinRule) {
                    case ConfigSetting.BETWIN_RULE.Rule_0:                    
                        mConfigJson.ConditionWinA = "{\"ScoreInterval\":3000.0,\"WinOverBet\":0.9,\"WinCount\":300,\"MinPay\":1}";
                        break;
                    case ConfigSetting.BETWIN_RULE.Rule_1:
                    default:
                        mConfigJson.ConditionWinA = "{\"ScoreInterval\":3000.0,\"WinRatio\":0.002,\"MinTotalBet\":0.0,\"MinPay\":1}";
                        break;
                }
            }
            try {
                if (config.hasOwnProperty('ConditionWinB') == false) throw new Error("no settings for ConditionWinB");
                // confirm settings
                const obj = JSON.parse(config.ConditionWinB);
                mConfigJson.ConditionWinB = JSON.stringify(obj);
            }
            catch(err) {
                switch(mConfigJson.BetWinRule) {
                    case ConfigSetting.BETWIN_RULE.Rule_0:                    
                        mConfigJson.ConditionWinB = "{\"ScoreInterval\":3000.0,\"WinOverBet\":0.7,\"WinCount\":100,\"MinPay\":1}";
                        break;
                    case ConfigSetting.BETWIN_RULE.Rule_1:
                    default:
                        mConfigJson.ConditionWinB = "{\"ScoreInterval\":3000.0,\"WinRatio\":0.006,\"MinTotalBet\":0.0,\"MinPay\":1}";
                        break;
                }
            }
            try {
                if (config.hasOwnProperty('ConditionWinCR') == false) throw new Error("no settings for ConditionWinCR");
                // confirm settings
                const obj = JSON.parse(config.ConditionWinCR);
                mConfigJson.ConditionWinCR = JSON.stringify(obj);
            }
            catch(err) {
                switch(mConfigJson.BetWinRule) {
                    case ConfigSetting.BETWIN_RULE.Rule_0:                    
                        mConfigJson.ConditionWinCR = "{\"ScoreInterval\":3000.0,\"WinOverBet\":0.9,\"WinCount\":300,\"MinPay\":1}";
                        break;
                    case ConfigSetting.BETWIN_RULE.Rule_1:
                    default:
                        mConfigJson.ConditionWinCR = "{\"ScoreInterval\":3000.0,\"WinRatio\":0.002,\"MinTotalBet\":0.0,\"MinPay\":1}";
                        break;
                }
            }
            mConfigJson.SSL_PATH = config.hasOwnProperty('SSL_PATH') ? config.SSL_PATH : null;

            // store filename
            mConfigFileName = filename;
            return mConfigJson;
        }
        catch (error) {
            Log.storeMsg(`Error loading config.bin: ${error.message}. Ensure the file exists and has the correct format.`);
        }
    };

    static async reload()
    {
        try {
            if (!mConfigFileName) throw new Error("invalid filename");
            await ConfigSetting.loadConfig(mConfigFileName);
        }
        catch(err) {
            const errMsg = err.message ? err.message : err;
            console.error(`reload() got ${errMsg}`);
        }
    }

    static async storeConfig(objParams)
    {
        try {
            if (!mConfigFileName) throw new Error("invalid filename");

            if (objParams.hasOwnProperty('BonusServerDomain')) mConfigJson.BonusServerDomain = objParams.BonusServerDomain;
            if (objParams.hasOwnProperty('BonusServerPort')) mConfigJson.BonusServerPort = objParams.BonusServerPort;
            if (objParams.hasOwnProperty('BonusServerPassword')) mConfigJson.BonusServerPassword = objParams.BonusServerPassword;
            if (objParams.hasOwnProperty('UpperDomain')) mConfigJson.UpperDomain = objParams.UpperDomain;
            if (objParams.hasOwnProperty('SubDomains')) mConfigJson.SubDomains = objParams.SubDomains;
            if (objParams.hasOwnProperty('APITransferPoints')) mConfigJson.APITransferPoints = objParams.APITransferPoints;
            if (objParams.hasOwnProperty('CollectSubScale')) mConfigJson.CollectSubScale = objParams.CollectSubScale;
            if (objParams.hasOwnProperty('RabbitMQServer')) mConfigJson.RabbitMQServer = objParams.RabbitMQServer;
            if (objParams.hasOwnProperty('RabbitMQUserName')) mConfigJson.RabbitMQUserName = objParams.RabbitMQUserName;
            if (objParams.hasOwnProperty('RabbitMQPassword')) mConfigJson.RabbitMQPassword = objParams.RabbitMQPassword;
            if (objParams.hasOwnProperty('ConditionWinA')) mConfigJson.ConditionWinA = objParams.ConditionWinA;
            if (objParams.hasOwnProperty('ConditionWinB')) mConfigJson.ConditionWinB = objParams.ConditionWinB;
            if (objParams.hasOwnProperty('ConditionWinCR')) mConfigJson.ConditionWinCR = objParams.ConditionWinCR;

            const content = JSON.stringify(mConfigJson, null, 4);
            fs.writeFileSync(mConfigFileName, content);
        }
        catch(err) {
            Log.storeMsg(`Error writing config.bin: ${err.message}.`);
        }
    }
    
    static databases()
    {
        return mConfigJson["databases"];
    }

    static DBHost()
    {
        try
        {
            const dbs = this.databases();
            if (!dbs ||
                dbs.length == 0)
            {
                throw new Error("null databases");
            }

            const obj = dbs[0];
            if (obj == null) throw new Error("null obj in databases[0]");
            const domain = obj.domain;
            if (domain == null) throw new Error("null domain");
            return domain;
        }
        catch(err)
        {
            Log.storeMsg(err.message ? err.message : err);
            return "127.0.0.1";
        }
    }

    static DBPort()
    {
        try
        {
            const dbs = this.databases();
            if (!dbs ||
                dbs.length == 0)
            {
                throw new Error("null databases");
            }

            const obj = dbs[0];
            if (obj == null) throw new Error("null obj in databases[0]");
            const port = obj.port;
            if (port == null) throw new Error("null port");
            return parseInt(port);
        }
        catch(err)
        {
            Log.storeMsg(err.message ? err.message : err);
            return 27017;
        }
    }

    static WebSite()
    {
        return mConfigJson["WebSite"];
    }

    static RabbitMQServer()
    {
        return mConfigJson["RabbitMQServer"];
    }
    
    static RabbitMQUserName()
    {
        return mConfigJson["RabbitMQUserName"];
    }

    static RabbitMQPassword()
    {
        return mConfigJson["RabbitMQPassword"];
    }
    
    static BonusServerDomain()
    {
        return mConfigJson["BonusServerDomain"]
    }
    
    static BonusServerPort()
    {
        return mConfigJson["BonusServerPort"];
    }
    
    static BonusServerPassword()
    {
        return mConfigJson["BonusServerPassword"];
    }
    
    static UpperDomain()
    {
        return mConfigJson["UpperDomain"];
    }
    
    static SubDomains()
    {
        const list = [];
        const subDomains = mConfigJson["SubDomains"];
        if (subDomains &&
            subDomains.length > 0)
        {
            for (let i = 0; i < subDomains.length; i++)
            {
                const obj = subDomains[i];
                if (!obj) continue;
                const domain = obj["domain"];
                const password = obj["password"];
                const info = {
                    Domain: domain ? domain : "",
                    Password: password ? password : ""
                };
                list.push(info);
            }
        }
        return list;
    }

    static AddSubDomain(domain)
    {

    }
    
    static APITransferPoints()
    {
        return mConfigJson["TransferScores"];
    }
    
    static CollectSubScale()
    {
        return mConfigJson["CollectSubScale"];
    }
    
    static BetWinRule()
    {
        return mConfigJson["BetWinRule"];
    }
    
    static ConditionWinA()
    {
        return mConfigJson["ConditionWinA"];
    }
    
    static ConditionWinB()
    {
        return mConfigJson["ConditionWinB"];
    }
    
    static ConditionWinCR()
    {
        return mConfigJson["ConditionWinCR"];
    }
}
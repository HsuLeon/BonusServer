import express from "express";
import { handleError } from "../utils/response.js";
import BonusAgent from "../services/bonusAgent.js";
import verifyToken from "../APIs/Bonus/verifyToken.js";

const router = express.Router();

router.get("/launchTime", async (req, res) => {
    /**
     * 啟動時間對象
     * @typedef {Object} LaunchTime
     * @property {string} launchTime - 啟動時間
     */

    /** @type {LaunchTime} */
    try {
        const { ip } = req;

        const objInfo = {
            launchTime: BonusAgent.launchTime(),
        };

        res.json(objInfo);
    }
    catch (err) {
        handleError(res, err);
    }
});

router.get("/notify", async (req, res) => {
    /**
     * 通知數據對象
     * @typedef {Object} NotifyData
     * @property {string} records - 通知記錄
     * @property {number} totalBet_A - 總投注金額A
     * @property {number} totalBet_B - 總投注金額B
     * @property {number} totalBet_CR - 總投注金額CR
     */

    try {
        const { ip } = req;

        const ruleTrigger = BonusAgent.ruleTrigger();
        if (!ruleTrigger) throw new Error("null RuleTrigger");

        const collectDataA = ruleTrigger.WinCollection_A();
        const collectDataB = ruleTrigger.WinCollection_B();
        const collectDataCR = ruleTrigger.WinCollection_CR();
        const conditionA = ruleTrigger.Condition_A();
        const conditionB = ruleTrigger.Condition_B();
        const conditionCR = ruleTrigger.Condition_CR();

        const dicNotifies = BonusAgent.getNotify();

        const objNotify = {
            records: JSON.stringify(dicNotifies),
            totalBet_A: collectDataA.TotalBet / conditionA.ScoreInterval,
            totalBet_B: collectDataB.TotalBet / conditionB.ScoreInterval,
            totalBet_CR: collectDataCR.TotalBet / conditionCR.ScoreInterval
        };

        res.json(objNotify);
    }
    catch (err) {
        handleError(res, err);
    }
});

router.post("/notify", async (req, res) => {
    try {
        const { ip } = req;
        const authHeader = req.headers['authorization'];
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            throw new Error('Token missing or incorrect format');
        }
        // Extract the token from the header
        const token = authHeader.split(' ')[1];  // Split the Bearer part out
        // Verify the token using the secret key
        const tokenInfo = await verifyToken(token);
        // 讀取 token 中的 claims
        const upperDomain = tokenInfo.upperDomain;
        if (upperDomain == null) throw new Error("null upperDomain");
        if (BonusAgent.isUpperDomain(upperDomain) == false) throw new Error(`invalid domain: ${upperDomain}`);

        BonusAgent.AddNotifyData(notifyData.WebSite, notifyData.WinSpinType, notifyData.Message, notifyData.Scores, notifyData.MachineName, notifyData.UserAccount);

        res.send("Ok");
    }
    catch (err) {
        handleError(res, err);
    }
});

export default router;

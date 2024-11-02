import express from "express";
import { handleError } from "../utils/response.js";
import ConfigSetting from "../configSetting.js";

const router = express.Router();

router.get("/settings", async (req, res) => {
    /**
     * 配置數據對象
     * @typedef {Object} ConfigData
     * @property {string} BonusServerDomain - 獎勵伺服器域名
     * @property {number} BonusServerPort - 獎勵伺服器端口
     * @property {string} BonusServerPassword - 獎勵伺服器密碼
     * @property {string} UrlUpperDomain - 上層域名
     * @property {string} SubDomains - 子域名
     * @property {number} APITransferPoints - API轉換點數
     * @property {number} CollectSubScale - 收集子域比例
     * @property {string} RabbitMQServer - RabbitMQ伺服器
     * @property {string} RabbitMQUserName - RabbitMQ使用者名稱
     * @property {string} RabbitMQPassword - RabbitMQ密碼
     * @property {number} ConditionWinA - 條件贏金額A
     * @property {number} ConditionWinB - 條件贏金額B
     * @property {number} ConditionWinCR - 條件贏金額CR
     */

    /** @type {ConfigData} */
    try {
        const { ip } = req;

        await ConfigSetting.reload();

        const config = {};
        config.BonusServerDomain = ConfigSetting.BonusServerDomain();
        config.BonusServerPort = ConfigSetting.BonusServerPort();
        config.BonusServerPassword = ConfigSetting.BonusServerPassword();
        config.UrlUpperDomain = ConfigSetting.UpperDomain();
        config.SubDomains = ConfigSetting.SubDomains();
        config.APITransferPoints = ConfigSetting.APITransferPoints();
        config.CollectSubScale = ConfigSetting.CollectSubScale();
        config.RabbitMQServer = ConfigSetting.RabbitMQServer();
        config.RabbitMQUserName = ConfigSetting.RabbitMQUserName();
        config.RabbitMQPassword = ConfigSetting.RabbitMQPassword();
        config.ConditionWinA = ConfigSetting.ConditionWinA();
        config.ConditionWinB = ConfigSetting.ConditionWinB();
        config.ConditionWinCR = ConfigSetting.ConditionWinCR();

        res.json(config);
    }
    catch (err) {
        handleError(res, err);
    }
});

router.put("/settings", async (req, res) => {
    try {
        const { ip } = req;
        const {
            BonusServerDomain,
            BonusServerPort,
            BonusServerPassword,
            UpperDomain,
            SubDomains,
            APITransferPoints,
            CollectSubScale,
            RabbitMQServer,
            RabbitMQUserName,
            RabbitMQPassword,
            ConditionWinA,
            ConditionWinB,
            ConditionWinCR
        } = req.body;
        
        ConfigSetting.storeConfig({
            BonusServerDomain,
            BonusServerPort,
            BonusServerPassword,
            UpperDomain,
            SubDomains,
            APITransferPoints,
            CollectSubScale,
            RabbitMQServer,
            RabbitMQUserName,
            RabbitMQPassword,
            ConditionWinA,
            ConditionWinB,
            ConditionWinCR
        });

        res.send("OK");
    }
    catch (err) {
        handleError(res, err);
    }
});

export default router;

import express from "express";
//import RabbitMQManager from "../utils/rabbitMqClass.js";
import { handleError } from "../utils/response.js";
import login from "../APIs/Bonus/login.js";
import spin from "../APIs/Bonus/spin.js";
import joinWin from "../APIs/Bonus/joinWin.js";
import verifyToken from "../APIs/Bonus/verifyToken.js";
import CSpinCollection from "../models/spinCollection.js";

const router = express.Router();

router.post("/login", async (req, res) => {
    /**
     * 登錄數據對象
     * 登入數據對象
     * @typedef {Object} LoginData
     * @property {string} [MachineName] - 機器名稱
     * @property {number} [ScoreScale] - 分數比例
     */

    /** @type {LoginData} */
    try {
        // // // 初始化 RabbitMQ 連接和通道
        // // await createRabbitMQSender("localhost", "WinLobby", "12345");
        // const rabbitMQManager = RabbitMQManager.getInstance();
        // await rabbitMQManager.publish("SpinB", "Hello Leon B");
        
        const {MachineName, ScoreScale} = req.body;
        const response = login(MachineName, ScoreScale);

        res.json(response);
    }
    catch (err) {
        handleError(res, err);
    }
});

router.post("/spin", async (req, res) => {
    /**
     * 旋轉數據對象
     * @typedef {Object} SpinData
     * @property {number} TotalBet - 總投注金額
     * @property {number} TotalWin - 總贏金額
     * @property {number} WinA - 贏金額A
     * @property {number} WinB - 贏金額B
     */

    /** @type {SpinData} */
    try {
        // // 初始化 RabbitMQ 連接和通道
        // await createRabbitMQSender("localhost", "WinLobby", "12345");
        // await publish('SpinB', 'Hello Leon B');
        
        const authHeader = req.headers['authorization'];
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            throw new Error('Token missing or incorrect format');
        }
        // Extract the token from the header
        const token = authHeader.split(' ')[1];  // Split the Bearer part out
        // Verify the token
        const tokenInfo = await verifyToken(token);
        const machineName = tokenInfo.machineName;
        const scoreScale = tokenInfo.scoreScale;
        // 
        const spinCollection = CSpinCollection.FromJson(req.body);
        if (!spinCollection) throw new Error("null spinCollection");

        const error = await spin(machineName, scoreScale, spinCollection);
        if (error) throw new Error(error);

        res.send('OK');
    }
    catch (err) {
        handleError(res, err);
    }
});

router.post("/lower/joinWin", async (req, res) => {
    try {
        //const { ip } = req;        
        const { BonusServerDomain, BonusServerPort, Password } = req.body;        
        const domain = `${BonusServerDomain}:${BonusServerPort}`;
        const password = Password;
        const token = joinWin(domain, password);

        res.send(token);
    }
	catch (err) {
        handleError(res, err);
    }
});

router.post("/lower/collect", async (req, res) => {
    try {
        const ip = req.socket.remoteAddress;

        const authHeader = req.headers['authorization'];
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            throw new Error('Token missing or incorrect format');
        }
        // Extract the token from the header
        const token = authHeader.split(' ')[1];  // Split the Bearer part out
        // Verify the token
        const tokenInfo = await verifyToken(token);
        // 讀取 token 中的 claims
        if (tokenInfo.machineName == null ||
            tokenInfo.scoreScale == null ||
            tokenInfo.expiredAt == null)
        {
            throw new Error("invalid token params");
        }
        const machineName = tokenInfo.machineName;
        const scoreScale = parseFloat(tokenInfo.scoreScale);

        res.send("Ok");
    }
	catch (err) {
        handleError(res, err);
    }
});

router.post("/lower/rushToWinA", async (req, res) => {
    try {
        const { ip, body } = req;

        res.json({ status: "success" });
    }
    catch (err) {
        handleError(res, err);
    }
});

router.post("/lower/rushToWinCR", async (req, res) => {
    try {
        const { ip, body } = req;

        res.json({ status: "success" });
    }
    catch (err) {
        handleError(res, err);
    }
});

//==========================================================================================================
// replyToWinA and replyToWinCR should only be called by upper bonus server...
//==========================================================================================================
router.post("/lower/replyToWinA", async (req, res) => {
    try {
        const { ip, body } = req;

        res.sendStatus(200);
    }
    catch (err) {
        handleError(res, err);
    }
});

router.post("/lower/replyToWinCR", async (req, res) => {
    try {
        const { ip, body } = req;

        res.sendStatus(200);
    }
    catch (err) {
        handleError(res, err);
    }
});

export default router;

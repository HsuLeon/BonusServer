import express from "express";
import axios from "axios";
import { handleError } from "../utils/response.js";

const router = express.Router();

router.post("/login", async (req, res) => {
    /**
     * 登入數據對象
     * @typedef {Object} LoginData
     * @property {string} [machineName] - 機器名稱
     * @property {number} [scoreScale] - 分數比例
     */

    /** @type {LoginData} */

    try {
        const { ip, protocol, body } = req;
        const { machineName, scoreScale } = body;
        const loginData = {
            MachineName: machineName,
            ScoreScale: scoreScale
        };
        const url = `${protocol}://${req.headers.host}/bonus/login`;
        const result = await axios.post(url, loginData);

        if (result.status != 200 || result.statusText != "OK") throw new Error("result.statusText", result.statusText);
        res.json(result.data);
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
        const { ip, protocol, body } = req;
        const authHeader = req.headers['authorization'];
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
            throw new Error('Token missing or incorrect format');
        }
        // Extract the token from the header
        const token = authHeader.split(' ')[1];  // Split the Bearer part out
        const userAccount = body.UserAccount ? body.UserAccount : "testAccount";        
        let bonusType = body.BonusType ? parseInt(body.BonusType) : 1;
        bonusType = bonusType > 0 && bonusType <= 3 ? bonusType : 1;

        const objSpinData = {
            TotalBet: body.totalBet,
            TotalWin: body.totalWin,
            WinA: body.winA,
            WinB: body.winB
        };
        const spinData = {
            UserAccount: userAccount,
            BonusType: bonusType,
            SpinData: JSON.stringify(objSpinData),
            SyncTime: Date.now(),
            AbleToRushBonus: true,
        };
        const config = {
            headers: {
                "Authorization": `Bearer ${token}`
            },
        };

        const url = `${protocol}://${req.headers.host}/bonus/spin`;
        const result = await axios.post(url, spinData, config);

        if (result.status != 200 || result.statusText != "OK") throw new Error("result.statusText", result.statusText);
        res.json(result.data);
    }
    catch (err) {
        handleError(res, err);
    }
});

router.get("/settings", async (req, res) => {
    try {
        const { ip, protocol, body } = req;
        const schema = protocol === "https" ? "wss" : "ws";
        const url = `${schema}://${req.headers.host}/ws`;
        res.send(url);
    }
    catch (err) {
        handleError(res, err);
    }
});

export default router;

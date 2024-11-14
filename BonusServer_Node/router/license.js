import express from "express";
import { handleError } from "../utils/response.js";

const router = express.Router();

router.post("/hstone", async (req, res) => {
    try {
        const { ip, protocol, body } = req;
        const { ip_public, ip_local } = body;

        console.log(`ip_public:${ip_public.toString()}, ip_local:${ip_local.toString()}`);

        res.send('OK');
    }
    catch (err) {
        handleError(res, err);
    }
});

router.post("/checkdb", async (req, res) => {
    try {
        const { ip, protocol, body } = req;

        console.log(`checkdb...`);

        res.send('OK');
    }
    catch (err) {
        handleError(res, err);
    }
});

router.post("/log", async (req, res) => {
    try {
        const { ip, protocol, body } = req;

        console.log(`log...`);

        res.send('OK');
    }
    catch (err) {
        handleError(res, err);
    }
});

export default router;
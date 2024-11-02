
import jwt from "jsonwebtoken";
import Log from "../../utils/log.js";
import config from "../../utils/config.js";

export default function GenerateLoginToken(machineName, scoreScale)
{
    let token = null;
    try
    {
        if (!machineName) throw new Error("null machineName");
        if (!scoreScale || parseFloat(scoreScale) <= 0) throw new Error("invalid scoreScale");

        const expiringHours = 24;
        const curDate = new Date();
        const expiredDate = new Date(curDate.getTime() + expiringHours * 60 * 60 * 1000);
        const payload = {
            machineName: machineName,
            scoreScale: parseFloat(scoreScale),
            expiredAt: expiredDate.getTime(),
        };
        
        token = jwt.sign({
            payload: payload,
            exp: Math.floor(expiredDate.getTime() / 1000)
        }, config.SecretKey);
    }
    catch(err) {
        Log.storeMsg(err.message ? err.message : err);
    }
    return token;
}
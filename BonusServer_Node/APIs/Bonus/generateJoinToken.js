
import jwt from "jsonwebtoken";
import Log from "../../utils/utils.js";
import config from "../../utils/config.js";
import DBAgent from "../../services/dbAgent.js";

export default function GenerateJoinToken(domain)
{
    let token = null;
    try
    {
        if (!domain) throw new Error("null domain");

        const agentInfo = DBAgent.Agent();
        if (!agentInfo) throw new Error("null agentInfo");

        const expiringHours = 24;
        const curDate = new Date();
        const expiredDate = new Date(curDate.getTime() + expiringHours * 60 * 60 * 1000);        
        const payload = {
            upperDomain: agentInfo.Domain,
            domain: domain,
            expiredAt: expiredDate.getTime(),
        };

        token = jwt.sign({
            payload: payload,
            exp: Math.floor(expiredDate.getTime() / 1000)
        }, config.SecretKey);
    }
    catch (err)
    {
        Log.storeMsg(err.message ? err.message : err);
    }
    return token;
}

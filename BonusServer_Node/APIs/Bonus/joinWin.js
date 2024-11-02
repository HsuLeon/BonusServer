
import BonusAgent from "../../services/bonusAgent.js";
import generateJoinToken from "./generateJoinToken.js";

export default function JoinWin(domain, password)
{
    const errMsg = BonusAgent.addSubDomain(domain, password);
    if (!errMsg) throw new Error(errMsg);

    const token = generateJoinToken(domain);
    if (!token) throw new Error("generate token failed");
    return token;
}
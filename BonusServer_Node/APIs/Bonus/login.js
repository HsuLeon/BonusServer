
import BonusAgent from "../../services/bonusAgent.js";
import generateLoginToken from "./generateLoginToken.js";

export default function Login(machineName, scoreScale)
{
    const token = generateLoginToken(machineName, scoreScale);
    if (!token) throw new Error("null token");
    const webSite = BonusAgent.webSite();
    const ruleTrigger = BonusAgent.ruleTrigger();

    return {
        token: token,
        webSite: webSite,
        ruleId: ruleTrigger ? parseInt(ruleTrigger.RuleId) : 0 
    }
}
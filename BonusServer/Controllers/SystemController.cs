using BonusServer.Models;
using BonusServer.Services;
using BonusServer.Services.RuleTrigger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BonusServer.Controllers
{
    [Route("system")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("launchTime")]
        public IActionResult GetLaunchTime()
        {
            JObject objInfo = new JObject();
            objInfo["launchTime"] = BonusAgent.LaunchTime.ToString();

            return Ok(JsonConvert.SerializeObject(objInfo));
        }

        [AllowAnonymous]
        [HttpGet("notify")]
        public IActionResult GetNotify()
        {
            if (BonusAgent.RuleTrigger == null) throw new Exception("null RuleTrigger");
            CollectData collectDataA = BonusAgent.RuleTrigger.WinCollection_A;
            CollectData collectDataB = BonusAgent.RuleTrigger.WinCollection_B;
            CollectData collectDataCR = BonusAgent.RuleTrigger.WinCollection_CR;
            BonusRule.TriggeringCondition conditionA = BonusAgent.RuleTrigger.Condition_A;
            BonusRule.TriggeringCondition conditionB = BonusAgent.RuleTrigger.Condition_B;
            BonusRule.TriggeringCondition conditionCR = BonusAgent.RuleTrigger.Condition_CR;

            Dictionary<string, BonusRecords> dicNotifies = BonusAgent.GetNotify();

            JObject objNotify = new JObject();
            objNotify["records"] = JsonConvert.SerializeObject(dicNotifies);
            objNotify["totalBet_A"] = collectDataA.TotalBet / conditionA.ScoreInterval;
            objNotify["totalBet_B"] = collectDataB.TotalBet / conditionB.ScoreInterval;
            objNotify["totalBet_CR"] = collectDataCR.TotalBet / conditionCR.ScoreInterval;

            return Ok(JsonConvert.SerializeObject(objNotify));
        }

        //==========================================================================================================
        // notify should only be called by upper bonus server...
        //==========================================================================================================
        [Authorize]
        [HttpPost("notify")]
        public IActionResult AddNotify([FromBody] NotifyData notifyData)
        {
            try
            {
                // 取得 Bearer token
                string token_original = HttpContext.Request.Headers["Authorization"].ToString();
                string token = token_original.Replace("Bearer ", "");
                // 將 token 解析成 JWT token 物件
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken parsedToken = tokenHandler.ReadJwtToken(token);
                // 讀取 token 中的 claims
                Claim? upperDmainClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "UpperDomain");
                string? upperDomain = upperDmainClaim?.Value;
                if (upperDomain == null) throw new Exception("null upperDomain");
                if (BonusAgent.IsUpperDomain(upperDomain) == false) throw new Exception(string.Format("invalid domain: {0}", upperDomain));

                BonusAgent.AddNotifyData(notifyData.WebSite, notifyData.WinSpinType, notifyData.Message, notifyData.Scores, notifyData.MachineName, notifyData.UserAccount);
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        //==========================================================================================================
    }
}

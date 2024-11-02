using BonusServer.Models;
using BonusServer.Services;
using FunLobbyUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static FunLobbyUtils.Utils;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BonusServer.Controllers
{
    public class FakeLogin
    {
        public string MachineName { get; set; }
        public float ScoreScale { get; set; }
    }
    public class FakeSpin
    {
        public string? UserAccount { get; set; }
        public int BonusType { get; set; }
        public int TotalBet { get; set; }
        public int TotalWin { get; set; }
        public int WinA { get; set; }
        public int WinB { get; set; }
    }

    [Route("test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _config;

        public TestController(IConfiguration config)
        {
            _config = config;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] FakeLogin data)
        {
            IActionResult response;
            try
            {
                var requestUrl = $"{Request.Scheme}://{Request.Host}/bonus/login";
                JObject loginData = new JObject();
                loginData["MachineName"] = data.MachineName;
                loginData["ScoreScale"] = data.ScoreScale;

                string? strRes = Utils.HttpPost(requestUrl, loginData);
                if (strRes == null) throw new Exception(string.Format("failed to call {0}", requestUrl));
                response = Ok(strRes);
            }
            catch (Exception ex)
            {
                response = BadRequest(new { error = ex.Message });
            }
            return response;
        }

        [Authorize]
        [HttpPost("spin")]
        public IActionResult Spin([FromBody] FakeSpin data)
        {
            IActionResult response;
            try
            {
                // 取得 Bearer token
                string token_original = HttpContext.Request.Headers["Authorization"].ToString();
                string token = token_original.Replace("Bearer ", "");

                string userAccount = data.UserAccount != null ? data.UserAccount : "testAccount";

                int bonusType = data.BonusType;
                // if out of range, use slot type as default
                if (bonusType < 1 || bonusType > 3) bonusType = 1;

                JObject objSpinData = new JObject();
                objSpinData["TotalBet"] = data.TotalBet;
                objSpinData["TotalWin"] = data.TotalWin;
                objSpinData["WinA"] = data.WinA;
                objSpinData["WinB"] = data.WinB;

                var requestUrl = $"{Request.Scheme}://{Request.Host}/bonus/spin";
                JObject spinData = new JObject();
                spinData["UserAccount"] = userAccount;
                spinData["BonusType"] = bonusType;
                spinData["SpinData"] = JsonConvert.SerializeObject(objSpinData);
                spinData["SyncTime"] = Utils.dateTimeToString(DateTime.Now);
                spinData["AbleToRushBonus"] = true;

                string? strRes = Utils.HttpPost(requestUrl, spinData, token);
                if (strRes == null) throw new Exception(string.Format("failed to call {0}", requestUrl));
                response = Ok(strRes);
            }
            catch (Exception ex)
            {
                response = BadRequest(ex.Message);
            }
            return response;
        }


        [AllowAnonymous]
        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            IActionResult response;
            try
            {
                string schema = Request.Scheme == "https" ? "wss" : "ws";
                string requestUrl = $"{schema}://{Request.Host}/ws";
                response = Ok(requestUrl);
            }
            catch(Exception ex)
            {
                response = BadRequest(ex.Message);
            }
            return response;
        }
    }
}

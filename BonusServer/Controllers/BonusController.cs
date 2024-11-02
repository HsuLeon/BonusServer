using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using BonusServer.Models;
using Newtonsoft.Json;
using FunLobbyUtils;
using BonusServer.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using FunLobbyUtils.Database.Schema;
using Microsoft.Extensions.Primitives;
using BonusServer.Services.RuleTrigger;

namespace BonusServer.Controllers
{
    [Route("bonus")]
    [ApiController]
    public class BonusController : ControllerBase
    {
        private IConfiguration _config;

        public BonusController(IConfiguration config)
        {
            _config = config;
        }

        private string? GenerateLoginToken(LoginData loginData)
        {
            string? token = null;
            try
            {
                if (loginData == null) throw new Exception("null loginData");

                string key = _config["Jwt:Key"];
                SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                const int expiringHours = 24 * 1000;
                DateTime expiredDate = DateTime.Now.AddHours(expiringHours);
                long expiredAt = expiredDate.Ticks;
                Claim[] claims = new[] {
                    new Claim("MachineName", loginData.MachineName),
                    new Claim("ScoreScale", loginData.ScoreScale.ToString()),
                    new Claim("ExpiredAt", expiredAt.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };
                string issuer = _config["Jwt:Issuer"];
                string audience = _config["Jwt:Issuer"];
                JwtSecurityToken jwtToken = new JwtSecurityToken(
                    issuer,
                    audience,
                    claims,
                    expires: expiredDate,
                    signingCredentials: credentials
                );

                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                token = handler.WriteToken(jwtToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return token;
        }

        private string? GenerateJoinToken(string domain)
        {
            string? token = null;
            try
            {
                if (domain == null) throw new Exception("null domain");

                string key = _config["Jwt:Key"];
                SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                AgentInfo? agentInfo = DBAgent.Agent();
                if (agentInfo == null) throw new Exception("null agentInfo");

                const int expiringHours = 12;
                DateTime expiredDate = DateTime.Now.AddHours(expiringHours);
                long expiredAt = expiredDate.Ticks;
                Claim[] claims = new[] {
                    new Claim("UpperDomain", agentInfo.Domain),
                    new Claim("Domain", domain),
                    new Claim("ExpiredAt", expiredAt.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };
                string issuer = _config["Jwt:Issuer"];
                string audience = _config["Jwt:Issuer"];
                JwtSecurityToken jwtToken = new JwtSecurityToken(
                    issuer,
                    audience,
                    claims,
                    expires: expiredDate,
                    signingCredentials: credentials
                );

                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                token = handler.WriteToken(jwtToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return token;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login()
        {
            Log.StoreMsg("calling login");
            IActionResult response;
            try
            {
                LoginData? req = null;

                // Check if the content type is JSON
                string? contentType = HttpContext.Request.ContentType;
                if (contentType == null) throw new Exception("invalid contentType");
                contentType = contentType.ToLower();

                if (contentType.IndexOf("application/json") >= 0)
                {
                    // Read the JSON from the request body asynchronously using Task.Run
                    Task<string> readTask = Task.Run(async () =>
                    {
                        using (StreamReader reader = new StreamReader(Request.Body))
                        {
                            return await reader.ReadToEndAsync();
                        }
                    });
                    readTask.Wait(); // Wait for the reading to complete
                    string json = readTask.Result; // Get the result of reading
                    req = JsonConvert.DeserializeObject<LoginData>(json);
                }
                else if (contentType.IndexOf("application/x-www-form-urlencoded") >= 0 ||
                    contentType.IndexOf("multipart/form-data") >= 0)
                {
                    HttpContext.Request.Form.TryGetValue("MachineName", out StringValues machineName);
                    HttpContext.Request.Form.TryGetValue("ScoreScale", out StringValues scoreScale);

                    req = new LoginData();
                    req.MachineName = machineName;
                    req.ScoreScale = Convert.ToInt32(scoreScale);
                }
                else
                {
                    throw new Exception("Unsupported content type");
                }
                if (req == null) throw new Exception("null req");
                if (req.MachineName.Length == 0) throw new Exception("invalid machineName");
                if (req.ScoreScale <= 0) throw new Exception("invalid scoreScale");

                string? tokenString = GenerateLoginToken(req);
                if (tokenString == null) throw new Exception("generate token failed");
                JObject resObj = new JObject();
                resObj["token"] = tokenString;
                resObj["webSite"] = BonusAgent.WebSite;
                resObj["ruleId"] = BonusAgent.RuleTrigger != null ? (int)BonusAgent.RuleTrigger.RuleId : 0 ;

                response = Ok(JsonConvert.SerializeObject(resObj));
            }
            catch (Exception ex)
            {
                response = BadRequest(new { error = ex.Message });
            }
            return response;
        }

        [Authorize]
        [HttpPost("spin")]
        public IActionResult Spin()
        {
            Log.StoreMsg("calling spin");
            try
            {
                // Check if the content type is JSON
                string? contentType = HttpContext.Request.ContentType;
                if (contentType == null) throw new Exception("invalid contentType");
                contentType = contentType.ToLower();

                CSpinCollection? spinCollection = null;
                if (contentType.IndexOf("application/json") >= 0)
                {
                    // Read the JSON from the request body asynchronously using Task.Run
                    Task<string> readTask = Task.Run(async () =>
                    {
                        using (StreamReader reader = new StreamReader(Request.Body))
                        {
                            return await reader.ReadToEndAsync();
                        }
                    });
                    readTask.Wait(); // Wait for the reading to complete
                    string json = readTask.Result; // Get the result of reading
                    JObject? obj = JsonConvert.DeserializeObject<JObject>(json);
                    if (obj == null) throw new Exception("failed to deserialize content");
                    spinCollection = CSpinCollection.FromJson(obj);
                }
                else if (contentType.IndexOf("application/x-www-form-urlencoded") >= 0 ||
                    contentType.IndexOf("multipart/form-data") >= 0)
                {
                    HttpContext.Request.Form.TryGetValue("UserAccount", out StringValues reqUserAccount);
                    HttpContext.Request.Form.TryGetValue("BonusType", out StringValues reqBonusType);
                    HttpContext.Request.Form.TryGetValue("SpinData", out StringValues reqSpinData);
                    HttpContext.Request.Form.TryGetValue("SyncTime", out StringValues reqSyncTime);
                    HttpContext.Request.Form.TryGetValue("AbleToRushBonus", out StringValues reqAbleToRushBonus);

                    spinCollection = new CSpinCollection();
                    spinCollection.UserAccount = reqUserAccount;
                    spinCollection.BonusType = (CSpinCollection.BETWIN_TYPE)Convert.ToInt32(reqBonusType);
                    spinCollection.SpinData = JsonConvert.DeserializeObject<CSpinCollection.CSpinData>(reqSpinData);
                    spinCollection.SyncTime = Convert.ToInt64(reqSyncTime);
                    spinCollection.AbleToRushBonus = bool.Parse(reqAbleToRushBonus);
                }
                else
                {
                    throw new Exception("Unsupported content type");
                }
                if (spinCollection == null) throw new Exception("null spinCollection");
                if (spinCollection.SpinData == null) throw new Exception("null spinData");

                // 取得 Bearer token
                string token_original = HttpContext.Request.Headers["Authorization"].ToString();
                string token = token_original.Replace("Bearer ", "");
                // 將 token 解析成 JWT token 物件
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken parsedToken = tokenHandler.ReadJwtToken(token);
                // 讀取 token 中的 claims
                Claim? machineNameClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "MachineName");
                Claim? scoreScaleClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "ScoreScale");
                Claim? expiredAtClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "ExpiredAt");
                if (machineNameClaim == null ||
                    scoreScaleClaim == null ||
                    expiredAtClaim == null)
                {
                    throw new Exception("incorrect token");
                }

                string machineName = machineNameClaim.Value;
                float scoreScale = float.Parse(scoreScaleClaim.Value);

                string userAccount = spinCollection.UserAccount != null ? spinCollection.UserAccount : "";
                CSpinCollection.CSpinData spinData = spinCollection.SpinData;
                CSpinCollection.BETWIN_TYPE bonusType = spinCollection.BonusType;
                float totalBet = spinData.TotalBet * scoreScale;
                float totalWin = spinData.TotalWin * scoreScale;
                int winA = spinData.WinA;
                int winB = spinData.WinB;

                if (totalBet > 0 ||
                    totalWin > 0 ||
                    winA > 0 ||
                    winB > 0)
                {
                    CollectData data = new CollectData(
                        totalBet,
                        totalWin,
                        winA,
                        winB,
                        (int)bonusType
                    );
                    BonusAgent.CollectBonus(data);
                }
                // if able to rush for WinA/WinB
                if (spinCollection.AbleToRushBonus)
                {
                    switch (bonusType)
                    {
                        case CSpinCollection.BETWIN_TYPE.Slot:
                            if (winA > 0)
                            {
                                SpinData data = new SpinData();
                                data.Domain = BonusAgent.BonusServerDomain;
                                data.Port = BonusAgent.BonusServerPort;
                                data.WebSite = BonusAgent.WebSite;
                                data.MachineName = machineName;
                                data.UserAccount = userAccount;
                                data.Win = winA;
                                data.UtcTicks = DateTime.UtcNow.Ticks;
                                JObject obj = new JObject();
                                obj["account"] = userAccount;
                                obj["urlTransferPoints"] = BonusAgent.APITransferPoints;
                                obj["spinData"] = JsonConvert.SerializeObject(data);

                                // try upper server first
                                string? errMsg = BonusAgent.RushUpperWinSpinA(obj);
                                // if upper server doesn't accept request, check self winA
                                if (errMsg != null) BonusAgent.PushToWinSpinA(obj);
                            }
                            else if (winB > 0)
                            {
                                if (BonusAgent.RuleTrigger != null &&
                                    BonusAgent.RuleTrigger.IsWinSpinEnabled(BonusRule.WIN_TYPE.WinB))
                                {
                                    SpinData data = new SpinData();
                                    data.Domain = BonusAgent.BonusServerDomain;
                                    data.Port = BonusAgent.BonusServerPort;
                                    data.WebSite = BonusAgent.WebSite;
                                    data.MachineName = machineName;
                                    data.UserAccount = userAccount;
                                    data.Win = winB;
                                    data.UtcTicks = DateTime.UtcNow.Ticks;
                                    JObject obj = new JObject();
                                    obj["account"] = userAccount;
                                    obj["urlTransferPoints"] = BonusAgent.APITransferPoints;
                                    obj["spinData"] = JsonConvert.SerializeObject(data);

                                    BonusAgent.PushToWinSpinB(obj);
                                }
                            }
                            break;
                        case CSpinCollection.BETWIN_TYPE.Cr:
                        case CSpinCollection.BETWIN_TYPE.ChinaCr:
                            if (winB > 0)
                            {
                                SpinData data = new SpinData();
                                data.Domain = BonusAgent.BonusServerDomain;
                                data.Port = BonusAgent.BonusServerPort;
                                data.WebSite = BonusAgent.WebSite;
                                data.MachineName = machineName;
                                data.UserAccount = userAccount;
                                data.Win = winB;
                                data.UtcTicks = DateTime.UtcNow.Ticks;
                                JObject obj = new JObject();
                                obj["account"] = userAccount;
                                obj["urlTransferPoints"] = BonusAgent.APITransferPoints;
                                obj["spinData"] = JsonConvert.SerializeObject(data);

                                // try upper server first
                                string? errMsg = BonusAgent.RushUpperWinSpinCR(obj);
                                // if upper server doesn't accept request, check self winCR
                                if (errMsg != null) BonusAgent.PushToWinSpinCR(obj);
                            }
                            break;
                    }
                }

                return Ok("Ok");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //==========================================================================================================
        // joinWin & collect & rushToWinA & rushToWinCR should only be called by lower bonus server...
        //==========================================================================================================
        [AllowAnonymous]
        [HttpPost("lower/joinWin")]
        public IActionResult JoinWin([FromBody] JoinWinData req)
        {
            Log.StoreMsg("calling lower/joinWin");
            IActionResult response;
            try
            {
                string domain = string.Format("{0}:{1}", req.BonusServerDomain, req.BonusServerPort);
                string password = req.Password;
                string? errMsg = BonusAgent.AddSubDomain(domain, password);
                if (errMsg != null) throw new Exception(errMsg);

                string? tokenString = GenerateJoinToken(domain);
                if (tokenString == null) throw new Exception("generate token failed");

                response = Ok(tokenString);
            }
            catch (Exception ex)
            {
                response = BadRequest(new { error = ex.Message });
            }
            return response;
        }

        [Authorize]
        [HttpPost("lower/collect")]
        public IActionResult Collect([FromBody] CollectData data)
        {
            Log.StoreMsg("lower/collect");
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
                if (upperDmainClaim == null) throw new Exception("null UpperDomain");
                Claim? domainClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "Domain");
                if (domainClaim == null) throw new Exception("null domainClaim");

                string upperDomain = upperDmainClaim.Value;
                string domain = domainClaim.Value;

                AgentInfo? agentInfo = DBAgent.Agent();
                if (agentInfo == null) throw new Exception("null agentInfo");
                if (upperDomain != agentInfo.Domain) throw new Exception(string.Format("invalid upperDomain: {0}", upperDomain));
                if (domain == null || BonusAgent.IsSubDomain(domain) == false) throw new Exception(string.Format("invalid domain: {0}", domain));
                BonusAgent.RefreshToken(domain, token);

                CollectData subData = data.Clone();
                subData.TotalBet *= BonusAgent.CollectSubScale;
                subData.TotalWin *= BonusAgent.CollectSubScale;
                BonusAgent.CollectBonus(subData);

                return Ok("Ok");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("lower/rushToWinA")]
        public IActionResult RushToWinA([FromBody] JObject objData)
        {
            Log.StoreMsg("lower/rushToWinA");
            Dictionary<string, object> dic = new Dictionary<string, object>();
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
                if (upperDmainClaim == null) throw new Exception("null UpperDomain");
                Claim? domainClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "Domain");
                if (domainClaim == null) throw new Exception("null Domain");

                string upperDomain = upperDmainClaim.Value;
                string domain = domainClaim.Value;

                AgentInfo? agentInfo = DBAgent.Agent();
                if (agentInfo == null) throw new Exception("null agentInfo");
                if (upperDomain != agentInfo.Domain) throw new Exception(string.Format("invalid upperDomain: {0}", upperDomain));
                if (BonusAgent.IsSubDomain(domain) == false) throw new Exception(string.Format("invalid domain: {0}", domain));
                BonusAgent.RefreshToken(domain, token);

                string? errMsg = BonusAgent.RushUpperWinSpinA(objData);
                // if upper bonusServer replies error
                if (errMsg != null)
                {
                    // try current bonusServer accepts request or not
                    errMsg = BonusAgent.PushToWinSpinA(objData);
                    if (errMsg != null) throw new Exception("no bonus server accepts rushToWinA");
                }
                // rushToWinA was accepted
                dic["status"] = "success";
            }
            catch (Exception ex)
            {
                // both upper & current bonusServer accepts this request, discard it...
                dic["status"] = "failed";
                dic["error"] = ex.Message;
            }
            return Ok(dic);
        }

        [Authorize]
        [HttpPost("lower/rushToWinCR")]
        public IActionResult RushToWinCR([FromBody] JObject objData)
        {
            Log.StoreMsg("lower/rushToWinCR");
            Dictionary<string, object> dic = new Dictionary<string, object>();
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
                if (upperDmainClaim == null) throw new Exception("null UpperDomain");
                Claim? domainClaim = parsedToken.Claims.FirstOrDefault(c => c.Type == "Domain");
                if (domainClaim == null) throw new Exception("null Domain");

                string upperDomain = upperDmainClaim.Value;
                string domain = domainClaim.Value;

                AgentInfo? agentInfo = DBAgent.Agent();
                if (agentInfo == null) throw new Exception("null agentInfo");
                if (upperDomain != agentInfo.Domain) throw new Exception(string.Format("invalid upperDomain: {0}", upperDomain));
                if (BonusAgent.IsSubDomain(domain) == false) throw new Exception(string.Format("invalid domain: {0}", domain));
                BonusAgent.RefreshToken(domain, token);

                string? errMsg = BonusAgent.RushUpperWinSpinCR(objData);
                // if upper bonusServer replies error
                if (errMsg != null)
                {
                    // try current bonusServer accepts request or not
                    errMsg = BonusAgent.PushToWinSpinCR(objData);
                    if (errMsg != null) throw new Exception("no bonus server accepts rushToWinCR");
                }
                // rushToWinA was accepted
                dic["status"] = "success";
            }
            catch (Exception ex)
            {
                // both upper & current bonusServer accepts this request, discard it...
                dic["status"] = "failed";
                dic["error"] = ex.Message;
            }
            return Ok(dic);
        }
        //==========================================================================================================

        //==========================================================================================================
        // replyToWinA and replyToWinCR should only be called by upper bonus server...
        //==========================================================================================================
        [Authorize]
        [HttpPost("upper/replyToWinA")]
        public IActionResult ReplyToWinA([FromBody] JObject objData)
        {
            Log.StoreMsg("upper/replyToWinA");
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
                if (upperDmainClaim == null) throw new Exception("null UpperDomain");
                string upperDomain = upperDmainClaim.Value;
                if (BonusAgent.IsUpperDomain(upperDomain) == false) throw new Exception(string.Format("invalid domain: {0}", upperDomain));

                string? errMsg = BonusAgent.PushToWinSpinA(objData);
                if (errMsg != null) throw new Exception(errMsg);
            }
            catch(Exception ex)
            {
                // if currentbonus server not accept request, reply to sub bonus server
                if (objData.ContainsKey("from"))
                {
                    string? urlFrom = objData["from"]?.Value<string>();
                    JObject? data = objData["data"]?.Value<JObject>();
                    // pass back to sub bonus server
                    if (urlFrom != null && data != null)
                    {
                        string? token = BonusAgent.GetTokenOfDomain(urlFrom);
                        string url = string.Format("{0}/bonus/upper/replyToWinA", urlFrom);
                        Utils.HttpPost(url, data, token);
                    }
                }
            }
            return Ok("Ok");
        }

        [Authorize]
        [HttpPost("upper/replyToWinCR")]
        public IActionResult ReplyToWinCR([FromBody] JObject objData)
        {
            Log.StoreMsg("upper/replyToWinCR");
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
                if (upperDmainClaim == null) throw new Exception("null UpperDomain");
                string upperDomain = upperDmainClaim.Value;
                if (BonusAgent.IsUpperDomain(upperDomain) == false) throw new Exception(string.Format("invalid domain: {0}", upperDomain));

                string? errMsg = BonusAgent.PushToWinSpinCR(objData);
                if (errMsg != null) throw new Exception(errMsg);
            }
            catch (Exception ex)
            {
                // if currentbonus server not accept request, reply to sub bonus server
                if (objData.ContainsKey("from"))
                {
                    string? urlFrom = objData["from"]?.Value<string>();
                    JObject? data = objData["data"]?.Value<JObject>();
                    // pass back to sub bonus server
                    if (urlFrom != null && data != null)
                    {
                        string? token = BonusAgent.GetTokenOfDomain(urlFrom);
                        string url = string.Format("{0}/bonus/upper/replyToWinCR", urlFrom);
                        Utils.HttpPost(url, data, token);
                    }
                }
            }
            return Ok("Ok");
        }
        //==========================================================================================================
    }
}

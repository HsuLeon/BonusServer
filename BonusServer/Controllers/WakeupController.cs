using FunLobbyUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BonusServer.Controllers
{
    [ApiController]
    [Route("wakeup")]
    public class WakeupController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WakeupController> _logger;

        public WakeupController(ILogger<WakeupController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("heartbeat")]
        public IEnumerable<WeatherForecast> Get()
        {
            string msg = string.Format("heartbeat {0}", DateTime.Now.ToString());
            Log.StoreMsg(msg);
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [AllowAnonymous]
        [HttpGet("test")]
        public IActionResult Test()
        {
            string host = Request.Host.Host;
            int? port = Request.Host.Port;

            string strHeaders = Request.Headers.ToString();
            string strHost = Request.Host.ToString();

            Dictionary<string, string> result = new Dictionary<string, string>();
            result["strHeaders"] = strHeaders;
            result["strHost"] = strHost;
            result["host"] = host;
            result["port"] = port.ToString();

            return Ok(result);
        }
    }
}
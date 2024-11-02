using BonusServer.Models;
using FunLobbyUtils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BonusServer.Controllers
{
    [Route("configSetting")]
    [ApiController]
    public class ConfigSettingController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            try
            {
                ConfigSetting? configSetting = Config.Instance as ConfigSetting;
                if (configSetting == null) throw new Exception("");

                configSetting.Reload();

                ConfigSettingData config = new ConfigSettingData();
                config.BonusServerDomain = configSetting.BonusServerDomain;
                config.BonusServerPort = configSetting.BonusServerPort;
                config.BonusServerPassword = configSetting.BonusServerPassword;
                config.UpperDomain = configSetting.UpperDomain;
                config.SubDomains = configSetting.SubDomains;
                config.APITransferPoints = configSetting.APITransferPoints;
                config.CollectSubScale = configSetting.CollectSubScale;
                config.RabbitMQServer = configSetting.RabbitMQServer;
                config.RabbitMQUserName = configSetting.RabbitMQUserName;
                config.RabbitMQPassword = configSetting.RabbitMQPassword;
                config.ConditionWinA = configSetting.ConditionWinA;
                config.ConditionWinB = configSetting.ConditionWinB;
                config.ConditionWinCR = configSetting.ConditionWinCR;
                return Ok(config);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPut("settings")]
        public IActionResult SetSettings([FromBody] ConfigSettingData config)
        {
            try
            {
                ConfigSetting? configSetting = Config.Instance as ConfigSetting;
                if (configSetting == null) throw new Exception("");

                if (config.BonusServerDomain != null) configSetting.BonusServerDomain = config.BonusServerDomain;
                if (config.BonusServerPort != null) configSetting.BonusServerPort = (int)config.BonusServerPort;
                if (config.BonusServerPassword != null) configSetting.BonusServerPassword = config.BonusServerPassword;
                if (config.UpperDomain != null) configSetting.UpperDomain = config.UpperDomain;
                if (config.SubDomains != null) configSetting.SubDomains = config.SubDomains;
                if (config.APITransferPoints != null) configSetting.APITransferPoints = config.APITransferPoints;
                if (config.CollectSubScale != null) configSetting.CollectSubScale = (float)config.CollectSubScale;
                if (config.RabbitMQServer != null) configSetting.RabbitMQServer = config.RabbitMQServer;
                if (config.RabbitMQUserName != null) configSetting.RabbitMQUserName = config.RabbitMQUserName;
                if (config.RabbitMQPassword != null) configSetting.RabbitMQPassword = config.RabbitMQPassword;
                if (config.ConditionWinA != null) configSetting.ConditionWinA = config.ConditionWinA;
                if (config.ConditionWinB != null) configSetting.ConditionWinB = config.ConditionWinB;
                if (config.ConditionWinCR != null) configSetting.ConditionWinCR = config.ConditionWinCR;
                configSetting.storeConfig();
                // delay for done
                Thread.Sleep(1000);
                // reload config
                configSetting.Reload();
                Dictionary<string, string> dicSettings = configSetting.getContent();
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

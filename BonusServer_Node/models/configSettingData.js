
export default class ConfigSettingData
{
    BonusServerDomain;
    BonusServerPort;
    BonusServerPassword;
    UpperDomain;
    SubDomains;
    APITransferPoints;
    CollectSubScale;
    RabbitMQServer;
    RabbitMQUserName;
    RabbitMQPassword;
    ConditionWinA;
    ConditionWinB;
    ConditionWinCR;

    constructor()
    {
        this.BonusServerDomain = ''
        this.BonusServerPort = 80
        this.BonusServerPassword = ''
        this.UpperDomain = ''
        this.SubDomains = []
        this.APITransferPoints = ''
        this.CollectSubScale = 1
        this.RabbitMQServer = '';
        this.RabbitMQUserName = 'WinLobby'
        this.RabbitMQPassword = '12345'
        this.ConditionWinA = '';
        this.ConditionWinB = '';
        this.ConditionWinCR = '';
    }
}
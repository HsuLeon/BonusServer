

export default class NotifyData
{
    static WinSpin = {
        None: 0,
        A: 1,
        B: 2,
        CR: 3
    };

    WinSpinType;
    WebSite;
    Message;
    Scores;
    MachineName;
    UserAccount;
    CreateTime;
    LifeInMinutes;

    constructor()
    {
        this.WinSpinType = NotifyData.WinSpin.None;
        this.WebSite = "";
        this.Message = "";
        this.Scores = 0;
        this.MachineName = "";
        this.UserAccount = "";
        this.CreateTime = new Date();
    }
}
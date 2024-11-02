
import fs from "fs";

export default class Log
{
    static Path = null;
    static #mInstance = null;

    constructor() {
        this.mLogger = null;
        this.mInterval = null;
        this.mQueue = [];

        this.#init(Log.Path);
    }

    static getInstance() {
        if (!Log.#mInstance) {
            const instance = new Log();
            Log.#mInstance = instance;
        }
        return Log.#mInstance;
    }

    #dateToString = function(dateTime, withDate, withTime) {
        const yy = dateTime.getFullYear();
        const mm = dateTime.getMonth() + 1; // getMonth() is zero-based
        const dd = dateTime.getDate();
        const hh = dateTime.getHours();
        const MM = dateTime.getMinutes();
        const ss = dateTime.getSeconds();

        const strDate = [
            yy,
            (mm>9 ? '' : '0') + mm,
            (dd>9 ? '' : '0') + dd
           ].join('-');
        const strTime = [
            (hh>9 ? '' : '0') + hh,
            (MM>9 ? '' : '0') + MM,
            (ss>9 ? '' : '0') + ss
           ].join(':');
        if (withDate && withTime) return `${strDate} ${strTime}`;
        else if (withDate) return strDate;
        else if (withTime) return strTime;
        else return "";
    };

    #init(path) {
        if (!path) path = "./Log";
        path = path.toString().replace('\\', '/');
        let tmpPath = "";
        const strPath = path.split("/");
        for (let i = 0 ; i < strPath.length ; i++) {            
            tmpPath += strPath[i];
            if (fs.existsSync(tmpPath) == false) fs.mkdirSync(tmpPath);
            tmpPath += "/";
        }

        const curTime = new Date();
        const yy = curTime.getFullYear();
        const mm = curTime.getMonth() + 1; // getMonth() is zero-based
        const dd = curTime.getDate();
        const hh = curTime.getHours();
        const MM = curTime.getMinutes();
        const ss = curTime.getSeconds();
        const fileName = path + "/" +
        [
            yy,
            (mm>9 ? '' : '0') + mm,
            (dd>9 ? '' : '0') + dd,
            (hh>9 ? '' : '0') + hh,
            (MM>9 ? '' : '0') + MM,
            (ss>9 ? '' : '0') + ss
        ].join('_') + ".txt";
        this.mLogger = fs.createWriteStream(fileName, {
            flags: 'a' // 'a' means appending (old data will be preserved)
        });
        this.mInterval = setInterval(() => {
            Log.flush();
        }, 20000);
    }

    static storeMsg(msg) {
        if (!msg) return;

        const instance = Log.getInstance();
        const curTime = new Date();
        const log = instance.#dateToString(curTime, false, true) + " => " + msg;
        instance.mQueue.push(log);

        if (instance.mQueue.length > 200) {
            instance.flush();
        }
    }

    static flush() {
        const instance = Log.getInstance();
        for (let i = 0 ; i < instance.mQueue.length ; i++) {
            instance.mLogger.write(`\n${instance.mQueue[i]}`);
        }
        instance.mQueue = [];
    }
}
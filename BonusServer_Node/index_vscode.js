import { exec } from "child_process";
import express from "express";
import fs from "fs/promises";
import https from "https";
import path from "path";
import swaggerUi from "swagger-ui-express";
import { fileURLToPath } from "url";
import YAML from "yamljs";
import bonusRouter from "./router/bonus.js";
import configSettingRouter from "./router/configSetting.js";
import setupMiddleware from "./router/middleware/app.js";
import systemRouter from "./router/system.js";
import testRouter from "./router/test.js";
// import RabbitMQManager from "./utils/rabbitMqClass.js";
import BonusAgent from './services/bonusAgent.js';
import Utils from "./utils/utils.js";
import ConfigSetting from './configSetting.js';
import DBAgent from './services/dbAgent.js';
import Log from './utils/log.js';

// 取得 __dirname 的替代方案
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const defaultPort = 8000;

// 解析命令行參數
const parseArgs = () => {
    const args = process.argv.slice(2);
    args.forEach((arg) => {
        const [key, value] = arg.split("=");
        if (key && value) {
            switch (key.toLowerCase()) {
            case "env":
                process.env.Deploy = value;
                break;
            case "port":
                process.env.PORT = parseInt(value, 10);
                break;
            default:
                break;
            }
        }
    });

    // 使用預設值如果未指定
    const Deploy = process.env.Deploy || "localhost";
    const PORT = process.env.PORT || defaultPort;
    if (!process.env.Deploy || !process.env.PORT) {
        console.log("args not assigned completely, using default settings...");
        process.env.Deploy = Deploy;
        process.env.PORT = PORT;
    }
};

// 設定 Express 應用
const setupExpressApp = () => {
    const app = express();

    // 使用 setupMiddleware 函數來設置中間件
    setupMiddleware(app);

    // 加載 OpenAPI 規範文件
    const swaggerDocument = YAML.load("./openapi.yaml");

    // 設置 Swagger UI 路由
    app.use("/api-docs", swaggerUi.serve, swaggerUi.setup(swaggerDocument));

    // 路由設定
    app.use("/bonus", bonusRouter);
    app.use("/configSetting", configSettingRouter);
    app.use("/system", systemRouter);
    app.use("/test", testRouter);

    app.use(express.static(path.join(__dirname, "dist")));
    app.get("*", (req, res) => {
        res.sendFile(path.join(__dirname, "dist", "index.html"));
    });
    return app;
};

// 自動開啟 Swagger 文件
const openSwaggerInBrowser = (port) => {
    const swaggerUrl = `http://localhost:${port}/api-docs`;
    switch (process.platform) {
        case "darwin": // macOS
            exec(`open ${swaggerUrl}`);
            break;
        case "win32": // Windows
            exec(`start ${swaggerUrl}`);
            break;
        default: // Linux
            exec(`xdg-open ${swaggerUrl}`);
            break;
    }
};

// 啟動伺服器
const startServer = async () => {
    try {
        parseArgs();
        Utils.setWorkDir(__dirname);

        const confPath = "C:/SignalR/BonusServer";
        const confFile = confPath + "/config.bin";
        const recordsFile = confPath + "/BonusRecords.json";
        Log.Path = `${confPath}/Log`;
        await ConfigSetting.loadConfig(path.resolve(confFile));

        DBAgent.initDB(ConfigSetting.DBHost(), ConfigSetting.DBPort());

        BonusAgent.initParams(
            ConfigSetting.WebSite(),
            ConfigSetting.BonusServerDomain(),
            ConfigSetting.BonusServerPort(),
            ConfigSetting.UpperDomain(),
            ConfigSetting.APITransferPoints(),
            ConfigSetting.CollectSubScale()
        );
        BonusAgent.initQueues(ConfigSetting.RabbitMQServer(), ConfigSetting.RabbitMQUserName(), ConfigSetting.RabbitMQPassword());
        BonusAgent.initTriggers(ConfigSetting.BetWinRule(), ConfigSetting.ConditionWinA(), ConfigSetting.ConditionWinB(), ConfigSetting.ConditionWinCR());
        BonusAgent.restoreRecords(recordsFile);

        const app = setupExpressApp();
        if (ConfigSetting.SSL_PATH) {
            const sslPath = path.resolve(__dirname, ConfigSetting.SSL_PATH);
            const [key, cert, ca] = await Promise.all([
                fs.readFile(path.join(sslPath, "private.key")),
                fs.readFile(path.join(sslPath, "cert.crt")),
                fs.readFile(path.join(sslPath, "ca.crt")),
            ]);

            const options = { key, cert, ca };
            const httpsServer = https.createServer(options, app);
            httpsServer.listen(443, () => {
                //app.use(express.static(path.join(__dirname, "../dist")));
                console.log("HTTPS Server listening on port 443");
            });
        }
        else {
            //app.use(express.static(path.join(__dirname, "../dist")));
            const PORT = parseInt(process.env.PORT, 10) || defaultPort;
            app.listen(PORT, () => {
                console.log(`HTTP Server listening on port ${PORT}`);
            });
        }

        // 自動開啟 Swagger 文件
        openSwaggerInBrowser(process.env.PORT || defaultPort);
    }
    catch (error) {
        console.error("Server initialization error:", error.message);
    }
};

// 執行啟動伺服器
startServer();

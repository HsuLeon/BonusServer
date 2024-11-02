import crypto from "crypto";
import path from "path";
import qs from "qs";
import { fileURLToPath } from "url";

// 初始化工作目錄
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
let mWorkDir = __dirname;

export default class Utils
{
    // 加密模式常數
    static ENCRYPT = {
        MD5: "MD5",
        AES256: "AES256",
    }

    /**
     * 生成交易 ID
     * 格式：YYYYMMDD + 12 位隨機字符
     * @returns {string} 交易 ID
     */
    static getTradeId()
    {
        const curDate = new Date();
        const year = curDate.getFullYear();
        const month = String(curDate.getMonth() + 1).padStart(2, "0");
        const day = String(curDate.getDate()).padStart(2, "0");

        const codes = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".split("");
        let tradeId = `${year}${month}${day}`;

        for (let i = 0; i < 12; i++) {
            const index = Math.floor(Math.random() * codes.length);
            tradeId += codes[index];
        }

        return tradeId;
    }

    /**
     * 生成隨機碼
     * @param {number} length - 隨機碼長度，默認為 10
     * @returns {string} 隨機碼
     */
    static getRandomCode(length = 10)
    {
        const codes = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".split("");
        let randomId = "";

        for (let i = 0; i < length; i++) {
            const index = Math.floor(Math.random() * codes.length);
            randomId += codes[index];
        }

        return randomId;
    }

    /**
     * 設定工作目錄
     * @param {string} workDir - 工作目錄路徑
     */
    static setWorkDir(workDir)
    {
        const index = workDir.indexOf("functions");
        mWorkDir = index < 0 ? workDir : workDir.substring(0, index);
        console.log(`workDir: ${mWorkDir}`);
    }

    /**
     * 獲取工作目錄
     * @returns {string} 工作目錄路徑
     */
    static getWorkDir()
    {
        return mWorkDir;
    }

    /**
     * SHA1 加密
     * @param {string} input - 輸入字串
     * @returns {string} 加密後的字串（大寫）
     */
    static sha1_Encrypt(input)
    {
        return crypto
            .createHash("sha1")
            .update(input, "utf8")
            .digest("hex")
            .toUpperCase();
    }

    /**
     * SHA256 加密
     * @param {string} input - 輸入字串
     * @returns {string} 加密後的字串（大寫）
     */
    static sha256_Encrypt(input)
    {
        return crypto
            .createHash("sha256")
            .update(input, "utf8")
            .digest("hex")
            .toUpperCase();
    }

    /**
     * HMAC SHA256 加密
     * @param {string} input - 輸入字串
     * @param {string} key - 密鑰
     * @returns {string} 加密後的字串（大寫）
     */
    static hmacsha256_Encrypt(input, key)
    {
        return crypto
            .createHmac("sha256", key)
            .update(input, "utf8")
            .digest("hex")
            .toUpperCase();
    }

    /**
     * 加密函數
     * @param {string} content - 要加密的內容
     * @param {string} encryptKey - 加密密鑰
     * @param {string} encryptIV - 加密向量
     * @param {string} mode - 加密模式（默認為 MD5）
     * @returns {string|null} 加密後的內容，若失敗則返回 null
     */
    static encrypt(content, encryptKey, encryptIV, mode = ENCRYPT.MD5)
    {
        try {
            let byteContent = Buffer.from(content, "utf8");
            const byteKey = Buffer.from(encryptKey, "utf8");
            const byteIV = Buffer.from(encryptIV, "utf8");
            let cipher;

            if (mode === ENCRYPT.MD5) {
                const md5Key = crypto.createHash("md5").update(byteKey).digest();
                const md5IV = crypto.createHash("md5").update(byteIV).digest();
                if (md5Key.length === 16) {
                    cipher = crypto.createCipheriv("aes-128-cbc", md5Key, md5IV);
                }
                else if (md5Key.length === 32) {
                    cipher = crypto.createCipheriv("aes-256-cbc", md5Key, md5IV);
                }
                else {
                    throw new Error(`Invalid key length: ${md5Key.length}`);
                }
            }
            else if (mode === ENCRYPT.AES256) {
                cipher = crypto.createCipheriv("aes-256-cbc", byteKey, byteIV);
                // 使用零填充，確保內容長度為 16 的倍數
                cipher.setAutoPadding(false);
                const paddingLength = 16 - (byteContent.length % 16);
                if (paddingLength > 0 && paddingLength < 16) {
                    byteContent = Buffer.concat([
                        byteContent,
                        Buffer.alloc(paddingLength, 0x00),
                    ]);
                }
            }
            else {
                throw new Error(`Unknown encryption mode: ${mode}`);
            }

            const encrypted = Buffer.concat([
                cipher.update(byteContent),
                cipher.final(),
            ]).toString("base64");
            return encrypted;
        }
        catch (error) {
            console.error(`Utils.Encrypt error: ${error.message}`);
            return null;
        }
    }

    /**
     * 解密函數
     * @param {string} content - 要解密的內容（Base64 編碼）
     * @param {string} encryptKey - 解密密鑰
     * @param {string} encryptIV - 解密向量
     * @param {string} mode - 解密模式（默認為 MD5）
     * @returns {string|null} 解密後的內容，若失敗則返回 null
     */
    static decrypt(content, encryptKey, encryptIV, mode = ENCRYPT.MD5)
    {
        try {
            const byteContent = Buffer.from(content, "base64");
            const byteKey = Buffer.from(encryptKey, "utf8");
            const byteIV = Buffer.from(encryptIV, "utf8");
            let decipher;

            if (mode === ENCRYPT.MD5) {
                const md5Key = crypto.createHash("md5").update(byteKey).digest();
                const md5IV = crypto.createHash("md5").update(byteIV).digest();
                if (md5Key.length === 16) {
                    decipher = crypto.createDecipheriv("aes-128-cbc", md5Key, md5IV);
                }
                else if (md5Key.length === 32) {
                    decipher = crypto.createDecipheriv("aes-256-cbc", md5Key, md5IV);
                }
                else {
                    throw new Error(`Invalid key length: ${md5Key.length}`);
                }
            }
            else if (mode === ENCRYPT.AES256) {
                decipher = crypto.createDecipheriv("aes-256-cbc", byteKey, byteIV);
                // 禁用自動填充
                decipher.setAutoPadding(false);
            }
            else {
                throw new Error(`Unknown decryption mode: ${mode}`);
            }

            let decrypted = Buffer.concat([
            decipher.update(byteContent),
            decipher.final(),
            ]).toString("utf8");
            // 移除尾部的零字節
            decrypted = decrypted.replace(/\0+$/, "");
            return decrypted;
        }
        catch (error) {
            console.error(`Utils.Decrypt error: ${error.message}`);
            return null;
        }
    }

    /**
     * FunPoint 加密
     * @param {object} params - 要加密的參數對象
     * @param {string} hashKey - HashKey
     * @param {string} hashIV - HashIV
     * @returns {string} 加密後的哈希值
     */
    static funPointEncrypt(params, hashKey, hashIV)
    {
        const sortedParams = Object.keys(params)
            .sort()
            .reduce((result, key) => {
                result[key] = params[key];
                return result;
            }, {});

        let queryString = qs.stringify(sortedParams, { encode: false });
        queryString = `HashKey=${hashKey}&${queryString}&HashIV=${hashIV}`;

        let encodedString = encodeURIComponent(queryString)
            .toLowerCase()
            .replace(/%20/g, "+");
        const hash = sha256_Encrypt(encodedString);

        return hash;
    }

    static zeroDateTime()
    {
        return new Date(0);
    }
}
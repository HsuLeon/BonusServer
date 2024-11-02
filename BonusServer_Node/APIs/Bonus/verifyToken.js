
import jwt from "jsonwebtoken";
import Config from "../../utils/config.js";

export default function VerifyToken(token)
{
    const SECRET_KEY = Config.SecretKey;
    return new Promise((resolve, reject) => {
        jwt.verify(token, SECRET_KEY, (err, decoded) => {
            if (err) reject(err);
            resolve(decoded.payload);
        });
    });
}
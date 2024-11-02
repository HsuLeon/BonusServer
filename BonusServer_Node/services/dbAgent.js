
import CMongoDB from '../utils/database/mongodb.js';

export default class DBAgent
{
    static #mWinLobbyDB = null;

    static initDB(dbHost, dbPort)
    {
        DBAgent.#mWinLobbyDB  = new CMongoDB();
        DBAgent.#mWinLobbyDB.init(dbHost, dbPort);
    }

    static addBonusRecord(winType, totalBet, scoreInterval, objBonus, urlTransferPoints, strTransferResponse)
    {
        return DBAgent.#mWinLobbyDB.addBonusRecord(
            winType,
            totalBet,
            scoreInterval,
            JSON.stringify(objBonus),
            urlTransferPoints,
            strTransferResponse
        );
    }
}
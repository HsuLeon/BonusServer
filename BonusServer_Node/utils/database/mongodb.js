
import mongoose from "mongoose";
import Utils from "../utils.js";

export default class CMongoDB
{
    init(server, port) {
        try {
            mongoose.connect(`mongodb://${server}:${port}/BonusServer`);
            // Get Mongoose to use the global promise library
            mongoose.Promise = global.Promise;
            //Get the default connection
            const db = mongoose.connection;
            //Bind connection to error event (to get notification of connection errors)
            db.on("open", (stream) => {
                console.log("MongoDB connection open");
            });
            db.on("error", (stream) => {
                console.log("MongoDB connection error");
            });
            //Define a schema
            const winAwardSchema = new mongoose.Schema({
                WinType: Number,
                TotalBet: Number,
                ScoreInterval: Number,
                Bonus: String,
                UrlTransferPoints: String,
                Response: String,
            });
            // Compile model from schema
            this.WinAwardModel = mongoose.model("WinAwards", winAwardSchema);
        }
        catch (err) {
            console.log(err.message);
        }
    };

    addBonusRecord(winType, totalBet, scoreInterval, bonus, urlTransferPoints, strTransferResponse) {
        /*
        winType: number
        totalBet: number
        scoreInterval: number
        bonus: string
        urlTransferPoints: string
        response: string
        */
        return new Promise((resolve, reject) => {
            try {
                // Create an instance of model WinAwardModel
                const model = new WinAwardModel();
                model.WinType = winType;
                model.TotalBet = totalBet;
                model.ScoreInterval = scoreInterval;
                model.Bonus = bonus;
                model.UrlTransferPoints = urlTransferPoints;
                model.Response = strTransferResponse;
                model.save(function (err) {
                    if (err) reject(err);
                    resolve();
                });
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    }

    addOrder(order) {
        return new Promise((resolve, reject) => {
            try {
                if (!order) throw new Error("null order");
                // Create an instance of model OrderModel
                const orderModel = new OrderModel();
                orderModel.merTradeID = order.MerTradeID;
                orderModel.merProductID = order.MerProductID;
                orderModel.merUserID = order.MerUserID;
                orderModel.merUserAccount = order.MerUserAccount;
                if (order.UserName) orderModel.userName = order.UserName;
                if (order.CitizenId) orderModel.citizenId = order.CitizenId;
                if (order.ChoosePayment) orderModel.choosePayment = order.ChoosePayment;
                if (order.ChooseStoreID) orderModel.chooseStoreID = order.ChooseStoreID;
                if (order.RemoteIP) orderModel.remoteIP = order.RemoteIP;
                orderModel.amount = order.Amount;
                orderModel.tradeDesc = order.TradeDesc;
                orderModel.itemName = order.ItemName;
                orderModel.customer = JSON.stringify(order.Customer);
                orderModel.payStatus = CDBMgr.PayStatus.unPay;
                orderModel.createDate = new Date();
                orderModel.options = "";
                orderModel.save(function (err) {
                    if (!err) {
                        resolve(orderModel.merTradeID);
                    }
                    else {
                        // don't use throw new Error because it's callback
                        reject(err);
                    }
                });
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };

    getOrder(tradeId) {
        return new Promise((resolve, reject) => {
            try {
                const query = OrderModel.find({ merTradeID: tradeId });
                // selecting the 'name' and 'age' fields
                query.select("merTradeID merUserID merUserAccount amount customer payStatus");
                // execute the query at a later time
                query.exec(function (err, athletes) {
                    if (!err) {
                        const list = [];
                        for (let i = 0; i < athletes.length; i++) {
                            const data = athletes[i];
                            list.push({
                                id: data.id,
                                merTradeID: data.merTradeID,
                                merUserID: data.merUserID,
                                merUserAccount: data.merUserAccount,
                                amount: data.amount,
                                customer: data.customer,
                                payStatus: data.payStatus,
                                options: data.options | "",
                            });
                        }
                        resolve(list.length > 0 ? list[0] : null);
                    }
                    else {
                        // don't use throw new Error because it's callback
                        reject(err);
                    }
                });
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };

    changePayStatus(
        tradeId,
        newPayStatus,
        option
    ) {
        const oInstance = this;
        return new Promise(async (resolve, reject) => {
            try {
                const orderInfo = await oInstance.getOrder(tradeId);
                if (!orderInfo) throw new Error(`wrong length for order ${tradeId}`);
                if (orderInfo.payStatus === newPayStatus) throw new Error("payStatus not changed");
                if (orderInfo.payStatus === CDBMgr.PayStatus.paied) throw new Error(`${tradeId} is already paied`);
                // should be only one
                // update pay status
                const updatedData = { payStatus: newPayStatus };
                if (option) {
                    const options = orderInfo.options ? JSON.parse(orderInfo.options) : [];
                    options.push(option);
                    updatedData.options = JSON.stringify(options);
                }
                OrderModel.findByIdAndUpdate(orderInfo.id, updatedData).exec();
                resolve("OK");
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };

    log(err, details) {
        return new Promise((resolve, reject) => {
            if (!details) details = "";
            // Create an instance of model OrderModel
            const logModel = new LogModel();
            logModel.err = err;
            logModel.details =
            typeof details === "object" ? JSON.stringify(details) : details;
            logModel.createDate = new Date();
            logModel.save(function (err) {
                if (!err) {
                    resolve("OK");
                }
                else {
                    // don't use throw new Error because it's callback
                    reject(err);
                }
            });
        });
    };

    createTicket(
        serialNo,
        ticketCode,
        ticketPrice,
        tradeId,
        userID,
        userAccount
    ) {
        const oInstance = this;
        return new Promise(async (resolve, reject) => {
            try {
                ticketPrice = parseInt(ticketPrice.toString());
                if (!serialNo) throw new Error("null serialNo");
                if (!ticketCode) throw new Error("null ticketCode");
                if (isNaN(ticketPrice) || ticketPrice < 0)
                throw new Error("invalid ticketPrice");
                if (!tradeId) throw new Error("null tradeId");
                if (!userAccount) throw new Error("null userAccount");

                const orderInfo = await oInstance.getOrder(tradeId);
                if (!orderInfo) throw new Error(`wrong length for order ${tradeId}`);
                if (orderInfo.payStatus === newPayStatus)
                throw new Error("payStatus not changed");
                if (orderInfo.payStatus === CDBMgr.PayStatus.paied)
                throw new Error(`${tradeId} is already paied`);
                // should be only one
                // find if there is ticket already added
                const ticket = await oInstance.getTicket(serialNo);
                if (!ticket) {
                    // Create an instance of model TicketModel
                    let ticketModel = new TicketModel();
                    ticketModel.serialNo = serialNo;
                    ticketModel.password = Utils.getRandomCode(6);
                    ticketModel.ticketCode = ticketCode;
                    ticketModel.ticketPrice = ticketPrice;
                    ticketModel.tradeId = tradeId;
                    ticketModel.userId = userID;
                    ticketModel.userAccount = userAccount;
                    ticketModel.createDate = new Date();
                    ticketModel.consumeDate = new Date(0);
                    ticketModel.save(function (err) {
                        if (!err) {
                            resolve(ticketModel);
                        }
                        else {
                            // don't use throw new Error because it's callback
                            reject(err);
                        }
                    });
                }
                else {
                // reset ticket
                const updatedData = {
                    serialNo: serialNo,
                    password: Utils.getRandomCode(6),
                    ticketCode: ticketCode,
                    ticketPrice: ticketPrice,
                    tradeId: tradeId,
                    userId: userId,
                    userAccount: userAccount,
                    createDate: new Date(),
                    consumeDate: new Date(0),
                };
                TicketModel.findByIdAndUpdate(ticket.id, updatedData).exec();
                resolve(updatedData);
                }
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };

    queryTickets(srcFilters) {
        return new Promise(async (resolve, reject) => {
            try {
                const filters = {};
                if (srcFilters.hasOwnProperty("serialNo"))
                filters["serialNo"] = srcFilters["serialNo"];
                if (srcFilters.hasOwnProperty("tradeId"))
                filters["tradeId"] = srcFilters["tradeId"];
                if (srcFilters.hasOwnProperty("userId"))
                filters["userId"] = srcFilters["userId"];
                if (srcFilters.hasOwnProperty("userAccount"))
                filters["userAccount"] = srcFilters["userAccount"];
                if (Object.keys(filters).length === 0)
                throw new Error("no filter assigned");
                // find with filter
                const query = TicketModel.find(filters);
                // selecting the 'name' and 'age' fields
                query.select("serialNo password ticketCode ticketPrice tradeId userId userAccount createDate consumeDate");
                // execute the query at a later time
                query.exec(function (err, athletes) {
                    if (!err) {
                        const list = [];
                        for (let i = 0; i < athletes.length; i++) {
                            const data = athletes[i];
                            list.push({
                                id: data.id,
                                serialNo: data.serialNo,
                                password: data.password,
                                ticketCode: data.ticketCode,
                                ticketPrice: data.ticketPrice,
                                tradeId: data.tradeId,
                                userId: data.userId,
                                userAccount: data.userAccount,
                                createDate: data.createDate,
                                consumeDate: data.consumeDate,
                            });
                        }
                        resolve(list);
                    }
                    else {
                        // don't use throw new Error because it's callback
                        reject(err);
                    }
                });
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };

    getTicket(serialNo) {
        return new Promise((resolve, reject) => {
            try {
                let query = TicketModel.find({ serialNo: serialNo });
                // selecting the 'name' and 'age' fields
                query.select("serialNo password ticketCode ticketPrice tradeId userId userAccount createDate consumeDate");
                // limit our results to 5 items
                query.limit(5);
                // execute the query at a later time
                query.exec(function (err, athletes) {
                    if (!err) {
                        const list = [];
                        for (let i = 0; i < athletes.length; i++) {
                            const data = athletes[i];
                            list.push({
                                id: data.id,
                                serialNo: data.serialNo,
                                password: data.password,
                                ticketCode: data.ticketCode,
                                ticketPrice: data.ticketPrice,
                                tradeId: data.tradeId,
                                userId: data.userId,
                                userAccount: data.userAccount,
                                createDate: data.createDate,
                                consumeDate: data.consumeDate,
                            });
                        }
                        resolve(list.length > 0 ? list[0] : null);
                    }
                    else {
                        // don't use throw new Error because it's callback
                        reject(err);
                    }
                });
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };

    consumeTicket(serialNo, password) {
        const oInstance = this;
        return new Promise(async (resolve, reject) => {
            try {
                const ticket = await oInstance.getTicket(serialNo);
                if (!ticket) throw new Error("no matched ticket found");
                if (ticket.password !== password) throw new Error(`wrong password`);
                const consumeDate = new Date(ticket.consumeDate);
                if (consumeDate.getTime() !== 0) throw new Error(`ticket ${serialNo} already been consumed`);
                // update pay status
                const updatedData = { consumeDate: new Date() };
                TicketModel.findByIdAndUpdate(ticket.id, updatedData).exec();
                resolve();
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };

    getOrderBySearch(_query) {
        return new Promise((resolve, reject) => {
            try {
                function extractIPv4(ipv6Address) {
                    if (!ipv6Address) {
                        return ""; // 或者返回一個默認值，比如空字符串
                    }

                    const match = ipv6Address.match(/::ffff:(\d+\.\d+\.\d+\.\d+)/);
                    return match ? match[1] : ipv6Address;
                }

                const { startDate, endDate, account, status } = _query;
                const query = OrderModel.find();

                query.where("createDate").gte(startDate).lte(endDate);

                if (account) {
                    query.where("merUserAccount").regex(new RegExp(account, "i"));
                }

                if (status) {
                    query.where("payStatus").equals(status);
                }

                query.sort({ createDate: -1 });
                query.select("createDate merTradeID merUserID merUserAccount amount customer payStatus remoteIP choosePayment");

                query.exec(function (err, athletes) {
                    if (!err) {
                        const list = [];
                        let allAmount = 0;

                        for (let i = 0; i < athletes.length; i++) {
                            const data = athletes[i];

                            const { IP, orderParams } = JSON.parse(data.customer);

                            if (data.payStatus === "已付款") {
                                allAmount += parseInt(data.amount);
                            }

                            list.push({
                                id: data.id,
                                merTradeID: data.merTradeID,
                                merUserID: data.merUserID,
                                merUserAccount: data.merUserAccount,
                                amount: data.amount,
                                customer: data.customer,
                                payStatus: data.payStatus,
                                paymentType: orderParams ? orderParams.ChoosePayment : data.choosePayment || "未紀錄",
                                ip: IP ? extractIPv4(IP) : extractIPv4(data.remoteIP) || "未紀錄",
                                options: data.options || "未紀錄", // 使用 || 確保空值處理
                                createDate: data.createDate,
                            });
                        }
                        resolve({ list, allAmount }); // 返回所有資料
                    }
                    else {
                        reject(err);
                    }
                });
            }
            catch (err) {
                reject(err.message ? err.message : err);
            }
        });
    };
}
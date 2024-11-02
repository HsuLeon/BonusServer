import amqp from "amqplib";

export default class RabbitMQManager
{
    constructor()
    {
        this.connection = null;
        this.channel = null;
        this.callbacks = {};
    }

    static #mInstance = null;

    static getInstance()
    {
        if (!RabbitMQManager.#mInstance) {
            RabbitMQManager.#mInstance = new RabbitMQManager();
        }
        return RabbitMQManager.#mInstance;
    }

    isConnected()
    {
        return this.connection !== null && this.channel !== null;
    }

    getChannel() {
        if (!this.channel) {
            throw new Error("RabbitMQ channel is not initialized");
        }
        return this.channel;
    }

    async createConnection(url, user, pwd)
    {
        if (!url || !user || !pwd) {
            throw new Error("Invalid RabbitMQ connection parameters");
        }

        if (this.isConnected()) {
            console.log("RabbitMQ connection already exists");
            return true;
        }

        try {
            this.connection = await amqp.connect(`amqp://${user}:${pwd}@${url}`);
            this.channel = await this.connection.createChannel();
            console.log("RabbitMQ connection established successfully");
            return true;
        }
        catch (error) {
            console.error(`RabbitMQ connection error: ${error.message}`);
            throw new Error(error);
        }
    }

    async closeConnection()
    {
        if (this.channel) {
            await this.channel.close();
        }
        if (this.connection) {
            await this.connection.close();
        }
        this.channel = null;
        this.connection = null;
    }

    async publish(queueName, message)
    {
        const channel = this.getChannel();

        if (!queueName || typeof queueName !== "string") {
            throw new Error("Invalid queue name");
        }
        if (!message || typeof message !== "string") {
            throw new Error("Invalid message");
        }

        try {
            await channel.assertQueue(queueName, { durable: false });
            channel.sendToQueue(queueName, Buffer.from(message));
            console.log(`Message sent to queue ${queueName}: ${message}`);
        }
        catch (error) {
            console.error(
                `Error sending message to queue ${queueName}:`,
                error.message
            );
            throw new Error(error);
        }
    }

    async setQueueCallback(queueName, callback)
    {
        const channel = this.getChannel();

        if (!queueName || typeof queueName !== "string") {
            throw new Error("Invalid queue name");
        }
        if (typeof callback !== "function") {
            throw new Error("Callback must be a function");
        }

        try {
            await channel.assertQueue(queueName, { durable: false });
            this.callbacks[queueName] = callback;

            await channel.consume(queueName, (msg) => {
                if (msg !== null) {
                    const messageContent = msg.content.toString();
                    const handler = this.callbacks[queueName];
                    if (handler) {
                        try {
                            handler(messageContent);
                        }
                        catch (error) {
                            console.error(`Error in callback for queue ${queueName}:`, error);
                        }
                    }
                    channel.ack(msg);
                }
            });
            console.log(`Consumer set up successfully for queue: ${queueName}`);
        }
        catch (error) {
            console.error(
                `Error setting up consumer for ${queueName}:`,
                error.message
            );
            throw new Error(error);
        }
    }
}

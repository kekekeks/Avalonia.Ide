import * as net from 'net';
import { PreviewerControlUplink } from "./avaloniaRemote/bsonUplink";

export function listenForTcpClient(port: number): Promise<PreviewerControlUplink> {
    return new Promise<PreviewerControlUplink>((resolve) => {
        var server = new net.Server();
        server.listen(port, "::ffff:127.0.0.1");
        server.on('connection', async function (client) {
            server.close();
            const uplink = new PreviewerControlUplink(client);
            resolve(uplink);
            await uplink.startReading();
        });
        server.on('error', error => console.log("GOT" + error));
    });
};
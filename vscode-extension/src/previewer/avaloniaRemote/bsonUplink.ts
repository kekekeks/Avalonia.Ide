import { PromiseDuplex } from "promise-duplex";
import { Duplex } from "stream";
import { Observable, Subject } from "rxjs";
import { Guid, fromBytes, toBytes } from "../guid";
import { Disposable } from "vscode-languageclient";
import { deserialize, serialize } from "./bsonSerializer";
import { getAvaloniaRemoteMessageGuid } from "./avaloniaRemoteTypeRegistry";
import * as net from 'net';
import PromiseSocket from "promise-socket";

export class PreviewerControlUplink implements Disposable {

    _stream: PromiseDuplex<Duplex>;
    _messages = new Subject<object>();
    _errors = new Subject<Error>();
    private _writerIsBroken: boolean = false;
    private _socket: net.Socket;

    public message(): Observable<object> {
        return this._messages;
    }

    constructor(socket: net.Socket) {
        this._stream = new PromiseSocket(socket);
        this._socket = socket;


        this.message().subscribe(msg => {
            console.log("Got new message: " + JSON.stringify(msg));
        });


    }

    public async startReading() {
        try {
            while (true) {
                var infoBlock = Buffer.alloc(20);
                await this.readExact(infoBlock);
                const length = infoBlock.readUInt16LE(0);
                let uuid: Guid = fromBytes(infoBlock, 4);

                const buffer = Buffer.alloc(length);
                await this.readExact(buffer);

                const obj = deserialize(uuid, buffer);
                this._messages.next(obj);
            }
        }
        catch (error) {
            this._errors.next(error);
        }
    }

    public async send(object: object): Promise<void> {
        if (this._writerIsBroken) {//Ignore further calls, since there is no point of writing to "broken" stream
            return;
        }

            let serialized = serialize(object);
            let output = Buffer.alloc(serialized.length + 20);
            const guidBuffer = Buffer.from(toBytes(getAvaloniaRemoteMessageGuid(object)));

                output.writeInt32LE(serialized.length, 0);
                guidBuffer.copy(output, 4);
                serialized.copy(output, 20);
            try {
                // Write synchronously to file, otherwise we would have to synchronize this block
                this._socket.write(output);
            }
            catch (e) {
                this._writerIsBroken = true;
                this._errors.next(e);
            }
    }

    private async readExact(buffer: Buffer) {
        let readAlready = 0;
        while (readAlready < buffer.length) {
            
            
            const result = <Buffer>await this._stream.read(buffer.length - readAlready);
            if (result === undefined) {
                throw Error("EOF");
            }
            result.copy(buffer, readAlready, 0);
            readAlready += result.length;
        }
    }

    dispose(): void {
        this._messages.complete();
        this._errors.complete();
        this._stream.destroy();
    }
}
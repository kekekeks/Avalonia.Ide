import * as vscode from 'vscode';
import * as net from 'net';
import { normalize, join } from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions, StreamInfo, NotificationType, RequestType } from 'vscode-languageclient';
import { ChildProcess, spawn } from 'child_process';
import { Disposable, Trace } from 'vscode-jsonrpc';


interface IAvaloniaServerInfo {
    webBaseUri: string;
}
const avaloniaServerInfoNotification: NotificationType<IAvaloniaServerInfo, any> = new NotificationType('avalonia/serverInfo');
const avaloniaServerInfoRequest: RequestType<any, any, any, any> = new RequestType('avalonia/getServerInfoRequest');

export let serverInfo: Promise<IAvaloniaServerInfo>;

let serverProcess: ChildProcess;

function connectToTcp(): ServerOptions {
    const serverOptions: ServerOptions = function () {
        console.log("Connection initiated");
        return new Promise((resolve, _reject) => {
            console.log("Connecting");
            var client = new net.Socket();
            client.connect(26001, "127.0.0.1", function () {
                console.log("Connected");
                let nfo: StreamInfo =
                {
                    reader: client,
                    writer: client
                };
                resolve(nfo);
            });
        });
    };
    return serverOptions;
}

function startServer(): Disposable {
    let runFromSource = true;
    let dotnetArgs;
    if (runFromSource) {
        // for dev purposes, later it can be removed
        let serverBin = normalize(join(__dirname, "..", "..", "..", "src/Avalonia.Ide.LanguageServer/bin/Debug/netcoreapp2.1/Avalonia.Ide.LanguageServer.dll"));
        dotnetArgs = ["exec", serverBin];
    }
    else {
        // NOTE: binary must be in extensions folder "vscode-extension" , which will be compressed by 'vsce package'
        let serverBin = normalize(join(__dirname, "..", "bin/vscode-lsp-proxy.dll"));
        dotnetArgs = [serverBin];
    }

    serverProcess = spawn("dotnet", dotnetArgs, { stdio: ["pipe", "pipe", "pipe"], shell: false });
    return Disposable.create(() =>
        // NOTE: need to close gracefully: - send message, wait, optionally kill
        serverProcess?.kill());
}


export function startLspClient(): [Promise<LanguageClient>, Disposable] {

    let clientProcess: Disposable;
    const serverProcess = startServer();
    let languageClient: Promise<LanguageClient>; 
    try {

        let clientOptions: LanguageClientOptions = {
            documentSelector: [
                {
                    language: 'xaml'
                }
            ],
            synchronize: {
                // Synchronize the setting section 'languageServerExample' to the server
                configurationSection: 'avaloniaXaml',
                fileEvents: vscode.workspace.createFileSystemWatcher('**/*.xaml')
            },
        };

        let opts = connectToTcp();

        languageClient = new Promise((resolveClient, rejectClient) => {

            try {
                const client = new LanguageClient('avaloniaXaml', 'Avalonia', opts, clientOptions);
                client.trace = Trace.Verbose;
                clientProcess = client.start();
                serverInfo = new Promise((resolve, _) => {
                    client.onReady().then(() => {
                        client.onNotification(avaloniaServerInfoNotification, info => {
                            console.log("Obtained avalonia server info - " + info.webBaseUri);
                            resolve(info);
                        });
                        client.sendRequest(avaloniaServerInfoRequest, {});
                        resolveClient(client);
                    });
                });
            }
            catch (error) {
                rejectClient(error);
            }
        });
    }
    catch (e) {
        serverProcess.dispose();
        throw e;
    }

    return [languageClient, Disposable.create(() => {
        serverProcess.dispose();
        clientProcess.dispose();
    })];
}
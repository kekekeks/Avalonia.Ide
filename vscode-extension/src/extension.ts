import * as vscode from 'vscode';
import * as net from 'net';
import {normalize, join} from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions, StreamInfo,
     NotificationType, RequestType } from 'vscode-languageclient';
import { Trace } from 'vscode-jsonrpc';
import { ChildProcess, spawn } from 'child_process';

interface IAvaloniaServerInfo
{
    webBaseUri : string;
}

const avaloniaServerInfoNotification: NotificationType<IAvaloniaServerInfo, any> = new NotificationType('avalonia/serverInfo');
const avaloniaServerInfoRequest: RequestType<any, any, any, any> = new RequestType('avalonia/getServerInfo');

export let serverInfo : Promise<IAvaloniaServerInfo>;

let serverProcess: ChildProcess;

function connectToTcp() :  ServerOptions
{
    const serverOptions: ServerOptions = function() {
        console.log("Connection initiated");
		return new Promise((resolve, _reject) => {
            console.log("Connecting");
			var client = new net.Socket();
			client.connect(26001, "127.0.0.1", function() {
                console.log("Connected");
                let nfo : StreamInfo =                  
                {
                    reader: client,
                    writer: client
                };
				resolve(nfo);
			});
		});
    }
    return serverOptions;
}

function startServer() : void
{
    // NOTE: binary must be in extensions folder "vscode-extension" , which will be compressed by 'vsce package'
    let serverBin = normalize(join(__dirname, "..","bin/vscode-lsp-proxy.dll"));

    //if (process.platform === "win32")
    // NOTE: dotnetcore (target version of dll) required, running dll via "dotnet" will work on all platforms
    serverProcess = spawn("dotnet", [serverBin], { stdio: ["pipe", "pipe", "pipe"], shell: false});
}

export function activate(context: vscode.ExtensionContext) 
{
    startServer();

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
    }

    let opts = connectToTcp();
    
    const client = new LanguageClient('avaloniaXaml', 'Avalonia', opts, clientOptions);

    client.trace = Trace.Verbose;
    let disposable = client.start();
    serverInfo = new Promise((resolve, _) => {
        client.onReady().then(() => {
            client.onNotification(avaloniaServerInfoNotification, info => {
                console.log("Obtained avalonia server info - " + info.webBaseUri);
                resolve(info);
            });
            client.sendRequest(avaloniaServerInfoRequest, {});
        });
    });
    
    context.subscriptions.push(disposable);
}

export function deactivate() {
    if (serverProcess !== undefined) {
        // NOTE: need to close gracefully: - send message, wait, optionally kill
        serverProcess.kill();
    }
}
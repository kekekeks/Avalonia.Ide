'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';


import * as net from 'net';

import * as path from 'path';

import { workspace, Disposable, ExtensionContext } from 'vscode';
import { LanguageClient, LanguageClientOptions, SettingMonitor, ServerOptions, TransportKind, InitializeParams, StreamInfo,
     NotificationType, RequestType } from 'vscode-languageclient';
import { Trace } from 'vscode-jsonrpc';

interface IAvaloniaServerInfo
{
    webBaseUri : string;
}

const avaloniaServerInfoNotification: NotificationType<IAvaloniaServerInfo, any> = new NotificationType('avalonia/serverInfo');
const avaloniaServerInfoRequest: RequestType<any, any, any, any> = new RequestType('avalonia/getServerInfo');

export let serverInfo : Promise<IAvaloniaServerInfo>;

function connectToTcp() :  ServerOptions
{
    const serverOptions: ServerOptions = function() {
        console.log("Connection initiated");
		return new Promise((resolve, reject) => {
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

function startServer() : ServerOptions
{
    //TODO
    let serverExe = "/home/kekekeks/bin/vscode-lsp-proxy";
    let serverOptions: ServerOptions = {
        run: { command: serverExe, args: [''] },
        debug: { command: serverExe, args: [''] },
    }
    return serverOptions;
}

export function activate(context: vscode.ExtensionContext) 
{
    let clientOptions: LanguageClientOptions = {
        documentSelector: [
            {
                language: 'xaml'
            }
        ],
        synchronize: {
            // Synchronize the setting section 'languageServerExample' to the server
            configurationSection: 'avaloniaXaml',
            fileEvents: workspace.createFileSystemWatcher('**/*.xaml')
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

// this method is called when your extension is deactivated
export function deactivate() {
}
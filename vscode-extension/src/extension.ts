import * as net from 'net';
import * as path from 'path';
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient';
import { Trace } from 'vscode-jsonrpc';
import { ChildProcess, spawn } from 'child_process';

import { IAvaloniaServerInfo, avaloniaServerInfoNotification, avaloniaServerInfoRequest } from './types';

export const languageId: string = 'xml';
export const serverPort: number = 26001;

export let serverInfo: IAvaloniaServerInfo;
export let spawnedProcess: ChildProcess;


export function activate(context: vscode.ExtensionContext) {
    const logWriter = t => console.log(`${t}`);

    const clientOptions: LanguageClientOptions = {
        documentSelector: [
            {
                language: 'xml',
                pattern: '*.xaml'
            }
        ],
        synchronize: {
            configurationSection: 'avaloniaXaml',
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.xaml')
        },
    };

    const serverOptions: ServerOptions = () => {
        return new Promise((resolve, reject) => {
            logWriter("Connecting");
            let client = new net.Socket();
            client.connect(serverPort, "127.0.0.1", () => resolve({ reader: client, writer: client }));
            client.on("error", (err) => {
                logWriter(err);
                reject(err);
            });
        });
    };

    const startServer: Function = () => {
        const serverExe = "dotnet";

        // TODO: build the file first with msbuild, dll will be packed to *.vsix (run "npm pack" to copy automatically)
        // If debugging in Visual Studio 2017, this will show log but allow to continue 
        const serverDllPath = path.normalize(path.join(__dirname, "..", "lsp_binaries/Avalonia.Ide.LanguageServer.dll"));

        spawnedProcess = spawn(serverExe, [serverDllPath], { stdio: ["pipe", "pipe", "pipe"], shell: true });
        spawnedProcess.on("error", logWriter);
        spawnedProcess.on("exit", logWriter);
        spawnedProcess.stdout.on("data", logWriter);
        spawnedProcess.stderr.on("data", logWriter);
    };

    startServer();

    const client = new LanguageClient('avaloniaXaml', 'Avalonia', serverOptions, clientOptions);

    client.trace = Trace.Verbose;

    // NOTE: https://microsoft.github.io/language-server-protocol/specification
    let disposable = client.start();
    console.log(`Checking server availability`);
    client.onReady().then(() => {
        client.onNotification(avaloniaServerInfoNotification, info => {
            console.log("Obtained avalonia server info - " + info.webBaseUri);
            serverInfo = info;
        });
        client.sendRequest(avaloniaServerInfoRequest, {});
    });

    context.subscriptions.push(disposable);
}

export function deactivate() {
    // TODO
    spawnedProcess.kill();
}
import * as net from 'net';
import * as path from 'path';
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient';
import { Trace } from 'vscode-jsonrpc';
import { ChildProcess, spawn } from 'child_process';

import { IAvaloniaServerInfo, avaloniaServerInfoNotification, avaloniaServerInfoRequest } from './types';

export const languageId: string = 'xml';
export const serverPort: number = 26001;
const dotnetExe = "dotnet";
const serverPublishPath = path.normalize(path.join(__dirname, "..", "languageserver/bin/Release/netcoreapp2.1/publish"));
const serverProjectPath = path.normalize(path.join(__dirname, "..", "languageserver"));

export let serverInfo: IAvaloniaServerInfo;
export let spawnedProcess: ChildProcess;


export async function activate(context: vscode.ExtensionContext) {
    try {
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

        // TODO: handle server folder corruption or dotnet update 
        var fs = require('fs');
        if (!fs.existsSync(serverPublishPath)) {
            console.log(`Building language server`);
            await buildServer();
        }
        
        console.log(`Starting language server`);
        await startServer();

        const client = new LanguageClient('avaloniaXaml', 'Avalonia', serverOptions, clientOptions);

        client.trace = Trace.Verbose;

        // NOTE: https://microsoft.github.io/language-server-protocol/specification
        let disposable = client.start();
        console.log(`Checking server availability`);
        await client.onReady();
        client.onNotification(avaloniaServerInfoNotification, info => {
            console.log("Obtained avalonia server info - " + info.webBaseUri);
            serverInfo = info;
        });
        client.sendRequest(avaloniaServerInfoRequest, {});

        context.subscriptions.push(disposable);
    }
    catch (err) {
        vscode.window.showErrorMessage(err);
    }
}

export async function deactivate() {
    // TODO
    spawnedProcess.kill();
}

const logWriter = t => console.log(`${t}`);

const buildServer: Function = async () => {
    await new Promise<boolean>((resolve, reject) => {
        let publishProcess = spawn(dotnetExe, ["publish", "-c", "Release"],
            {
                cwd: serverProjectPath,
                stdio: ["pipe", "pipe", "pipe"],
                shell: true
            });
        publishProcess.on("error", (err) => reject(err));
        publishProcess.on("exit", (code) => resolve(code === 0));
        publishProcess.stdout.on("data", logWriter);
        publishProcess.stderr.on("data", logWriter);
    });
};

const startServer: Function = async () => {
    await new Promise((resolve) => {
        spawnedProcess = spawn(dotnetExe, ["languageserver.dll"],
            {
                cwd: serverPublishPath,
                stdio: ["pipe", "pipe", "pipe"],
                shell: true
            });
        spawnedProcess.on("error", logWriter);
        spawnedProcess.on("exit", logWriter);
        spawnedProcess.stdout.on("data", logWriter);
        spawnedProcess.stderr.on("data", logWriter);

        setTimeout(()=> resolve(), 500);
    });
};
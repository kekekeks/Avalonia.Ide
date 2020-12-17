
import { Disposable } from 'vscode-languageclient';
import { ChildProcess, spawn } from 'child_process';
import { assert } from 'console';
import * as path from 'path';
import getPort = require('get-port');
import { listenForTcpClient } from './previewerConnection';
import { UpdateXamlMessage, UpdateXamlResultMessage } from './avaloniaRemote/messages/designMessages';
import { PreviewerControlUplink } from './avaloniaRemote/bsonUplink';
import { serialize } from './avaloniaRemote/bsonSerializer';

export class PreviewerProcess implements Disposable {
    updateXaml(newText: string) {
        const assemblyPath = <string>process.env.AvaloniaPreviewerAppPath;
        const msg = new UpdateXamlMessage();
        msg.Xaml = newText;
        msg.AssemblyPath = assemblyPath;
        this._previewerControl?.send(msg);
    }

    private _serverProcess: ChildProcess | undefined;
    private _previewerControl: PreviewerControlUplink | undefined;

    public async start(assemblyPath: string, previewerPath: string) {
        assert(previewerPath.endsWith(".dll"));
        assert(assemblyPath.endsWith(".dll"));

        const htmlPort = await getPort({ port: getPort.makeRange(55001, 65000) });
        const htmlHost = "http://127.0.0.1:" + htmlPort;

        const previewerPort = await getPort({ port: getPort.makeRange(45000, 55000) });
        const previewerHost = "127.0.0.1:" + previewerPort;

        console.log("Starting previewer " + htmlHost + " " + previewerHost);

        const previewerConnectionPromise = listenForTcpClient(previewerPort);
        this.startServerProcess(assemblyPath, previewerPath, previewerHost, htmlHost);

        const previewerConnection = await previewerConnectionPromise;
        this._previewerControl = previewerConnection;
        previewerConnection._errors.subscribe(error => "ERROR: " + console.log(error));

        return htmlHost;
    }

    private startServerProcess(appPath: string, previewerPath: string, previewerHost: string, htmlHost: string) {
        const workDir = path.dirname(appPath);
        const targetName = path.basename(appPath, path.extname(appPath));
        const deps = path.join(workDir, targetName + ".deps.json");
        const runtimeConfig = path.join(workDir, targetName + ".runtimeconfig.json");


        const excArgs = ["exec", "--depsfile", deps, "--runtimeconfig", runtimeConfig, previewerPath];
        const appArgs = ["--method", "html", "--transport", "tcp-bson://" + previewerHost, "--html-url", htmlHost, appPath];


        console.log("Starting dotnet");


        console.log("Running");
        console.log("dotnet " + excArgs.concat(appArgs).join(" "));
        console.log("at");
        console.log(workDir);

        const process = spawn("dotnet", excArgs.concat(appArgs), { stdio: ["pipe", "pipe", "pipe"], shell: false, cwd: workDir });
        this._serverProcess = process;

        console.log("Started process " + process.pid);

        process.stdout.on('data', function (data) {
            console.log("PRV: " + data.toString());
        });

        process.on('data', function (data) {
            console.log("PRV: " + data.toString());
        });
    }

    dispose(): void {
        this._serverProcess?.kill();
        this._previewerControl?.dispose();
    }
}

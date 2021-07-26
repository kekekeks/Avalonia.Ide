import { CancellationToken, CustomTextEditorProvider, TextDocument, Uri, WebviewPanel } from "vscode";
import * as vscode from 'vscode';
import { PreviewerProcess } from "../previewer/previewerProcess";
import { IAssemblyForXamlProvider as AssemblyForXamlProvider } from "./assemblyForXamlProvider";

export class XamlEditorProvider implements CustomTextEditorProvider{

   	private static readonly viewType = 'avalonia.xamlPreviewer';
    private _process: PreviewerProcess | undefined;

	public static register(context: vscode.ExtensionContext, assemblyProvider:AssemblyForXamlProvider): vscode.Disposable {
		const provider = new XamlEditorProvider(context, assemblyProvider);
		const providerRegistration = vscode.window.registerCustomEditorProvider(XamlEditorProvider.viewType, provider);
		return providerRegistration;
    }

	constructor(
        private readonly context: vscode.ExtensionContext,
        private readonly _assemblyProvider: AssemblyForXamlProvider
	) { }

    async resolveCustomTextEditor(document: TextDocument, webviewPanel: WebviewPanel, _token: CancellationToken): Promise<void> {
        if(document.uri.scheme !== "file"){
            webviewPanel.dispose();
        }

        const previewerProcess = new PreviewerProcess();
        this._process = previewerProcess;
        

        webviewPanel.onDidDispose(() => {
            previewerProcess.dispose();
        });
        
        let metadata = await this._assemblyProvider.getMetadataForXamlFile(document.uri.toString());
        const host = await previewerProcess.start(metadata.assemblyPath, metadata.previewerPath);
        webviewPanel.webview.options = {
            enableScripts: true,
            localResourceRoots: [Uri.parse(host)]
   		}; 
        
		this.updateXamlWhenDocumentChanges(document, previewerProcess, webviewPanel);
        webviewPanel.webview.html = `<!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <title>Previewer window</title>
                <style>
                    html { width: 100%; height: 100%; min-height: 100%; display: flex; }
                    body { flex: 1; display: flex; }
                    iframe { flex: 1; border: none; background: lime; }
                </style>
            </head>
            <body>
                <iframe src="${host}"></iframe>
            </body>
            </html>`;
        previewerProcess.updateXaml(document.getText());

        // TODO: previewer window displays nothing untill I call updateXaml
        // TODO: this should probably be fixed in previewer, it should display doucment as soon as WS connection is established
        setTimeout(() => {   
            previewerProcess.updateXaml(document.getText());
        }, 5000);
    }


    private updateXamlWhenDocumentChanges(document: TextDocument, previewerProcess: PreviewerProcess, webviewPanel: WebviewPanel) {
        const changeDocumentSubscription = vscode.workspace.onDidChangeTextDocument(e => {
            if (e.document.uri.toString() === document.uri.toString()) {
                previewerProcess.updateXaml(document.getText());
            }
        });
        webviewPanel.onDidDispose(() => {
            this.context.subscriptions.push(changeDocumentSubscription);
        });
    }
}
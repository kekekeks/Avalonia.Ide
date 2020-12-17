import { CancellationToken, CustomTextEditorProvider, TextDocument, Uri, WebviewPanel } from "vscode";
import * as vscode from 'vscode';
import { PreviewerProcess } from "../previewer/previewerProcess";

export class XamlEditorProvider implements CustomTextEditorProvider{

   	private static readonly viewType = 'avalonia.xamlPreviewer';
    private _process: PreviewerProcess | undefined;

	public static register(context: vscode.ExtensionContext): vscode.Disposable {
		const provider = new XamlEditorProvider(context);
		const providerRegistration = vscode.window.registerCustomEditorProvider(XamlEditorProvider.viewType, provider);
		return providerRegistration;
	}
     
	constructor(
		private readonly context: vscode.ExtensionContext
	) { }

    async resolveCustomTextEditor(document: TextDocument, webviewPanel: WebviewPanel, _token: CancellationToken): Promise<void> {
        const previewerProcess = new PreviewerProcess();
        this._process = previewerProcess;
         
        webviewPanel.onDidDispose(() => {
            previewerProcess.dispose();
        });


        console.log("Starting previewer process");
        
        const host = await process.start();
        // TODO: get data from language client
        let previewerPath = <string>process.env.AvaloniaPreviewerDevPath;
        let assemblyPath = <string>process.env.AvaloniaPreviewerAppPath;

        const host = await previewerProcess.start(assemblyPath, previewerPath);
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
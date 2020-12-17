import * as vscode from 'vscode';
import { XamlEditorProvider } from './editor/xamlEditorProvider';
import { startLspClient } from './lsp/lspProcess';

export function activate(context: vscode.ExtensionContext) 
{
    //context.subscriptions.push(startLspClient());
    context.subscriptions.push(XamlEditorProvider.register(context));
}

export function deactivate() {
}
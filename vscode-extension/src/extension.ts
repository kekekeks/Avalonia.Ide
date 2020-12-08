import * as vscode from 'vscode';
import { startLspClient } from './lsp/lspProcess'

export function activate(context: vscode.ExtensionContext) 
{
    const lspSubscription = startLspClient();
    context.subscriptions.push(lspSubscription);
}

export function deactivate() {
}
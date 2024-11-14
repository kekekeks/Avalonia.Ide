import * as vscode from 'vscode';
import { LanguageServierAssemblyForXamlProvider } from './editor/assemblyForXamlProvider';
import { XamlEditorProvider } from './editor/xamlEditorProvider';
import { startLspClient } from './lsp/lspProcess';

export async function activate(context: vscode.ExtensionContext) 
{
    const [clientPromise, subscription] = startLspClient();

    let client = await clientPromise;
    let assemblyProvider = new LanguageServierAssemblyForXamlProvider(client);

    context.subscriptions.push(subscription);
    context.subscriptions.push(XamlEditorProvider.register(context, assemblyProvider));
}

export function deactivate() {
}
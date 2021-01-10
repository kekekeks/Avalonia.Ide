import { LanguageClient, RequestType } from "vscode-languageclient";


class AssemblyMetadata {
    constructor(
        public readonly assemblyPath: string,
        public readonly previewerPath: string) {

    }
}

export interface IAssemblyForXamlProvider {
    getMetadataForXamlFile(xamlFilePath: string): Promise<AssemblyMetadata>;
}


interface IAvaloniaXamlInfoNotification {
    xamlFile: string;
    assemblyPath: string;
    previewerPath: string;
}

interface IAvaloniaXamlInforRequest{
    xamlFile: string;
}

const avaloniaXamlInfoRequest: RequestType<IAvaloniaXamlInforRequest, IAvaloniaXamlInfoNotification, any, any> = new RequestType('avalonia/getXamlInfoRequest');

export class LanguageServierAssemblyForXamlProvider {

    constructor( private readonly _client: LanguageClient){

    }

    public getMetadataForXamlFile(xamlFilePath: string): Promise<AssemblyMetadata> {
        let promise = new Promise<AssemblyMetadata>((resolve, reject) => {

            this._client.sendRequest(avaloniaXamlInfoRequest, {xamlFile : xamlFilePath })
            .then(notification => {
                 const metadata = new AssemblyMetadata(notification.assemblyPath, notification.previewerPath);
                 resolve(metadata);
            }, reject);
        });

        return promise;
    }
}
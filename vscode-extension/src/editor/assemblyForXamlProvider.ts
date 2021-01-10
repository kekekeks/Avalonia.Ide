import { LanguageClient, NotificationType, RequestType } from "vscode-languageclient";


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

const avaloniaXamlInfoNotification: NotificationType<IAvaloniaXamlInfoNotification, any> = new NotificationType('avalonia/xamlInfo');
const avaloniaXamlInfoRequest: RequestType<IAvaloniaXamlInforRequest, any, any, any> = new RequestType('avalonia/getXamlInfoRequest');

class RequestData {
    constructor(public resolve: (_: AssemblyMetadata) => void,
        public promise: Promise<AssemblyMetadata>) {

    }
}

export class LanguageServierAssemblyForXamlProvider {


    _pendingRequests: { [xamlPath: string]: RequestData } = {};
    constructor(private readonly _client: LanguageClient) {
        _client.onNotification(avaloniaXamlInfoNotification, notification => {
            if (notification.xamlFile in this._pendingRequests) {
                let requestData = this._pendingRequests[notification.xamlFile];
                const metadata = new AssemblyMetadata(notification.assemblyPath, notification.previewerPath);
                requestData.resolve(metadata);
                delete this._pendingRequests[notification.xamlFile];
            }
        });
    }

    public getMetadataForXamlFile(xamlFilePath: string): Promise<AssemblyMetadata> {

        if (xamlFilePath in this._pendingRequests) {
            let promise = this._pendingRequests[xamlFilePath].promise;
            this._client.sendRequest(avaloniaXamlInfoRequest, {xamlFile : xamlFilePath });
            return promise;
        }

        let resolveFunc: (_: AssemblyMetadata) => void = null!;
        let promise = new Promise<AssemblyMetadata>((resolve, _) => {
            resolveFunc = resolve;
        });
        this._pendingRequests[xamlFilePath] = new RequestData(resolveFunc, promise);
        this._client.sendRequest(avaloniaXamlInfoRequest, {xamlFile : xamlFilePath });
        return promise;
    }
}
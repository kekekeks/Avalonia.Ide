import { RequestType, NotificationType } from "vscode-jsonrpc";

export interface IAvaloniaServerInfo {
    webBaseUri: string;
}

export const avaloniaServerInfoNotification: NotificationType<IAvaloniaServerInfo, any> = new NotificationType('avalonia/serverInfo');
export const avaloniaServerInfoRequest: RequestType<any, any, any, any> = new RequestType('avalonia/getServerInfo');

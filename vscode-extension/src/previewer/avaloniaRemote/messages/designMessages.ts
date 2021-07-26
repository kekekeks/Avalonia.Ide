/* eslint-disable @typescript-eslint/naming-convention */
import { avaloniaRemoteMessageGuid } from "../avaloniaRemoteTypeRegistry";

@avaloniaRemoteMessageGuid("9AEC9A2E-6315-4066-B4BA-E9A9EFD0F8CC")
export class UpdateXamlMessage
{
    public Xaml: string = "";
    public AssemblyPath: string = "";
    public XamlFileProjectPath: string = "";
}

@avaloniaRemoteMessageGuid("B7A70093-0C5D-47FD-9261-22086D43A2E2")
export class UpdateXamlResultMessage
{
    public Error: string = "";
    public Handle: string = "";
    public Exception: ExceptionDetails | undefined;
}

@avaloniaRemoteMessageGuid("854887CF-2694-4EB6-B499-7461B6FB96C7")
export class StartDesignerSessionMessage
{
    public SessionId: string = "";
}

export class ExceptionDetails
{
    public ExceptionType: string = "";
    public Message: string = "";
    public LineNumber: number | undefined;
    public LinePosition: number | undefined;
}

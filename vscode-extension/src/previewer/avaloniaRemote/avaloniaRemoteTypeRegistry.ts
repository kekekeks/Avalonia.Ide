import { Guid, makeGuid } from "../guid";

const guidToType = new Map<Guid, Function>();

export function getObjectConstructorForGuid(guid: Guid){
    return guidToType.get(guid);
}


export function getAvaloniaRemoteMessageGuid(target: any): Guid {
    return target.guid;
}

export function avaloniaRemoteMessageGuid(guid: string): any{
    return function(constructorFunction: new () => any) {
        guidToType.set(makeGuid(guid), constructorFunction);
        return function () {
            const obj = new constructorFunction();
            obj['guid'] = makeGuid(guid);
            return obj;
        };
    };
}

import BSON from "bson";
import { Guid } from "../guid";

export function serialize(object: any): Buffer {
    const guid = object['guid'];
    delete object['guid'];
    const serialized = Buffer.from(BSON.serialize(object));
    object['guid'] = guid;
    return serialized;
}
export function deserialize(guid: Guid, bytes: Buffer): object {
    const pojo:any = BSON.deserialize(bytes);
    pojo['guid'] = guid;
    return pojo;
}

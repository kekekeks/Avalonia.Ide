export type Guid = string & { _guidBrand: undefined };

export function makeGuid(text: string): Guid {
    return text as Guid;
}

export function fromBytes(data: Uint8Array, offset?: number) : Guid{
    if(offset === undefined){
        offset = 0;
    }
    let segment1 = bytesToHex(data, offset + 0, 4, true);
    let segment2 = bytesToHex(data, offset + 4, 2, true);
    let segment3 = bytesToHex(data, offset + 6, 2, true);
    let segment4 = bytesToHex(data, offset + 8, 2);
    let segment5 = bytesToHex(data, offset + 10, 6);

    return makeGuid(segment1 + segment2 + segment3 + segment4 + segment5);
}

export function toBytes(guid: Guid) : Uint8Array{
    let segments:Uint8Array[] = [];
    const strings = guid.split('-');
    let length = 0;
    strings.forEach((string, index) => {
        let data = (parseHexString(string));
        if(index < 3){
            data.reverse();
        }
        length += data.length;
        segments.push(data);
    });

    const result = new Uint8Array(length);
    let offset = 0;
    segments.forEach((segment) => {
        result.set(segment, offset);
        offset += segment.length;
    });
    return result;
}

function parseHexString(str:string) { 
    const data = new Uint8Array(str.length/2);
    for(let i =0 ; i < data.length; i++){
        let substr = str.substring(2*i, 2*(i + 1));
        data[i] = parseInt(substr, 16);
    }

    return data;
}

function bytesToHex(data:Uint8Array, start?:number, end?:number, reversed?: boolean){
    if(start === undefined){
        start = 0;
    }
    if(end === undefined){
        end = 0;
    }
    let result = "";
    if(!reversed){
         for(let i = start; i < end; i++){
            result += data[i].toString(16);
         }
    }
    else{
        for(let i = end-1; i >= start; i--){
            result += data[i].toString(16);
        }
    }

    return result;
}
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using Avalonia.Ide.LanguageServer.MSBuild.Requests;

namespace Avalonia.Ide.LanguageServer.MSBuild
{
    public class WireHelper
    {
        private readonly NetworkStream _stream;

        public WireHelper(NetworkStream stream)
        {
            _stream = stream;
        }

        void SendMessage(byte[] data)
        {
            var l = BitConverter.GetBytes(data.Length);
            _stream.Write(l, 0, 4);
            _stream.Write(data, 0, data.Length);
            _stream.Flush();
        }

        byte[] ReadExact(int len)
        {
            var data = new byte[len];
            int read = 0;
            while (read<len)
            {
                var rnow = _stream.Read(data, read, len - read);
                if (rnow <= 0)
                    throw new EndOfStreamException();
                read += rnow;
            }
            return data;
        }
        
        byte[] ReadMessage()
        {
            var l = ReadExact(4);
            var len = BitConverter.ToInt32(l, 0);
            return ReadExact(len);
        }
        
        public void Send(object data)
        {
            var ms = new MemoryStream();
            new DataContractSerializer(data.GetType()).WriteObject(ms, data);
            SendMessage(ms.ToArray());
        }

        public void SendRequest(object data)
        {
            Send(new NextRequestType {TypeName = data.GetType().FullName});
            Send(data);
        }

        public T Read<T>() => (T) Read(typeof(T));
        
        public object Read(Type t)
        {
            var msg = ReadMessage();
            return new DataContractSerializer(t).ReadObject(new MemoryStream(msg));
        }
    }
}
using System;
using System.IO;

namespace Avalonia.Ide.LanguageServer
{
    class ConsoleLogStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly Stream _logStream;

        public ConsoleLogStream(Stream baseStream, Stream logStream)
        {
            _baseStream = baseStream;
            _logStream = logStream;
        }
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var r = _baseStream.Read(buffer, offset, count);
            if(r>0)
                lock (_logStream)
                    _logStream.Write(buffer, offset, r);
            return r;
        }
            
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_logStream)
                _logStream.Write(buffer, offset, count);
            _baseStream.Write(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }



        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            set => throw new NotSupportedException();
            get => throw new NotSupportedException();
        }
    }

}
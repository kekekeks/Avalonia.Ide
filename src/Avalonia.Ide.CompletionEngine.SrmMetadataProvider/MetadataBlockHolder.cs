using System;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Avalonia.Ide.CompletionEngine.SrmMetadataProvider
{
    unsafe class MetadataBlockHolder : IDisposable
    {
        private IntPtr _mem;
        public MetadataReader Reader { get; private set; }

        public MetadataBlockHolder(PEMemoryBlock block)
        {
            _mem = Marshal.AllocHGlobal(block.Length);
            Buffer.MemoryCopy(block.Pointer, _mem.ToPointer(), block.Length, block.Length);
            Reader = new MetadataReader((byte*)_mem.ToPointer(), block.Length);
        }

        public void Dispose()
        {

            if (_mem != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_mem);
                _mem = IntPtr.Zero;
                Reader = null;
            }
            GC.SuppressFinalize(this);
        }

        ~MetadataBlockHolder()
        {
            Dispose();
        }
    }
}
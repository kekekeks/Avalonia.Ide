using System;
using System.Runtime.ConstrainedExecution;

namespace Avalonia.Ide.LanguageServer.Editor
{
    public interface IRef<T> : IDisposable
    {
        T Item { get; }
        IRef<T> Clone();
    }
    
    class RefCountable<T> where T : class, IDisposable
    {
        private T _item;
        private int _refs;
        private readonly object _lock = new object();

        
        public RefCountable(T item)
        {
            _item = item;
        }

        void AddRef()
        {
            lock (_lock)
            {
                _refs++;
            }
        }

        public bool IsAlive
        {
            get
            {
                lock (_lock)
                    return _refs > 0;
            }
        }

        void Release()
        {
            bool needDispose;
            lock (_lock)
            {
                if(_refs == 0)
                    throw new InvalidOperationException();
                _refs--;
                needDispose = _refs == 0;
            }
            if (needDispose)
            {
                _item.Dispose();
                _item = null;
            }
        }
        
        class Ref : CriticalFinalizerObject, IRef<T>
        {
            private RefCountable<T> _parent;
            private readonly object _lock = new object();

            public Ref(RefCountable<T> parent)
            {
                _parent = parent;
                parent.AddRef();
            }

            public void Dispose()
            {
                lock (_lock ?? new object())
                {
                    _parent?.Release();
                    _parent = null;
                    GC.SuppressFinalize(this);
                }
            }

            public T Item => _parent._item;
            public IRef<T> Clone() => new Ref(_parent);

            ~Ref()
            {
                Dispose();
            }
        }

        public IRef<T> CreateRef() => new Ref(this);
        
        public static (RefCountable<T> rc, IRef<T> r) Create(T item)
        {
            var r = new RefCountable<T>(item);
            return (r, new Ref(r));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Aspects;

namespace Celeste.Mod.AudioSplitter.Utility
{
    public class RecursionLocker
    {
        internal class Lock
        {
            internal bool IsLocked { get; set; } = false;
            public bool CanEnter => !IsLocked;

            public Lock() { }
            public IDisposable Enter() => new LockScope(this);
        }

        internal class LockScope : IDisposable
        {
            private readonly Lock _lock;

            public LockScope(Lock @lock)
            {
                this._lock = @lock;
                this._lock.IsLocked = true;
            }

            public void Dispose()
            {
                _lock.IsLocked = false;
            }
        }

        private Dictionary<object, Lock> locks = new();

        public RecursionLocker() { }

        public bool TryEnter(object obj, out IDisposable scope)
        {
            scope = null;

            Lock @lock = null;
            if (!locks.TryGetValue(obj, out @lock)) { 
                @lock = locks[obj] = new Lock();
            }

            if (!@lock.CanEnter)
                return false;
            
            scope = @lock.Enter();
            return true;
        }
    }
}

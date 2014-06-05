using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

#if DOT_NET_3_5
namespace System
#else
namespace LibGit2Sharp.Core.Compat
#endif
{
    enum LazyThreadSafetyMode
    {
        None,
        PublicationOnly,
        ExecutionAndPublication
    }

    static class Lazy
    {
        public static readonly object PublicationOnlySyncObject = new object();
    }

    [Serializable]
    [ComVisible(false)]
    [DebuggerTypeProxy(typeof(LazyDebuggerProxy<>))]
    [DebuggerDisplay("ThreadSafetyMode={Mode}, IsValueCreated={IsValueCreated}, IsValueFaulted={IsValueFaulted}, Value={ValueForDebugDisplay}")]
    class Lazy<T>
    {
        // Avoid boxing/unboxing for value types.
        [Serializable]
        private class Box
        {
            public Box(T value)
            {
                this.Value = value;
            }

            public T Value;
        }

        public bool IsValueCreated
        {
            get
            {
                return _state != null && _state is Box;
            }
        }

        // Prevent invoking via debugger.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get
            {
                if (_state != null)
                {
                    var box = _state as Box;
                    if (box != null)
                    {
                        return box.Value;
                    }

                    throw _state as Exception;
                }

                return LazyInitialize();
            }
        }

        public Lazy()
            : this(LazyThreadSafetyMode.ExecutionAndPublication)
        { }

        public Lazy(Func<T> valueFactory)
            : this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication)
        { }

        public Lazy(bool isThreadSafe)
            : this(isThreadSafe ?
                  LazyThreadSafetyMode.ExecutionAndPublication :
                  LazyThreadSafetyMode.None)
        { }

        public Lazy(LazyThreadSafetyMode mode)
        {
            _syncObject = GetSyncObjectFromMode(mode);
        }

        public Lazy(Func<T> valueFactory, bool isThreadSafe)
            : this (valueFactory, 
                  isThreadSafe ? 
                  LazyThreadSafetyMode.ExecutionAndPublication : 
                  LazyThreadSafetyMode.None)
        { }

        public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
        {
            if (valueFactory == null) throw new ArgumentNullException("valueFactory");

            _syncObject   = GetSyncObjectFromMode(mode);
            _valueFactory = valueFactory;
        }

        private T LazyInitialize()
        {
            Box box  = null;
            var mode = Mode;

            if (mode == LazyThreadSafetyMode.None)
            {
                box    = CreateValue();
                _state = box;
            }
            else if (mode == LazyThreadSafetyMode.PublicationOnly)
            {
                box = CreateValue();

                // IF (invoked by another thread OR stored by another thread)
                // THEN use the stored value
                if (box == null ||
                    Interlocked.CompareExchange(ref _state, box, null) != null)
                {
                    box = (Box)_state;
                }
                else
                {
                    // We won, release reference to the factory in case of leak.
                    _valueFactory = ALREADY_INVOKED;
                }
            }
            else
            {
                object syncObject = _syncObject;

                Thread.MemoryBarrier();

                bool lockTaken = false;
                try
                {
                    if (syncObject != (object)ALREADY_INVOKED)
                    {
                        Monitor.Enter(syncObject);
                        lockTaken = true;
                    }

                    if (_state == null)
                    {
                        box = CreateValue();
                        _state = box;

                        Thread.MemoryBarrier();

                        _syncObject = ALREADY_INVOKED;
                    }
                    else
                    {
                        box = _state as Box;
                        if (box == null) // Faulted on another thread.
                        {
                            throw _state as Exception;
                        }
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(syncObject);
                    }
                }
            }

            return box.Value;
        }

        private Box CreateValue()
        {
            Box box  = null;
            var mode = Mode;

            if (_valueFactory != null)
            {
                try
                {
                    if (mode != LazyThreadSafetyMode.PublicationOnly &&
                        _valueFactory == ALREADY_INVOKED)
                    {
                        throw new InvalidOperationException("Recursive calls to Value.");
                    }

                    var factory = _valueFactory;
                    if (mode != LazyThreadSafetyMode.PublicationOnly)
                    {
                        _valueFactory = ALREADY_INVOKED;
                    }
                    else if (factory == ALREADY_INVOKED)
                    {
                        // Already invoked by another thread.
                        return null;
                    }

                    box = new Box(factory());
                }
                catch (Exception ex)
                {
                    if (mode != LazyThreadSafetyMode.PublicationOnly)
                    {
                        _state = ex;
                    }
                    throw;
                }
            }
            else
            {
                try
                {
                    box = new Box(Activator.CreateInstance<T>());
                }
                catch (MissingMethodException)
                {
                    var ex = new MissingMemberException(
                        "No parameterless constructor of type " + typeof(T));
                    _state = ex;

                    throw ex;
                }
            }

            return box;
        }

        private object _state;
        [NonSerialized]
        private Func<T> _valueFactory;
        [NonSerialized]
        private object _syncObject;

        private static readonly Func<T> ALREADY_INVOKED = delegate
        {
            Debug.Assert(false, "This should never be invoked.");
            return default(T);
        };

        private static object GetSyncObjectFromMode(LazyThreadSafetyMode mode)
        {
            switch (mode)
            {
                case LazyThreadSafetyMode.None:
                    return null;
                case LazyThreadSafetyMode.PublicationOnly:
                    return Lazy.PublicationOnlySyncObject;
                case LazyThreadSafetyMode.ExecutionAndPublication:
                    return new object();
                default:
                    throw new ArgumentException("Invalid mode");
            }
        }

        internal T ValueForDebugDisplay
        {
            get
            {
                if (!IsValueCreated) return default(T);

                return ((Box)_state).Value;
            }
        }

        internal LazyThreadSafetyMode Mode
        {
            get
            {
                if (_syncObject == null)
                {
                    return LazyThreadSafetyMode.None;
                }
                if (_syncObject == Lazy.PublicationOnlySyncObject)
                {
                    return LazyThreadSafetyMode.PublicationOnly;
                }
                else
                {
                    return LazyThreadSafetyMode.ExecutionAndPublication;
                }
            }
        }

        internal bool IsValueFaulted
        {
            get
            {
                return _state is Exception;
            }
        }
    }

    internal sealed class LazyDebuggerProxy<T>
    {
        private readonly Lazy<T> _target;

        public LazyDebuggerProxy(Lazy<T> lazy)
        {
            _target = lazy;
        }

        public bool IsValueCreated
        {
            get { return _target.IsValueCreated; }
        }

        public T Value
        {
            get
            { return _target.ValueForDebugDisplay; }
        }

        public LazyThreadSafetyMode Mode
        {
            get { return _target.Mode; }
        }

        public bool IsValueFaulted
        {
            get { return _target.IsValueFaulted; }
        }
    }
}

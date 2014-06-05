using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if DOT_NET_3_5
namespace System
#else
namespace LibGit2Sharp.Core.Compat
#endif
{
    interface ITuple
    {
        int Size { get; }

        string ToString(StringBuilder sb);
        int GetHashCode(IEqualityComparer comparer);
    }

    [Serializable]
    class Tuple<T1, T2> : IComparable, ITuple
    {
        private readonly T1 _item1;
        private readonly T2 _item2;

        public T1 Item1 { get { return _item1; } }
        public T2 Item2 { get { return _item2; } }

        public Tuple(T1 item1, T2 item2)
        {
            _item1 = item1;
            _item2 = item2;
        }

        public int Size { get { return 2; } }

        public bool Equals(object other, IEqualityComparer comparer)
        {
            if (other == null) return false;

            var tuple = other as Tuple<T1, T2>;

            if (tuple == null) return false;

            return
                comparer.Equals(_item1, tuple._item1) &&
                comparer.Equals(_item2, tuple._item2);
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj, Comparer<object>.Default);
        }

        public int CompareTo(object other, IComparer comparer)
        {
            if (other == null) return 1;

            var tuple = other as Tuple<T1, T2>;

            if (tuple == null) throw new ArgumentException("Incorrect type.");

            int result = comparer.Compare(_item1, tuple._item1);

            if (result != 0) return result;

            return comparer.Compare(_item2, tuple._item2);
        }

        public override int GetHashCode()
        {
            return GetHashCode(EqualityComparer<object>.Default);
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return CombineHashCodes(
                comparer.GetHashCode(_item1), 
                comparer.GetHashCode(_item2));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        public string ToString(StringBuilder sb)
        {
            sb.Append(_item1);
            sb.Append(", ");
            sb.Append(_item2);
            sb.Append(")");

            return sb.ToString();
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }
    }
}

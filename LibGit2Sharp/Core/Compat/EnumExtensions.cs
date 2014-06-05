using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !DOT_NET_3_5
namespace LibGit2Sharp.Core.Compat
{
#endif
    static class EnumExtensions
    {
        public static bool HasFlag(this Enum value, Enum test)
        {
            if (test == null) return false;

            if (value == null) throw new ArgumentNullException("value");

            if (value.GetType() != test.GetType())
            {
                throw new ArgumentException("Enumeration type mismatch.");
            }

            long num = Convert.ToInt64(test);
            return ((Convert.ToInt64(value) & num) == num);
        }
    }
#if !DOT_NET_3_5
}
#endif

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardSharp
{
    public static class IEnumerableExtensions
    {
        public static List<T> ToListAndSort<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable.ToList();
            list.Sort();
            return list;
        }

        public static HashSet<T> ToSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }
    }
}
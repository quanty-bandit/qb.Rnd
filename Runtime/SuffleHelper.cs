using System;
using System.Collections.Generic;
namespace qb.Rnd
{
    public static class SuffleHelper
    {
        public static void Suffle<T>(IList<T> target)
        {
            if (target == null || target.Count <= 1) return;
            UnSafeSuffle(target, new Random());
        }
        public static void Suffle<T>(IList<T> target,int seed)
        {
            if (target == null || target.Count <= 1) return;
            UnSafeSuffle(target, new Random(seed));
        }

        public static void Suffle<T>(IList<T> target, Random random)
        {
            if (target == null || target.Count <= 1) return;
            UnSafeSuffle(target, random);
        }
        public static void UnSafeSuffle<T>(IList<T> target,Random random)
        {
            int count = target.Count;
            for (int i = count - 1; i > 0; i--)
            {
                var k = random.Next(i + 1);
                var value = target[k];
                target[k] = target[i];
                target[i] = value;
            }
        }
    }
}

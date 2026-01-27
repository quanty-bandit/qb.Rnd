using System.Collections.Generic;

namespace qb.Rnd
{
    public class SimpleRndIndexProvider
    {
        int[] indexes;
        List<int> validIndexes = new List<int>();
        int availableIndexesCount;
        System.Random random;

        public void Initialize(int count)=>Initialize(0, count-1);
        public void Initialize(int count, System.Random random) => Initialize(0, count - 1, random);
        public void Initialize(int startValue, int endValue, int randomSeed)=>Initialize(startValue, endValue, new System.Random(randomSeed));
        public void Initialize(int startValue,int endValue,System.Random random=null)
        {
            if (random == null)
            {
                random = new System.Random();   
            }
            int count,firstValue, lastValue,step;
            if(startValue < endValue)
            {
                firstValue = startValue;
                lastValue = endValue;
                step = 1;
                count = endValue - startValue + 1;
            }
            else
            {
                firstValue = endValue;
                lastValue = startValue;
                step = -1;
                count = startValue - endValue + 1;
            }
            indexes = new int[count];
            int value = firstValue;
            for (int i = 0; i < count; i++) {
                indexes[i] = firstValue;
                firstValue += step;
                validIndexes.Add(i);
            }
        }
        public void Initialize(params int[] indexValues)
        {

        }
    }
}
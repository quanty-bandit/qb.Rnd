
namespace qb.Rnd
{
    /// <summary>
    /// Provides random, non-repeating indices from a specified range,
    /// with support for restoring and resetting indices.
    /// </summary>
    public class SimpleRndIndexProvider
    {
        int[] indexes;
        int availableIndexesCount;
        System.Random random;

        /// <summary>
        /// Initializes a new instance with the specified index count 
        /// with the default random number generator used for index pop.
        /// </summary>
        /// <param name="count">The number of items to provide random indices for.</param>
        public SimpleRndIndexProvider(int count) => Initialize(count, new System.Random());
        /// <summary>
        /// Initializes a new instance with the specified index count 
        /// and random number generator used for index pop.
        /// </summary>
        /// <param name="count">The number of indices to provide.</param>
        /// <param name="random">The random number generator to use.</param>
        public SimpleRndIndexProvider(int count, System.Random random)=> Initialize(count, random);
        /// <summary>
        /// Initializes a new instance of the SimpleRndIndexProvider class 
        /// with the specified index count and a random seed for the .
        /// </summary>
        /// <param name="count">The number of indices to provide.</param>
        /// <param name="randomSeed">The seed value for the random number generator.</param>
        public SimpleRndIndexProvider(int count, int randomSeed) => Initialize(count, new System.Random(randomSeed));


        void Initialize(int count, System.Random random)
        {
            this.random = random;
            this.indexes = new int[count];
            Reset();
        }
        /// <summary>
        /// Removes and returns a random available index, updating the collection to exclude the returned index.
        /// </summary>
        /// <returns>A randomly selected available index, or -1 if no indexes are available.</returns>
        public int PopIndex()
        {
            if (availableIndexesCount == 0) return -1;
            
            int k = random.Next(0, availableIndexesCount);
            int rnd = indexes[k];

            availableIndexesCount--;
            if (availableIndexesCount > 1)
            {
                for (int i = k; i < availableIndexesCount; i++)
                {
                    indexes[i] = indexes[i + 1];
                }
                indexes[availableIndexesCount] = rnd;
            }
            return rnd;
        }

        /// <summary>
        /// Restores a previously poped index to the available indexes pool if possible.
        /// </summary>
        /// <param name="popedIndex">The index to be restored.</param>
        /// <returns>True if the index was successfully restored; otherwise, false.</returns>
        public bool RestoreIndex(int popedIndex)
        {
            if (availableIndexesCount < indexes.Length)
            {
                for(int i = availableIndexesCount; i < indexes.Length; i++)
                {
                    if(indexes[i] == popedIndex)
                    {
                        if(i>availableIndexesCount)
                        {
                            int x = indexes[availableIndexesCount];
                            indexes[availableIndexesCount] = x;
                            indexes[i] = popedIndex;
                        }
                        availableIndexesCount++;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Resets the count of available indexes to the total number of indexes.
        /// </summary>
        public void RestoreAllIndexes()
        {
            availableIndexesCount = indexes.Length;
        }   
        /// <summary>
        /// Resets the indexes array to its initial sequential state and updates the available indexes count.
        /// </summary>
        public void Reset()
        {
            int count = indexes.Length;
            for (int i = 0; i < count; i++)
                indexes[i] = i;
            availableIndexesCount = count;
        }

        /// <summary>
        /// Gets the number of available indexes.
        /// </summary>
        public int AvailableIndexesCount => availableIndexesCount;

        /// <summary>
        /// Gets the  number of indexes than the provider can deliver.
        /// </summary>
        public int TotalIndexesCount => indexes.Length;
    }
}

using System.Collections.Generic;
using UnityEngine;
using static qb.Rnd.RndIndexProvider;
#if UNITY_EDITOR
using TriInspector;
#endif
namespace qb.Rnd
{
    public class RndIndexProvider : List<IndexEntry>
    {
        #region private 
        int[] indexes;
        int[] drawCounts;
        List<int> validIndexes = new List<int>();
        int availableIndexesCount;
        System.Random random;

        void RemoveIndexFromRandomDraw(int index)
        {
            int j = 0;
            for (int i = 0; i < availableIndexesCount; i++)
            {
                if (indexes[i] != index)
                {
                    indexes[j++] = indexes[i];
                }
            }
            availableIndexesCount = j;
        }

        #endregion

        #region internal definitions

        public enum ProbabilityType 
        { 
            Constant, // The probability is constant for every random index draw
            Decrease, // The probability decrease each time the index value was drawn
            Countdown, // The probability is constant but is set to zero after the index value has been drawn a specific number of time
            DecreaseAndCountdown,// The probability decrease each time the index value was drawn and is set to zero after a specific number of time
        };

        public struct IndexEntry
        {
            public float probabilityWeight;
            public ProbabilityType type;
#if UNITY_EDITOR
            bool ShowMaxCount => type == ProbabilityType.Countdown||type == ProbabilityType.DecreaseAndCountdown;
            [ShowIf(nameof(ShowMaxCount))]
            [Min(1)]
#endif
            public int maxCount;

            public static IndexEntry Constant(float probabilityWeight = 1)
                => new IndexEntry
                {
                    type = ProbabilityType.Constant,
                    maxCount = int.MaxValue,
                    probabilityWeight = probabilityWeight
                };

            public static IndexEntry CountDownEntry(int maxCount, float probabilityWeight = 1)
                => new IndexEntry
                {
                    type = ProbabilityType.Countdown,
                    maxCount = maxCount,
                    probabilityWeight = probabilityWeight
                };
            public static IndexEntry DecreaseAndCountdown(int maxCount, float probabilityWeight = 1)
                => new IndexEntry
                {
                    type = ProbabilityType.DecreaseAndCountdown,
                    maxCount = maxCount,
                    probabilityWeight = probabilityWeight
                };
        }



        #endregion

        /// <summary>
        /// Return true if the provider public methods are ready to be isUsed
        /// </summary>
        public bool IsReadyToBeUsed => this.Count > 0 && indexes!=null && indexes.Length>0 && availableIndexesCount>0;

        /// <summary>
        /// Return true when there is no more indexes to get (GetIndex method return -1)
        /// This case can be happen when the entries list contains no entry with ProbabilityType.Constant.
        /// To reset the provider and obtain more indexes, the Reset() method must be called.
        /// </summary>
        public bool IsResetNeeded => this.Count > 0 && indexes != null && indexes.Length > 0 && availableIndexesCount == 0;

        /// <summary>
        /// Set all propabilities to be equal
        /// </summary>
        /// <returns>The common probabily</returns>
        public float EqualizeAllPropabilties()
        {
            if (this.Count == 0)
                return 0;
            for(int i = 0; i < Count; i++)
            {
                var entry = this[i];
                entry.probabilityWeight = 1;
                this[i] = entry;
            }
            Initialize();

            return GetProbabilityForIndex(0);
        }

        /// <summary>
        /// Initialize the provider random internal tables
        /// using the seed parameter to create the random generator
        /// </summary>
        /// <param name="seed">The input seed</param>
        public void Initialize(int seed)
        {
            random = new System.Random(seed);
            Initialize();
        }

        /// <summary>
        /// Initialize random indexes using the random generator parameter
        /// </summary>
        /// <param name="random">The random generator</param>
        public void Initialize(System.Random random)
        {
            this.random = random;
            Initialize();
        }

        /// <summary>
        /// Initialize the provider random internal tables
        /// with a common random generator
        /// </summary>
        public void Initialize()
        {
            if(random==null) 
                random = new System.Random();

            validIndexes = new List<int>();
            int entriesCount = this.Count;
            if (entriesCount == 0)
            {
#if !NO_DEBUG_LOG_WARNING
                Debug.LogWarning("[RandomIndexesProvider.Initialize]: No entry to initialize!");
#endif
                return;
            }

            int validEntriesCount = 0;
            float probaWeightSum = 0;
            foreach (var entry in this)
            {
                float pWeight = entry.probabilityWeight;
                if (pWeight > 0)
                {
                    validEntriesCount++;
                    probaWeightSum += pWeight;
                }
            }

            drawCounts = new int[entriesCount];

            for (int i = 0; i < entriesCount; i++)
            {
                drawCounts[i] = 0;
                var entry = this[i];
                if (entry.probabilityWeight > 0)
                {
                    float p = entry.probabilityWeight / probaWeightSum;
                    int indexCount = Mathf.CeilToInt(p * validEntriesCount*5);
                    for (int j = 0; j < indexCount; j++)
                    {
                        validIndexes.Add(i);
                    }
                }
            }
            availableIndexesCount = validIndexes.Count;
            if (availableIndexesCount > 0)
            {
                SuffleHelper.Suffle(validIndexes, random);
                indexes = validIndexes.ToArray();
            }
        }
       
        /// <summary>
        /// Get a random index depending probabilty and random mode entries parameters.
        /// </summary>
        /// <returns>
        /// A valid randomized index or -1 in case or no more index can be got.
        /// When no more index can be got the provider can be reseted, using the Reset method, 
        /// to reset the internal random indexes array with all values.
        /// </returns>
        public int GetIndex()
        {
            if (availableIndexesCount == 0)
            {
                return -1;
            }
            int k = random.Next(0, availableIndexesCount);
            int index = indexes[k];
            var entry = this[index];

            if (availableIndexesCount > 1)
            {
                switch (entry.type)
                {
                    case ProbabilityType.Countdown:
                        drawCounts[index]++;
                        if (drawCounts[index] >= entry.maxCount)
                        {
                            RemoveIndexFromRandomDraw(index);
                        }
                        break;
                    case ProbabilityType.DecreaseAndCountdown:
                        drawCounts[index]++;
                        if (drawCounts[index] >= entry.maxCount)
                        {
                            RemoveIndexFromRandomDraw(index);
                        }
                        else
                        {
                            // Remove one occurence of index from the random indexes array
                            availableIndexesCount--;
                            if (availableIndexesCount > 1)
                            {
                                for (int i = k; i < availableIndexesCount; i++)
                                {
                                    indexes[i] = indexes[i + 1];
                                }
                            }
                        }
                        break;
                    case ProbabilityType.Decrease:
                        // Remove one occurence of index from the random indexes array
                        availableIndexesCount--;
                        if (availableIndexesCount > 1)
                        {
                            for (int i = k; i < availableIndexesCount; i++)
                            {
                                indexes[i] = indexes[i + 1];
                            }
                        }
                        break;
                }
            }
            return index;
        }

        /// <summary>
        /// Reset the internal random indexes
        /// </summary>
        /// <param name="resetDrawCounts">Reset internal draw counts flag for countdown probability type entries</param>
        /// <returns>
        /// true if the reset succeeded
        /// false if no entry in the provider or Initialize method was no call previously
        /// </returns>
        public bool Reset(bool resetDrawCounts=true)
        {
            if(indexes==null || indexes.Length == 0)
            {
                return false;
            }

            availableIndexesCount = validIndexes.Count;
            for (int i = 0; i < availableIndexesCount; i++)
            {
                indexes[i] = validIndexes[i];
            }

            if (resetDrawCounts)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    drawCounts[i] = 0;  
                }
            }
            else
            {
                for (int i = 0; i < this.Count; i++)
                {
                    var entry = this[i];
                    if(entry.type == ProbabilityType.Countdown 
                       && entry.probabilityWeight > 0 
                       && drawCounts[i] >= entry.maxCount)
                    {
                        RemoveIndexFromRandomDraw(i);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Get the random draw count for an index since the last initialization or reset
        /// </summary>
        /// <param name="index">The index to query</param>
        /// <returns>The Selected random draw count from the index parameter</returns>
        public int GetDrawCountForIndex(int index)
        {
            if(drawCounts==null || index<0 || index >= drawCounts.Length)
            {
                return 0;
            }
            return drawCounts[index];
        }

        /// <summary>
        /// Get the probability to get the parameter index
        /// </summary>
        /// <param name="index">The index target</param>
        /// <returns>The Selected probability for the index to be get</returns>
        public float GetProbabilityForIndex(int index)
        {
            if(!IsReadyToBeUsed || index < 0 || index>=this.Count)
            {
                return 0f;
            }
            int counter = 0;
            for(int i=0;i< availableIndexesCount; i++)
            {
                if (indexes[i] == index)
                {
                    counter++;
                }
            }
            return (float)counter / (float)availableIndexesCount;
        }

        /// <summary>
        /// Set all probability weights to adjust the probability of the index draw with the input 
        /// </summary>
        /// <param name="index">The index element to target</param>
        /// <param name="probability">The probabilty to apply</param>
        /// <param name="initialize">
        /// Flag to indicate if the Initialize method must be call after the propabilities 
        /// were computed.
        /// </param>
        /// <returns>The new probabilty weight</returns>
        public float SetProbabilityForIndex(int index, float probability,bool initialize=true)
        {
            if (!IsReadyToBeUsed || index < 0 || index >= this.Count) return 0;
            
            probability = Mathf.Clamp01(probability);
            
            int validEntriesCount = 0;
            float probaWeightSum = 0;
            int count = this.Count;
            for (int i=0;i<count;i++)
            {
                var entry = this[i];
                float pWeight = entry.probabilityWeight;
                if (pWeight > 0)
                {
                    validEntriesCount++;
                    probaWeightSum += pWeight;
                }
            }

            float w;
            var target = this[index];
            if (validEntriesCount > 0)
            {
                if (target.probabilityWeight == 0)
                    validEntriesCount++;

                w = probability * probaWeightSum;
                var delta = target.probabilityWeight-w;
                if (delta < 0)
                {
                    var offset = delta / (validEntriesCount-1);
                    for (int i = 0; i < count; i++)
                    {
                        if (i != index)
                        {
                            var entry = this[i];
                            float pWeight = entry.probabilityWeight;
                            
                            if (pWeight > 0)
                            {
                                float nw = pWeight + offset;
                                if (nw < 0)
                                {
                                    nw = 0;
                                    offset -= nw;//readjut the offset with the overflow
                                }
                                entry.probabilityWeight = nw;
                            }
                            this[i] = entry;
                        }
                    }
                }
                else
                {
                    var offset = delta / (count-1f);
                    for (int i = 0; i < count; i++)
                    {
                        if (i != index)
                        {
                            var entry = this[i];    
                            entry.probabilityWeight += offset;
                            this[i] = entry;
                        }
                    }
                }
            }
            else
            {
                w = probability * count;
                var offset = (1f-w)/(count-1);
                for (int i = 0; i < count; i++)
                {
                    if (i != index)
                    {
                        var entry = this[i];
                        entry.probabilityWeight = offset;
                        this[i] = entry;
                    }
                }
            }

            target.probabilityWeight = w;
            this[index] = target;
            
            if(initialize)
                Initialize();            
            return w;
        }
    }
}

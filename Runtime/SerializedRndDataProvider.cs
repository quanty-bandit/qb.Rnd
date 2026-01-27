using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;
using static qb.Rnd.RndIndexProvider;

namespace qb.Rnd
{
    /// <summary>
    /// Serialized random data provider
    /// to be isUsed as entry in MonoBehaviour inspector.
    /// Manage editor ui and provide an interface to get data randomly
    /// from propabilities settings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class SerializedRndDataProvider<T>
    {
        [Serializable]
        public class RandomEntry
        {

            [HideInInspector]
            public int index;
#if UNITY_EDITOR
            string title => $"{index}";
            [Title("$" + nameof(title), HorizontalLine = false)]
#endif
#if UNITY_EDITOR
            [OnValueChanged(nameof(OnProbabiltyTypeChange))]
#endif
            public RndIndexProvider.ProbabilityType probabilityType = RndIndexProvider.ProbabilityType.Constant;

#if UNITY_EDITOR
            bool ShowMaxCount => probabilityType == ProbabilityType.Countdown || probabilityType == ProbabilityType.DecreaseAndCountdown;
            [ShowIf(nameof(ShowMaxCount))]

            [OnValueChanged(nameof(OnMaxCountChange))]
            [Min(1)]
#endif
            public int maxCount = 1;

#if UNITY_EDITOR
            [OnValueChanged(nameof(OnProbabiltyChange))]
#endif
            [Range(0, 1)]
            [SerializeField]
            float probability;
            public float Probability => probability;

            [HideInInspector]
            public float probabilityWeight = 1;

            [Required]
            public T data;

#if UNITY_EDITOR
            bool silent;
            public void SilentSetProbability(float value)
            {
                silent = true;
                probability = value;
            }

            public Action<RandomEntry> OnProbabiltyChangeAction;
            void OnProbabiltyChange()
            {
                if (!silent)
                    OnProbabiltyChangeAction?.Invoke(this);
                silent = false;
            }

            public Action<RandomEntry> OnProbabiltyTypeChangeAction;
            void OnProbabiltyTypeChange()
            {
                OnProbabiltyTypeChangeAction?.Invoke(this);
            }

            public Action<RandomEntry> OnMaxCountChangeAction;
            void OnMaxCountChange()
            {
                OnMaxCountChangeAction?.Invoke(this);
            }
#endif
        }

        #region editor
#if UNITY_EDITOR
        void OnProbabiltyChange(RandomEntry entry)
        {
            entry.probabilityWeight = randomIndexesProvider.SetProbabilityForIndex(entry.index, entry.Probability);
            for (int i = 0; i < randomEntries.Count; i++)
            {
                var e = randomEntries[i];
                if (e != entry)
                {
                    var pe = randomIndexesProvider[i];
                    e.probabilityWeight = pe.probabilityWeight;
                    e.SilentSetProbability(randomIndexesProvider.GetProbabilityForIndex(i));
                }
            }
        }
        void OnProbabiltyTypeChange(RandomEntry entry)
        {
            var pentry = randomIndexesProvider[entry.index];
            pentry.type = entry.probabilityType;
            randomIndexesProvider[entry.index] = pentry;
        }
        void OnMaxCountChange(RandomEntry entry)
        {
            var pentry = randomIndexesProvider[entry.index];
            pentry.maxCount = entry.maxCount;
            randomIndexesProvider[entry.index] = pentry;
        }

        void UpdateProvider()
        {
            if (randomIndexesProvider.Count != randomEntries.Count)
            {
                randomIndexesProvider.Clear();
                for (int i = 0; i < randomEntries.Count; i++)
                {
                    var entry = randomEntries[i];
                    entry.index = i;
                    if (entry.OnProbabiltyChangeAction == null)
                    {
                        entry.probabilityWeight = 1;
                    }
                    entry.OnProbabiltyChangeAction = OnProbabiltyChange;
                    entry.OnProbabiltyTypeChangeAction = OnProbabiltyTypeChange;
                    entry.OnMaxCountChangeAction = OnMaxCountChange;
                    randomIndexesProvider.Add(new IndexEntry
                    {
                        probabilityWeight = entry.probabilityWeight,
                        type = entry.probabilityType,
                        maxCount = entry.maxCount
                    });
                }
                randomIndexesProvider.Initialize();
                for (int i = 0; i < randomEntries.Count; i++)
                {
                    var entry = randomEntries[i];
                    entry.SilentSetProbability(randomIndexesProvider.GetProbabilityForIndex(i));
                }
            }
            else
            {
                for (int i = 0; i < randomEntries.Count; i++)
                {
                    var entry = randomEntries[i];
                    entry.index = i;
                    entry.OnProbabiltyChangeAction = OnProbabiltyChange;
                    entry.OnProbabiltyTypeChangeAction = OnProbabiltyTypeChange;
                    entry.OnMaxCountChangeAction = OnMaxCountChange;
                }
            }
        }

        [Button(ButtonSizes.Large), PropertySpace(SpaceAfter = 10)]
        /// <summary>
        /// Equalize all probabilities
        /// </summary>
        public void EqualizeAllPropabilties()
        {
            if (randomEntries.Count > 0)
            {
                float commonProbability;
                if (randomIndexesProvider.Count != randomEntries.Count)
                {
                    randomIndexesProvider.Clear();
                    foreach (var entry in randomEntries)
                    {
                        randomIndexesProvider.Add(new IndexEntry
                        {
                            probabilityWeight = entry.probabilityWeight,
                            type = entry.probabilityType
                        });
                    }
                    randomIndexesProvider.Initialize();
                    commonProbability = randomIndexesProvider.GetProbabilityForIndex(0);
                }
                else
                {
                    commonProbability = randomIndexesProvider.EqualizeAllPropabilties();
                }
                var w = randomIndexesProvider[0].probabilityWeight;
                foreach (var entry in randomEntries)
                {
                    entry.probabilityWeight = w;
                    entry.SilentSetProbability(commonProbability);
                }

            }
        }

        [OnValueChanged(nameof(UpdateProvider))]
#endif
        #endregion

        [SerializeField]
        List<RandomEntry> randomEntries = new List<RandomEntry>();
        public int Count => randomEntries.Count;

        public List<RandomEntry> RandomEntries => randomEntries;

        RndIndexProvider randomIndexesProvider = new RndIndexProvider();

        /// <summary>
        /// Fill the provider entries with a data list.
        /// The existing random entries will be clear and replace by a new one filled
        /// with new entries based on datas.
        /// Each random entry will be set with a constant probability type
        /// and a same probability weight
        /// </summary>
        /// <param name="datas">The data list to isUsed as entries</param>
        public void FillFromData(List<T> datas)
        {
            randomEntries.Clear();

            for (int i = 0; i < datas.Count; i++)
            {
                var entry = datas[i];
                if (entry != null)
                {
                    randomEntries.Add(new RandomEntry
                    {
                        data = entry,
                        probabilityType = ProbabilityType.Constant,
                        probabilityWeight = 1
                    });
                }
                else
                {
#if !NO_DEBUG_LOG_WARNING
                    Debug.LogWarning($"[SerializedRandomDataProvider.FillFromData]: Null entry detected at index [{i}].");
#endif
                }
            }
        }

        /// <summary>
        /// Return the index entries list isUsed as random settings 
        /// </summary>
        public List<IndexEntry> RandomIndexEntries => new List<IndexEntry>(randomIndexesProvider);

        /// <summary>
        /// Return true if the provider public methods are ready to be isUsed
        /// </summary>
        public bool IsReadyToBeUsed => randomIndexesProvider.IsReadyToBeUsed;

        /// <summary>
        /// Return true when there is no more data to get (GetData method return null).
        /// This case can be happen when the entries list contains no entry with ProbabilityType.Constant.
        /// To reset the provider and obtain more datas, the Reset() method must be called.
        /// </summary>
        public bool IsResetNeeded => randomIndexesProvider.IsResetNeeded;

        /// <summary>
        /// Initialize the provider random internal tables
        /// using the seed parameter to create the random generator
        /// </summary>
        /// <param name="seed">The input seed</param>
        public void Initialize(int seed)
        {
            Initialize(new System.Random(seed));
        }

        /// <summary>
        /// Initialize the provider random internal tables
        /// using the random generator
        /// </summary>
        /// <param name="random">The random generator</param>
        public void Initialize(System.Random random)
        {
            //if(randomIndexesProvider.Count!= randomEntries.Count)
            {
                randomIndexesProvider.Clear();
                for (int i = 0; i < randomEntries.Count; i++)
                {
                    var entry = randomEntries[i];
                    randomIndexesProvider.Add(new IndexEntry
                    {
                        probabilityWeight = entry.probabilityWeight,
                        type = entry.probabilityType,
                        maxCount = entry.maxCount
                    });
                }
            }
            randomIndexesProvider.Initialize(random);
        }

        /// <summary>
        /// Initialize the provider random internal tables
        /// with a common random generator
        /// </summary>
        public void Initialize()
        {
            Initialize(new System.Random());
        }

        /// <summary>
        /// Reset the internal random indexes
        /// </summary>
        /// <param name="resetDrawCounts">Reset internal draw counts flag for countdown probability type entries</param>
        /// <returns>
        /// true if the reset succeeded
        /// false if no entry in the provider or Initialize method was no call previously
        /// </returns>

        public bool Reset(bool resetDrawCounts = true) => randomIndexesProvider.Reset(resetDrawCounts);

        /// <summary>
        /// Try to get data from a random draw.
        /// </summary>
        /// <param name="data">The data if the random draw succeeded or null</param>
        /// <returns>
        /// If the random draw succeeded or false when no more data can be got 
        /// in case of no random type constant usage.
        /// When no more data can be got the provider can be reseted, using the Reset method, 
        /// to reset the internal random indexes array with all values.        
        /// </returns>
        public bool TryToGetData(out T data)
        {
            int index = randomIndexesProvider.GetIndex();
            if (index < 0)
            {
                data = default(T);
                return false;
            }
            data = randomEntries[index].data;
            return true;
        }

        /// <summary>
        /// Get a random index depending probabilty and random mode entries parameters.
        /// </summary>
        /// <returns>
        /// A valid randomized index or -1 in case or no more index can be got.
        /// When no more index can be got the provider can be reseted, using the Reset method, 
        /// to reset the internal random indexes array with all values.
        /// </returns>
        public int GetIndex() => randomIndexesProvider.GetIndex();


        /// <summary>
        /// Get the random draw count for an index since the last initialization or reset
        /// </summary>
        /// <param name="index">The index to query</param>
        /// <returns>The Selected random draw count from the index parameter</returns>
        public int GetDrawCountForIndex(int index) => randomIndexesProvider.GetDrawCountForIndex(index);

        /// <summary>
        /// Remove entries which data are null 
        /// </summary>
        /// <returns>The removed entries count</returns>
        public int RemoveNullDataEntries()
        {
            int removedCount = 0;
            for (var i = randomEntries.Count - 1; i >= 0; i--)
            {
                var entry = randomEntries[i];
                bool isDataNull = false;
                try
                {
                    // Null entry test!
                    if (entry.data == null)
                    {
                        isDataNull = true;
                    }
                }
                catch (System.Exception e)
                {
                    isDataNull = true;
                }

                if (isDataNull)
                {
                    randomEntries.RemoveAt(i);
                    removedCount++;
#if !NO_DEBUG_LOG
                    Debug.LogWarning($"[SerializedRandomDataProvider.RemoveNullDataEntries]: Null entry detected and removed at index [{i}].");
#endif
                }
            }
            return removedCount;
        }

    }
}

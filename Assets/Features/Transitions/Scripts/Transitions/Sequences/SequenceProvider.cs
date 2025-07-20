// Fichier: Features/Transitions/Sequences/SequenceProvider.cs (Version Finale Sans DLL)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Features.Transitions.Sequences
{
    public class SequenceProvider : MonoBehaviour
    {
        public static SequenceProvider Instance { get; private set; }

        private readonly Dictionary<string, Sprite[]> activeSequences = new Dictionary<string, Sprite[]>();
        private readonly Dictionary<string, AsyncOperationHandle<IList<Sprite>>> loadingHandles = new Dictionary<string, AsyncOperationHandle<IList<Sprite>>>();

        private enum InitState { NotInitialized, Initializing, Initialized }
        private InitState initState = InitState.NotInitialized;
        private AsyncOperationHandle initHandle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator Initialize()
        {
            if (initState == InitState.Initialized) yield break;
            if (initState == InitState.Initializing)
            {
                yield return initHandle;
                yield break;
            }

            initState = InitState.Initializing;
            initHandle = Addressables.InitializeAsync();
            yield return initHandle;

            if (initHandle.Status == AsyncOperationStatus.Succeeded)
            {
                initState = InitState.Initialized;
            }
            else
            {
                initState = InitState.NotInitialized;
                Debug.LogError($"SequenceProvider: Failed to initialize Addressables. Exception: {initHandle.OperationException}");
            }
        }

        public IEnumerator PreloadSequence(string sequenceLabel)
        {
            if (string.IsNullOrEmpty(sequenceLabel)) yield break;
            if (activeSequences.ContainsKey(sequenceLabel)) yield break;
            if (loadingHandles.ContainsKey(sequenceLabel))
            {
                yield return loadingHandles[sequenceLabel];
                yield break;
            }

            var handle = Addressables.LoadAssetsAsync<Sprite>(sequenceLabel, null);
            loadingHandles[sequenceLabel] = handle;
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var loadedAssets = handle.Result;
                if (loadedAssets == null || loadedAssets.Count == 0)
                {
                    Debug.LogError($"Addressables loading for label '{sequenceLabel}' succeeded, but 0 assets were found.");
                    Addressables.Release(handle);
                    loadingHandles.Remove(sequenceLabel);
                    yield break;
                }
                
                var sortedSprites = new List<Sprite>(loadedAssets);
                // On utilise le nouveau comparateur qui est 100% C#
                sortedSprites.Sort((a, b) => new AlphanumComparatorFast().Compare(a.name, b.name));

                activeSequences[sequenceLabel] = sortedSprites.ToArray();
            }
            else
            {
                Debug.LogError($"Failed to load assets with label '{sequenceLabel}'. Exception: {handle.OperationException}");
                Addressables.Release(handle);
                loadingHandles.Remove(sequenceLabel);
            }
        }

        public void UnloadSequence(string sequenceLabel)
        {
            if (string.IsNullOrEmpty(sequenceLabel) || !activeSequences.ContainsKey(sequenceLabel)) return;
            
            activeSequences.Remove(sequenceLabel);
            if (loadingHandles.TryGetValue(sequenceLabel, out var handle))
            {
                Addressables.Release(handle);
                loadingHandles.Remove(sequenceLabel);
            }
        }
        
        public Sprite[] GetSequence(string sequenceLabel)
        {
            activeSequences.TryGetValue(sequenceLabel, out var sequence);
            return sequence;
        }
    }
    
    /// <summary>
    /// Comparateur alphanumérique rapide et 100% C#, compatible avec toutes les plateformes (WebGL inclus).
    /// Trie les chaînes comme un humain s'y attendrait (ex: "z2.png" avant "z10.png").
    /// </summary>
    public class AlphanumComparatorFast : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            if (s1 == null && s2 == null) return 0;
            if (s1 == null) return -1;
            if (s2 == null) return 1;

            int len1 = s1.Length;
            int len2 = s2.Length;
            int marker1 = 0;
            int marker2 = 0;
            
            while (marker1 < len1 && marker2 < len2)
            {
                char ch1 = s1[marker1];
                char ch2 = s2[marker2];
                
                char[] space1 = new char[len1];
                int loc1 = 0;
                char[] space2 = new char[len2];
                int loc2 = 0;

                do
                {
                    space1[loc1++] = ch1;
                    marker1++;
                    if (marker1 < len1) ch1 = s1[marker1]; else break;
                } while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

                do
                {
                    space2[loc2++] = ch2;
                    marker2++;
                    if (marker2 < len2) ch2 = s2[marker2]; else break;
                } while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

                string str1 = new string(space1, 0, loc1);
                string str2 = new string(space2, 0, loc2);

                int result;

                if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]))
                {
                    int thisNumericChunk = int.Parse(str1);
                    int thatNumericChunk = int.Parse(str2);
                    result = thisNumericChunk.CompareTo(thatNumericChunk);
                }
                else
                {
                    result = string.Compare(str1, str2, StringComparison.Ordinal);
                }

                if (result != 0) return result;
            }
            return len1 - len2;
        }
    }
}
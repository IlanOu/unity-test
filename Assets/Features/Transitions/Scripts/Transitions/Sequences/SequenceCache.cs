using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Features.Transitions.Configuration;
using Features.Transitions.Utils;

namespace Features.Transitions.Sequences
{
    public class SequenceCache : MonoBehaviour
    {
        private static SequenceCache _instance;
        private Dictionary<string, CachedSequence> cachedSequences = new Dictionary<string, CachedSequence>();
        private TransitionSettings settings;

        public static SequenceCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[SequenceCache]");
                    _instance = go.AddComponent<SequenceCache>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeCache();
        }
        
        private void InitializeCache()
        {
            settings = new TransitionSettings();
        }

        public IEnumerator PreloadAllSequences(Action<float> onProgressUpdate = null, Action<string> onStatusUpdate = null)
        {
            string sequencesPath = Path.Combine(Application.streamingAssetsPath, "Sequences");
            
            if (!Directory.Exists(sequencesPath))
            {
                Debug.LogWarning("Sequences folder not found!");
                onProgressUpdate?.Invoke(1f);
                yield break;
            }
            
            var sequenceInfos = GetSequenceInfos(sequencesPath);
            if (sequenceInfos.Count == 0)
            {
                Debug.LogWarning("No sequences found!");
                onProgressUpdate?.Invoke(1f);
                yield break;
            }

            int totalFrames = CalculateTotalFrames(sequenceInfos);
            int processedFrames = 0;
            
            foreach (var sequenceInfo in sequenceInfos)
            {
                onStatusUpdate?.Invoke($"Loading {sequenceInfo.name}...");
                
                yield return PreloadSequenceInternal(sequenceInfo, (frameProgress) => {
                    float totalProgress = (processedFrames + frameProgress) / (float)totalFrames;
                    onProgressUpdate?.Invoke(totalProgress);
                });
                
                processedFrames += sequenceInfo.frameCount;
                onProgressUpdate?.Invoke(processedFrames / (float)totalFrames);
                yield return null;
            }
            
            onProgressUpdate?.Invoke(1f);
            onStatusUpdate?.Invoke("Loading complete!");
        }

        public CachedSequence GetSequence(string sequenceName)
        {
            return cachedSequences.TryGetValue(sequenceName, out var sequence) ? sequence : null;
        }

        public bool IsSequenceCached(string sequenceName)
        {
            return cachedSequences.ContainsKey(sequenceName);
        }

        public int GetCachedSequenceCount()
        {
            return cachedSequences.Count;
        }

        public void ClearCache()
        {
            foreach (var sequence in cachedSequences.Values)
            {
                sequence.Cleanup();
            }
            cachedSequences.Clear();
            Debug.Log("Cache cleared");
        }

        private List<SequenceInfo> GetSequenceInfos(string sequencesPath)
        {
            var sequenceInfos = new List<SequenceInfo>();
            string[] sequenceFolders = Directory.GetDirectories(sequencesPath);
            
            foreach (string sequenceFolder in sequenceFolders)
            {
                string sequenceName = Path.GetFileName(sequenceFolder);
                string framesPath = Path.Combine(sequenceFolder, "frames");
                
                if (Directory.Exists(framesPath))
                {
                    string[] pngFiles = Directory.GetFiles(framesPath, "*.png");
                    sequenceInfos.Add(new SequenceInfo 
                    { 
                        name = sequenceName, 
                        frameCount = pngFiles.Length,
                        files = pngFiles
                    });
                }
            }
            
            return sequenceInfos;
        }

        private int CalculateTotalFrames(List<SequenceInfo> sequenceInfos)
        {
            int total = 0;
            foreach (var info in sequenceInfos)
            {
                total += info.frameCount;
            }
            return total;
        }

        private IEnumerator PreloadSequenceInternal(SequenceInfo sequenceInfo, System.Action<float> onFrameProgress)
        {
            if (cachedSequences.ContainsKey(sequenceInfo.name))
                yield break;

            System.Array.Sort(sequenceInfo.files);

            var cachedSequence = new CachedSequence(sequenceInfo.name, sequenceInfo.files.Length);

            const int batchSize = 2;
            
            for (int i = 0; i < sequenceInfo.files.Length; i += batchSize)
            {
                int endIndex = Mathf.Min(i + batchSize, sequenceInfo.files.Length);
                
                for (int j = i; j < endIndex; j++)
                {
                    var frameResult = LoadFrame(sequenceInfo.files[j]);
                    if (frameResult.success)
                    {
                        cachedSequence.SetFrame(j, frameResult.sprite, frameResult.texture);
                    }
                    
                    onFrameProgress?.Invoke(j / (float)sequenceInfo.files.Length);
                }
                
                yield return null;
            }

            cachedSequences[sequenceInfo.name] = cachedSequence;
            Debug.Log($"Successfully cached sequence: {sequenceInfo.name} ({sequenceInfo.frameCount} frames)");
        }

        private FrameLoadResult LoadFrame(string filePath)
        {
            try
            {
                byte[] frameData = File.ReadAllBytes(filePath);
                if (frameData.Length == 0)
                    return new FrameLoadResult { success = false };
                
                var frameTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!frameTexture.LoadImage(frameData))
                {
                    Destroy(frameTexture);
                    return new FrameLoadResult { success = false };
                }
                
                var frameSprite = Sprite.Create(frameTexture, 
                    new Rect(0, 0, frameTexture.width, frameTexture.height), 
                    new Vector2(0.5f, 0.5f));
                
                return new FrameLoadResult 
                { 
                    success = true, 
                    sprite = frameSprite, 
                    texture = frameTexture 
                };
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading frame {filePath}: {e.Message}");
                return new FrameLoadResult { success = false };
            }
        }

        private struct FrameLoadResult
        {
            public bool success;
            public Sprite sprite;
            public Texture2D texture;
        }

        private class SequenceInfo
        {
            public string name;
            public int frameCount;
            public string[] files;
        }
    }
}
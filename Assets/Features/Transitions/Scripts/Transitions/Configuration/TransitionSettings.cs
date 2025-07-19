using UnityEngine;

namespace Features.Transitions.Configuration
{
    [System.Serializable]
    public class TransitionSettings
    {
        [Header("Memory Management")]
        public long maxCacheMemoryMB = 100;
        public bool enableMemoryMonitoring = true;
        
        [Header("Performance")]
        public int sequenceLoadBatchSize = 2;
        public bool useAsyncLoading = true;
        
        [Header("Debug")]
        public bool enableDebugLogs = false;
        public bool showMemoryUsage = false;
        
        public long MaxCacheMemoryBytes => maxCacheMemoryMB * 1024 * 1024;
        
        public void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Transitions] {message}");
            }
        }
        
        public void LogMemoryUsage(long currentUsage)
        {
            if (showMemoryUsage)
            {
                Debug.Log($"[Transitions] Memory usage: {Utils.MemoryHelper.FormatBytes(currentUsage)} / {Utils.MemoryHelper.FormatBytes(MaxCacheMemoryBytes)}");
            }
        }
    }
}
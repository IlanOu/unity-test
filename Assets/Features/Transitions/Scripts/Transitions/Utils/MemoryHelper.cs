using UnityEngine;

namespace Features.Transitions.Utils
{
    public static class MemoryHelper
    {
        private const long BYTES_TO_MB = 1024 * 1024;
        
        public static long GetTextureMemoryUsage(Texture2D texture)
        {
            if (texture == null) return 0;
            
            try
            {
                return texture.GetRawTextureData().Length;
            }
            catch (System.Exception)
            {
                // Fallback calculation if GetRawTextureData fails
                return texture.width * texture.height * GetBytesPerPixel(texture.format);
            }
        }
        
        public static long GetSpriteMemoryUsage(Sprite sprite)
        {
            if (sprite?.texture == null) return 0;
            return GetTextureMemoryUsage(sprite.texture);
        }
        
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < BYTES_TO_MB) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (float)BYTES_TO_MB:F1} MB";
        }
        
        public static bool IsMemoryUsageAcceptable(long currentUsage, long maxUsage)
        {
            return currentUsage <= maxUsage;
        }
        
        public static void ForceGarbageCollection()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }
        
        private static int GetBytesPerPixel(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    return 4;
                case TextureFormat.RGB24:
                    return 3;
                case TextureFormat.RGBA4444:
                case TextureFormat.RGB565:
                case TextureFormat.ARGB4444:
                    return 2;
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                    return 1;
                default:
                    return 4; // Default to 4 bytes per pixel
            }
        }
    }
}
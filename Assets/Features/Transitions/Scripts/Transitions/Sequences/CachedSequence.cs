using UnityEngine;

namespace Features.Transitions.Sequences
{
    [System.Serializable]
    public class CachedSequence
    {
        [SerializeField] private Sprite[] sprites;
        [SerializeField] private Texture2D[] textures;
        [SerializeField] private string sequenceName;
        [SerializeField] private int frameCount;
        [SerializeField] private long memoryUsage;

        public Sprite[] Sprites => sprites;
        public Texture2D[] Textures => textures;
        public string SequenceName => sequenceName;
        public int FrameCount => frameCount;
        public long MemoryUsage => memoryUsage;

        public CachedSequence(string name, int count)
        {
            sequenceName = name;
            frameCount = count;
            sprites = new Sprite[count];
            textures = new Texture2D[count];
            memoryUsage = 0;
        }

        public void SetFrame(int index, Sprite sprite, Texture2D texture)
        {
            if (index >= 0 && index < frameCount)
            {
                sprites[index] = sprite;
                textures[index] = texture;
                
                if (texture != null)
                {
                    memoryUsage += texture.GetRawTextureData().Length;
                }
            }
        }

        public bool IsValid()
        {
            return sprites != null && textures != null && frameCount > 0;
        }

        public void Cleanup()
        {
            if (sprites != null)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                    {
                        Object.Destroy(sprites[i]);
                        sprites[i] = null;
                    }
                }
            }

            if (textures != null)
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    if (textures[i] != null)
                    {
                        Object.Destroy(textures[i]);
                        textures[i] = null;
                    }
                }
            }

            memoryUsage = 0;
        }
    }
}
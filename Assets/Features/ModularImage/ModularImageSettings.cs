using UnityEngine;

namespace Features.ModularImage
{
    public enum CornerMode
    {
        Uniform,
        Individual,
        FullRounded
    }

    [System.Serializable]
    public struct CornerRadius
    {
        public float topLeft;
        public float topRight;
        public float bottomRight;
        public float bottomLeft;
    
        public CornerRadius(float uniform)
        {
            topLeft = topRight = bottomRight = bottomLeft = uniform;
        }
    
        public CornerRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            this.bottomLeft = bottomLeft;
        }
    }

    [System.Serializable]
    public struct ModularImageSettings
    {
        public bool enabled;
        public float width;
        public Color color;
    
        public ModularImageSettings(bool enabled, float width, Color color)
        {
            this.enabled = enabled;
            this.width = width;
            this.color = color;
        }
    
        public static ModularImageSettings None => new ModularImageSettings(false, 0, Color.clear);
    }
    
    public enum OutlineType
    {
        Outer,
        Center,
        Inner
    }

    [System.Serializable]
    public struct OutlineSettings
    {
        public bool enabled;
        public float width;
        public Color color;
        public OutlineType type;
    
        public OutlineSettings(bool enabled, float width, Color color, OutlineType type)
        {
            this.enabled = enabled;
            this.width = width;
            this.color = color;
            this.type = type;
        }
    
        public static OutlineSettings None => new OutlineSettings(false, 0, Color.black, OutlineType.Outer);
    }
}
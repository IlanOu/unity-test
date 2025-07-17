#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ModularImage
{
    public static class ModularImageUtility
    {
        [MenuItem("CONTEXT/Image/Replace with Modular Image")]
        private static void ReplaceImageWithModular(MenuCommand command)
        {
            Image image = (Image)command.context;
            ReplaceWithModularImage(image.gameObject);
        }
    
        [MenuItem("GameObject/UI/Modular Image", false, 2001)]
        private static void CreateModularImage()
        {
            GameObject go = new GameObject("Modular Image");
        
            if (Selection.activeGameObject != null)
            {
                Canvas canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    go.transform.SetParent(Selection.activeGameObject.transform, false);
                }
            }
        
            var rectTransform = go.AddComponent<RectTransform>();
            var modularImage = go.AddComponent<ModularImage>();
        
            rectTransform.sizeDelta = new Vector2(100, 100);
            modularImage.color = Color.white;
        
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Modular Image");
        }
    
        public static void ReplaceWithModularImage(GameObject gameObject)
        {
            Image existingImage = gameObject.GetComponent<Image>();
            if (existingImage == null) return;
        
            var savedProperties = new ImageProperties(existingImage);
        
            Undo.RecordObject(gameObject, "Replace with Modular Image");
            Undo.DestroyObjectImmediate(existingImage);
        
            var modularImage = Undo.AddComponent<ModularImage>(gameObject);
            savedProperties.ApplyTo(modularImage);
        
            Debug.Log($"Replaced Image with ModularUIImage on {gameObject.name}");
        }
    
        private class ImageProperties
        {
            public Color color;
            public Sprite sprite;
            public Material material;
            public bool raycastTarget;
            public Vector4 raycastPadding;
            public bool maskable;
            public Image.Type type;
            public bool preserveAspect;
            public bool useSpriteMesh;
            public bool fillCenter;
            public float pixelsPerUnitMultiplier;
            public Image.FillMethod fillMethod;
            public float fillAmount;
            public bool fillClockwise;
            public int fillOrigin;
        
            public ImageProperties(Image image)
            {
                color = image.color;
                sprite = image.sprite;
                material = image.material;
                raycastTarget = image.raycastTarget;
                raycastPadding = image.raycastPadding;
                maskable = image.maskable;
                type = image.type;
                preserveAspect = image.preserveAspect;
                useSpriteMesh = image.useSpriteMesh;
                fillCenter = image.fillCenter;
                pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
                fillMethod = image.fillMethod;
                fillAmount = image.fillAmount;
                fillClockwise = image.fillClockwise;
                fillOrigin = image.fillOrigin;
            }
        
            public void ApplyTo(ModularImage modularImage)
            {
                modularImage.color = color;
                modularImage.sprite = sprite;
                modularImage.material = material;
                modularImage.raycastTarget = raycastTarget;
                modularImage.raycastPadding = raycastPadding;
                modularImage.maskable = maskable;
                modularImage.type = type;
                modularImage.preserveAspect = preserveAspect;
                modularImage.useSpriteMesh = useSpriteMesh;
                modularImage.fillCenter = fillCenter;
                modularImage.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
                modularImage.fillMethod = fillMethod;
                modularImage.fillAmount = fillAmount;
                modularImage.fillClockwise = fillClockwise;
                modularImage.fillOrigin = fillOrigin;
            
                modularImage.UseModularShape = true;
                modularImage.UniformRadius = 5f;
            }
        }
    }
    
    // Extensions for fluid usage
    public static class ModularUIImageExtensions
    {
        public static ModularImage SetRoundedCorners(this ModularImage image, float radius)
        {
            image.UniformRadius = radius;
            return image;
        }
    
        public static ModularImage SetOutline(this ModularImage image, float width, Color color)
        {
            image.SetOutline(width, color);
            return image;
        }
    
        public static ModularImage SetColor(this ModularImage image, Color color)
        {
            image.color = color;
            return image;
        }
    }
}
#endif

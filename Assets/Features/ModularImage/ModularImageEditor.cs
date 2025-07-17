#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Features.ModularImage
{
    [CustomEditor(typeof(ModularImage))]
    public class ModularImageEditor : Editor
    {
        private SerializedProperty useModularShapeProperty;
        private SerializedProperty cornerModeProperty;
        private SerializedProperty uniformRadiusProperty;
        private SerializedProperty cornerRadiusProperty;
        private SerializedProperty cornerSegmentsProperty;
        private SerializedProperty outlineProperty;

        void OnEnable()
        {
            useModularShapeProperty = serializedObject.FindProperty("useModularShape");
            cornerModeProperty = serializedObject.FindProperty("cornerMode");
            uniformRadiusProperty = serializedObject.FindProperty("uniformRadius");
            cornerRadiusProperty = serializedObject.FindProperty("cornerRadius");
            cornerSegmentsProperty = serializedObject.FindProperty("cornerSegments");
            outlineProperty = serializedObject.FindProperty("outline");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var image = (ModularImage)target;
            
            // Draw default image properties
            DrawDefaultImageProperties();

            GUILayout.Space(10);

            // Section Modular
            EditorGUILayout.LabelField("Modular Shape", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useModularShapeProperty, new GUIContent("Use Modular Shape"));

            if (useModularShapeProperty.boolValue)
            {
                EditorGUI.indentLevel++;

                // Corner Mode
                EditorGUILayout.PropertyField(cornerModeProperty, new GUIContent("Corner Mode"));

                var mode = (CornerMode)cornerModeProperty.enumValueIndex;

                switch (mode)
                {
                    case CornerMode.Uniform:
                        EditorGUILayout.PropertyField(uniformRadiusProperty, new GUIContent("Radius"));
                        break;

                    case CornerMode.Individual:
                        DrawIndividualCorners();
                        break;

                    case CornerMode.FullRounded:
                        EditorGUILayout.HelpBox("Corners will be fully rounded to create a pill/circle shape.",
                            MessageType.Info);
                        break;
                }

                EditorGUILayout.PropertyField(cornerSegmentsProperty, new GUIContent("Corner Segments"));

                GUILayout.Space(10);

                // Section Outline
                DrawOutlineSettings();

                GUILayout.Space(5);

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultImageProperties()
        {
            // Base properties of the Image
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastPadding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Maskable"));
            
            // Type and associated properties
            var imageTypeProperty = serializedObject.FindProperty("m_Type");
            EditorGUILayout.PropertyField(imageTypeProperty);

            var imageType = (Image.Type)imageTypeProperty.enumValueIndex;

            switch (imageType)
            {
                case Image.Type.Simple:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PreserveAspect"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_UseSpriteMesh"));
                    break;

                case Image.Type.Sliced:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillCenter"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PixelsPerUnitMultiplier"));
                    break;

                case Image.Type.Tiled:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillCenter"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PixelsPerUnitMultiplier"));
                    break;

                case Image.Type.Filled:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillMethod"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillAmount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillClockwise"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillOrigin"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PreserveAspect"));
                    break;
            }
        }

        private void DrawIndividualCorners()
        {
            EditorGUILayout.LabelField("Individual Corners", EditorStyles.boldLabel);

            var topLeftProp = cornerRadiusProperty.FindPropertyRelative("topLeft");
            var topRightProp = cornerRadiusProperty.FindPropertyRelative("topRight");
            var bottomRightProp = cornerRadiusProperty.FindPropertyRelative("bottomRight");
            var bottomLeftProp = cornerRadiusProperty.FindPropertyRelative("bottomLeft");
            
            // Each corner on its own line
            EditorGUILayout.PropertyField(topLeftProp, new GUIContent("Top Left"));
            EditorGUILayout.PropertyField(topRightProp, new GUIContent("Top Right"));
            EditorGUILayout.PropertyField(bottomRightProp, new GUIContent("Bottom Right"));
            EditorGUILayout.PropertyField(bottomLeftProp, new GUIContent("Bottom Left"));

            GUILayout.Space(5);
            
            // Button to synchronize all corners
            if (GUILayout.Button("Make All Same"))
            {
                float value = topLeftProp.floatValue;
                topRightProp.floatValue = value;
                bottomRightProp.floatValue = value;
                bottomLeftProp.floatValue = value;
            }
        }

        private void DrawOutlineSettings()
        {
            EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
    
            var enabledProperty = outlineProperty.FindPropertyRelative("enabled");
            var widthProperty = outlineProperty.FindPropertyRelative("width");
            var colorProperty = outlineProperty.FindPropertyRelative("color");
            var typeProperty = outlineProperty.FindPropertyRelative("type"); // Nouvelle ligne
    
            EditorGUILayout.PropertyField(enabledProperty, new GUIContent("Enable Outline"));
    
            if (enabledProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(widthProperty, new GUIContent("Width"));
                EditorGUILayout.PropertyField(colorProperty, new GUIContent("Color"));
                EditorGUILayout.PropertyField(typeProperty, new GUIContent("Type")); // Nouvelle ligne
                EditorGUI.indentLevel--;
            }
        }

    }
}
#endif
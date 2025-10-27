using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScoringManager))]
public class ScoringManagerEditor : Editor
{
    private SerializedProperty standardVariantProp;
    private SerializedProperty connecticutVariantProp;
    private SerializedProperty customVariantProp;
    private SerializedProperty selectedVariantProp;

    private string[] variantOptions;
    private int selectedIndex = 0;

    private void OnEnable()
    {
        standardVariantProp = serializedObject.FindProperty("standardVariant");
        connecticutVariantProp = serializedObject.FindProperty("connecticutVariant");
        customVariantProp = serializedObject.FindProperty("customVariant");
        selectedVariantProp = serializedObject.FindProperty("selectedVariant");

        UpdateVariantOptions();
    }

    private void UpdateVariantOptions()
    {
        // Build list of available variants
        var options = new System.Collections.Generic.List<string>();

        if (standardVariantProp.objectReferenceValue != null)
        {
            var standardVar = standardVariantProp.objectReferenceValue as ScoreVariables;
            if (standardVar != null)
                options.Add(standardVar.VariantName);
        }

        if (connecticutVariantProp.objectReferenceValue != null)
        {
            var ctVar = connecticutVariantProp.objectReferenceValue as ScoreVariables;
            if (ctVar != null)
                options.Add(ctVar.VariantName);
        }

        if (customVariantProp.objectReferenceValue != null)
        {
            var customVar = customVariantProp.objectReferenceValue as ScoreVariables;
            if (customVar != null)
                options.Add(customVar.VariantName);
        }

        variantOptions = options.Count > 0 ? options.ToArray() : new string[] { "No variants configured" };

        // Find current selection index
        string currentSelection = selectedVariantProp.stringValue;
        selectedIndex = System.Array.IndexOf(variantOptions, currentSelection);
        if (selectedIndex < 0) selectedIndex = 0;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Scoring Presets", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(standardVariantProp, new GUIContent("Standard Variant"));
        EditorGUILayout.PropertyField(connecticutVariantProp, new GUIContent("Connecticut Variant"));
        EditorGUILayout.PropertyField(customVariantProp, new GUIContent("Custom Variant"));

        EditorGUILayout.Space();

        // Update options if variants changed
        if (GUI.changed)
        {
            UpdateVariantOptions();
        }

        EditorGUILayout.LabelField("Active Variant", EditorStyles.boldLabel);

        // Dropdown for variant selection
        EditorGUI.BeginChangeCheck();
        selectedIndex = EditorGUILayout.Popup("Selected Variant", selectedIndex, variantOptions);

        if (EditorGUI.EndChangeCheck())
        {
            if (selectedIndex >= 0 && selectedIndex < variantOptions.Length)
            {
                selectedVariantProp.stringValue = variantOptions[selectedIndex];
            }
        }

        EditorGUILayout.HelpBox(
            "Select which scoring variant to use. Variants are defined in ScoreVariables ScriptableObjects.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }
}

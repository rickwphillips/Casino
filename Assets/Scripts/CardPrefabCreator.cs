using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script to automatically create a properly configured Card Prefab
/// Updated for Unity 2023+ (no deprecated methods)
/// Run from menu: GameObject > Create Card Prefab
/// </summary>
public class CardPrefabCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Create Card Prefab", false, 10)]
    static void CreateCardPrefab()
    {
        // Ensure we have a Canvas - using newer Unity API (2023.1+)
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            // Create UI Canvas with EventSystem
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create EventSystem if needed - using newer Unity API
            if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // Note: StandaloneInputModule is deprecated in newer Unity versions
                // Use InputSystemUIInputModule if using the new Input System
                #if ENABLE_INPUT_SYSTEM
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                #else
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                #endif
            }
        }
        
        // Create the Card GameObject
        GameObject cardPrefab = new GameObject("CardPrefab");
        cardPrefab.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform and set size
        RectTransform cardRect = cardPrefab.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(80, 120); // Standard card size
        
        // Add Image component (card background)
        Image cardImage = cardPrefab.AddComponent<Image>();
        cardImage.color = Color.white;
        
        // Add modern Button component (not legacy GUI)
        Button cardButton = cardPrefab.AddComponent<Button>();
        cardButton.transition = Selectable.Transition.ColorTint;
        
        ColorBlock colors = cardButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.8f); // Light yellow
        colors.pressedColor = new Color(0.8f, 0.9f, 1f);   // Light blue
        colors.selectedColor = new Color(0.8f, 0.9f, 1f);  // Light blue
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        cardButton.colors = colors;
        
        // Create Text child with TextMeshPro
        GameObject textGO = new GameObject("CardText");
        textGO.transform.SetParent(cardPrefab.transform, false);
        
        // Add RectTransform to text
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(5, 5);   // 5 pixel padding
        textRect.offsetMax = new Vector2(-5, -5); // 5 pixel padding
        
        // Add TextMeshProUGUI component
        TextMeshProUGUI cardText = textGO.AddComponent<TextMeshProUGUI>();
        cardText.text = "A♠"; // Sample text
        cardText.fontSize = 36;
        cardText.fontStyle = FontStyles.Bold;
        cardText.color = Color.black;
        cardText.alignment = TextAlignmentOptions.Center;
        cardText.enableAutoSizing = true;
        cardText.fontSizeMin = 18;
        cardText.fontSizeMax = 36;
        
        // Create prefab from our GameObject
        string prefabPath = "Assets/CardPrefab.prefab";
        
        // Ensure the Assets folder exists
        if (!AssetDatabase.IsValidFolder("Assets"))
        {
            Debug.LogError("Assets folder not found!");
            return;
        }
        
        // Save as prefab asset
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cardPrefab, prefabPath);
        
        // Clean up the scene instance
        DestroyImmediate(cardPrefab);
        
        // Select the created prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"Card Prefab created at: {prefabPath}");
        Debug.Log("Structure: CardPrefab (Image, Button) -> CardText (TextMeshProUGUI)");
    }
    
    [MenuItem("GameObject/UI/Create Card Prefab (Simple Text)", false, 11)]
    static void CreateCardPrefabSimpleText()
    {
        // Ensure we have a Canvas - using newer Unity API
        Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            // Create UI Canvas with EventSystem
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create EventSystem if needed
            if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                #if ENABLE_INPUT_SYSTEM
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                #else
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                #endif
            }
        }
        
        // Create the Card GameObject
        GameObject cardPrefab = new GameObject("CardPrefab_SimpleText");
        cardPrefab.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform and set size
        RectTransform cardRect = cardPrefab.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(80, 120); // Standard card size
        
        // Add Image component (card background)
        Image cardImage = cardPrefab.AddComponent<Image>();
        cardImage.color = Color.white;
        
        // Add modern Button component
        Button cardButton = cardPrefab.AddComponent<Button>();
        cardButton.transition = Selectable.Transition.ColorTint;
        
        ColorBlock colors = cardButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.8f); // Light yellow
        colors.pressedColor = new Color(0.8f, 0.9f, 1f);   // Light blue
        colors.selectedColor = new Color(0.8f, 0.9f, 1f);  // Light blue
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        cardButton.colors = colors;
        
        // Create Text child with regular Text component
        GameObject textGO = new GameObject("CardText");
        textGO.transform.SetParent(cardPrefab.transform, false);
        
        // Add RectTransform to text
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(5, 5);   // 5 pixel padding
        textRect.offsetMax = new Vector2(-5, -5); // 5 pixel padding
        
        // Add regular Text component
        Text cardText = textGO.AddComponent<Text>();
        cardText.text = "A♠"; // Sample text
        cardText.fontSize = 24;
        cardText.fontStyle = FontStyle.Bold;
        cardText.color = Color.black;
        cardText.alignment = TextAnchor.MiddleCenter;
        cardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // Add Outline for better visibility
        Outline outline = textGO.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);
        
        // Create prefab from our GameObject
        string prefabPath = "Assets/CardPrefab_SimpleText.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cardPrefab, prefabPath);
        
        // Clean up the scene instance
        DestroyImmediate(cardPrefab);
        
        // Select the created prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"Card Prefab (Simple Text) created at: {prefabPath}");
        Debug.Log("Structure: CardPrefab_SimpleText (Image, Button) -> CardText (Text)");
    }
    
    // Alternative for older Unity versions (pre-2023.1)
    static T FindFirstObjectByTypeCompatible<T>() where T : Object
    {
        #if UNITY_2023_1_OR_NEWER
        return GameObject.FindFirstObjectByType<T>();
        #else
        return GameObject.FindObjectOfType<T>();
        #endif
    }
#endif
}

/// <summary>
/// Runtime card prefab validator - checks if a prefab is set up correctly
/// </summary>
public class CardPrefabValidator : MonoBehaviour
{
    [Header("Validation Results")]
    [SerializeField] private bool hasValidStructure;
    [SerializeField] private bool hasModernButton;
    [SerializeField] private bool hasTextChild;
    [SerializeField] private string validationMessage;
    
    [Header("Detected Components")]
    [SerializeField] private Button buttonComponent;
    [SerializeField] private Image imageComponent;
    [SerializeField] private TextMeshProUGUI textMeshProComponent;
    [SerializeField] private Text textComponent;
    
    // Public property to check validation status
    public bool IsValid => hasValidStructure;
    public string ValidationMessage => validationMessage;
    
    public bool Validate(GameObject cardPrefab)
    {
        // Reset validation state
        hasValidStructure = false;
        hasModernButton = false;
        hasTextChild = false;
        validationMessage = "Not validated";
        
        if (cardPrefab == null)
        {
            validationMessage = "No prefab to validate!";
            return false;
        }
        
        // Check for Image component
        imageComponent = cardPrefab.GetComponent<Image>();
        if (imageComponent == null)
        {
            validationMessage = "Missing Image component on root GameObject";
            return false;
        }
        
        // Check for Button component
        buttonComponent = cardPrefab.GetComponent<Button>();
        if (buttonComponent == null)
        {
            validationMessage = "Missing Button component on root GameObject";
            return false;
        }
        
        // Check if it's the modern UI Button (not legacy)
        hasModernButton = buttonComponent.GetType().FullName == "UnityEngine.UI.Button";
        if (!hasModernButton)
        {
            validationMessage = "Button is not the modern UI Button!";
            return false;
        }
        
        // Check for text component in children
        textMeshProComponent = cardPrefab.GetComponentInChildren<TextMeshProUGUI>();
        textComponent = cardPrefab.GetComponentInChildren<Text>();
        
        hasTextChild = (textMeshProComponent != null || textComponent != null);
        if (!hasTextChild)
        {
            validationMessage = "No Text or TextMeshProUGUI component found in children!";
            return false;
        }
        
        // Check if text is on child, not root
        if (cardPrefab.GetComponent<Text>() != null || cardPrefab.GetComponent<TextMeshProUGUI>() != null)
        {
            validationMessage = "Text component should be on a child GameObject, not the root!";
            hasTextChild = false; // Override since it's in wrong place
            return false;
        }
        
        // All checks passed
        hasValidStructure = true;
        validationMessage = "Card prefab is set up correctly!";
        return hasValidStructure;
    }
    
    [ContextMenu("Validate This GameObject")]
    void ValidateThisGameObject()
    {
        bool isValid = Validate(gameObject);
        Debug.Log($"Validation Result: {(isValid ? "PASS" : "FAIL")} - {validationMessage}");
    }
    
    // Helper method to get a validation report
    public string GetValidationReport()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("=== Card Prefab Validation Report ===");
        report.AppendLine($"Valid Structure: {(hasValidStructure ? "✓" : "✗")}");
        report.AppendLine($"Modern Button: {(hasModernButton ? "✓" : "✗")}");
        report.AppendLine($"Text Child: {(hasTextChild ? "✓" : "✗")}");
        report.AppendLine($"Message: {validationMessage}");
        
        if (buttonComponent != null)
            report.AppendLine($"Button Type: {buttonComponent.GetType().FullName}");
        
        if (textMeshProComponent != null)
            report.AppendLine($"Text Type: TextMeshProUGUI on {textMeshProComponent.gameObject.name}");
        else if (textComponent != null)
            report.AppendLine($"Text Type: Text on {textComponent.gameObject.name}");
            
        return report.ToString();
    }
}ß
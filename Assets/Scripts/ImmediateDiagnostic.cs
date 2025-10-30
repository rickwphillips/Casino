using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>
/// Quick click diagnostic - Press C key during gameplay
/// </summary>
public class ClickDiagnostic : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            RunDiagnostic();
        }
    }
    
    private void RunDiagnostic()
    {
        Debug.Log("\n" + new string('=', 80));
        Debug.Log("CLICK DIAGNOSTIC - Press C anytime to run this");
        Debug.Log(new string('=', 80));
        
        // Check EventSystem
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("❌ NO EVENTSYSTEM!");
            Debug.LogError("   FIX: Add EventSystem to scene");
            Debug.Log(new string('=', 80) + "\n");
            return;
        }
        Debug.Log("✓ EventSystem exists");
        
        // Check if EventSystem is enabled
        if (!eventSystem.enabled)
        {
            Debug.LogError("❌ EventSystem is DISABLED!");
            Debug.LogError("   FIX: Enable EventSystem component");
        }
        
        // Check Canvas and Raycaster
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ NO CANVAS!");
            Debug.Log(new string('=', 80) + "\n");
            return;
        }
        Debug.Log("✓ Canvas exists");
        
        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("❌ Canvas missing GraphicRaycaster!");
            Debug.LogError("   FIX: Add GraphicRaycaster to Canvas");
        }
        else
        {
            Debug.Log("✓ GraphicRaycaster exists");
            if (!raycaster.enabled)
            {
                Debug.LogError("❌ GraphicRaycaster is DISABLED!");
            }
        }
        
        // Check game state
        if (GameManager.Instance != null)
        {
            var current = GameManager.Instance.GetCurrentPlayer();
            Debug.Log($"\nCurrent Player: {current.Name}");
            Debug.Log($"  Is Human: {current.IsHuman()}");
            Debug.Log($"  Waiting for input: {GameManager.Instance.IsWaitingForHumanInput()}");
        }
        
        // Check buttons
        var allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        Debug.Log($"\n--- BUTTONS IN SCENE ---");
        Debug.Log($"Total buttons: {allButtons.Length}");
        
        int clickableCount = 0;
        int cardButtonCount = 0;
        
        foreach (var btn in allButtons)
        {
            bool isCard = btn.GetComponent<CardUI>() != null;
            if (isCard) cardButtonCount++;
            
            if (btn.interactable)
            {
                clickableCount++;
                if (isCard)
                {
                    Debug.Log($"  ✓ CLICKABLE CARD: {btn.gameObject.name}");
                }
            }
            else
            {
                if (isCard)
                {
                    Debug.Log($"  ✗ NOT CLICKABLE CARD: {btn.gameObject.name}");
                }
            }
        }
        
        Debug.Log($"\nCard buttons found: {cardButtonCount}");
        Debug.Log($"Clickable buttons: {clickableCount}/{allButtons.Length}");
        
        // Check CardUI components
        var cardUIs = FindObjectsByType<CardUI>(FindObjectsSortMode.None);
        Debug.Log($"\n--- CARDUI COMPONENTS ---");
        Debug.Log($"CardUI components: {cardUIs.Length}");
        
        // Summary
        Debug.Log("\n" + new string('-', 80));
        if (cardButtonCount == 0)
        {
            Debug.LogError("❌ NO CARD BUTTONS FOUND!");
            Debug.LogError("   Problem: Cards don't have Button components");
            Debug.LogError("   FIX: Recreate CardPrefab with Button component");
        }
        else if (clickableCount == 0 && GameManager.Instance != null && 
                 GameManager.Instance.GetCurrentPlayer().IsHuman() && 
                 GameManager.Instance.IsWaitingForHumanInput())
        {
            Debug.LogError("❌ IT'S YOUR TURN BUT NO BUTTONS ARE CLICKABLE!");
            Debug.LogError("   Problem: UIManager isn't setting cards to interactable");
            Debug.LogError("   FIX: Check UIManager.ShouldPlayerCardsBeSelectable()");
        }
        else if (clickableCount > 0)
        {
            Debug.Log($"✓ {clickableCount} clickable buttons found - clicking SHOULD work");
        }
        
        Debug.Log(new string('=', 80) + "\n");
    }
}
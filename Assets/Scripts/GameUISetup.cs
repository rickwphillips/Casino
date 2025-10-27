using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to automatically create UI elements for the Casino game.
/// Add this to an empty GameObject and click "Setup UI" in the context menu.
/// </summary>
public class GameUISetup : MonoBehaviour
{
    [ContextMenu("Setup UI")]
    public void SetupUI()
    {
        // Create Canvas
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create GameUIManager GameObject
        GameObject uiManagerObj = new GameObject("GameUIManager");
        uiManagerObj.transform.SetParent(canvas.transform);
        GameUIManager uiManager = uiManagerObj.AddComponent<GameUIManager>();

        // Set up layout
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Player 1 Panel (Top Left)
        GameObject player1Panel = CreatePanel("Player1Panel", canvasRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(400, 150));
        var p1Name = CreateText("P1Name", player1Panel.transform, "Player 1 (Dealer)", 14, TextAnchor.UpperLeft, new Vector2(10, -10), new Vector2(-10, -10));
        var p1Score = CreateText("P1Score", player1Panel.transform, "Score: 0 | Sweeps: 0", 12, TextAnchor.UpperLeft, new Vector2(10, -30), new Vector2(-10, -30));
        var p1Hand = CreateText("P1Hand", player1Panel.transform, "Hand: ", 12, TextAnchor.UpperLeft, new Vector2(10, -50), new Vector2(-10, -50));
        var p1Captured = CreateText("P1Captured", player1Panel.transform, "Captured: ", 12, TextAnchor.UpperLeft, new Vector2(10, -70), new Vector2(-10, -70));

        // Player 2 Panel (Top Right)
        GameObject player2Panel = CreatePanel("Player2Panel", canvasRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-410, -10), new Vector2(-10, 150));
        var p2Name = CreateText("P2Name", player2Panel.transform, "Player 2", 14, TextAnchor.UpperRight, new Vector2(10, -10), new Vector2(-10, -10));
        var p2Score = CreateText("P2Score", player2Panel.transform, "Score: 0 | Sweeps: 0", 12, TextAnchor.UpperRight, new Vector2(10, -30), new Vector2(-10, -30));
        var p2Hand = CreateText("P2Hand", player2Panel.transform, "Hand: ", 12, TextAnchor.UpperRight, new Vector2(10, -50), new Vector2(-10, -50));
        var p2Captured = CreateText("P2Captured", player2Panel.transform, "Captured: ", 12, TextAnchor.UpperRight, new Vector2(10, -70), new Vector2(-10, -70));

        // Table Panel (Center)
        GameObject tablePanel = CreatePanel("TablePanel", canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -50), new Vector2(200, 50));
        var tableCards = CreateText("TableCards", tablePanel.transform, "Table Cards: ", 12, TextAnchor.MiddleCenter, new Vector2(10, 20), new Vector2(-10, -10));
        var builds = CreateText("Builds", tablePanel.transform, "Builds: None", 12, TextAnchor.MiddleCenter, new Vector2(10, -20), new Vector2(-10, -40));

        // Game State Panel (Bottom)
        GameObject gameStatePanel = CreatePanel("GameStatePanel", canvasRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-200, 10), new Vector2(200, 80));
        var currentTurn = CreateText("CurrentTurn", gameStatePanel.transform, "Current Turn: ", 12, TextAnchor.MiddleCenter, new Vector2(10, 50), new Vector2(-10, -10));
        var variant = CreateText("Variant", gameStatePanel.transform, "Variant: ", 12, TextAnchor.MiddleCenter, new Vector2(10, 30), new Vector2(-10, -30));
        var winScore = CreateText("WinScore", gameStatePanel.transform, "Win Score: 21", 12, TextAnchor.MiddleCenter, new Vector2(10, 10), new Vector2(-10, -50));

        // Assign references to GameUIManager
        var managerType = typeof(GameUIManager);
        managerType.GetField("player1NameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p1Name);
        managerType.GetField("player1ScoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p1Score);
        managerType.GetField("player1HandText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p1Hand);
        managerType.GetField("player1CapturedText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p1Captured);

        managerType.GetField("player2NameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p2Name);
        managerType.GetField("player2ScoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p2Score);
        managerType.GetField("player2HandText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p2Hand);
        managerType.GetField("player2CapturedText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, p2Captured);

        managerType.GetField("tableCardsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, tableCards);
        managerType.GetField("buildsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, builds);

        managerType.GetField("currentTurnText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, currentTurn);
        managerType.GetField("gamePhaseText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, winScore);
        managerType.GetField("variantText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(uiManager, variant);

        Debug.Log("Game UI setup complete!");
    }

    private GameObject CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.5f);

        return panel;
    }

    private Text CreateText(string name, Transform parent, string defaultText, int fontSize, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = textObj.AddComponent<Text>();
        text.text = defaultText;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return text;
    }
}

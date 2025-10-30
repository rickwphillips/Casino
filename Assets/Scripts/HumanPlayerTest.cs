using UnityEngine;

/// <summary>
/// Simple test script to verify human player configuration is working.
/// Add this to any GameObject and run the game. Check the Console for output.
/// </summary>
public class HumanPlayerTest : MonoBehaviour
{
    private void Start()
    {
        // Wait a bit for game to initialize
        Invoke(nameof(RunTest), 1f);
    }
    
    private void RunTest()
    {
        Debug.Log("\n" + new string('=', 60));
        Debug.Log("HUMAN PLAYER CONFIGURATION TEST");
        Debug.Log(new string('=', 60));
        
        // Test 1: Check GameManager exists
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ TEST FAILED: GameManager.Instance is NULL!");
            Debug.LogError("   → Make sure you have a GameObject with GameManager script");
            return;
        }
        Debug.Log("✓ GameManager.Instance exists");
        
        // Test 2: Check players exist
        var dealer = GameManager.Instance.GetDealer();
        var nonDealer = GameManager.Instance.GetNonDealer();
        
        if (dealer == null)
        {
            Debug.LogError("❌ TEST FAILED: Dealer is NULL!");
            return;
        }
        if (nonDealer == null)
        {
            Debug.LogError("❌ TEST FAILED: Non-Dealer is NULL!");
            return;
        }
        Debug.Log("✓ Both players exist");
        
        // Test 3: Check player types
        Debug.Log($"\n--- PLAYER CONFIGURATION ---");
        Debug.Log($"Dealer: {dealer.Name}");
        Debug.Log($"  Type: {dealer.Type}");
        Debug.Log($"  IsHuman(): {dealer.IsHuman()}");
        Debug.Log($"  IsAI(): {dealer.IsAI()}");
        
        Debug.Log($"\nNon-Dealer: {nonDealer.Name}");
        Debug.Log($"  Type: {nonDealer.Type}");
        Debug.Log($"  IsHuman(): {nonDealer.IsHuman()}");
        Debug.Log($"  IsAI(): {nonDealer.IsAI()}");
        
        // Test 4: Check current player
        var currentPlayer = GameManager.Instance.GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("❌ TEST FAILED: Current player is NULL!");
            return;
        }
        
        Debug.Log($"\n--- CURRENT TURN ---");
        Debug.Log($"Current Player: {currentPlayer.Name}");
        Debug.Log($"  Type: {currentPlayer.Type}");
        Debug.Log($"  IsHuman(): {currentPlayer.IsHuman()}");
        
        // Test 5: Check waiting for input flag
        bool isWaiting = GameManager.Instance.IsWaitingForHumanInput();
        Debug.Log($"\n--- GAME STATE ---");
        Debug.Log($"IsWaitingForHumanInput: {isWaiting}");
        Debug.Log($"Current Phase: {GameManager.Instance.GetCurrentPhase()}");
        
        // Test 6: Expectations
        Debug.Log($"\n--- EXPECTED BEHAVIOR ---");
        if (currentPlayer.IsHuman())
        {
            if (isWaiting)
            {
                Debug.Log("✓ CORRECT: Game is waiting for human input");
                Debug.Log("  → Your cards SHOULD be clickable now");
            }
            else
            {
                Debug.LogError("❌ PROBLEM: Current player is human but game is NOT waiting for input!");
                Debug.LogError("   → This is why cards aren't clickable!");
            }
        }
        else
        {
            Debug.Log("✓ Current player is AI");
            Debug.Log("  → AI should play automatically soon");
        }
        
        // Test 7: Check UIManager
        Debug.Log($"\n--- UI MANAGER ---");
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("⚠ WARNING: UIManager.Instance is NULL!");
            Debug.LogWarning("   → This might cause issues with card interaction");
        }
        else
        {
            Debug.Log("✓ UIManager.Instance exists");
        }
        
        // Final summary
        Debug.Log("\n" + new string('=', 60));
        Debug.Log("TEST COMPLETE");
        
        // Check for success
        bool success = dealer.IsAI() && nonDealer.IsHuman();
        if (success)
        {
            Debug.Log("✓✓✓ CONFIGURATION LOOKS GOOD! ✓✓✓");
            Debug.Log("If cards still aren't clickable, the issue is in the UI code.");
        }
        else
        {
            Debug.LogError("❌❌❌ CONFIGURATION PROBLEM FOUND! ❌❌❌");
            Debug.LogError("Expected: Dealer=AI, NonDealer=Human");
            Debug.LogError($"Actual: Dealer={dealer.Type}, NonDealer={nonDealer.Type}");
            Debug.LogError("\nFIX: Set NonDealerPlayerType to 'Human' in GameManager Inspector!");
        }
        Debug.Log(new string('=', 60) + "\n");
    }
    
    private void Update()
    {
        // Continuously check during play
        if (Input.GetKeyDown(KeyCode.T))
        {
            RunTest();
        }
    }
}

# Casino Card Game

A Unity implementation of the classic Casino card game with AI opponents and configurable scoring variants.

## Game Overview

Casino is a fishing-style card game for two players where you capture cards from the table by matching or combining them with cards from your hand. Points are scored for capturing specific cards and meeting certain criteria.

## How to Play

### Setup
- The game uses a standard 52-card deck
- One player is designated as the **Dealer**, the other as the **Non-Dealer**
- Each player is dealt 4 cards
- 4 cards are dealt face-up to the table
- The Non-Dealer plays first

### Game Flow

#### Playing a Card
On your turn, you must play one card from your hand. When you play a card, one of two things happens:

1. **Capture**: If your card can capture cards from the table, those cards (plus your played card) go into your captured pile
2. **Trail**: If your card cannot capture anything, it is added to the table for future captures

#### Making Captures

**Face Cards (Jack, Queen, King)**
- Face cards can only capture matching face cards
- Example: A Jack can only capture other Jacks from the table

**Numbered Cards & Aces**
- Aces are worth 1
- Number cards (2-10) are worth their face value
- You can capture cards in two ways:
  - **Direct Match**: Capture cards with the same rank (e.g., a 5 captures another 5)
  - **Combination**: Capture multiple cards that sum to your card's value (e.g., a 5 captures a 2 and a 3)

**Important Notes**:
- When multiple valid captures exist, the game automatically selects the best combination (prioritizing direct matches, then most cards, then high-value cards)
- You capture ALL cards in the selected combination

#### Special: Sweeps
- If you capture ALL cards from the table (leaving it empty), you score a **Sweep**
- Sweeps are worth bonus points at the end of the round

#### Dealing Additional Cards
- After both players have played their 4 cards, each player is dealt 4 more cards
- Play continues until the deck is empty
- No additional cards are dealt to the table after the initial deal

#### End of Round
- When both players have played all cards and the deck is empty, the round ends
- Any remaining cards on the table go to the **last player who made a capture**
- Scoring then occurs

### Scoring

At the end of each round, points are awarded for:

#### Standard Scoring Rules

| Achievement | Points | Description |
|-------------|--------|-------------|
| **Most Cards** | 3 | Player who captured the most cards (no points if tied) |
| **Most Spades** | 1 | Player who captured the most spades (no points if tied) |
| **Big Casino** | 2 | Capturing the 10 of Diamonds |
| **Little Casino** | 1 | Capturing the 2 of Spades |
| **Aces** | 1 each | Each Ace captured is worth 1 point |
| **Sweeps** | 1 each | Each sweep performed during the round |

#### Connecticut Variant
The game includes a Connecticut variant with different scoring. Check the ScoringManager settings to see the active variant.

### Winning the Game

- The first player to reach **21 points** (or the configured win score) wins the game
- After each scoring round:
  - If no one has won, the dealer role swaps
  - A new deck is created and shuffled
  - Play continues with new hands

## Game Strategy Tips

### High-Value Cards to Prioritize
1. **10 of Diamonds** (Big Casino) - Worth 2 points
2. **2 of Spades** (Little Casino) - Worth 1 point
3. **All Aces** - Worth 1 point each
4. **Spades** - Count toward "Most Spades" bonus
5. **Any Cards** - Count toward "Most Cards" bonus (3 points)

### Strategic Considerations
- **Going for Sweeps**: Clearing the table is valuable but can be risky
- **Trailing Strategically**: Sometimes trailing a low-value card is better than giving your opponent capture opportunities
- **Counting Cards**: Keep track of what's been played to maximize your capture potential
- **Most Cards Bonus**: Capturing 27+ cards (more than half the deck) guarantees this 3-point bonus

## AI Difficulty Levels

### Easy
- Plays random valid moves
- Good for learning the game

### Medium
- Prioritizes captures over trails
- Prefers capturing high-value cards (Aces, Big/Little Casino, Spades)
- Makes tactical decisions

### Hard
- Uses strategic evaluation with lookahead
- Scores each possible move considering:
  - Potential sweeps (highest priority)
  - Big Casino and Little Casino captures
  - Ace captures
  - Spade captures (for "Most Spades")
  - Card count (for "Most Cards")
- Minimizes value lost when trailing

## Controls

- **Select a Card**: Click on a card in your hand
- **Play Card**: Click the "Play Card" button after selecting
- **Restart Game**: Click "Restart" when the game ends

## Configuration

### Changing AI Difficulty
You can configure AI difficulty for each player independently in the GameManager inspector:
- `Dealer AI Difficulty`
- `Non-Dealer AI Difficulty`

### Scoring Variants
The game supports multiple scoring variants configured via ScriptableObjects:
- **Standard**: Traditional Casino scoring
- **Connecticut**: Connecticut variant rules
- **Custom**: Create your own variant

To change variants, modify the ScoringManager settings in the Unity inspector.

## Technical Details

- **Game Engine**: Unity
- **Language**: C#
- **Architecture**: Singleton-based managers with clean separation of concerns
- **AI**: Multi-level difficulty system with strategic evaluation

## Credits

Generated with [Claude Code](https://claude.com/claude-code)

---

*Enjoy the game! Try to master the strategy and beat the Hard AI!*

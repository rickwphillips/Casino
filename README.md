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

#### Advanced: Building
**Builds** allow you to set up future captures by combining cards on the table with a card from your hand.

**How to Create a Build:**
1. Play a card from your hand onto one or more cards on the table
2. Announce the total value (e.g., "Building Sevens")
3. You **MUST** have a card in your hand that can capture that value
4. The build is now "owned" by you

**Example:**
- You have a 3, 7, and King in your hand
- There's a 4 on the table
- You play your 3 onto the 4 and say "Building Sevens"
- On your next turn, you can capture the build with your 7

**Build Rules:**
- You **MUST** capture your own build on your next turn (unless you can make a different capture or create another build)
- You **CANNOT** trail (play a card to the table without capturing) if you have a pending build
- Your opponent can capture your build if they have the appropriate card
- The build value must equal the sum of all cards in the build (no cheating!)

**Modifying Opponent Builds (Singular Builds Only):**
Your opponent can **increase the value** of your build by adding a card from their hand:
- **Example**: You create a build of 7 (3 + 4). Your opponent has a 2 and a 9 in hand
  - They can add their 2 to your build, making it a build of 9 (3 + 4 + 2)
  - They declare "Building Nines" and now **own** the build
  - They must have the 9 in hand to capture it on their next turn
- **Multi-Builds Cannot Be Modified**: If a build contains multiple separate combinations (e.g., a build of 7 with both "6 + A" AND "5 + 2"), it becomes a multi-build and **cannot** be modified by opponents
- The new value must be **greater** than the original build value
- Ownership transfers to the player who last modified the build

**Strategic Advantage:**
- Builds protect valuable combinations from your opponent
- Builds can help you capture more cards at once
- Creating builds when you don't have the capture card is illegal and will be rejected
- Modifying opponent builds lets you steal their setup while increasing the capture value

#### Special: Sweeps
- If you capture ALL cards from the table AND all builds (leaving everything empty), you score a **Sweep**
- Sweeps are worth bonus points at the end of the round

#### Dealing Additional Cards
- After both players have played their 4 cards, each player is dealt 4 more cards
- Play continues until the deck is empty
- No additional cards are dealt to the table after the initial deal

#### End of Hand/Round Rules

**Remaining Table Cards:**
The game can be configured to award remaining table cards in two ways:

1. **After Each Hand (Traditional)**:
   - After each 4-card hand, remaining table cards go to the last player who made a capture
   - This happens 6 times per full game (52 cards ÷ 4 cards per hand ÷ 2 players)
   - Builds also awarded to their owners after each hand

2. **Only at Game End (Variant)**:
   - Table cards remain on the table throughout all hands
   - Only when the entire deck is exhausted do remaining cards go to the last capturer
   - Builds persist across hands until the game ends
   - This creates a longer strategic game with persistent table state

The timing is configurable via the scoring variant settings.

**When Scoring Occurs:**
- Scoring happens when the entire deck is exhausted (all 52 cards played)
- After scoring, if no one has won, the dealer role swaps and a new game begins

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

Each variant can be configured with:
- **Point values** for all scoring categories
- **Win score** (default 21)
- **Table Card Award Timing**:
  - `AfterEachHand` (Traditional): Award remaining cards after each 4-card hand
  - `OnlyAtGameEnd` (Variant): Keep cards on table until entire deck is played

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

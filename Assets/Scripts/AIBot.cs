using UnityEngine;
using System.Collections.Generic;

public class AIBot : MonoBehaviour
{
    private string aiPlayer = "O";
    private string humanPlayer = "X";
    private int maxDepth = 5;  // Changed from 100 to 3 for more efficient search
    private float timeLimit = 1.0f * 5;  // Time limit for minimax search in seconds
    private System.Diagnostics.Stopwatch searchTimer;

    // Threat patterns and scores
    private const int FIVE_IN_A_ROW = 1000000;   // Win - increased for higher priority
    private const int OPEN_FOUR = 500000;        // Guaranteed win next move - greatly increased
    private const int FOUR = 100000;             // Forced defense - greatly increased
    private const int OPEN_THREE = 1000;         // Strong threat
    private const int THREE = 100;               // Potential threat
    private const int OPEN_TWO = 50;             // Early development

    // Add new offensive pattern constants
    private const int DOUBLE_THREAT = 8000;  // Creating two threats at once (fork)

    private Dictionary<string, int> _transpositionTable = new Dictionary<string, int>();
    private bool _useTimeLimit = true;
    private bool _useTranspositionTable = true;

    public void MakeMove(Cell[,] grid)
    {
        // Clear cache for new move
        _transpositionTable.Clear();
        
        // Start timer for enforcing time limit
        searchTimer = new System.Diagnostics.Stopwatch();
        searchTimer.Start();
        
        // First, check for immediate threats that require response
        Vector2Int immediateMove = FindCriticalMove(grid);
        if (immediateMove.x != -1)
        {
            Debug.Log($"AI detected critical threat at ({immediateMove.x}, {immediateMove.y})");
            grid[immediateMove.x, immediateMove.y].MakeMove();
            return;
        }
        
        // Replace full-grid iteration with valid moves in 2-cell vicinity
        int bestScore = int.MinValue;
        Vector2Int bestMove = new Vector2Int(-1, -1);
        var validMoves = GetValidMoves(grid, 2);
        
        // Sort moves by a quick heuristic evaluation to improve alpha-beta pruning
        List<ScoredMove> scoredMoves = OrderMoves(grid, validMoves);
        
        // Use iterative deepening to get results within time limit
        for (int currentDepth = 1; currentDepth <= maxDepth; currentDepth++)
        {
            bool timeOut = false;
            Vector2Int tempBestMove = new Vector2Int(-1, -1);
            int tempBestScore = int.MinValue;
            
            foreach (var scoredMove in scoredMoves)
            {
                Vector2Int move = scoredMove.position;
                grid[move.x, move.y].MakeTemporaryMove(aiPlayer);
                
                int score = Minimax(grid, currentDepth - 1, int.MinValue, int.MaxValue, false);
                
                grid[move.x, move.y].UndoTemporaryMove();
                
                if (score > tempBestScore)
                {
                    tempBestScore = score;
                    tempBestMove = move;
                }
                
                // Check if we're out of time
                if (_useTimeLimit && searchTimer.ElapsedMilliseconds > timeLimit * 1000)
                {
                    timeOut = true;
                    break;
                }
            }
            
            // Update best move if we completed this depth
            if (!timeOut || tempBestMove.x != -1)
            {
                bestScore = tempBestScore;
                bestMove = tempBestMove;
            }
            
            // Break if we're out of time
            if (timeOut)
            {
                Debug.Log($"AI search timeout at depth {currentDepth}, using best move found");
                break;
            }
        }
        
        searchTimer.Stop();
        Debug.Log($"AI thinking time: {searchTimer.ElapsedMilliseconds}ms");
        
        if (bestMove.x != -1)
        {
            Debug.Log($"AI selects move at ({bestMove.x}, {bestMove.y}) with score {bestScore}");
            grid[bestMove.x, bestMove.y].MakeMove();
        }
    }

    // Add this new method to detect critical threats requiring immediate response
    private Vector2Int FindCriticalMove(Cell[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        // First check if AI can win in one move
        var validMoves = GetValidMoves(grid, 2);
        foreach (var move in validMoves)
        {
            grid[move.x, move.y].MakeTemporaryMove(aiPlayer);
            bool isWin = IsWinningState(grid, aiPlayer);
            grid[move.x, move.y].UndoTemporaryMove();
            
            if (isWin)
                return move;
        }
        
        // Then check if opponent has an immediate win that must be blocked
        foreach (var move in validMoves)
        {
            grid[move.x, move.y].MakeTemporaryMove(humanPlayer);
            bool isWin = IsWinningState(grid, humanPlayer);
            grid[move.x, move.y].UndoTemporaryMove();
            
            if (isWin)
                return move;
        }
        
        // Check for opponent open threes that need to be blocked
        Vector2Int threatMove = new Vector2Int(-1, -1);
        int maxThreatScore = 0;
        
        foreach (var move in validMoves)
        {
            // If we make this move, does it stop opponent threats?
            grid[move.x, move.y].MakeTemporaryMove(aiPlayer);
            int opponentThreats = CountThreats(grid, humanPlayer, true);
            grid[move.x, move.y].UndoTemporaryMove();
            
            // Check if opponent can create an open four with this move
            grid[move.x, move.y].MakeTemporaryMove(humanPlayer);
            bool createsOpenFour = HasOpenFour(grid, humanPlayer);
            int myThreats = CountThreats(grid, aiPlayer, true); 
            grid[move.x, move.y].UndoTemporaryMove();
            
            // Score this defensive move
            int threatScore = 0;
            if (createsOpenFour) threatScore += 1000; // Critical to block
            threatScore += myThreats * 50;  // Blocking that also creates our threats
            
            if (threatScore > maxThreatScore)
            {
                maxThreatScore = threatScore;
                threatMove = move;
            }
        }
        
        // Only return a critical move if it's really important
        if (maxThreatScore >= 1000)
            return threatMove;
            
        return new Vector2Int(-1, -1); // No critical threats found
    }

    // Add this helper method to check for open fours
    private bool HasOpenFour(Cell[,] grid, string player)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // Horizontal
            new Vector2Int(0, 1),   // Vertical
            new Vector2Int(1, 1),   // Diagonal \
            new Vector2Int(1, -1)   // Diagonal /
        };
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                foreach (var dir in directions)
                {
                    ThreatInfo threat = EvaluatePattern(grid, x, y, dir, player);
                    if (threat.length >= 4 && threat.isOpen)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    private int Minimax(Cell[,] grid, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        // Check transposition table for cached results
        string boardHash = _useTranspositionTable ? GetBoardHash(grid) + depth + (maximizingPlayer ? "1" : "0") : null;
        if (_useTranspositionTable && _transpositionTable.TryGetValue(boardHash, out int cachedEval))
        {
            return cachedEval;
        }
        
        // Evaluate the board once
        int eval = EvaluateBoard(grid);
        
        // Check for terminal state: win condition
        if (Mathf.Abs(eval) >= FIVE_IN_A_ROW)
        {
            if (_useTranspositionTable) _transpositionTable[boardHash] = eval;
            return eval;
        }
        
        // Check for terminal state: depth limit or time limit
        if (depth == 0 || _useTimeLimit && searchTimer.ElapsedMilliseconds > timeLimit * 1000)
        {
            if (_useTranspositionTable) _transpositionTable[boardHash] = eval;
            return eval;
        }
        
        var validMoves = GetValidMoves(grid, 2);
        
        // Order moves for better pruning
        List<ScoredMove> orderedMoves = OrderMoves(grid, validMoves);
        
        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var scoredMove in orderedMoves)
            {
                Vector2Int move = scoredMove.position;
                grid[move.x, move.y].MakeTemporaryMove(aiPlayer);
                int evalMove = Minimax(grid, depth - 1, alpha, beta, false);
                grid[move.x, move.y].UndoTemporaryMove();
                        
                maxEval = Mathf.Max(maxEval, evalMove);
                alpha = Mathf.Max(alpha, evalMove);
                if (beta <= alpha)
                    break;  // Beta cutoff
                    
                // Check time limit during search
                if (_useTimeLimit && searchTimer.ElapsedMilliseconds > timeLimit * 1000)
                    break;
            }
            
            if (_useTranspositionTable) _transpositionTable[boardHash] = maxEval;
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var scoredMove in orderedMoves)
            {
                Vector2Int move = scoredMove.position;
                grid[move.x, move.y].MakeTemporaryMove(humanPlayer);
                int evalMove = Minimax(grid, depth - 1, alpha, beta, true);
                grid[move.x, move.y].UndoTemporaryMove();
                        
                minEval = Mathf.Min(minEval, evalMove);
                beta = Mathf.Min(beta, evalMove);
                if (beta <= alpha)
                    break;  // Alpha cutoff
                    
                // Check time limit during search
                if (_useTimeLimit && searchTimer.ElapsedMilliseconds > timeLimit * 1000)
                    break;
            }
            
            if (_useTranspositionTable) _transpositionTable[boardHash] = minEval;
            return minEval;
        }
    }

    private List<Vector2Int> GetValidMoves(Cell[,] grid, int distance)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        HashSet<Vector2Int> moves = new HashSet<Vector2Int>();
        bool anyPlayed = false;
        
        // Scan board for played moves
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!string.IsNullOrEmpty(grid[x, y].GetSymbol()))
                {
                    anyPlayed = true;
                    
                    // Add all empty cells within the specified distance
                    for (int dx = -distance; dx <= distance; dx++)
                    {
                        for (int dy = -distance; dy <= distance; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            // Check if in bounds and empty
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height && 
                                string.IsNullOrEmpty(grid[nx, ny].GetSymbol()))
                            {
                                moves.Add(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
            }
        }
        
        // If no moves played yet, consider all empty cells
        if (!anyPlayed)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (string.IsNullOrEmpty(grid[x, y].GetSymbol()))
                    {
                        moves.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        return new List<Vector2Int>(moves);
    }

    // Modify the board evaluation to weight defense more heavily
    private int EvaluateBoard(Cell[,] grid)
    {
        // Keep the winning checks
        if (IsWinningState(grid, aiPlayer))
            return FIVE_IN_A_ROW;
        if (IsWinningState(grid, humanPlayer))
            return -FIVE_IN_A_ROW;
        
        // Initialize threat scores
        int aiScore = 0;
        int humanScore = 0;
        
        // Sample a subset of the board for threats when it's large
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        bool fullEvaluation = width <= 10 && height <= 10;  // Only do full eval on smaller boards
        
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // Horizontal
            new Vector2Int(0, 1),   // Vertical
            new Vector2Int(1, 1),   // Diagonal \
            new Vector2Int(1, -1)   // Diagonal /
        };
        
        // Fast path: check for immediate threats first
        List<Vector2Int> playedPositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!string.IsNullOrEmpty(grid[x, y].GetSymbol()))
                {
                    playedPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        
        // Evaluate only around played positions for efficiency
        foreach (var pos in playedPositions)
        {
            foreach (var dir in directions)
            {
                // Check AI threats
                ThreatInfo aiThreat = EvaluatePattern(grid, pos.x, pos.y, dir, aiPlayer);
                aiScore += aiThreat.score;
                
                // Check human threats
                ThreatInfo humanThreat = EvaluatePattern(grid, pos.x, pos.y, dir, humanPlayer);
                humanScore += humanThreat.score;
            }
        }
        
        if (fullEvaluation)
        {
            // Only evaluate potential forks on smaller boards to save time
            EvaluateForks(grid, ref aiScore, ref humanScore);
        }
        
        // Adjust final score - increase weight for human threats to prioritize defense
        return aiScore - (humanScore * 3); // Increased from 2x to 3x
    }

    private struct ThreatInfo
    {
        public int score;
        public bool isOpen;
        public int length;
    }

    private ThreatInfo EvaluatePattern(Cell[,] grid, int startX, int startY, Vector2Int dir, string player)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        // Skip if starting cell isn't empty or isn't the player's
        string startCell = grid[startX, startY].GetSymbol();
        if (startCell != player)
            return new ThreatInfo { score = 0, isOpen = false, length = 0 };
        
        // Only start pattern evaluation from the beginning of a sequence
        // Check if there's a player's piece before the current position
        int prevX = startX - dir.x;
        int prevY = startY - dir.y;
        if (prevX >= 0 && prevX < width && prevY >= 0 && prevY < height && 
            grid[prevX, prevY].GetSymbol() == player)
        {
            return new ThreatInfo { score = 0, isOpen = false, length = 0 };
        }
        
        // Count consecutive pieces and check if ends are open
        int count = 1;  // Start with current cell
        int emptyAfterCount = 0;
        int emptyBeforeCount = 0;
        bool blocked = false;
        
        // Check if the cell before start is open
        if (prevX >= 0 && prevX < width && prevY >= 0 && prevY < height)
        {
            string prevCell = grid[prevX, prevY].GetSymbol();
            if (string.IsNullOrEmpty(prevCell))
                emptyBeforeCount = 1;
            else if (prevCell != player)
                blocked = true;
        }
        else
        {
            blocked = true; // Edge of board blocks this end
        }
        
        // Count consecutive pieces in the direction
        for (int i = 1; i < 5; i++)
        {
            int nextX = startX + (i * dir.x);
            int nextY = startY + (i * dir.y);
            
            if (nextX >= 0 && nextX < width && nextY >= 0 && nextY < height)
            {
                string cellSymbol = grid[nextX, nextY].GetSymbol();
                if (cellSymbol == player)
                {
                    count++;
                }
                else if (string.IsNullOrEmpty(cellSymbol))
                {
                    // Count empty spaces after the sequence
                    int checkX = nextX;
                    int checkY = nextY;
                    while (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height && 
                           string.IsNullOrEmpty(grid[checkX, checkY].GetSymbol()) && emptyAfterCount < 2)
                    {
                        emptyAfterCount++;
                        checkX += dir.x;
                        checkY += dir.y;
                    }
                    break;
                }
                else
                {
                    // Opponent piece blocks this end
                    blocked = true;
                    break;
                }
            }
            else
            {
                // Edge of board blocks this end
                blocked = true;
                break;
            }
        }
        
        bool isOpen = !blocked && emptyAfterCount > 0 && emptyBeforeCount > 0;
        int score = 0;
        
        // Assign score based on threat level
        if (count >= 4)
        {
            if (isOpen)
                score = OPEN_FOUR;
            else if (emptyAfterCount > 0 || emptyBeforeCount > 0)
                score = FOUR;
        }
        else if (count == 3)
        {
            if (isOpen)
                score = OPEN_THREE;
            else if (emptyAfterCount > 0 || emptyBeforeCount > 0)
                score = THREE;
        }
        else if (count == 2)
        {
            if (isOpen)
                score = OPEN_TWO;
        }
        
        // Return detailed information about this threat
        return new ThreatInfo { score = score, isOpen = isOpen, length = count };
    }

    // Function to check for two-way open sequences (split fours, forks, etc.)
    private void EvaluateForks(Cell[,] grid, ref int aiScore, ref int humanScore)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        // Check each empty cell for potential forks
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (string.IsNullOrEmpty(grid[x, y].GetSymbol()))
                {
                    // Try AI move here and count threats
                    grid[x, y].MakeTemporaryMove(aiPlayer);
                    int aiThreats = CountThreats(grid, aiPlayer);
                    grid[x, y].UndoTemporaryMove();
                    
                    // Try Human move here and count threats
                    grid[x, y].MakeTemporaryMove(humanPlayer);
                    int humanThreats = CountThreats(grid, humanPlayer);
                    grid[x, y].UndoTemporaryMove();
                    
                    // Add fork scores
                    if (aiThreats >= 2)
                        aiScore += DOUBLE_THREAT;
                    if (humanThreats >= 2)
                        humanScore += DOUBLE_THREAT;
                }
            }
        }
    }

    // Modify CountThreats to detect open threes more effectively
    private int CountThreats(Cell[,] grid, string player, bool openThreatsOnly = false)
    {
        // Count open threes and fours in all directions
        int threatCount = 0;
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1)
        };
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                foreach (var dir in directions)
                {
                    ThreatInfo threat = EvaluatePattern(grid, x, y, dir, player);
                    if (openThreatsOnly)
                    {
                        if (threat.isOpen && threat.length == 3)
                        {
                            threatCount++;
                        }
                    }
                    else if ((threat.score >= OPEN_THREE) || threat.score == FOUR)
                    {
                        threatCount++;
                    }
                }
            }
        }
        
        return threatCount;
    }

    private bool IsWinningState(Cell[,] grid, string player)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1)
        };
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].GetSymbol() == player)
                {
                    foreach (var dir in directions)
                    {
                        int count = 1;
                        int dx = x, dy = y;
                        for (int i = 1; i < 5; i++)
                        {
                            dx += dir.x;
                            dy += dir.y;
                            if (dx >= 0 && dx < width && dy >= 0 && dy < height && grid[dx, dy].GetSymbol() == player)
                                count++;
                            else
                                break;
                        }
                        if (count >= 5)
                            return true;
                    }
                }
            }
        }
        return false;
    }

    // Helper structure for move ordering
    private struct ScoredMove
    {
        public Vector2Int position;
        public int score;
    }

    // Modify OrderMoves to prioritize open three blocks
    private List<ScoredMove> OrderMoves(Cell[,] grid, List<Vector2Int> moves)
    {
        List<ScoredMove> scoredMoves = new List<ScoredMove>();
        
        foreach (var move in moves)
        {
            int score = 0;
            
            // Check for immediate wins
            grid[move.x, move.y].MakeTemporaryMove(aiPlayer);
            if (IsWinningState(grid, aiPlayer))
                score += 10000;  // Prioritize winning moves
            grid[move.x, move.y].UndoTemporaryMove();
            
            // Check for immediate blocks
            grid[move.x, move.y].MakeTemporaryMove(humanPlayer);
            if (IsWinningState(grid, humanPlayer))
                score += 9500;   // Prioritize blocking opponent wins
            
            // Check if this move would create an open four for opponent
            bool createsOpenFour = HasOpenFour(grid, humanPlayer);
            if (createsOpenFour)
                score += 9000;   // Prioritize blocking open fours
            grid[move.x, move.y].UndoTemporaryMove();
            
            // Check if this blocks opponent open threes
            int openThreatsBeforeMove = CountThreats(grid, humanPlayer, true);
            grid[move.x, move.y].MakeTemporaryMove(aiPlayer);
            int openThreatsAfterMove = CountThreats(grid, humanPlayer, true);
            if (openThreatsAfterMove < openThreatsBeforeMove)
                score += 8000;   // Prioritize blocking open threes
            grid[move.x, move.y].UndoTemporaryMove();
            
            // Quick proximity heuristic - prioritize moves near existing pieces
            int proximityScore = CountProximityPieces(grid, move.x, move.y, 1);
            score += proximityScore * 10;
            
            // Center preference
            int centerX = grid.GetLength(0) / 2;
            int centerY = grid.GetLength(1) / 2;
            int distanceToCenter = Mathf.Abs(move.x - centerX) + Mathf.Abs(move.y - centerY);
            score += (10 - distanceToCenter) * 5;
            
            scoredMoves.Add(new ScoredMove { position = move, score = score });
        }
        
        // Sort moves by score (descending)
        scoredMoves.Sort((a, b) => b.score.CompareTo(a.score));
        
        return scoredMoves;
    }

    // Count pieces around a position for move ordering
    private int CountProximityPieces(Cell[,] grid, int x, int y, int distance)
    {
        int count = 0;
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        for (int dx = -distance; dx <= distance; dx++)
        {
            for (int dy = -distance; dy <= distance; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && 
                    !string.IsNullOrEmpty(grid[nx, ny].GetSymbol()))
                {
                    count++;
                }
            }
        }
        
        return count;
    }

    // Generate a simple hash of the board state for the transposition table
    private string GetBoardHash(Cell[,] grid)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                string symbol = grid[x, y].GetSymbol();
                sb.Append(string.IsNullOrEmpty(symbol) ? "_" : symbol);
            }
        }
        
        return sb.ToString();
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Reversi.Domain
{
    public class AIPlayer
    {
        private StoneType _aiColor;
        private int _level; // 1 ~ ?

        private struct DifficultySetting
        {
            public int MaxDepth;
            public float ErrorProbability; 
            public bool UseMobility;
            public bool UseEndgameSolver;
        }

        private DifficultySetting GetDifficultySetting(int level)
        {
            // Use System.Math for thread safety
            int depth = level * 2;
            if (depth < 2) depth = 2;
            if (depth > 12) depth = 12;

            return new DifficultySetting
            {
                MaxDepth = depth,
                ErrorProbability = Math.Max(0f, (5 - level) * 0.1f), 
                UseMobility = level >= 3,
                UseEndgameSolver = level >= 5
            };
        }

        private enum TTFlag { Exact, LowerBound, UpperBound }
        
        private struct TTEntry
        {
            public int Depth;
            public int Score;
            public TTFlag Flag;
            public BoardPosition BestMove;
        }
        
        // Key: Zobrist Hash (long)
        private Dictionary<long, TTEntry> _transpositionTable = new Dictionary<long, TTEntry>();

        public AIPlayer(StoneType color, int difficulty)
        {
            _aiColor = color;
            _level = difficulty;
        }

        public async Task<BoardPosition> GetBestMoveAsync(BoardModel board)
        {
            // Run on thread pool
            return await Task.Run(() => GetBestMove(board));
        }

        private BoardPosition GetBestMove(BoardModel originalBoard)
        {
            // Clone the board ONCE for simulation to avoid affecting the live UI
            BoardModel board = originalBoard.Clone();

            var settings = GetDifficultySetting(_level);
            var validMoves = board.GetValidMoves(_aiColor);
            
            if (validMoves.Count == 0) return BoardPosition.Nowhere;
            if (validMoves.Count == 1) return validMoves[0];

            // Thread-safe Random
            System.Random rng = new System.Random();

            // 1. Error Injection
            if (rng.NextDouble() < settings.ErrorProbability)
            {
                return validMoves[rng.Next(validMoves.Count)];
            }

            // 2. Endgame Solver Check
            int emptyCount = 64 - (board.BlackScore + board.WhiteScore);
            if (settings.UseEndgameSolver && emptyCount <= 12)
            {
                // Uncapped depth for endgame
                settings.MaxDepth = 64; 
            }

            _transpositionTable.Clear();

            BoardPosition bestMove = validMoves[0];
            int maxDepth = settings.MaxDepth;
            long timeLimit = 2000; 
            
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                for (int d = 2; d <= maxDepth; d += 2)
                {
                    if (sw.ElapsedMilliseconds > timeLimit) break;
                    
                    int val = NegaMax(board, d, int.MinValue + 1, int.MaxValue - 1, _aiColor, settings, sw, timeLimit);
                    
                    // Retrieve best move from TT for this root state logic (conceptually)
                    // Or we assume the NegaMax root call updated correct bestMove if we structured it that way.
                    // But NegaMax internal doesn't return the move.
                    // We need to re-scan or trust the first node logic.
                    // Actually, simple way: Extract move from TT of root node.
                    if (_transpositionTable.TryGetValue(board.CurrentHash, out var entry))
                    {
                        if (entry.BestMove.IsValid) bestMove = entry.BestMove;
                    }
                }
            }
            catch (TimeoutException) { }

            return bestMove;
        }

        private int NegaMax(BoardModel board, int depth, int alpha, int beta, StoneType currentTurn, DifficultySetting settings, System.Diagnostics.Stopwatch sw, long timeLimit)
        {
            if (sw.ElapsedMilliseconds > timeLimit) throw new TimeoutException();

            int originalAlpha = alpha;

            // 1. Transposition Table Lookup
            if (_transpositionTable.TryGetValue(board.CurrentHash, out var ttEntry))
            {
                if (ttEntry.Depth >= depth)
                {
                    if (ttEntry.Flag == TTFlag.Exact) return ttEntry.Score;
                    if (ttEntry.Flag == TTFlag.LowerBound) alpha = Math.Max(alpha, ttEntry.Score);
                    else if (ttEntry.Flag == TTFlag.UpperBound) beta = Math.Min(beta, ttEntry.Score);

                    if (alpha >= beta) return ttEntry.Score;
                }
            }

            if (depth == 0 || board.IsGameOver())
            {
                return Evaluate(board, currentTurn, settings);
            }

            var validMoves = board.GetValidMoves(currentTurn);
            if (validMoves.Count == 0)
            {
                // Pass logic: same player moves again? No, opponent moves.
                // Reversi rule: if no moves, pass. If opponent also no moves, game over (handled by IsGameOver).
                // So pass to opponent. Depth convention varies, usually maintain depth or decrement.
                // Let's decrement to advance simulation.
                return -NegaMax(board, depth - 1, -beta, -alpha, currentTurn.Opposite(), settings, sw, timeLimit);
            }

            // 2. Move Ordering
            // A. TT Best Move (Hash Move)
            BoardPosition hashMove = BoardPosition.Nowhere;
            if (ttEntry.BestMove.IsValid) hashMove = ttEntry.BestMove;

            // B. Sort
            // Sort by: HashMove first, then Static Weight
            validMoves.Sort((a, b) => 
            {
                if (a.Equals(hashMove)) return -1;
                if (b.Equals(hashMove)) return 1;
                
                // Position Weight Ordering
                int weightA = BoardModel.PositionWeights[a.Row, a.Col];
                int weightB = BoardModel.PositionWeights[b.Row, b.Col];
                return weightB.CompareTo(weightA); // Descending
            });

            int bestVal = int.MinValue;
            BoardPosition bestMove = BoardPosition.Nowhere;

            foreach (var move in validMoves)
            {
                board.PlaceStone(move, currentTurn);
                int val = -NegaMax(board, depth - 1, -beta, -alpha, currentTurn.Opposite(), settings, sw, timeLimit, timeLimit > 0);
                board.Undo();

                if (val > bestVal)
                {
                    bestVal = val;
                    bestMove = move;
                }
                
                alpha = Math.Max(alpha, val);
                if (alpha >= beta) break;
            }

            // 3. Store in TT
            var newEntry = new TTEntry
            {
                Depth = depth,
                Score = bestVal,
                BestMove = bestMove,
                Flag = (bestVal <= originalAlpha) ? TTFlag.UpperBound : (bestVal >= beta) ? TTFlag.LowerBound : TTFlag.Exact
            };
            
            // Should we overwrite? Standard: overwrite if new depth >= old depth
            if (!_transpositionTable.ContainsKey(board.CurrentHash) || depth >= _transpositionTable[board.CurrentHash].Depth)
            {
                _transpositionTable[board.CurrentHash] = newEntry;
            }

            return bestVal;
        }
        
         // Helper overload for recursive calls without re-checking stopwatch too aggressively if needed, 
         // but simpler to keep one method signature. I'll stick to the one above but fix the call signature in loop.

        private int NegaMax(BoardModel board, int depth, int alpha, int beta, StoneType currentTurn, DifficultySetting settings, System.Diagnostics.Stopwatch sw, long timeLimit, bool checkTime)
        {
             return NegaMax(board, depth, alpha, beta, currentTurn, settings, sw, timeLimit);
        }

        private int Evaluate(BoardModel board, StoneType currentTurn, DifficultySetting settings)
        {
             // Perspective: currentTurn is the player to evaluate FOR.
             // BoardModel scores are absolute Black/White.
             
             int myScore, oppScore;
             int myWeightScore, oppWeightScore;
             
             if (currentTurn == StoneType.Black)
             {
                 myScore = board.BlackScore; oppScore = board.WhiteScore;
                 myWeightScore = board.BlackWeightScore; oppWeightScore = board.WhiteWeightScore;
             }
             else
             {
                 myScore = board.WhiteScore; oppScore = board.BlackScore;
                 myWeightScore = board.WhiteWeightScore; oppWeightScore = board.BlackWeightScore;
             }
             
             int score = 0;
             
             // 1. Position Weights
             score += (myWeightScore - oppWeightScore) * 2;
             
             // 2. Parity (Late Game)
             if (myScore + oppScore > 48)
             {
                 score += (myScore - oppScore) * 5;
             }
             
             // 3. Mobility (Conditional)
             if (settings.UseMobility)
             {
                 // Fast mobility check: just count valid moves. expensive but necessary for high difficulty
                 int myMoves = board.GetValidMoves(currentTurn).Count;
                 int oppMoves = board.GetValidMoves(currentTurn.Opposite()).Count;
                 score += (myMoves - oppMoves) * 5;
                 
                 // Corner Capture Bonus (Simple check)
                 // This is partly covered by PositionWeights, but explicit state check can be good
             }
             
             return score;
        }
    }
}

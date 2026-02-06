using System;
using System.Collections.Generic;

namespace Reversi.Domain
{
    public class BoardModel
    {
        public const int BoardSize = 8;
        
        // Step 1: Position Weights
        public static readonly int[,] PositionWeights = new int[8, 8]
        {
            { 120, -20,  20,   5,   5,  20, -20, 120 },
            { -20, -40,  -5,  -5,  -5,  -5, -40, -20 },
            {  20,  -5,  15,   3,   3,  15,  -5,  20 },
            {   5,  -5,   3,   3,   3,   3,  -5,   5 },
            {   5,  -5,   3,   3,   3,   3,  -5,   5 },
            {  20,  -5,  15,   3,   3,  15,  -5,  20 },
            { -20, -40,  -5,  -5,  -5,  -5, -40, -20 },
            { 120, -20,  20,   5,   5,  20, -20, 120 }
        };

        // Zobrist Hashing
        // [Row, Col, StoneType(0=Black, 1=White)] - Using index 0 for Black, 1 for White
        private static readonly long[,,] _zobristTable = new long[8, 8, 2];
        
        static BoardModel()
        {
            var rng = new System.Random(12345); // Fixed seed for consistency
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    // Random long generation
                    byte[] buf = new byte[8];
                    rng.NextBytes(buf); _zobristTable[r, c, 0] = BitConverter.ToInt64(buf, 0); // Black
                    rng.NextBytes(buf); _zobristTable[r, c, 1] = BitConverter.ToInt64(buf, 0); // White
                }
            }
        }

        private readonly StoneType[,] _board = new StoneType[BoardSize, BoardSize];
        
        // Incremental State
        public int BlackScore { get; private set; }
        public int WhiteScore { get; private set; }
        public int BlackWeightScore { get; private set; }
        public int WhiteWeightScore { get; private set; }
        
        public long CurrentHash { get; private set; }

        public event Action<BoardPosition, StoneType> OnStoneChanged;
        public event Action<List<BoardPosition>> OnStonesFlipped;
        public event Action<int, int> OnScoreChanged; // black, white

        private Stack<(BoardPosition pos, StoneType type, List<BoardPosition> flipped)> _history = new Stack<(BoardPosition, StoneType, List<BoardPosition>)>();


        public BoardModel()
        {
            Initialize();
        }

        public void Initialize()
        {
            Array.Clear(_board, 0, _board.Length);
            _history.Clear();
            
            // Reset Scores & Hash
            BlackScore = 0;
            WhiteScore = 0;
            BlackWeightScore = 0;
            WhiteWeightScore = 0;
            CurrentHash = 0;
            
            // Setup Initial Stones
            PlaceStoneInternal(3, 3, StoneType.White);
            PlaceStoneInternal(3, 4, StoneType.Black);
            PlaceStoneInternal(4, 3, StoneType.Black);
            PlaceStoneInternal(4, 4, StoneType.White);
        }

        private void PlaceStoneInternal(int r, int c, StoneType stone)
        {
            _board[r, c] = stone;
            
            int weight = PositionWeights[r, c];
            int typeIdx = (stone == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, typeIdx]; // XOR IN

            if (stone == StoneType.Black)
            {
                BlackScore++;
                BlackWeightScore += weight;
            }
            else if (stone == StoneType.White)
            {
                WhiteScore++;
                WhiteWeightScore += weight;
            }
        }

        private void RemoveStoneInternal(int r, int c)
        {
            StoneType stone = _board[r, c];
            if (stone == StoneType.None) return;

            int weight = PositionWeights[r, c];
            int typeIdx = (stone == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, typeIdx]; // XOR OUT

            if (stone == StoneType.Black)
            {
                BlackScore--;
                BlackWeightScore -= weight;
            }
            else if (stone == StoneType.White)
            {
                WhiteScore--;
                WhiteWeightScore -= weight;
            }

            _board[r, c] = StoneType.None;
        }

        private void FlipStoneInternal(int r, int c, StoneType newOwner)
        {
            // Remove from old owner
            StoneType oldOwner = _board[r, c];
            int weight = PositionWeights[r, c];
            int oldIdx = (oldOwner == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, oldIdx]; // XOR OUT old

            if (oldOwner == StoneType.Black)
            {
                BlackScore--;
                BlackWeightScore -= weight;
            }
            else if (oldOwner == StoneType.White)
            {
                WhiteScore--;
                WhiteWeightScore -= weight;
            }

            // Add to new owner
            _board[r, c] = newOwner;
            int newIdx = (newOwner == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, newIdx]; // XOR IN new

            if (newOwner == StoneType.Black)
            {
                BlackScore++;
                BlackWeightScore += weight;
            }
            else if (newOwner == StoneType.White)
            {
                WhiteScore++;
                WhiteWeightScore += weight;
            }
        }

        public StoneType GetStone(BoardPosition pos)
        {
            if (!pos.IsValid) return StoneType.None;
            return _board[pos.Row, pos.Col];
        }

        public StoneType GetStone(int row, int col)
        {
            return GetStone(new BoardPosition(row, col));
        }

        public bool CanPlaceStone(BoardPosition pos, StoneType stone)
        {
            if (!pos.IsValid) return false;
            if (_board[pos.Row, pos.Col] != StoneType.None) return false;
            
            return GetFlippableStones(pos, stone).Count > 0;
        }

        public List<BoardPosition> GetFlippableStones(BoardPosition pos, StoneType stone)
        {
            var flippable = new List<BoardPosition>();
            
            if (!pos.IsValid || stone == StoneType.None) return flippable;
            
            StoneType opponent = stone.Opposite();
            
            foreach (var (dr, dc) in BoardPosition.Directions)
            {
                var line = new List<BoardPosition>();
                int r = pos.Row + dr;
                int c = pos.Col + dc;
                
                while (r >= 0 && r < BoardSize && c >= 0 && c < BoardSize)
                {
                    if (_board[r, c] == opponent)
                    {
                        line.Add(new BoardPosition(r, c));
                        r += dr;
                        c += dc;
                    }
                    else if (_board[r, c] == stone)
                    {
                        flippable.AddRange(line);
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            
            return flippable;
        }

        public List<BoardPosition> GetValidMoves(StoneType stone)
        {
            var validMoves = new List<BoardPosition>();
            
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    var pos = new BoardPosition(r, c);
                    if (CanPlaceStone(pos, stone))
                    {
                        validMoves.Add(pos);
                    }
                }
            }
            
            return validMoves;
        }

        public bool PlaceStone(BoardPosition pos, StoneType stone)
        {
            if (!CanPlaceStone(pos, stone)) return false;
            
            var flippable = GetFlippableStones(pos, stone);
            
            // 1. Place new stone
            PlaceStoneInternal(pos.Row, pos.Col, stone);
            OnStoneChanged?.Invoke(pos, stone);
            
            // 2. Flip stones
            foreach (var flipPos in flippable)
            {
                FlipStoneInternal(flipPos.Row, flipPos.Col, stone);
            }
            OnStonesFlipped?.Invoke(flippable);
            
            // Push to history
            _history.Push((pos, stone, flippable));

            OnScoreChanged?.Invoke(BlackScore, WhiteScore);
            
            return true;
        }

        public void Undo()
        {
            if (_history.Count == 0) return;

            var (pos, type, flipped) = _history.Pop();

            // 1. Remove the placed stone
            RemoveStoneInternal(pos.Row, pos.Col);
            // OnStoneChanged?.Invoke(pos, StoneType.None); 

            // 2. Revert flipped stones (Flip back to opponent)
            StoneType originalOwner = type.Opposite();
            foreach (var flipPos in flipped)
            {
                FlipStoneInternal(flipPos.Row, flipPos.Col, originalOwner);
            }
        }

        public (int black, int white) GetScore()
        {
            return (BlackScore, WhiteScore);
        }

        public bool IsGameOver()
        {
            return GetValidMoves(StoneType.Black).Count == 0 
                && GetValidMoves(StoneType.White).Count == 0;
        }

        public bool IsBoardFull()
        {
            return (BlackScore + WhiteScore) >= 64;
        }
        
        // Private constructor for efficient cloning (if needed, though Undo is preferred)
        private BoardModel(StoneType[,] board, int bScore, int wScore, int bWeight, int wWeight, long hash)
        {
            _board = board;
            BlackScore = bScore;
            WhiteScore = wScore;
            BlackWeightScore = bWeight;
            WhiteWeightScore = wWeight;
            CurrentHash = hash;
        }

        public BoardModel Clone()
        {
            StoneType[,] newBoard = new StoneType[BoardSize, BoardSize];
            Array.Copy(_board, newBoard, _board.Length);
            return new BoardModel(newBoard, BlackScore, WhiteScore, BlackWeightScore, WhiteWeightScore, CurrentHash);
        }
    }
}

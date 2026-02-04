using System;
using System.Collections.Generic;

namespace Reversi.Game
{
    public class BoardModel
    {
        public const int BoardSize = 8;
        
        private readonly StoneType[,] _board = new StoneType[BoardSize, BoardSize];
        
        public event Action<BoardPosition, StoneType> OnStoneChanged;
        public event Action<List<BoardPosition>> OnStonesFlipped;
        public event Action<int, int> OnScoreChanged; // black, white

        public BoardModel()
        {
            Initialize();
        }

        public void Initialize()
        {
            Array.Clear(_board, 0, _board.Length);
            
            _board[3, 3] = StoneType.White;
            _board[3, 4] = StoneType.Black;
            _board[4, 3] = StoneType.Black;
            _board[4, 4] = StoneType.White;
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
            
          
            _board[pos.Row, pos.Col] = stone;
            OnStoneChanged?.Invoke(pos, stone);
            
          
            foreach (var flipPos in flippable)
            {
                _board[flipPos.Row, flipPos.Col] = stone;
            }
            OnStonesFlipped?.Invoke(flippable);
            
          
            var (black, white) = GetScore();
            OnScoreChanged?.Invoke(black, white);
            
            return true;
        }

        public (int black, int white) GetScore()
        {
            int black = 0, white = 0;
            
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    switch (_board[r, c])
                    {
                        case StoneType.Black: black++; break;
                        case StoneType.White: white++; break;
                    }
                }
            }
            
            return (black, white);
        }

        public bool IsGameOver()
        {
            return GetValidMoves(StoneType.Black).Count == 0 
                && GetValidMoves(StoneType.White).Count == 0;
        }

        public bool IsBoardFull()
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (_board[r, c] == StoneType.None) return false;
                }
            }
            return true;
        }
        private BoardModel(StoneType[,] board)
        {
            _board = board;
        }

        public BoardModel Clone()
        {
            StoneType[,] newBoard = new StoneType[BoardSize, BoardSize];
            Array.Copy(_board, newBoard, _board.Length);
            return new BoardModel(newBoard);
        }
    }
}

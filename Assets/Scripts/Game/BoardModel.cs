using System;
using System.Collections.Generic;

namespace Reversi.Game
{
    /// <summary>
    /// 보드 모델
    /// 게임 보드 상태 및 로직 관리
    /// </summary>
    public class BoardModel
    {
        public const int BoardSize = 8;
        
        // 위치별 가중치 (코너가 가장 높음)
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

        // Zobrist 해싱 테이블
        // [행, 열, 돌타입(0=흑, 1=백)]
        private static readonly long[,,] _zobristTable = new long[8, 8, 2];
        
        static BoardModel()
        {
            var rng = new System.Random(12345); // 일관성을 위한 고정 시드
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    // 랜덤 long 값 생성
                    byte[] buf = new byte[8];
                    rng.NextBytes(buf); _zobristTable[r, c, 0] = BitConverter.ToInt64(buf, 0); // 흑돌
                    rng.NextBytes(buf); _zobristTable[r, c, 1] = BitConverter.ToInt64(buf, 0); // 백돌
                }
            }
        }

        private readonly StoneType[,] _board = new StoneType[BoardSize, BoardSize];
        
        // 점진적 상태 (매번 계산 대신 증분 업데이트)
        public int BlackScore { get; private set; }
        public int WhiteScore { get; private set; }
        public int BlackWeightScore { get; private set; }
        public int WhiteWeightScore { get; private set; }
        
        public long CurrentHash { get; private set; }

        // 이벤트
        public event Action<BoardPosition, StoneType> OnStoneChanged;   // 돌 변경 시
        public event Action<List<BoardPosition>> OnStonesFlipped;       // 돌 뒤집힘 시
        public event Action<int, int> OnScoreChanged;                    // 점수 변경 시 (흑, 백)

        // 실행취소를 위한 히스토리 스택
        private Stack<(BoardPosition pos, StoneType type, List<BoardPosition> flipped)> _history = 
            new Stack<(BoardPosition, StoneType, List<BoardPosition>)>();


        public BoardModel()
        {
            Initialize();
        }

        /// <summary>
        /// 보드 초기화 (초기 4개 돌 배치)
        /// </summary>
        public void Initialize()
        {
            Array.Clear(_board, 0, _board.Length);
            _history.Clear();
            
            // 점수 및 해시 리셋
            BlackScore = 0;
            WhiteScore = 0;
            BlackWeightScore = 0;
            WhiteWeightScore = 0;
            CurrentHash = 0;
            
            // 초기 돌 배치
            PlaceStoneInternal(3, 3, StoneType.White);
            PlaceStoneInternal(3, 4, StoneType.Black);
            PlaceStoneInternal(4, 3, StoneType.Black);
            PlaceStoneInternal(4, 4, StoneType.White);
        }

        /// <summary>
        /// 내부 돌 배치 (이벤트 발생 없음)
        /// </summary>
        private void PlaceStoneInternal(int r, int c, StoneType stone)
        {
            _board[r, c] = stone;
            
            int weight = PositionWeights[r, c];
            int typeIdx = (stone == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, typeIdx]; // XOR 삽입

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

        /// <summary>
        /// 내부 돌 제거 (실행취소용)
        /// </summary>
        private void RemoveStoneInternal(int r, int c)
        {
            StoneType stone = _board[r, c];
            if (stone == StoneType.None) return;

            int weight = PositionWeights[r, c];
            int typeIdx = (stone == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, typeIdx]; // XOR 제거

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

        /// <summary>
        /// 내부 돌 뒤집기
        /// </summary>
        private void FlipStoneInternal(int r, int c, StoneType newOwner)
        {
            // 이전 소유자에서 제거
            StoneType oldOwner = _board[r, c];
            int weight = PositionWeights[r, c];
            int oldIdx = (oldOwner == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, oldIdx]; // 이전 값 XOR 제거

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

            // 새 소유자에게 추가
            _board[r, c] = newOwner;
            int newIdx = (newOwner == StoneType.Black) ? 0 : 1;
            
            CurrentHash ^= _zobristTable[r, c, newIdx]; // 새 값 XOR 삽입

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

        /// <summary>
        /// 특정 위치의 돌 조회
        /// </summary>
        public StoneType GetStone(BoardPosition pos)
        {
            if (!pos.IsValid) return StoneType.None;
            return _board[pos.Row, pos.Col];
        }

        public StoneType GetStone(int row, int col)
        {
            return GetStone(new BoardPosition(row, col));
        }

        /// <summary>
        /// 해당 위치에 돌을 놓을 수 있는지 확인
        /// </summary>
        public bool CanPlaceStone(BoardPosition pos, StoneType stone)
        {
            if (!pos.IsValid) return false;
            if (_board[pos.Row, pos.Col] != StoneType.None) return false;
            
            return GetFlippableStones(pos, stone).Count > 0;
        }

        /// <summary>
        /// 뒤집을 수 있는 돌 목록 반환
        /// </summary>
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

        /// <summary>
        /// 유효한 수 목록 반환
        /// </summary>
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

        /// <summary>
        /// 돌 놓기 (이벤트 발생)
        /// </summary>
        public bool PlaceStone(BoardPosition pos, StoneType stone)
        {
            if (!CanPlaceStone(pos, stone)) return false;
            
            var flippable = GetFlippableStones(pos, stone);
            
            // 1. 새 돌 놓기
            PlaceStoneInternal(pos.Row, pos.Col, stone);
            OnStoneChanged?.Invoke(pos, stone);
            
            // 2. 돌 뒤집기
            foreach (var flipPos in flippable)
            {
                FlipStoneInternal(flipPos.Row, flipPos.Col, stone);
            }
            OnStonesFlipped?.Invoke(flippable);
            
            // 히스토리에 저장
            _history.Push((pos, stone, flippable));

            OnScoreChanged?.Invoke(BlackScore, WhiteScore);
            
            return true;
        }

        /// <summary>
        /// 실행 취소
        /// </summary>
        public void Undo()
        {
            if (_history.Count == 0) return;

            var (pos, type, flipped) = _history.Pop();

            // 1. 놓은 돌 제거
            RemoveStoneInternal(pos.Row, pos.Col);

            // 2. 뒤집힌 돌 원래대로 복원
            StoneType originalOwner = type.Opposite();
            foreach (var flipPos in flipped)
            {
                FlipStoneInternal(flipPos.Row, flipPos.Col, originalOwner);
            }
        }

        /// <summary>
        /// 현재 점수 조회
        /// </summary>
        public (int black, int white) GetScore()
        {
            return (BlackScore, WhiteScore);
        }

        /// <summary>
        /// 게임 종료 여부 확인
        /// </summary>
        public bool IsGameOver()
        {
            return GetValidMoves(StoneType.Black).Count == 0 
                && GetValidMoves(StoneType.White).Count == 0;
        }

        /// <summary>
        /// 보드가 가득 찼는지 확인
        /// </summary>
        public bool IsBoardFull()
        {
            return (BlackScore + WhiteScore) >= 64;
        }
        
        // 효율적인 복제를 위한 private 생성자
        private BoardModel(StoneType[,] board, int bScore, int wScore, int bWeight, int wWeight, long hash)
        {
            _board = board;
            BlackScore = bScore;
            WhiteScore = wScore;
            BlackWeightScore = bWeight;
            WhiteWeightScore = wWeight;
            CurrentHash = hash;
        }

        /// <summary>
        /// 보드 복제 (AI 시뮬레이션용)
        /// </summary>
        public BoardModel Clone()
        {
            StoneType[,] newBoard = new StoneType[BoardSize, BoardSize];
            Array.Copy(_board, newBoard, _board.Length);
            return new BoardModel(newBoard, BlackScore, WhiteScore, BlackWeightScore, WhiteWeightScore, CurrentHash);
        }
    }
}

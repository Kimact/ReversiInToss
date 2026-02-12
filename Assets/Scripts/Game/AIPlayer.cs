using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Reversi.Game
{
    /// <summary>
    /// AI 플레이어
    /// Negamax 알고리즘 + Alpha-Beta 가지치기 + Transposition Table 사용
    /// </summary>
    public class AIPlayer
    {
        private StoneType _aiColor;
        private int _level; // 난이도 레벨 (1~)

        /// <summary>
        /// 난이도 설정 구조체
        /// </summary>
        private struct DifficultySetting
        {
            public int MaxDepth;           // 최대 탐색 깊이
            public float ErrorProbability; // 실수 확률
            public bool UseMobility;       // 이동성 평가 사용 여부
            public bool UseEndgameSolver;  // 엔드게임 솔버 사용 여부
        }

        /// <summary>
        /// 난이도 레벨에 따른 설정 반환
        /// </summary>
        private DifficultySetting GetDifficultySetting(int level)
        {
            // 스레드 안전을 위해 System.Math 사용
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

        // Transposition Table 플래그
        private enum TTFlag { Exact, LowerBound, UpperBound }
        
        /// <summary>
        /// Transposition Table 엔트리
        /// </summary>
        private struct TTEntry
        {
            public int Depth;              // 탐색 깊이
            public int Score;              // 평가 점수
            public TTFlag Flag;            // 점수 유형
            public BoardPosition BestMove; // 최선의 수
        }
        
        // Zobrist 해시를 키로 사용하는 Transposition Table
        private Dictionary<long, TTEntry> _transpositionTable = new Dictionary<long, TTEntry>();

        public AIPlayer(StoneType color, int difficulty)
        {
            _aiColor = color;
            _level = difficulty;
        }

        /// <summary>
        /// 비동기로 최선의 수 계산
        /// </summary>
        public async Task<BoardPosition> GetBestMoveAsync(BoardModel board)
        {
            // 스레드 풀에서 실행
            return await Task.Run(() => GetBestMove(board));
        }

        /// <summary>
        /// 최선의 수 계산 (메인 로직)
        /// </summary>
        private BoardPosition GetBestMove(BoardModel originalBoard)
        {
            // UI에 영향 주지 않도록 보드 복제
            BoardModel board = originalBoard.Clone();

            var settings = GetDifficultySetting(_level);
            var validMoves = board.GetValidMoves(_aiColor);
            
            if (validMoves.Count == 0) return BoardPosition.Nowhere;
            if (validMoves.Count == 1) return validMoves[0];

            // 스레드 안전 랜덤
            System.Random rng = new System.Random();

            // 1. 실수 주입 (낮은 난이도에서 가끔 랜덤 수 선택)
            if (rng.NextDouble() < settings.ErrorProbability)
            {
                return validMoves[rng.Next(validMoves.Count)];
            }

            // 2. 엔드게임 솔버 체크
            int emptyCount = 64 - (board.BlackScore + board.WhiteScore);
            if (settings.UseEndgameSolver && emptyCount <= 12)
            {
                // 엔드게임은 깊이 제한 없이 탐색
                settings.MaxDepth = 64; 
            }

            _transpositionTable.Clear();

            BoardPosition bestMove = validMoves[0];
            int maxDepth = settings.MaxDepth;
            long timeLimit = 2000; // 2초 제한
            
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 반복 심화 (Iterative Deepening)
                for (int d = 2; d <= maxDepth; d += 2)
                {
                    if (sw.ElapsedMilliseconds > timeLimit) break;
                    
                    int val = NegaMax(board, d, int.MinValue + 1, int.MaxValue - 1, _aiColor, settings, sw, timeLimit);
                    
                    // TT에서 최선의 수 추출
                    if (_transpositionTable.TryGetValue(board.CurrentHash, out var entry))
                    {
                        if (entry.BestMove.IsValid) bestMove = entry.BestMove;
                    }
                }
            }
            catch (TimeoutException) { }

            return bestMove;
        }

        /// <summary>
        /// Negamax 알고리즘 (Alpha-Beta 가지치기 포함)
        /// </summary>
        private int NegaMax(BoardModel board, int depth, int alpha, int beta, StoneType currentTurn, DifficultySetting settings, System.Diagnostics.Stopwatch sw, long timeLimit)
        {
            if (sw.ElapsedMilliseconds > timeLimit) throw new TimeoutException();

            int originalAlpha = alpha;

            // 1. Transposition Table 조회
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

            // 종료 조건: 깊이 0 또는 게임 종료
            if (depth == 0 || board.IsGameOver())
            {
                return Evaluate(board, currentTurn, settings);
            }

            var validMoves = board.GetValidMoves(currentTurn);
            if (validMoves.Count == 0)
            {
                // 패스: 상대방 턴으로 넘어감
                return -NegaMax(board, depth - 1, -beta, -alpha, currentTurn.Opposite(), settings, sw, timeLimit);
            }

            // 2. 수 정렬 (Move Ordering)
            // A. TT에서 최선의 수 (Hash Move) 먼저
            BoardPosition hashMove = BoardPosition.Nowhere;
            if (ttEntry.BestMove.IsValid) hashMove = ttEntry.BestMove;

            // B. 위치 가중치로 정렬
            validMoves.Sort((a, b) => 
            {
                if (a.Equals(hashMove)) return -1;
                if (b.Equals(hashMove)) return 1;
                
                int weightA = BoardModel.PositionWeights[a.Row, a.Col];
                int weightB = BoardModel.PositionWeights[b.Row, b.Col];
                return weightB.CompareTo(weightA); // 내림차순
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
                if (alpha >= beta) break; // 가지치기
            }

            // 3. TT에 저장
            var newEntry = new TTEntry
            {
                Depth = depth,
                Score = bestVal,
                BestMove = bestMove,
                Flag = (bestVal <= originalAlpha) ? TTFlag.UpperBound : (bestVal >= beta) ? TTFlag.LowerBound : TTFlag.Exact
            };
            
            // 더 깊은 탐색 결과가 있으면 덮어쓰기
            if (!_transpositionTable.ContainsKey(board.CurrentHash) || depth >= _transpositionTable[board.CurrentHash].Depth)
            {
                _transpositionTable[board.CurrentHash] = newEntry;
            }

            return bestVal;
        }
        
        // 재귀 호출용 오버로드
        private int NegaMax(BoardModel board, int depth, int alpha, int beta, StoneType currentTurn, DifficultySetting settings, System.Diagnostics.Stopwatch sw, long timeLimit, bool checkTime)
        {
            return NegaMax(board, depth, alpha, beta, currentTurn, settings, sw, timeLimit);
        }

        /// <summary>
        /// 보드 상태 평가 함수
        /// </summary>
        private int Evaluate(BoardModel board, StoneType currentTurn, DifficultySetting settings)
        {
            // currentTurn 관점에서 평가
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
            
            // 1. 위치 가중치 점수
            score += (myWeightScore - oppWeightScore) * 2;
            
            // 2. 패리티 (후반전)
            if (myScore + oppScore > 48)
            {
                score += (myScore - oppScore) * 5;
            }
            
            // 3. 이동성 (조건부)
            if (settings.UseMobility)
            {
                // 유효한 수 개수로 이동성 평가
                int myMoves = board.GetValidMoves(currentTurn).Count;
                int oppMoves = board.GetValidMoves(currentTurn.Opposite()).Count;
                score += (myMoves - oppMoves) * 5;
            }
            
            return score;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Reversi.Game
{
    public class AIPlayer
    {
        private StoneType _aiColor;
        private int _difficulty; // 1 ~ 10

        public AIPlayer(StoneType color, int difficulty)
        {
            _aiColor = color;
            _difficulty = Mathf.Clamp(difficulty, 1, 10);
        }

        public async Task<BoardPosition> GetBestMoveAsync(BoardModel board)
        {
            // 비동기로 계산하여 메인 스레드 프리징 방지
            return await Task.Run(() => GetBestMove(board));
        }

        private BoardPosition GetIterativeDeepeningMove(BoardModel board, List<BoardPosition> validMoves)
        {
            System.Random rng = new System.Random();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            
            // 턴마다 랜덤한 시간 제한 설정 (1초 ~ 15초)
            int timeLimitMs = rng.Next(1000, 15001);
            
            sw.Start();

            BoardPosition bestMove = validMoves[0];
            int maxDepth = 12; // 충분히 깊게

            // 초기 정렬 (Move Ordering)
            validMoves.Sort((a, b) => EvaluateMovePriority(board, b).CompareTo(EvaluateMovePriority(board, a)));

            try
            {
                for (int depth = 1; depth <= maxDepth; depth++)
                {
                    if (sw.ElapsedMilliseconds > timeLimitMs) break;
                    
                    // 이번 Depth에서의 최고 수 찾기
                    BoardPosition currentBestMove = validMoves[0];
                    int bestValue = int.MinValue;
                    int alpha = int.MinValue;
                    int beta = int.MaxValue;

                    foreach (var move in validMoves)
                    {
                        // Pass randomized timeLimitMs to Minimax
                        if (sw.ElapsedMilliseconds > timeLimitMs) throw new System.TimeoutException();

                        var clone = board.Clone();
                        clone.PlaceStone(move, _aiColor);

                        int value = Minimax(clone, depth - 1, alpha, beta, false, sw, timeLimitMs);

                        if (value > bestValue || (value == bestValue && rng.Next(0, 2) == 0))
                        {
                            bestValue = value;
                            currentBestMove = move;
                        }
                        alpha = Mathf.Max(alpha, bestValue);
                    }

                    bestMove = currentBestMove;
                }
            }
            catch (System.TimeoutException)
            {
                // 시간 초과 시 이전 Depth까지 찾은 bestMove 반환
            }

            return bestMove;
        }

        private int EvaluateMovePriority(BoardModel board, BoardPosition pos)
        {
            // Move Ordering을 위한 간단한 가중치 (코너 10점, 그 외 0점)
            if ((pos.Row == 0 || pos.Row == 7) && (pos.Col == 0 || pos.Col == 7)) return 10;
            return 0;
        }

        private int Minimax(BoardModel board, int depth, int alpha, int beta, bool maximizingPlayer, System.Diagnostics.Stopwatch sw, int timeLimitMs)
        {
            if (sw.ElapsedMilliseconds > timeLimitMs) throw new System.TimeoutException();

            if (depth == 0 || board.IsGameOver())
            {
                return Evaluate(board);
            }

            StoneType currentTurn = maximizingPlayer ? _aiColor : _aiColor.Opposite();
            var moves = board.GetValidMoves(currentTurn);

            if (moves.Count == 0)
            {
                // Pass case
                return Minimax(board, depth - 1, alpha, beta, !maximizingPlayer, sw, timeLimitMs);
            }

            if (maximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (var move in moves)
                {
                    var clone = board.Clone();
                    clone.PlaceStone(move, currentTurn);
                    int eval = Minimax(clone, depth - 1, alpha, beta, false, sw, timeLimitMs);
                    maxEval = Mathf.Max(maxEval, eval);
                    alpha = Mathf.Max(alpha, eval);
                    if (beta <= alpha) break;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in moves)
                {
                    var clone = board.Clone();
                    clone.PlaceStone(move, currentTurn);
                    int eval = Minimax(clone, depth - 1, alpha, beta, true, sw, timeLimitMs);
                    minEval = Mathf.Min(minEval, eval);
                    beta = Mathf.Min(beta, eval);
                    if (beta <= alpha) break;
                }
                return minEval;
            }
        }

        // 위치 가중치 (Positional Weights)
        // 코너(100)는 매우 좋음, X-Square(-50)는 매우 나쁨
        private static readonly int[,] PositionWeights = new int[8, 8]
        {
            { 100, -20,  10,   5,   5,  10, -20, 100 },
            { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
            {  10,  -2,  -1,  -1,  -1,  -1,  -2,  10 },
            {   5,  -2,  -1,  -1,  -1,  -1,  -2,   5 },
            {   5,  -2,  -1,  -1,  -1,  -1,  -2,   5 },
            {  10,  -2,  -1,  -1,  -1,  -1,  -2,  10 },
            { -20, -50,  -2,  -2,  -2,  -2, -50, -20 },
            { 100, -20,  10,   5,   5,  10, -20, 100 }
        };

        // 평가 함수 (Heuristic)
        private int Evaluate(BoardModel board)
        {
            int score = 0;
            StoneType myColor = _aiColor;
            StoneType oppColor = _aiColor.Opposite();

            // 1. 돌 개수 (초반엔 오히려 적은 게 유리할 수도 있으나, 여기선 단순 가산)
            // (가중치를 낮춤)
            var (black, white) = board.GetScore();
            int myCount = (myColor == StoneType.Black) ? black : white;
            int oppCount = (myColor == StoneType.Black) ? white : black;
            score += (myCount - oppCount); // 1점씩

            // 2. 위치 가중치 (매우 중요)
            score += EvaluatePositionWeights(board, myColor);

            // 3. 기동성 (Mobility) - 내가 둘 곳이 많을수록 유리
            int myMoves = board.GetValidMoves(myColor).Count;
            int oppMoves = board.GetValidMoves(oppColor).Count;
            score += (myMoves - oppMoves) * 10;

            return score;
        }

        private int EvaluatePositionWeights(BoardModel board, StoneType myColor)
        {
            int weightScore = 0;
            StoneType oppColor = myColor.Opposite();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    StoneType s = board.GetStone(r, c);
                    if (s == myColor) weightScore += PositionWeights[r, c];
                    else if (s == oppColor) weightScore -= PositionWeights[r, c];
                }
            }
            return weightScore;
        }
        
        // Remove old EvaluateCorners as it is subsumed by PositionWeights
        // Or keep it if you want extra corner weight, but table handles it.
    }
}

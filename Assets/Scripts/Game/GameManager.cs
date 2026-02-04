using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reversi.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float turnTimeLimit = 15f;

        [Header("AI Settings")]
        [SerializeField] private bool isAIEnabled = false;
        [SerializeField] private int aiDifficulty = 1;
        
        private AIPlayer _aiPlayer;

        private BoardModel _boardModel;
        private GameState _currentState = GameState.Waiting;
        private float _turnTimer;

        private List<PlaceStoneCommand> _commandHistory = new List<PlaceStoneCommand>();

        
        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnTurnTimerUpdated;
        public event Action<StoneType> OnGameEnded; 

        public BoardModel BoardModel => _boardModel;
        public GameState CurrentState => _currentState;
        public StoneType CurrentTurn => _currentState.GetCurrentStone();
        public float TurnTimeLimit => turnTimeLimit;
        public float RemainingTime => _turnTimer;
        
        public bool IsAIEnabled => isAIEnabled;
        public int AiDifficulty => aiDifficulty;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _boardModel = new BoardModel();
        }

        private void Update()
        {
            if (_currentState.IsPlaying())
            {
                UpdateTurnTimer();
            }
        }

        public void StartGame()
        {
            StartGame(false); // Default to PvP
        }

        public void StartGame(bool useAI = false, int difficulty = 1)
        {
            isAIEnabled = useAI;
            aiDifficulty = difficulty;
            
            if (isAIEnabled)
            {
                // AI는 백돌(후공)
                _aiPlayer = new AIPlayer(StoneType.White, aiDifficulty);
            }
            
            _boardModel.Initialize();
            _commandHistory.Clear();
            SetState(GameState.BlackTurn); // 흑이 먼저
        }

        public bool TryPlaceStone(BoardPosition pos)
        {
            if (!_currentState.IsPlaying()) return false;

            StoneType currentStone = _currentState.GetCurrentStone();
            
            if (!_boardModel.CanPlaceStone(pos, currentStone)) return false;

            // AI 턴에 플레이어가 조작하는 것을 방지
            if (isAIEnabled && currentStone == StoneType.White) return false;
        
            var command = new PlaceStoneCommand(pos, currentStone);
            _commandHistory.Add(command);

        
            _boardModel.PlaceStone(pos, currentStone);

        
            ProcessNextTurn();

            return true;
        }

        private void ProcessNextTurn()
        {
        
            if (_boardModel.IsGameOver())
            {
                EndGame();
                return;
            }

            // 다음 턴으로 전환
            GameState nextState = _currentState.GetNextTurnState();
            SetState(nextState); // 상태 먼저 변경

            StoneType nextStone = nextState.GetCurrentStone();
            
            // AI 턴인지 확인
            if (isAIEnabled && nextStone == StoneType.White) // AI는 백돌로 가정
            {
                StartCoroutine(ProcessAITurn());
                return;
            }

            // 둘 곳이 있는지 확인
            if (_boardModel.GetValidMoves(nextStone).Count == 0)
            {
        
                if (_boardModel.GetValidMoves(nextStone.Opposite()).Count == 0)
                {
                    EndGame();
                    return;
                }
        
                Debug.Log($"{nextStone.ToDisplayString()} 패스!");
                
                // 패스면 다시 상태 변경 (Recursive call might affect AI logic, handle carefully)
                // 단순히 상대 턴으로 넘김
                SetState(_currentState.GetNextTurnState());
                // 만약 넘긴 턴이 또 AI라면? (Human Pass -> AI turn)
                if (isAIEnabled && _currentState.GetCurrentStone() == StoneType.White)
                {
                    StartCoroutine(ProcessAITurn());
                }
            }
        }
        
        private System.Collections.IEnumerator ProcessAITurn()
        {
            // AI 생각하는 척 딜레이 (최소 1초)
            yield return new WaitForSeconds(1f);
            
            if (_aiPlayer != null)
            {
                var task = _aiPlayer.GetBestMoveAsync(_boardModel);
                yield return new WaitUntil(() => task.IsCompleted);
                
                BoardPosition bestMove = task.Result;
                
                if (bestMove.IsValid)
                {
                    // AI 착수
                    _boardModel.PlaceStone(bestMove, StoneType.White);
                    var command = new PlaceStoneCommand(bestMove, StoneType.White);
                    _commandHistory.Add(command);
                    
                    ProcessNextTurn(); // 턴 넘기기
                }
                else
                {
                    // AI도 둘 곳이 없으면 패스 (GetBestMove가 (-1,-1) 반환)
                     Debug.Log("AI 패스!");
                     ProcessNextTurn();
                }
            }
        }

        private void EndGame()
        {
            SetState(GameState.GameOver);

            var (black, white) = _boardModel.GetScore();
            StoneType winner = StoneType.None;
            
            if (black > white) winner = StoneType.Black;
            else if (white > black) winner = StoneType.White;
            
            Debug.Log($"게임 종료! 흑: {black}, 백: {white}, 승자: {winner.ToDisplayString()}");
            OnGameEnded?.Invoke(winner);
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
            _turnTimer = turnTimeLimit;
            OnGameStateChanged?.Invoke(newState);
        }

        private void UpdateTurnTimer()
        {

            _turnTimer -= Time.deltaTime;
            OnTurnTimerUpdated?.Invoke(_turnTimer);

            if (_turnTimer <= 0)
            {
                Debug.Log($"{CurrentTurn.ToDisplayString()} 시간 초과!");
                ProcessNextTurn();
            }
        }

        public void ResetGame()
        {
            SetState(GameState.Waiting);
            _boardModel.Initialize();
            _commandHistory.Clear();
        }
    }
}

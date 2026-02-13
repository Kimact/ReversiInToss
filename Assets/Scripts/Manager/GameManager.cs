using System;
using System.Collections.Generic;
using UnityEngine;
using Reversi.Game;
using Reversi.UI;

namespace Reversi.Manager
{

    public class GameManager : MonoBehaviour
    {
        // 뷰 참조
        private BoardView _boardView;
        private GameUI _gameUI;

        [Header("설정")]
        [SerializeField] private float turnTimeLimit = 15f;

        [Header("AI 설정")]
        [SerializeField] private bool isAIEnabled = true;
        [SerializeField] private int aiDifficulty = 1;
        
        // 도메인 객체
        private AIPlayer _aiPlayer;
        private BoardModel _boardModel;
        private GameState _currentState = GameState.Waiting;
        private float _turnTimer;
        private List<PlaceStoneCommand> _commandHistory = new List<PlaceStoneCommand>();

        private void Awake()
        {
            // 품질 설정 (부드러운 화면)
            UnityEngine.Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4; // MSAA 4x 강제
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _boardModel = new BoardModel();
        }

        public void Initialize(BoardView boardView, GameUI gameUI)
        {
            _boardView = boardView;
            _gameUI = gameUI;

            // 뷰 이벤트 구독
            if (_boardView != null) _boardView.OnBoardClick += HandleBoardClick;
            if (_gameUI != null)
            {
                _gameUI.OnStartGameClick += () => StartGame(isAIEnabled, aiDifficulty); // 인스펙터 설정 사용
                _gameUI.OnRestartGameClick += ResetGame;
                _gameUI.OnReplayClick += StartReplay;
            }
        }


        private void StartReplay()
        {
            StartCoroutine(ReplayRoutine());
        }

  
        private System.Collections.IEnumerator ReplayRoutine()
        {
            // 1. 히스토리 유지하며 보드 리셋
            var historyCopy = new List<PlaceStoneCommand>(_commandHistory);
            
            _boardModel.Initialize();
            
            _gameUI.ShowGamePanel();
            _boardView?.RefreshBoard(_boardModel); // 초기 상태
            
            yield return new WaitForSeconds(1f);

            // 2. 수 재생
            foreach (var cmd in historyCopy)
            {
                _boardModel.PlaceStone(cmd.Position, cmd.Stone);
                // 뷰는 이벤트로 업데이트
                
                yield return new WaitForSeconds(0.5f); // 빠른 재생
            }
        }

        private void Update()
        {
            if (_currentState.IsPlaying())
            {
                UpdateTurnTimer();
            }
        }


        public void StartGame(bool useAI, int difficulty)
        {
            isAIEnabled = useAI;
            aiDifficulty = difficulty;
            
            if (isAIEnabled)
            {
                _aiPlayer = new AIPlayer(StoneType.White, aiDifficulty);
            }
            else
            {
                _aiPlayer = null;
            }
            
            _boardModel.Initialize();
            _commandHistory.Clear();
            
            //// 앱인토스: 유저 키 로그
            //if (TossService.Instance != null && TossService.Instance.IsInitialized)
            //{
            //    Debug.Log($"[앱인토스] 유저 키: {TossService.Instance.UserKey}");
            //}
            
            // UI 업데이트
            _gameUI?.ShowGamePanel();
            
            // 중요: BoardView를 BoardModel과 연결하여 이벤트 구독
            _boardView?.Initialize(_boardModel);
            _boardView?.RefreshBoard(_boardModel);
            
            SetState(GameState.BlackTurn);
        }

 
        public void ResetGame()
        {
            SetState(GameState.Waiting);
            _boardModel.Initialize();
            _commandHistory.Clear();
            
            _gameUI?.ShowStartPanel();
            _boardView?.RefreshBoard(_boardModel);
        }

        private void HandleBoardClick(BoardPosition pos)
        {
            TryPlaceStone(pos);
        }

     
        public void TryPlaceStone(BoardPosition pos)
        {
            if (!_currentState.IsPlaying()) return;

            StoneType currentStone = _currentState.GetCurrentStone();
            
            if (!_boardModel.CanPlaceStone(pos, currentStone)) return;

            if (isAIEnabled && currentStone == StoneType.White) return;
        
            var command = new PlaceStoneCommand(pos, currentStone);
            _commandHistory.Add(command);

            _boardModel.PlaceStone(pos, currentStone);
            
            // 앱인토스: 돌 놓을 때 햅틱 피드백
            //TossService.Instance?.TriggerHaptic("Tap");
            
            ProcessNextTurn();
        }

        private void ProcessNextTurn()
        {
            if (_boardModel.IsGameOver())
            {
                EndGame();
                return;
            }

            GameState nextState = _currentState.GetNextTurnState();
            SetState(nextState);

            StoneType nextStone = nextState.GetCurrentStone();
            
            // AI 체크
            if (isAIEnabled && nextStone == StoneType.White)
            {
                StartCoroutine(ProcessAITurn());
                return;
            }

            // 유효한 수 체크
            if (_boardModel.GetValidMoves(nextStone).Count == 0)
            {
                if (_boardModel.GetValidMoves(nextStone.Opposite()).Count == 0)
                {
                    EndGame();
                    return;
                }
                
                Debug.Log($"{nextStone} 패스");
                SetState(_currentState.GetNextTurnState());
                
                // AI로 다시 넘어가면 AI 턴 처리
                if (isAIEnabled && _currentState.GetCurrentStone() == StoneType.White)
                {
                    StartCoroutine(ProcessAITurn());
                }
            }
        }
        
 
        private System.Collections.IEnumerator ProcessAITurn()
        {
            yield return new WaitForSeconds(1f);
            
            if (_aiPlayer != null)
            {
                var task = _aiPlayer.GetBestMoveAsync(_boardModel);
                yield return new WaitUntil(() => task.IsCompleted);
                
                BoardPosition bestMove = task.Result;
                
                if (bestMove.IsValid)
                {
                    _boardModel.PlaceStone(bestMove, StoneType.White);
                    _commandHistory.Add(new PlaceStoneCommand(bestMove, StoneType.White));
                    ProcessNextTurn();
                }
                else
                {
                    Debug.Log("AI 패스");
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
            
            //// 앱인토스: 게임 종료 시 햅틱 (승리=Heavy, 그 외=Light)
            //if (TossService.Instance != null)
            //{
            //    bool playerWon = (winner == StoneType.Black);
            //    TossService.Instance.TriggerHaptic(playerWon ? "Heavy" : "Light");
            //}
            
            _gameUI?.ShowGameResult(winner, black, white);
        }


        private void SetState(GameState newState)
        {
            _currentState = newState;
            _turnTimer = turnTimeLimit;
            
            _gameUI?.UpdateTurn(newState);
            _boardView?.UpdateHighlights(_boardModel, _currentState.GetCurrentStone());
        }

       
        private void UpdateTurnTimer()
        {
            _turnTimer -= Time.deltaTime;
            _gameUI?.UpdateTimer(_turnTimer);

            if (_turnTimer <= 0)
            {
                Debug.Log("시간 초과");
                ProcessNextTurn();
            }
        }
    }
}

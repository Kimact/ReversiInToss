using System;
using System.Collections.Generic;
using UnityEngine;
using Reversi.Domain;
using Reversi.View;

namespace Reversi.Presentation
{
    public class GamePresenter : MonoBehaviour
    {
        // View References
        private BoardView _boardView;
        private GameUI _gameUI;

        [Header("Settings")]
        [SerializeField] private float turnTimeLimit = 15f;

        [Header("AI Settings")]
        [SerializeField] private bool isAIEnabled = true;
        [SerializeField] private int aiDifficulty = 1;
        
        // Domain Objects
        private AIPlayer _aiPlayer;
        private BoardModel _boardModel;
        private GameState _currentState = GameState.Waiting;
        private float _turnTimer;
        private List<PlaceStoneCommand> _commandHistory = new List<PlaceStoneCommand>();

        private void Awake()
        {
            _boardModel = new BoardModel();
        }

        public void Initialize(BoardView boardView, GameUI gameUI)
        {
            _boardView = boardView;
            _gameUI = gameUI;

            // Subscribe to View Events
            if (_boardView != null) _boardView.OnBoardClick += HandleBoardClick;
            if (_gameUI != null)
            {
                _gameUI.OnStartGameClick += () => StartGame(isAIEnabled, aiDifficulty); // Use Inspector settings
                _gameUI.OnRestartGameClick += ResetGame;
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
            
            // UI Update
            _gameUI?.ShowGamePanel();
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

        // Changed return type to void as it's event handler mostly
        public void TryPlaceStone(BoardPosition pos)
        {
            if (!_currentState.IsPlaying()) return;

            StoneType currentStone = _currentState.GetCurrentStone();
            
            if (!_boardModel.CanPlaceStone(pos, currentStone)) return;

            if (isAIEnabled && currentStone == StoneType.White) return;
        
            var command = new PlaceStoneCommand(pos, currentStone);
            _commandHistory.Add(command);

            _boardModel.PlaceStone(pos, currentStone);
            
            // View Update
            _boardView?.RefreshBoard(_boardModel); // Or optimize to update specific cell
            
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
            
            // Check AI
            if (isAIEnabled && nextStone == StoneType.White)
            {
                StartCoroutine(ProcessAITurn());
                return;
            }

            // Check Valid Moves
            if (_boardModel.GetValidMoves(nextStone).Count == 0)
            {
                if (_boardModel.GetValidMoves(nextStone.Opposite()).Count == 0)
                {
                    EndGame();
                    return;
                }
                
                Debug.Log($"{nextStone} Pass");
                SetState(_currentState.GetNextTurnState());
                
                // Recurse check for AI logic if passed back to AI
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
                    _boardView?.RefreshBoard(_boardModel);
                    ProcessNextTurn();
                }
                else
                {
                     Debug.Log("AI Pass");
                     ProcessNextTurn(); // Pass
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
                Debug.Log("Time Over");
                ProcessNextTurn(); // Auto pass or lose? Previously just ProcessNextTurn implies pass/random move? Logic was vague. Strict MVP just calls logic.
            }
        }
    }
}

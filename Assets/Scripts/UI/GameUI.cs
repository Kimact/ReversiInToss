using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Reversi.Game;

namespace Reversi.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI blackScoreText;
        [SerializeField] private TextMeshProUGUI whiteScoreText;
        
        [Header("Turn Display")]
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image turnIndicator;
        
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;
        
        [Header("Panels")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        
        [Header("Colors")]
        [SerializeField] private Color blackTurnColor = Color.black;
        [SerializeField] private Color whiteTurnColor = Color.white;

        private void Start()
        {
            SetupButtons();
            SubscribeToEvents();
            ShowStartPanel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SetupButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            }
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance == null) return;
            
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnTurnTimerUpdated += HandleTimerUpdated;
            GameManager.Instance.OnGameEnded += HandleGameEnded;
            GameManager.Instance.BoardModel.OnScoreChanged += HandleScoreChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance == null) return;
            
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnTurnTimerUpdated -= HandleTimerUpdated;
            GameManager.Instance.OnGameEnded -= HandleGameEnded;
            GameManager.Instance.BoardModel.OnScoreChanged -= HandleScoreChanged;
        }

        private void OnStartButtonClicked()
        {
            StartGame();
        }

        private void OnRestartButtonClicked()
        {
            StartGame();
        }

        private void StartGame()
        {
            if (GameManager.Instance == null) return;
            
            ShowGamePanel();
            
            // GameManager의 Inspector 설정을 그대로 사용
            GameManager.Instance.StartGame(GameManager.Instance.IsAIEnabled, GameManager.Instance.AiDifficulty);
            
            var boardView = FindObjectOfType<View.BoardView>();
            if (boardView != null)
            {
                boardView.RefreshBoard();
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            UpdateTurnDisplay(state);
            
            if (state == GameState.GameOver)
            {
                ShowResultPanel();
            }
        }

        private void UpdateTurnDisplay(GameState state)
        {
            if (turnText == null) return;
            
            string turnString = state switch
            {
                GameState.BlackTurn => "흑 차례",
                GameState.WhiteTurn => "백 차례",
                GameState.GameOver => "게임 종료",
                _ => "대기 중"
            };
            
            turnText.text = turnString;
            
            if (turnIndicator != null)
            {
                turnIndicator.color = state switch
                {
                    GameState.BlackTurn => blackTurnColor,
                    GameState.WhiteTurn => whiteTurnColor,
                    _ => Color.gray
                };
            }
        }

        private void HandleTimerUpdated(float remainingTime)
        {
            if (timerText == null) return;
            timerText.text = $"{Mathf.CeilToInt(remainingTime)}초";
        }

        private void HandleScoreChanged(int black, int white)
        {
            if (blackScoreText != null)
            {
                blackScoreText.text = black.ToString();
            }
            if (whiteScoreText != null)
            {
                whiteScoreText.text = white.ToString();
            }
        }

        private void HandleGameEnded(StoneType winner)
        {
            if (resultText == null) return;
            
            var (black, white) = GameManager.Instance.BoardModel.GetScore();
            
            string resultString = winner switch
            {
                StoneType.Black => $"흑 승리!\n{black} : {white}",
                StoneType.White => $"백 승리!\n{black} : {white}",
                _ => $"무승부!\n{black} : {white}"
            };
            
            resultText.text = resultString;
            ShowResultPanel();
        }

        private void ShowStartPanel()
        {
            SetPanelActive(startPanel, true);
            SetPanelActive(gamePanel, false);
            SetPanelActive(resultPanel, false);
        }

        private void ShowGamePanel()
        {
            SetPanelActive(startPanel, false);
            SetPanelActive(gamePanel, true);
            SetPanelActive(resultPanel, false);
        }

        private void ShowResultPanel()
        {
            SetPanelActive(startPanel, false);
            SetPanelActive(gamePanel, true);
            SetPanelActive(resultPanel, true);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Reversi.Domain;

namespace Reversi.View
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

        // Events
        public event System.Action OnStartGameClick;
        public event System.Action OnRestartGameClick;

        private void Start()
        {
            SetupButtons();
            ShowStartPanel();
        }

        private void SetupButtons()
        {
            if (startButton != null)
                startButton.onClick.AddListener(() => OnStartGameClick?.Invoke());
            
            if (restartButton != null)
                restartButton.onClick.AddListener(() => OnRestartGameClick?.Invoke());
        }

        public void UpdateScore(int black, int white)
        {
            if (blackScoreText != null) blackScoreText.text = black.ToString();
            if (whiteScoreText != null) whiteScoreText.text = white.ToString();
        }

        public void UpdateTurn(GameState state)
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

            if (state == GameState.GameOver) ShowResultPanel();
        }

        public void UpdateTimer(float remainingTime)
        {
            if (timerText != null) 
                timerText.text = $"{Mathf.CeilToInt(remainingTime)}초";
        }

        public void ShowGameResult(StoneType winner, int blackScore, int whiteScore)
        {
            if (resultText == null) return;
            
            string resultString = winner switch
            {
                StoneType.Black => $"흑 승리!\n{blackScore} : {whiteScore}",
                StoneType.White => $"백 승리!\n{blackScore} : {whiteScore}",
                _ => $"무승부!\n{blackScore} : {whiteScore}"
            };
            
            resultText.text = resultString;
            ShowResultPanel();
        }

        public void ShowStartPanel()
        {
            SetPanelActive(startPanel, true);
            SetPanelActive(gamePanel, false);
            SetPanelActive(resultPanel, false);
        }

        public void ShowGamePanel()
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
            if (panel != null) panel.SetActive(active);
        }
    }
}

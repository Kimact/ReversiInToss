using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Reversi.Game;

namespace Reversi.UI
{
    /// <summary>
    /// 게임 UI 관리
    /// 점수, 턴, 타이머, 패널 등 UI 요소 제어
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("점수 표시")]
        [SerializeField] private TextMeshProUGUI blackScoreText;
        [SerializeField] private TextMeshProUGUI whiteScoreText;
        
        [Header("턴 표시")]
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image turnIndicator;
        
        [Header("버튼")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button replayButton; // 리플레이 버튼
        
        [Header("패널")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        
        // 이벤트
        public event System.Action OnStartGameClick;
        public event System.Action OnRestartGameClick;
        public event System.Action OnReplayClick;

        private void Start()
        {
            SetupButtons();
            ShowStartPanel();
        }


        /// <summary>
        /// 버튼 이벤트 설정
        /// </summary>
        private void SetupButtons()
        {
            if (startButton != null)
                startButton.onClick.AddListener(() => OnStartGameClick?.Invoke());
            
            if (restartButton != null)
                restartButton.onClick.AddListener(() => OnRestartGameClick?.Invoke());

            if (replayButton != null)
                replayButton.onClick.AddListener(() => OnReplayClick?.Invoke());
        }

        /// <summary>
        /// 점수 표시 업데이트
        /// </summary>
        public void UpdateScore(int black, int white)
        {
            if (blackScoreText != null) blackScoreText.text = $"<color=#000000>흑</color>\n{black}";
            if (whiteScoreText != null) whiteScoreText.text = $"<color=#FFFFFF>백</color>\n{white}"; 
        }

        /// <summary>
        /// 턴 표시 업데이트
        /// </summary>
        public void UpdateTurn(GameState state)
        {
            if (turnText == null) return;
            
            string turnString = state switch
            {
                GameState.BlackTurn => "흑돌 차례",
                GameState.WhiteTurn => "백돌 차례",
                GameState.GameOver => "게임 종료",
                _ => "준비"
            };
            
            turnText.text = turnString;
            
            if (turnIndicator != null)
            {
                // 이미지 교체 로직 제거 (StyleHelper/GameTheme 의존성 제거)
            }

            if (state == GameState.GameOver) ShowResultPanel();
        }

        /// <summary>
        /// 타이머 표시 업데이트
        /// </summary>
        public void UpdateTimer(float remainingTime)
        {
            if (timerText != null) 
            {
                timerText.text = $"{Mathf.CeilToInt(remainingTime)}초";
                if (remainingTime < 5f) timerText.color = new Color(1f, 0.2f, 0.2f); // 빨간색 경고
            }
        }

        /// <summary>
        /// 게임 결과 표시
        /// </summary>
        public void ShowGameResult(StoneType winner, int blackScore, int whiteScore)
        {
            if (resultText == null) return;
            
            string resultString = winner switch
            {
                StoneType.Black => $"<size=150%>흑돌 승리!</size>\n{blackScore} vs {whiteScore}",
                StoneType.White => $"<size=150%>백돌 승리!</size>\n{blackScore} vs {whiteScore}",
                _ => $"<size=150%>무승부!</size>\n{blackScore} vs {whiteScore}"
            };
            
            resultText.text = resultString;
            ShowResultPanel();
        }

        /// <summary>
        /// 시작 패널 표시
        /// </summary>
        public void ShowStartPanel()
        {
            SetPanelActive(startPanel, true);
            SetPanelActive(gamePanel, false);
            SetPanelActive(resultPanel, false);
        }

        /// <summary>
        /// 게임 패널 표시
        /// </summary>
        public void ShowGamePanel()
        {
            SetPanelActive(startPanel, false);
            SetPanelActive(gamePanel, true);
            SetPanelActive(resultPanel, false);
        }

        /// <summary>
        /// 결과 패널 표시
        /// </summary>
        private void ShowResultPanel()
        {
            SetPanelActive(startPanel, false);
            SetPanelActive(gamePanel, true);
            SetPanelActive(resultPanel, true);
        }

        /// <summary>
        /// 패널 활성화/비활성화
        /// </summary>
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}

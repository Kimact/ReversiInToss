using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Reversi.Domain;
using Reversi.Utils;

namespace Reversi.View
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
            ApplyTheme();
            SetupButtons();
            ShowStartPanel();
        }

        /// <summary>
        /// 테마 적용 (Toss 스타일)
        /// </summary>
        private void ApplyTheme()
        {
            // 패널 배경 스타일링
            StylePanel(startPanel);
            StylePanel(gamePanel);
            StylePanel(resultPanel);

            // 버튼 스타일링
            StyleButton(startButton, GameTheme.PrimaryBlue);
            StyleButton(restartButton, GameTheme.PrimaryBlue);

            // 텍스트 스타일링
            StyleText(blackScoreText, GameTheme.TextColorLight);
            StyleText(whiteScoreText, GameTheme.TextColorLight);
            StyleText(turnText, GameTheme.TextColorDark);
            StyleText(timerText, GameTheme.TextColorDark);
            StyleText(resultText, GameTheme.TextColorDark);
        }

        /// <summary>
        /// 패널 스타일 적용
        /// </summary>
        private void StylePanel(GameObject panel)
        {
            if (panel == null) return;
            
            // Image 컴포넌트 확인/추가
            var img = panel.GetComponent<Image>();
            if (img == null) img = panel.AddComponent<Image>();
            
            if (panel == gamePanel)
            {
                // 게임 패널은 투명 (보드가 보이도록)
                img.color = Color.clear;
            }
            else
            {
                // 시작/결과 패널: 글래스모피즘 효과 (반투명 흰색)
                img.sprite = StyleHelper.CreateRoundedSprite(128, 128, GameTheme.CornerRadius, new Color(1, 1, 1, 0.9f));
                img.type = Image.Type.Sliced;
            }
        }

        /// <summary>
        /// 버튼 스타일 적용
        /// </summary>
        private void StyleButton(Button btn, Color color)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = StyleHelper.CreateRoundedSprite(128, 64, 32f, color); // 알약 모양
                img.type = Image.Type.Sliced;
            }
            
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = Color.white;
                tmp.fontStyle = FontStyles.Bold;
            }
        }

        /// <summary>
        /// 텍스트 색상 적용
        /// </summary>
        private void StyleText(TextMeshProUGUI txt, Color color)
        {
            if (txt == null) return;
            txt.color = color;
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
                turnIndicator.sprite = StyleHelper.CreateCircleSprite(64, state == GameState.BlackTurn ? GameTheme.BlackStoneColor : GameTheme.WhiteStoneColor);
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
                else timerText.color = GameTheme.TextColorDark;
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

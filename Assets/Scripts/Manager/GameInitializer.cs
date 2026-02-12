using UnityEngine;
using Reversi.Manager;
using Reversi.UI;

namespace Reversi.Manager
{
    /// <summary>
    /// 게임 초기화 담당 (기존 Bootstrapper)
    /// 애플리케이션 시작 시 MVP 아키텍처 초기화
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameUI gameUI;

private void Start()
        {
            if (gameManager == null) gameManager = Object.FindFirstObjectByType<GameManager>();
            if (boardView == null) boardView = Object.FindFirstObjectByType<BoardView>();
            if (gameUI == null) gameUI = Object.FindFirstObjectByType<GameUI>();

            if (gameManager != null && boardView != null)
            {
                gameManager.Initialize(boardView, gameUI);
                Debug.Log("MVP 아키텍처 초기화 완료.");

                // UI가 없으면 자동 게임 시작
                if (gameUI == null)
                {
                    gameManager.StartGame(true, 1);
                }
            }
            else
            {
                Debug.LogError("MVP \ucd08\uae30\ud654 \uc2e4\ud328: \ud544\uc694\ud55c \ucef4\ud3ec\ub10c\ud2b8\uac00 \uc5c6\uc2b5\ub2c8\ub2e4.");
            }
        }
    }
}

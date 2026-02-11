using UnityEngine;
using Reversi.Presentation;
using Reversi.View;

namespace Reversi.Application
{
    /// <summary>
    /// 부트스트래퍼
    /// 애플리케이션 시작 시 MVP 아키텍처 초기화
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private GamePresenter gamePresenter;
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameUI gameUI;

private void Start()
        {
            if (gamePresenter == null) gamePresenter = Object.FindFirstObjectByType<GamePresenter>();
            if (boardView == null) boardView = Object.FindFirstObjectByType<BoardView>();
            if (gameUI == null) gameUI = Object.FindFirstObjectByType<GameUI>();

            if (gamePresenter != null && boardView != null)
            {
                gamePresenter.Initialize(boardView, gameUI);
                Debug.Log("MVP \uc544\ud0a4\ud14d\ucc98 \ucd08\uae30\ud654 \uc644\ub8cc.");

                // UI\uac00 \uc5c6\uc73c\uba74 \uc790\ub3d9 \uac8c\uc784 \uc2dc\uc791
                if (gameUI == null)
                {
                    gamePresenter.StartGame(true, 1);
                }
            }
            else
            {
                Debug.LogError("MVP \ucd08\uae30\ud654 \uc2e4\ud328: \ud544\uc694\ud55c \ucef4\ud3ec\ub10c\ud2b8\uac00 \uc5c6\uc2b5\ub2c8\ub2e4.");
            }
        }
    }
}

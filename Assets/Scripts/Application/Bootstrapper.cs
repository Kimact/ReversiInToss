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
            // 할당되지 않은 경우 자동 검색
            if (gamePresenter == null) gamePresenter = Object.FindFirstObjectByType<GamePresenter>();
            if (boardView == null) boardView = Object.FindFirstObjectByType<BoardView>();
            if (gameUI == null) gameUI = Object.FindFirstObjectByType<GameUI>();

            if (gamePresenter != null && boardView != null && gameUI != null)
            {
                gamePresenter.Initialize(boardView, gameUI);
                Debug.Log("MVP 아키텍처 초기화 완료.");
            }
            else
            {
                Debug.LogError("MVP 초기화 실패: 필요한 컴포넌트가 없습니다.");
            }
        }
    }
}

using UnityEngine;
using Reversi.Presentation;
using Reversi.View;

namespace Reversi.Application
{
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private GamePresenter gamePresenter;
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameUI gameUI;

        private void Start()
        {
            // Auto-wire if not assigned
            if (gamePresenter == null) gamePresenter = Object.FindFirstObjectByType<GamePresenter>();
            if (boardView == null) boardView = Object.FindFirstObjectByType<BoardView>();
            if (gameUI == null) gameUI = Object.FindFirstObjectByType<GameUI>();

            if (gamePresenter != null && boardView != null && gameUI != null)
            {
                gamePresenter.Initialize(boardView, gameUI);
                Debug.Log("MVP Architecture Initialized.");
            }
            else
            {
                Debug.LogError("Failed to initialize MVP: Missing components.");
            }
        }
    }
}

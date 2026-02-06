using System.Collections.Generic;
using UnityEngine;
using Reversi.Domain;

namespace Reversi.View
{
    public class BoardView : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 boardOffset = Vector2.zero;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject highlightPrefab;
        
        [Header("Colors")]
        [SerializeField] private Color boardColor = new Color(0.1f, 0.5f, 0.2f);
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.5f);
        
        // Objects
        private StoneView[,] _stoneViews = new StoneView[8, 8];
        private GameObject[,] _cellObjects = new GameObject[8, 8];
        private List<GameObject> _highlightObjects = new List<GameObject>();
        
        private Camera _mainCamera;

        // Events for Presenter
        public event System.Action<BoardPosition> OnBoardClick;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            AdjustCameraSize();
            CreateBoard();
        }

        public void Initialize(BoardModel boardModel)
        {
            RefreshBoard(boardModel);
        }

        private void AdjustCameraSize()
        {
            if (_mainCamera == null) return;

            float targetWidth = 8f * cellSize + 1.0f;
            float screenAspect = (float)Screen.width / Screen.height;
            float requiredSize = (targetWidth / screenAspect) / 2f;
            _mainCamera.orthographicSize = Mathf.Max(5f, requiredSize);
        }

        private void CreateBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Vector3 worldPos = GetWorldPosition(row, col);

                    // Cell
                    if (cellPrefab != null)
                    {
                        var cell = Instantiate(cellPrefab, worldPos, Quaternion.identity, transform);
                        cell.name = $"Cell_{row}_{col}";
                        _cellObjects[row, col] = cell;

                        var sr = cell.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            float shade = ((row + col) % 2 == 0) ? 1f : 0.9f;
                            sr.color = boardColor * shade;
                        }
                    }

                    // Stone
                    if (stonePrefab != null)
                    {
                        var stoneGO = Instantiate(stonePrefab, worldPos, Quaternion.identity, transform);
                        stoneGO.name = $"Stone_{row}_{col}";
                        
                        var stoneView = stoneGO.GetComponent<StoneView>();
                        if (stoneView == null) stoneView = stoneGO.AddComponent<StoneView>();
                        
                        stoneView.SetPosition(new BoardPosition(row, col), worldPos);
                        stoneView.ResetStone();
                        _stoneViews[row, col] = stoneView;
                    }
                }
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && 
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                return;
            }
            if (UnityEngine.InputSystem.Touchscreen.current != null && 
                UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                HandleClick(UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue());
                return;
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick(Input.mousePosition);
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                HandleClick(Input.GetTouch(0).position);
            }
#endif
        }

        private void HandleClick(Vector3 screenPos)
        {
            if (_mainCamera == null) return;
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
            BoardPosition boardPos = GetBoardPosition(worldPos);
            
            if (boardPos.IsValid)
            {
                OnBoardClick?.Invoke(boardPos);
            }
        }

        public void RefreshBoard(BoardModel boardModel)
        {
            if (boardModel == null) return;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    StoneType type = boardModel.GetStone(row, col);
                    if (_stoneViews[row, col] != null)
                    {
                        _stoneViews[row, col].SetStoneType(type);
                    }
                }
            }
        }

        public void UpdateHighlights(BoardModel boardModel, StoneType currentTurn)
        {
            ClearHighlights();
            if (boardModel == null) return;

            var validMoves = boardModel.GetValidMoves(currentTurn);
            foreach (var pos in validMoves)
            {
                CreateHighlight(pos);
            }
        }

        private void CreateHighlight(BoardPosition pos)
        {
            if (highlightPrefab == null) return;
            
            Vector3 worldPos = GetWorldPosition(pos.Row, pos.Col);
            var highlight = Instantiate(highlightPrefab, worldPos, Quaternion.identity, transform);
            
            var sr = highlight.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = highlightColor;
            
            _highlightObjects.Add(highlight);
        }

        private void ClearHighlights()
        {
            foreach (var highlight in _highlightObjects)
            {
                if (highlight != null) Destroy(highlight);
            }
            _highlightObjects.Clear();
        }

        public Vector3 GetWorldPosition(int row, int col)
        {
            float x = (col - 3.5f) * cellSize + boardOffset.x;
            float y = (3.5f - row) * cellSize + boardOffset.y;
            return new Vector3(x, y, 0);
        }

        public BoardPosition GetBoardPosition(Vector3 worldPos)
        {
            int col = Mathf.RoundToInt((worldPos.x - boardOffset.x) / cellSize + 3.5f);
            int row = Mathf.RoundToInt(3.5f - (worldPos.y - boardOffset.y) / cellSize);
            return new BoardPosition(row, col);
        }
    }
}

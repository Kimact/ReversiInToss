using System.Collections.Generic;
using UnityEngine;
using Reversi.Game;

namespace Reversi.UI
{
    /// <summary>
    /// 보드 뷰 컴포넌트
    /// 고퀄리티 3D 보드 및 입력 처리 담당
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private float stoneScale = 0.5f;

        private StoneView[,] _stoneViews = new StoneView[8, 8];
        private List<GameObject> _highlightObjects = new List<GameObject>();

        private Camera _mainCamera;

        [SerializeField] private float boardWidth = 2.55f;
        [SerializeField] private float boardHeight = 2.55f;
        [SerializeField] private float gridOffsetX = 0f;
        [SerializeField] private float gridOffsetY = 0f;

        private float cellWidth => boardWidth / 8f;
        private float cellHeight => boardHeight / 8f;
        private float StartX => gridOffsetX + (-boardWidth / 2f + cellWidth / 2f);
        private float StartY => gridOffsetY + (-boardHeight / 2f + cellHeight / 2f);

        public event System.Action<BoardPosition> OnBoardClick;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            CreateBoardVisuals();
            SpawnStones();
        }

        public void Initialize(BoardModel boardModel)
        {
            if (boardModel == null) return;

            boardModel.OnStoneChanged -= HandleStoneChanged;
            boardModel.OnStonesFlipped -= HandleStonesFlipped;
            boardModel.OnStoneChanged += HandleStoneChanged;
            boardModel.OnStonesFlipped += HandleStonesFlipped;

            RefreshBoard(boardModel);
        }

        private void HandleStoneChanged(BoardPosition pos, StoneType type)
        {
            if (!pos.IsValid) return;
            var stone = _stoneViews[pos.Col, pos.Row]; // Col, Row 인덱싱 주의
            if (stone != null) stone.SetStoneType(type);
        }

        private void HandleStonesFlipped(List<BoardPosition> positions)
        {
            foreach (var pos in positions)
            {
                var stone = _stoneViews[pos.Col, pos.Row];
                if (stone != null)
                {
                    StoneType targetType = stone.CurrentType == StoneType.Black ? StoneType.White : StoneType.Black;
                    stone.Flip(targetType);
                }
            }
        }


        private void CreateBoardVisuals()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _highlightObjects.Clear();
            // 이미지(Sprite) 기반 보드로 교체됨
        }

        private void SpawnStones()
        {
            _stoneViews = new StoneView[8, 8];

            if (stonePrefab == null)
            {
                Debug.LogError("BoardView: Stone Prefab is not assigned!");
                return;
            }

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    GameObject stoneObj = Instantiate(stonePrefab, transform);
                    stoneObj.name = $"Stone_{x}_{y}";

                    // Row 0 = Top (+Y)
                    float posX = StartX + x * cellWidth;
                    float posY = StartY + (7 - y) * cellHeight;
                    stoneObj.transform.localPosition = new Vector3(posX, posY, -0.2f);
                    stoneObj.transform.localScale = Vector3.one * stoneScale;

                    StoneView stoneView = stoneObj.GetComponent<StoneView>();
                    if (stoneView == null) stoneView = stoneObj.AddComponent<StoneView>();

                    stoneView.InitializeVisuals();

                    _stoneViews[x, y] = stoneView; // [Col, Row]
                }
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame == true)
                HandleClick(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
#else
            if (Input.GetMouseButtonDown(0)) HandleClick(Input.mousePosition);
#endif
        }

        private void HandleClick(Vector3 screenPos)
        {
            if (_mainCamera == null) return;

            // 2D 좌표 기반 클릭 처리
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
            Vector3 localPoint = transform.InverseTransformPoint(worldPos);

            float gridStartX = gridOffsetX - boardWidth / 2f;
            float gridStartY = gridOffsetY - boardHeight / 2f;
            int col = Mathf.FloorToInt((localPoint.x - gridStartX) / cellWidth);
            int row = 7 - Mathf.FloorToInt((localPoint.y - gridStartY) / cellHeight);

            if (col >= 0 && col < 8 && row >= 0 && row < 8)
                OnBoardClick?.Invoke(new BoardPosition(row, col));
        }

        public void RefreshBoard(BoardModel boardModel)
        {
            if (boardModel == null) return;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    StoneType type = boardModel.GetStone(row, col);
                    var stone = _stoneViews[col, row];
                    if (stone != null && stone.CurrentType != type) stone.SetStoneType(type);
                }
            }
        }

        public void UpdateHighlights(BoardModel boardModel, StoneType currentTurn)
        {
            ClearHighlights();
            if (currentTurn == StoneType.None) return;

            var validMoves = boardModel.GetValidMoves(currentTurn);
            foreach (var move in validMoves)
            {
                GameObject highlight = new GameObject("Highlight");
                highlight.transform.SetParent(transform, false);

                float posX = StartX + move.Col * cellWidth;
                float posY = StartY + (7 - move.Row) * cellHeight;
                highlight.transform.localPosition = new Vector3(posX, posY, -0.15f);

                SpriteRenderer sr = highlight.AddComponent<SpriteRenderer>();
                // sr.sprite 설정 제거 (StyleHelper 의존성 제거)

                float cellMin = Mathf.Min(cellWidth, cellHeight);
                float baseScale = cellMin * 0.4f;
                highlight.transform.localScale = Vector3.one * baseScale;

                StartCoroutine(AnimateHighlight(highlight.transform, baseScale));
                _highlightObjects.Add(highlight);
            }
        }

        private System.Collections.IEnumerator AnimateHighlight(Transform target, float baseScale)
        {
            float timer = 0f;
            while (target != null)
            {
                timer += Time.deltaTime * 3f;
                float scaleFactor = 1f + Mathf.Sin(timer) * 0.2f; // 0.8 ~ 1.2
                target.localScale = Vector3.one * (baseScale * scaleFactor);
                yield return null;
            }
        }

        private void ClearHighlights()
        {
            foreach (var h in _highlightObjects) { if (h != null) Destroy(h); }
            _highlightObjects.Clear();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 씬 뷰에서 보드 그리드 시각화 (에디터 전용)
        /// </summary>
        private void OnDrawGizmos()
        {
            // 초록 영역 외곽선 (TransformPoint로 스케일 반영)
            Gizmos.color = Color.green;
            float hw = boardWidth / 2f, hh = boardHeight / 2f;
            Vector3 tl = transform.TransformPoint(new Vector3(gridOffsetX - hw, gridOffsetY + hh, 0));
            Vector3 tr = transform.TransformPoint(new Vector3(gridOffsetX + hw, gridOffsetY + hh, 0));
            Vector3 bl = transform.TransformPoint(new Vector3(gridOffsetX - hw, gridOffsetY - hh, 0));
            Vector3 br = transform.TransformPoint(new Vector3(gridOffsetX + hw, gridOffsetY - hh, 0));
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);

            // 격자선 (연한 초록)
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            for (int i = 1; i < 8; i++)
            {
                // 세로선
                float x = gridOffsetX - boardWidth / 2f + i * cellWidth;
                Vector3 vTop = transform.TransformPoint(new Vector3(x, gridOffsetY + boardHeight / 2f, 0));
                Vector3 vBot = transform.TransformPoint(new Vector3(x, gridOffsetY - boardHeight / 2f, 0));
                Gizmos.DrawLine(vTop, vBot);

                // 가로선
                float y = gridOffsetY - boardHeight / 2f + i * cellHeight;
                Vector3 hLeft = transform.TransformPoint(new Vector3(gridOffsetX - boardWidth / 2f, y, 0));
                Vector3 hRight = transform.TransformPoint(new Vector3(gridOffsetX + boardWidth / 2f, y, 0));
                Gizmos.DrawLine(hLeft, hRight);
            }

            // 각 셀 중심 (노란 점)
            Gizmos.color = Color.yellow;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    float posX = StartX + col * cellWidth;
                    float posY = StartY + (7 - row) * cellHeight;
                    Vector3 worldPos = transform.TransformPoint(new Vector3(posX, posY, 0));
                    Gizmos.DrawWireSphere(worldPos, 0.02f);
                }
            }
        }
#endif
    }
}

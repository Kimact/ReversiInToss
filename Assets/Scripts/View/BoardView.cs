using System.Collections.Generic;
using UnityEngine;
using Reversi.Domain;
using Reversi.Utils;

namespace Reversi.View
{
    /// <summary>
    /// 보드 뷰 컴포넌트
    /// 고퀄리티 3D 보드 및 입력 처리 담당
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private GameObject stonePrefab;

        private StoneView[,] _stoneViews = new StoneView[8, 8];
        private List<GameObject> _highlightObjects = new List<GameObject>();

        private Camera _mainCamera;

        // 보드 그리드 상수 (에디터 스크립트 MeasureBoardGrid로 측정)
        // 초록 영역: 271x288px, 로컬 유닛(PPU=100): 2.71 x 2.88
        private const float BOARD_WIDTH = 2.71f;
        private const float BOARD_HEIGHT = 2.88f;
        private const float CELL_WIDTH = BOARD_WIDTH / 8f;   // 0.33875
        private const float CELL_HEIGHT = BOARD_HEIGHT / 8f; // 0.36
        private const float GRID_OFFSET_X = 0.08f;  // 초록 영역 중심 오프셋
        private const float GRID_OFFSET_Y = -0.005f;
        private float _startX;
        private float _startY;

        public event System.Action<BoardPosition> OnBoardClick;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _startX = GRID_OFFSET_X + (-BOARD_WIDTH / 2f + CELL_WIDTH / 2f);
            _startY = GRID_OFFSET_Y + (-BOARD_HEIGHT / 2f + CELL_HEIGHT / 2f);
        }

        private void Start()
        {
            AdjustCameraSize();
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

        private void AdjustCameraSize()
        {
            if (_mainCamera == null) return;
            // 카메라는 씬에서 수동 설정 (orthographicSize = 5)
            // 배경색만 설정
            _mainCamera.backgroundColor = GameTheme.BackgroundColor;
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
                    float posX = _startX + x * CELL_WIDTH;
                    float posY = _startY + (7 - y) * CELL_HEIGHT;
                    stoneObj.transform.localPosition = new Vector3(posX, posY, -0.2f);

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

            float gridStartX = GRID_OFFSET_X - BOARD_WIDTH / 2f;
            float gridStartY = GRID_OFFSET_Y - BOARD_HEIGHT / 2f;
            int col = Mathf.FloorToInt((localPoint.x - gridStartX) / CELL_WIDTH);
            int row = 7 - Mathf.FloorToInt((localPoint.y - gridStartY) / CELL_HEIGHT);

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

                float posX = _startX + move.Col * CELL_WIDTH;
                float posY = _startY + (7 - move.Row) * CELL_HEIGHT;
                highlight.transform.localPosition = new Vector3(posX, posY, -0.15f);

                SpriteRenderer sr = highlight.AddComponent<SpriteRenderer>();
                sr.sprite = StyleHelper.CreateCircleSprite(64, new Color(1, 1, 1, 0.4f));

                float cellMin = Mathf.Min(CELL_WIDTH, CELL_HEIGHT);
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
    }
}

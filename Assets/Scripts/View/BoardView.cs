using System.Collections.Generic;
using UnityEngine;
using Reversi.Domain;
using Reversi.Utils;
using DG.Tweening;

namespace Reversi.View
{
    /// <summary>
    /// 보드 뷰 컴포넌트
    /// 고퀄리티 3D 보드 및 입력 처리 담당
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private StoneView[,] _stoneViews = new StoneView[8, 8];
        private List<GameObject> _highlightObjects = new List<GameObject>();
        
        private Camera _mainCamera;
        
        // 보드 크기 관련 상수
        private const float BOARD_SIZE = 8.5f;
        private const float CELL_SIZE = BOARD_SIZE / 8f;
        private float _startOffset;

        public event System.Action<BoardPosition> OnBoardClick;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _startOffset = -BOARD_SIZE / 2f + CELL_SIZE / 2f;
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
            float totalBoardWidth = BOARD_SIZE * 1.25f;
            float screenAspect = (float)Screen.width / Screen.height;
            _mainCamera.orthographicSize = Mathf.Max(6f, (totalBoardWidth / screenAspect) / 2f);
            _mainCamera.backgroundColor = GameTheme.BackgroundColor;
        }

        private void CreateBoardVisuals()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _highlightObjects.Clear();

            // 1. 나무 프레임
            GameObject frameObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frameObj.name = "WoodFrame";
            frameObj.transform.SetParent(transform, false);
            frameObj.transform.localScale = new Vector3(10f, 10f, 1f);
            frameObj.transform.localPosition = new Vector3(0, 0, 0.1f);
            Material frameMat = new Material(Shader.Find("Unlit/Texture"));
            frameMat.mainTexture = StyleHelper.CreateWoodTexture(512, 512);
            frameObj.GetComponent<MeshRenderer>().material = frameMat;

            // 2. 펠트 보드 (그리드 포함)
            GameObject boardObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            boardObj.name = "FeltBoard";
            boardObj.transform.SetParent(transform, false);
            boardObj.transform.localScale = new Vector3(BOARD_SIZE, BOARD_SIZE, 1f);
            boardObj.transform.localPosition = new Vector3(0, 0, 0f);
            Material boardMat = new Material(Shader.Find("Unlit/Texture"));
            boardMat.mainTexture = StyleHelper.CreateFeltTexture(512, 512); 
            boardObj.GetComponent<MeshRenderer>().material = boardMat;

            if (!boardObj.GetComponent<MeshCollider>()) boardObj.AddComponent<MeshCollider>();

            // 3. 조명
            GameObject lightGO = new GameObject("BoardLight");
            lightGO.transform.SetParent(transform); 
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private void SpawnStones()
        {
            _stoneViews = new StoneView[8, 8];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    GameObject stoneObj = new GameObject($"Stone_{x}_{y}");
                    stoneObj.transform.SetParent(transform, false);
                    
                    // Row 0 = Top (+Y)
                    float posX = _startOffset + x * CELL_SIZE;
                    float posY = _startOffset + (7 - y) * CELL_SIZE;
                    stoneObj.transform.localPosition = new Vector3(posX, posY, -0.2f);

                    StoneView stoneView = stoneObj.AddComponent<StoneView>();
                    stoneView.SaveTargetScale();
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
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.name == "FeltBoard")
                {
                    Vector3 localPoint = transform.InverseTransformPoint(hit.point);
                    float gridStart = -BOARD_SIZE / 2f;
                    int col = Mathf.FloorToInt((localPoint.x - gridStart) / CELL_SIZE);
                    int row = 7 - Mathf.FloorToInt((localPoint.y - gridStart) / CELL_SIZE);

                    if (col >= 0 && col < 8 && row >= 0 && row < 8)
                        OnBoardClick?.Invoke(new BoardPosition(row, col));
                }
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
                
                float posX = _startOffset + move.Col * CELL_SIZE;
                float posY = _startOffset + (7 - move.Row) * CELL_SIZE;
                highlight.transform.localPosition = new Vector3(posX, posY, -0.15f);

                SpriteRenderer sr = highlight.AddComponent<SpriteRenderer>();
                sr.sprite = StyleHelper.CreateCircleSprite(64, new Color(1, 1, 1, 0.4f));
                
                highlight.transform.localScale = Vector3.one * (CELL_SIZE * 0.4f);
                highlight.transform.DOScale(CELL_SIZE * 0.5f, 0.6f).SetLoops(-1, LoopType.Yoyo);
                _highlightObjects.Add(highlight);
            }
        }

        private void ClearHighlights()
        {
            foreach (var h in _highlightObjects) { if (h != null) Destroy(h); }
            _highlightObjects.Clear();
        }
    }
}

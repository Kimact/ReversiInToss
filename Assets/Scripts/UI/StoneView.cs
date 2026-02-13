using UnityEngine;
using Reversi.Game;
using DG.Tweening;

namespace Reversi.UI
{
    [RequireComponent(typeof(Animator))]
    public class StoneView : MonoBehaviour
    {
        [Header("Animators (Legacy Logic)")]
        // 기존 연결 유지를 위해 필드 보존
        public RuntimeAnimatorController BlackAnim;
        public RuntimeAnimatorController WhiteAnim;

        [Header("New Visual Settings")]
        [SerializeField] private Sprite blackSprite;
        [SerializeField] private Sprite whiteSprite;
        
        [Header("Shader Settings")]
        [SerializeField] private float pixelSize = 32f;
        [SerializeField] private Color blackGlow = new Color(0, 1, 1, 1);
        [SerializeField] private Color whiteGlow = new Color(1, 0.5f, 0, 1);

        [Header("Animation Settings")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float flipDuration = 0.5f;

        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private Material _processMaterial;
        
        private StoneType _currentType = StoneType.None;
        public StoneType CurrentType => _currentType;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void InitializeVisuals()
        {
            gameObject.SetActive(false);
            
            // 쉐이더 적용
            if (_processMaterial == null && _spriteRenderer != null)
            {
                Shader shader = Shader.Find("Reversi/UberShader");
                if (shader != null)
                {
                    _processMaterial = new Material(shader);
                    _spriteRenderer.material = _processMaterial;
                }
            }
        }

        public void SetStoneType(StoneType type)
        {
            _currentType = type;

            if (type == StoneType.None)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            UpdateVisuals(type);

            // Animator는 연결만 유지하고 실제로는 Idle 상태로 둠
            if (_animator != null)
            {
                UpdateController(type);
                _animator.Play("Idle", 0, 1.0f);
            }
        }

        public void Flip(StoneType newType)
        {
            if (_currentType == newType) return;
            _currentType = newType;
            
            UpdateVisuals(newType);

            // 물리적 점프 연출 (DOTween)
            transform.DOLocalMoveY(transform.localPosition.y + jumpHeight, flipDuration / 2f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo);

            // 쉐이더 픽셀화 효과
            if (_processMaterial != null)
            {
                DOTween.To(() => _processMaterial.GetFloat("_PixelSize"), 
                        x => _processMaterial.SetFloat("_PixelSize", x), 
                        8.0f, flipDuration / 2)
                    .SetLoops(2, LoopType.Yoyo);
            }
            
            // Animator 트리거 (혹시 모르니 실행)
            if (_animator != null) _animator.SetTrigger("Flip");
        }

        private void UpdateVisuals(StoneType type)
        {
            if (_spriteRenderer == null) return;

            // 1. 스프라이트 강제 할당 (애니메이터에 이미지가 없어도 보이게 함)
            if (type == StoneType.Black && blackSprite != null) _spriteRenderer.sprite = blackSprite;
            else if (type == StoneType.White && whiteSprite != null) _spriteRenderer.sprite = whiteSprite;

            // 2. 쉐이더 효과 적용
            if (_processMaterial != null)
            {
                _processMaterial.SetFloat("_EnablePixelate", 1.0f);
                _processMaterial.SetFloat("_PixelSize", pixelSize);
                
                if (type == StoneType.Black)
                    _processMaterial.SetColor("_GlowColor", blackGlow);
                else
                    _processMaterial.SetColor("_GlowColor", whiteGlow);
            }
        }

        private void UpdateController(StoneType type)
        {
            if (_animator == null) return;
            var targetAnim = (type == StoneType.Black) ? BlackAnim : WhiteAnim;
            if (targetAnim != null && _animator.runtimeAnimatorController != targetAnim)
            {
                _animator.runtimeAnimatorController = targetAnim;
            }
        }

        private void LateUpdate()
        {
            // Animator가 스프라이트를 덮어쓰는 것을 방지하기 위해 매 프레임 강제 할당
            if (_spriteRenderer != null)
            {
                if (_currentType == StoneType.Black && blackSprite != null) 
                    _spriteRenderer.sprite = blackSprite;
                else if (_currentType == StoneType.White && whiteSprite != null) 
                    _spriteRenderer.sprite = whiteSprite;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 자동으로 스프라이트 연결 시도
            if (blackSprite == null)
            {
                blackSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/stone_black.png");
            }
            if (whiteSprite == null)
            {
                whiteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/stone_white.png");
            }
        }
#endif
    }
}

using UnityEngine;
using Reversi.Game;
using DG.Tweening;

namespace Reversi.UI
{
    [RequireComponent(typeof(Animator))]
    public class StoneView : MonoBehaviour
    {
        [Header("Animators")]
        public RuntimeAnimatorController BlackAnim;
        public RuntimeAnimatorController WhiteAnim;

        private Animator _animator;
        private StoneType _currentType = StoneType.None;
        public StoneType CurrentType => _currentType;

        [Header("Animation Settings")]
        [SerializeField] private float jumpHeight = 0.2f;
        [SerializeField] private float jumpDuration = 0.3f;
        [SerializeField] private float jumpZOffset = -2.0f; // 카메라 쪽으로 당겨서 맨 위에 보이게 함

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void InitializeVisuals()
        {
            gameObject.SetActive(false);
        }

        public void SetStoneType(StoneType type)
        {
            if (type == StoneType.None)
            {
                gameObject.SetActive(false);
                _currentType = type;
                return;
            }

            _currentType = type;
            gameObject.SetActive(true);

            UpdateController(type);

            if (_animator != null)
            {
                // Idle 상태의 마지막 프레임(돌 정지 화면)으로 고정
                _animator.Play("Idle", 0, 1.0f);
                _animator.Update(0);
            }
        }

        public void Flip(StoneType newType)
        {
            if (_currentType == newType) return;
            _currentType = newType;

            if (_animator != null)
            {
                // 1. 애니메이션 속도 조절
                float animLen = 1.016f;
                float speed = animLen / jumpDuration;
                _animator.speed = speed;

                // 2. DOTween으로 점프 (위로 떴다가 착지) + Z축 앞으로 당김
                Vector3 originalPos = transform.localPosition;
                Vector3 targetPos = new Vector3(originalPos.x, originalPos.y + jumpHeight, originalPos.z + jumpZOffset);

                transform.DOLocalMove(targetPos, jumpDuration / 2f)
                    .SetEase(Ease.OutQuad)
                    .SetLoops(2, LoopType.Yoyo);

                // 3. Flip 재생
                _animator.SetTrigger("Flip");

                // 4. 점프/애니메이션이 끝나는 시간에 맞춰 교체
                StartCoroutine(SwitchControllerAfterFlip(newType, jumpDuration));
            }
        }

        private System.Collections.IEnumerator SwitchControllerAfterFlip(StoneType newType, float delay)
        {
            // 점프 시간만큼 대기 (정확한 동기화)
            yield return new WaitForSeconds(delay);

            // 새 색상 컨트롤러로 교체
            UpdateController(newType);

            if (_animator != null)
            {
                // 속도 원상복구 및 Idle 상태 고정
                _animator.speed = 1f;
                _animator.Play("Idle", 0, 1.0f);
                _animator.Update(0);
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
    }
}

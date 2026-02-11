using UnityEngine;
using Reversi.Domain;

namespace Reversi.View
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

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void InitializeVisuals()
        {
            gameObject.SetActive(false);
        }

        public void SaveTargetScale() { }

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
                // 초기화 시 애니메이션 끝 상태로 설정 (깜빡임 방지)
                _animator.Update(0);
                var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                _animator.Play(stateInfo.fullPathHash, 0, 1.0f);
            }
        }

        public void Flip(StoneType newType)
        {
            if (_currentType == newType) return;
            _currentType = newType;

            // 컨트롤러 교체 시 Default State(Flip 애니메이션)가 자동 재생됨
            UpdateController(newType);
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

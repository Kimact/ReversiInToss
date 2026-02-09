using UnityEngine;
using Reversi.Domain;
using DG.Tweening;

namespace Reversi.View
{
    /// <summary>
    /// 돌(Stone) 뷰 컴포넌트
    /// 3D Mesh (Cylinder) 기반 렌더링
    /// </summary>
    public class StoneView : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;
        private Vector3 _targetScale = Vector3.one;
        
        private StoneType _currentType = StoneType.None;
        public StoneType CurrentType => _currentType;

        // 런타임 머티리얼 인스턴스
        private static Material _blackMat;
        private static Material _whiteMat;

        private void Awake()
        {
            // [품질 설정] 계단 현상 제거 (Anti-Aliasing)
            QualitySettings.antiAliasing = 4;

            // 1. 3D 구체(Sphere) 생성 -> 납작하게 눌러서 바둑돌 모양(Lens shape) 만들기
            // Cylinder는 모서리가 각져서 깨져 보임(Aliasing). Sphere는 둥글어서 훨씬 부드러움.
            GameObject stoneMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stoneMesh.transform.SetParent(transform, false);
            
            // 납작한 타원형 (바둑돌 모양)
            stoneMesh.transform.localScale = new Vector3(1f, 0.2f, 1f);
            
            // [중요] 납작한 면이 카메라(Z축)를 향하도록 90도 회전
            // Sphere 기본형은 모든 방향이 같지만, scale을 (1, 0.2, 1)로 줄였으므로
            // 눕혀진 호떡 모양임. 이걸 세워야 정면에서 원으로 보임.
            stoneMesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            
            // 콜라이더 제거
            Destroy(stoneMesh.GetComponent<Collider>());

            _meshRenderer = stoneMesh.GetComponent<MeshRenderer>();
            
            // 그림자 설정
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _meshRenderer.receiveShadows = true;

            // 머티리얼 초기화
            if (_blackMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                _blackMat = new Material(shader);
                _blackMat.color = new Color32(30, 30, 30, 255); // 너무 어두우면 입체감 안보임 -> 살짝 밝게
                _blackMat.SetFloat("_Glossiness", 0.3f); // 은은한 광택 추가 (완전 무광은 밋밋함)
                _blackMat.SetFloat("_Metallic", 0.0f);

                _whiteMat = new Material(shader);
                _whiteMat.color = new Color32(250, 250, 250, 255);
                _whiteMat.SetFloat("_Glossiness", 0.3f);
                _whiteMat.SetFloat("_Metallic", 0.0f);
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

        public void InitializeVisuals()
        {
            gameObject.SetActive(false);
        }
        
        public void SaveTargetScale()
        {
            _targetScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        public void SetStoneType(StoneType type)
        {
            if (type == StoneType.None)
            {
                gameObject.SetActive(false);
                _currentType = type;
                return;
            }

            bool isNewSpawn = (_currentType == StoneType.None);
            _currentType = type;
            
            gameObject.SetActive(true);
            UpdateMaterial();
            
            transform.rotation = Quaternion.identity;

            if (isNewSpawn)
            {
                transform.localScale = Vector3.zero;
                transform.DOScale(_targetScale, 0.4f).SetEase(Ease.OutBack);
            }
            else
            {
                transform.localScale = _targetScale;
            }
        }

        public void Flip(StoneType newType)
        {
            if (_currentType == newType) return;
            
            transform.DOKill();

            // 1. 점프 (위로)
            transform.DOLocalMoveY(0.5f, 0.35f)
                .SetRelative(true)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);

            // 2. 세로 회전 (Vertical Flip)
            // X축을 기준으로 회전해야 위/아래로 뒤집힘
            Sequence flipSeq = DOTween.Sequence();
            
            // 90도까지 회전 (RotateMode.LocalAxisAdd 사용)
            flipSeq.Append(transform.DORotate(new Vector3(180, 0, 0), 0.35f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad));
            
            // 딱 중간(0.175초)에 머티리얼 교체
            flipSeq.InsertCallback(0.175f, () => 
            {
                _currentType = newType;
                UpdateMaterial();
            });
            
            flipSeq.OnComplete(() => transform.rotation = Quaternion.identity);
        }

        private void UpdateMaterial()
        {
            if (_meshRenderer == null) return;
            _meshRenderer.material = (_currentType == StoneType.Black) ? _blackMat : _whiteMat;
        }

        public void ResetStone()
        {
            _currentType = StoneType.None;
            gameObject.SetActive(false);
            transform.localScale = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.DOKill();
        }
    }
}

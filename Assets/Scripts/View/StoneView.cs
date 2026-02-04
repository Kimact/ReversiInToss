using UnityEngine;
using Reversi.Game;

namespace Reversi.View
{
    public class StoneView : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color blackColor = Color.black;
        [SerializeField] private Color whiteColor = Color.white;
        [SerializeField] private Color emptyColor = new Color(0, 0, 0, 0);
        
        private StoneType _currentType = StoneType.None;
        private BoardPosition _position;

        public StoneType CurrentType => _currentType;
        public BoardPosition Position => _position;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void SetPosition(BoardPosition pos, Vector3 worldPos)
        {
            _position = pos;
            transform.position = worldPos;
        }

        public void SetStoneType(StoneType type)
        {
            _currentType = type;
            UpdateVisual();
        }

        public void Flip(StoneType newType)
        {
            _currentType = newType;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null) return;

            spriteRenderer.color = _currentType switch
            {
                StoneType.Black => blackColor,
                StoneType.White => whiteColor,
                _ => emptyColor
            };

        
            gameObject.SetActive(_currentType != StoneType.None);
        }

        public void ResetStone()
        {
            _currentType = StoneType.None;
            gameObject.SetActive(false);
        }
    }
}

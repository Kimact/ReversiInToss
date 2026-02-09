using UnityEngine;

namespace Reversi.View
{
    /// <summary>
    /// 게임 테마 설정
    /// 색상 팔레트 및 스타일 상수 제공
    /// </summary>
    public static class GameTheme
    {
        // 디자인 컨셉: "프리미엄 보드 게임"
        // 깊은 녹색 펠트, 금/나무 액센트, 높은 대비
        
        /// <summary>배경 색상 (어두운 주변)</summary>
        public static readonly Color BackgroundColor = new Color32(20, 20, 20, 255);
        
        /// <summary>강조 색상 (골드/앰버)</summary>
        public static readonly Color PrimaryBlue = new Color32(255, 170, 0, 255);
        
        /// <summary>보드 색상 (깊은 녹색 펠트)</summary>
        public static readonly Color BoardColor = new Color32(40, 90, 50, 255);
        
        /// <summary>흑돌 색상</summary>
        public static readonly Color BlackStoneColor = new Color32(10, 10, 10, 255);
        
        /// <summary>백돌 색상</summary>
        public static readonly Color WhiteStoneColor = new Color32(240, 240, 240, 255);
        
        /// <summary>어두운 배경 위 텍스트 색상</summary>
        public static readonly Color TextColorDark = new Color32(220, 220, 220, 255);
        
        /// <summary>밝은 배경 위 텍스트 색상</summary>
        public static readonly Color TextColorLight = new Color32(150, 150, 150, 255);
        
        // UI 상수
        /// <summary>모서리 반경</summary>
        public const float CornerRadius = 4f;
        
        /// <summary>애니메이션 지속 시간</summary>
        public const float AnimationDuration = 0.5f;
    }
}

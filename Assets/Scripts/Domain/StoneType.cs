namespace Reversi.Domain
{
    /// <summary>
    /// 돌 타입 열거형
    /// </summary>
    public enum StoneType
    {
        /// <summary>없음 (빈 칸)</summary>
        None = 0,
        
        /// <summary>흑돌</summary>
        Black = 1,
        
        /// <summary>백돌</summary>
        White = 2
    }

    /// <summary>
    /// 돌 타입 확장 메서드
    /// </summary>
    public static class StoneTypeExtensions
    {
        /// <summary>
        /// 반대 색상 돌 반환
        /// </summary>
        public static StoneType Opposite(this StoneType stone)
        {
            return stone switch
            {
                StoneType.Black => StoneType.White,
                StoneType.White => StoneType.Black,
                _ => StoneType.None
            };
        }

        /// <summary>
        /// 표시용 문자열 반환
        /// </summary>
        public static string ToDisplayString(this StoneType stone)
        {
            return stone switch
            {
                StoneType.Black => "흑",
                StoneType.White => "백",
                _ => "-"
            };
        }
    }
}

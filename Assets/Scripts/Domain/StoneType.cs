namespace Reversi.Domain
{
    public enum StoneType
    {
        None = 0,
        Black = 1,
        White = 2
    }

    public static class StoneTypeExtensions
    {
        public static StoneType Opposite(this StoneType stone)
        {
            return stone switch
            {
                StoneType.Black => StoneType.White,
                StoneType.White => StoneType.Black,
                _ => StoneType.None
            };
        }

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

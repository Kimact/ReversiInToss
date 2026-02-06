using System;

namespace Reversi.Domain
{
    [Serializable]
    public class PlaceStoneCommand
    {
        public BoardPosition Position { get; private set; }
        public StoneType Stone { get; private set; }
        public long Timestamp { get; private set; }

        public PlaceStoneCommand(BoardPosition position, StoneType stone)
        {
            Position = position;
            Stone = stone;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public override string ToString()
        {
            return $"[{Stone}] {Position} at {Timestamp}";
        }
    }
}

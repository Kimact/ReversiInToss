using System;

namespace Reversi.Game
{
    /// <summary>
    /// 돌 놓기 명령
    /// 리플레이 및 히스토리 관리용
    /// </summary>
    [Serializable]
    public class PlaceStoneCommand
    {
        /// <summary>놓은 위치</summary>
        public BoardPosition Position { get; private set; }
        
        /// <summary>돌 타입</summary>
        public StoneType Stone { get; private set; }
        
        /// <summary>타임스탬프 (밀리초)</summary>
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

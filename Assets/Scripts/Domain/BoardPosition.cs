using System;

namespace Reversi.Domain
{
    /// <summary>
    /// 보드 위치 구조체
    /// 8x8 보드의 행/열 좌표 표현
    /// </summary>
    [Serializable]
    public struct BoardPosition : IEquatable<BoardPosition>
    {
        /// <summary>행 (0-7)</summary>
        public int Row;
        
        /// <summary>열 (0-7)</summary>
        public int Col;

        /// <summary>유효하지 않은 위치</summary>
        public static readonly BoardPosition Nowhere = new BoardPosition(-1, -1);

        public BoardPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        /// <summary>유효한 위치인지 확인 (0-7 범위 내)</summary>
        public bool IsValid => Row >= 0 && Row < 8 && Col >= 0 && Col < 8;

        public bool Equals(BoardPosition other)
        {
            return Row == other.Row && Col == other.Col;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Col);
        }

        public override string ToString()
        {
            return $"({Row}, {Col})";
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !left.Equals(right);
        }

        /// <summary>8방향 탐색용 델타값 (상하좌우 + 대각선)</summary>
        public static readonly (int dr, int dc)[] Directions = new[]
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1),           (0, 1),
            (1, -1),  (1, 0),  (1, 1)
        };
    }
}

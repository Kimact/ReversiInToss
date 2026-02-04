using System;

namespace Reversi.Game
{
    [Serializable]
    public struct BoardPosition : IEquatable<BoardPosition>
    {
        public int Row;
        public int Col;

        public BoardPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

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

        public static readonly (int dr, int dc)[] Directions = new[]
        {
            (-1, -1), (-1, 0), (-1, 1),
            (0, -1),           (0, 1),
            (1, -1),  (1, 0),  (1, 1)
        };
    }
}

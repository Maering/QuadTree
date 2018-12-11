using System;

namespace WoSS.Containers
{
    public class QuadTreeKey : IEquatable<QuadTreeKey>
    {
        private readonly float _x;
        private readonly float _y;

        public float X { get { return _x; } }
        public float Y { get { return _y; } }

        /// <summary>
        /// Implements an equality check on parameter x and y
        /// </summary>
        /// <param name="other">Second instance to compare this one with</param>
        /// <returns>True they are the same</returns>
        public bool Equals(QuadTreeKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            return _x.Equals(other._x) && _y.Equals(other._y);
        }

        /// <summary>
        /// Overrides default equality check
        /// </summary>
        /// <param name="obj">Second instance to compare this one with</param>
        /// <returns>True they are the same</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as QuadTreeKey);
        }

        /// <summary>
        /// Overrides default hashcode
        /// </summary>
        /// <returns>Hashcode of this key</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (_x.GetHashCode() << 16) ^ (_y.GetHashCode() & 65535);
            }
        }

        public static bool operator ==(QuadTreeKey left, QuadTreeKey right)
        {
            return ReferenceEquals(null, left) ? ReferenceEquals(null, right) : left.Equals(right);
        }

        public static bool operator !=(QuadTreeKey left, QuadTreeKey right)
        {
            return !(left == right);
        }

        public QuadTreeKey(float x, float y)
        {
            this._x = x;
            this._y = y;
        }
    }
}

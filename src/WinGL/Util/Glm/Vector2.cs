﻿using System;
using System.Runtime.CompilerServices;

namespace WinGL.Util
{
    /// <summary>
    /// Представляет двумерный вектор
    /// </summary>
    public struct Vector2
    {
        public float X;
        public float Y;

        public float this[int index]
        {
            get
            {
                if (index == 0) return X;
                else if (index == 1) return Y;
                else throw new Exception(Sr.OutOfRange);
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else throw new Exception(Sr.OutOfRange);
            }
        }

        public Vector2(float s)
        {
            X = Y = s;
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public Vector2(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 lhs, Vector2 rhs)
            => new Vector2(lhs.X + rhs.X, lhs.Y + rhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 lhs, float rhs)
            => new Vector2(lhs.X + rhs, lhs.Y + rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 lhs, Vector2 rhs)
            => new Vector2(lhs.X - rhs.X, lhs.Y - rhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 lhs, float rhs)
            => new Vector2(lhs.X - rhs, lhs.Y - rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 self, float s)
            => new Vector2(self.X * s, self.Y * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(float s, Vector2 self)
            => new Vector2(self.X * s, self.Y * s);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 lhs, Vector2 rhs)
            => new Vector2(rhs.X * lhs.X, rhs.Y * lhs.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 lhs, float rhs)
            => new Vector2(lhs.X / rhs, lhs.Y / rhs);

        public float[] ToArray() => new[] { X, Y };

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// The Difference is detected by the different values
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(Vector2))
            {
                var vec = (Vector2)obj;
                if (X == vec.X && Y == vec.Y)
                    return true;
            }

            return false;
        }
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="v1">The first Vector.</param>
        /// <param name="v2">The second Vector.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Vector2 v1, Vector2 v2) => v1.Equals(v2);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="v1">The first Vector.</param>
        /// <param name="v2">The second Vector.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Vector2 v1, Vector2 v2) => !v1.Equals(v2);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

        public override string ToString() => string.Format("{0:0.00}; {1:0.00}", X, Y);
    }
}

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace TimUtils
{
    public struct Vector2
    {
        public float X;
        public float Y;
        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 Up => new Vector2(0, -1);
        public static Vector2 Down => new Vector2(0, 1);
        public static Vector2 Left => new Vector2(-1, 0);
        public static Vector2 Right => new Vector2(1, 0);

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
        public Vector2(Vector2 v, float length)
        {
            X = v.X;
            Y = v.Y;
            Cap(length);
        }
        public Vector2(float value)
        {
            X = value;
            Y = value;
        }
        public Vector2(float length, float angle, bool FromAngle)
        {
            X = (float)Math.Cos(angle) * length;
            Y = (float)Math.Sin(angle) * length;
            Cap(length);
        }
        private Vector2 zeroed()
        {
            return new Vector2(0, 0);
        }
        public Vector2 Normalize()
        {
            float length = Length();
            if (length == 0)
                return Vector2.Zero;

            float f = 1 / length;
            return new Vector2((float)Math.Round(X * f), (float)Math.Round(Y * f));
        }
        public float Length()
        {
            return (float)Math.Sqrt(Math.Abs(X) * Math.Abs(X) + Math.Abs(Y) * Math.Abs(Y));
        }
        #region Operators
        public static Vector2 operator *(Vector2 v, float f)
        {
            return new Vector2(v.X * f, v.Y * f);
        }
        public static Vector2 operator /(Vector2 v, float f)
        {
            return new Vector2(v.X / f, v.Y / f);
        }
        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
        }
        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X - v2.X, v1.Y - v2.Y);
        }
        public static implicit operator Vector2(Point p)
        {
            return new Vector2(p.X, p.Y);
        }
        public static implicit operator Point(Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }
        public static implicit operator string(Vector2 v)
        {
            return $"({v.X},{v.Y})";
        }
        #endregion
        public void Cap(float limit)
        {
            if (this.Length() > limit || this.Length() < -limit)
            {
                this.X *= 1 / (this.Length() / limit);
                this.Y *= 1 / (this.Length() / limit);
            }
        }
    }
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 Up => new Vector3(0, -1, 0);
        public static Vector3 Down => new Vector3(0, 1, 0);

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public Vector3(Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }
        public Vector3(Vector3 v, float length)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            Normalize();
            this *= length;
        }
        public Vector3(float value)
        {
            X = value;
            Y = value;
            Z = value;
        }
        public Vector3 Normalize()
        {
            float length = Length();
            if (length == 0)
                return Vector3.Zero;

            float f = 1 / length;
            return new Vector3((float)Math.Round(X * f), (float)Math.Round(Y * f), (float)Math.Round(Z * f));
        }
        public float Length()
        {
            float xyLen = (float)Math.Sqrt(Math.Abs(X) * Math.Abs(X) + Math.Abs(Y) * Math.Abs(Y));
            return (float)Math.Sqrt(xyLen * xyLen + Math.Abs(Z) * Math.Abs(Z));
        }
        #region Operators
        public static Vector3 operator *(Vector3 v, float f)
        {
            return new Vector3(v.X * f, v.Y * f, v.Z * f);
        }
        public static Vector3 operator /(Vector3 v, float f)
        {
            return new Vector3(v.X / f, v.Y / f, v.Z / f);
        }
        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }
        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }
        public static implicit operator String(Vector3 v)
        {
            return $"({v.X},{v.Y},{v.Z})";
        }
        #endregion
        public void Cap(float limit)
        {
            if (this.Length() > limit || this.Length() < -limit)
            {
                this.X *= 1 / (this.Length() / limit);
                this.Y *= 1 / (this.Length() / limit);
            }
        }
    }
    public static class Functions
    {
        static void WaitForCondition(ref bool condition)
        {
            while (!condition)
            {
                Thread.Sleep(100);
            }
        }
        static void WaitForCondition(ref bool condition, int interval)
        {
            while (!condition)
            {
                Thread.Sleep(100);
            }
        }
        static void WaitForCondition(ref bool condition, int interval, int timeout)
        {
            while (!condition)
            {
                Thread.Sleep(100);
            }
        }
    }
}

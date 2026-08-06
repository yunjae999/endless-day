using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Vector
    {
        public double x, y, z;

        public Vector(double a, double b, double c)
        {
            x = a;
            y = b;
            z = c;
        }

        // 1. 벡터합
        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        // 2.벡터차
        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        // 3.스칼라곱
        public static Vector operator *(Vector a, Vector b)
        {
            return new Vector(a.x * b.x, a.y * b.y, a.z * b.z);
        }
        public static Vector operator *(Vector a, double b)
        {
            return new Vector(a.x * b, a.y * b, a.z * b);
        }
        public static Vector operator *(double a, Vector b)
        {
            return new Vector(a * b.x, a * b.y, a * b.z);
        }
        // 4-1. 벡터크기
        public double Length()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }
        // 4-2. 정규화
        public Vector Magnitude()
        {
            double size = Length();
            return new Vector(x / size, y / size, z / size);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Vector curPos = new Vector(13, 0, -21);
            Vector targetPos = new Vector(850.8, 0, 526.8);
            double speedPerSec = 130 / 60;
            double time = 147;

            Vector direction = (targetPos - curPos).Magnitude();

            Vector afterMove = direction * speedPerSec * time;

            Console.WriteLine("147초 후 도착 위치는 ({0}, {1}, {2})입니다.", afterMove.x, afterMove.y, afterMove.z);
            double leftDistance = (targetPos - afterMove).Length();
            Console.WriteLine("목적지까지 남은 거리는 {0}입니다.", leftDistance);

            // 경사 지나가기
            double hillDist = 30 / Math.Sin(30);
            double hillTime = hillDist / speedPerSec;

            Vector afterHill = curPos + direction * speedPerSec * hillTime;
            afterHill.y = 30;

            time -= hillTime;
            afterMove = afterHill + direction * speedPerSec * time;

            Console.WriteLine("147초 후 도착 위치는 ({0}, {1}, {2})입니다.", afterMove.x, afterMove.y, afterMove.z);
            leftDistance = (targetPos - afterMove).Length();
            Console.WriteLine("목적지까지 남은 거리는 {0}입니다.", leftDistance);
        }
    }
}

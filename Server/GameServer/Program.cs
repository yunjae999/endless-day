using System;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== GameServer 시작 ===");

            DBClient dbClient = new DBClient("127.0.0.1", 7778);
            bool dbConnected = dbClient.Connect();

            if (!dbConnected)
            {
                Console.WriteLine("GameDB 연결 실패. 서버를 종료합니다.");
                return;
            }

            TCPServer server = new TCPServer(7777, dbClient);
            dbClient.SetTCPServer(server);

            // 서버 시작 시 전체 유저 목록을 미리 로드해 캐싱 (로그인/중복확인 시 DB 왕복 없이 처리)
            dbClient.RequestGetUsers();

            Console.WriteLine("GameServer 실행 중. 종료하려면 ESC");
            while (true)
            {
                dbClient.MainLoop();
                server.MainLoop();

                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                        break;
                }
            }

            server.Release();
            dbClient.Release();
        }
    }
}
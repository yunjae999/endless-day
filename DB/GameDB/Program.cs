using System;

namespace GameDB
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== GameDB 시작 ===");

            DBAgentManager db = new DBAgentManager();
            bool dbConnected = db.ConnectDB("root", "1234", "endlessday");
            
            if (!dbConnected)
            {
                Console.WriteLine("DB 연결 실패. 종료합니다.");
                return;
            }

            DBServer dbServer = new DBServer(7778, db);

            Console.WriteLine("GameDB 실행 중. 종료하려면 ESC");
            while (true)
            {
                dbServer.MainLoop();

                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                        break;
                }
            }

            dbServer.Release();
            db.DisconnectDB();
        }
    }
}
using LearnParsingTable;
using System;

namespace GameDataTableConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ExcelConversion ec = new ExcelConversion();

            ec.InitParse(@"Table\GameDataTable.xlsx");
            ec.ConvertExcelDatas();

            string outputPath = @"C:\Users\Dclass\Desktop\yj\portfolio\endless-day\EndlessDay\Assets\Resources\Tables";
            ec.SaveJsonFile(outputPath);

            Console.ReadLine();
        }
    }
}

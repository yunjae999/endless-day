using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using LitJson;

namespace LearnParsingTable
{
    internal class ExcelConversion
    {
        // 시트번호(1번부터), 컬럼번호(1번부터), 컬럼데이터
        Dictionary<int, Dictionary<int, List<string>>> _excelDatas;

        // 이렇게 전부 string으로 들어간 형태로 만들어야 json변환이 쉬움.
        // 시트이름, 인덱스번호, 컬럼명, 컬럼데이터
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> _convertDatas;

        string[] _sheetNames;
        /// <summary>
        /// 해당 객체를 제거하는 함수
        /// </summary>
        /// <param name="obj">제거할 객체</param>
        void ReleaseExcelObject(object obj)
        {
            try
            {
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj);
                    obj = null;
                }
            }
            catch (Exception ex)
            {
                obj = null;
                throw ex;
            }
            finally
            {
                // Garbage Collection의 강제 발동(비 권장).
                GC.Collect();
            }
        }

        /// <summary>
        /// 해당 워크 시트의 목표 컬럼의 컬럼수를 받는 함수
        /// </summary>
        /// <param name="oSheet">컬럼수가 알고 싶은 시트</param>
        /// <param name="oRng">목표가 되는 컬럼</param>
        /// <returns>컬럼의 개수</returns>
        int ExcelFileColumnCount(Excel.Worksheet oSheet, Excel.Range oRng)
        {
            int colCount = oRng.Column;

            for (int n = 1; n <= colCount; n++)
            {
                Excel.Range cell = (Excel.Range)oSheet.Cells[1, n];
                if (cell.Value == null)
                {
                    ReleaseExcelObject(cell);
                    Console.WriteLine(oSheet.Name.ToString() + "[{0}]Sheet에 비어 있는 셀이 존재합니다({1}Column).", oSheet.Name, colCount--);
                    break;
                }
            }
            return colCount;
        }

        /// <summary>
        /// 엑셀이 지정한 ColumnName을 구성해서 List로 반환하는 함수,
        /// </summary>
        /// <param name="length">실질 데이터가 들어가 있는 컬럼수</param>
        /// <returns>엑셀컬럼이름들을 반환</returns>
        List<string> ExcelFileColumnsName(int length)
        {
            List<string> nameList = new List<string>();
            int baseNum = 26;
            for (int n = 0; n < length; n++)
            {
                if (n / baseNum == 0)
                    nameList.Add(Convert.ToString((char)(65 + n)));             // 알파벳 1개(ex:A, B, C, ...)
                else
                {
                    string temp = Convert.ToString((char)(64 + (n / baseNum))); // 알파벳 1개(ex:A, B, C, ...)
                    temp += Convert.ToString((char)(65 + (n % baseNum)));       // + 2번째 알파벳(ex: AA, AB, ...)
                    nameList.Add(Convert.ToString(temp));
                }
            }
            return nameList;
        }
        /// <summary>
        /// 엑셀내에 있는 유의미한 자료들을 Dictionary에 모두 저장시킨다.
        /// </summary>
        /// <param name="oSheets">엑셀 자료가 들어있는 곳</param>
        void SaveExcelDatasToDictionary(Excel.Sheets oSheets)
        {
            for (int n = 1; n <= oSheets.Count; n++)
            {
                List<string> columns;
                Excel.Worksheet oSheet = (Excel.Worksheet)oSheets.get_Item(n);  // 시트 1개 담아옴.
                Excel.Range oRng = oSheet.get_Range("A1").SpecialCells(Excel.XlCellType.xlCellTypeLastCell);    // 그 시트의 마지막 셀의 정보
                int colCount = ExcelFileColumnCount(oSheet, oRng);
                columns = ExcelFileColumnsName(colCount);
                _sheetNames[n - 1] = oSheet.Name;

                /// Excel 파일의 전체 자료를 _excelDatas에 담는다.
                // Sheet의 자료들을 담을 장소를 만든다. 
                Dictionary<int, List<string>> dicSheet = new Dictionary<int, List<string>>();
                for (int col = 1; col <= colCount; col++)
                {
                    // Column의 자료들을 담을 장소를 만든다.
                    List<string> columnDatas = new List<string>();
                    int count = 0;

                    Excel.Range collCell = (Excel.Range)oSheet.Columns[col];
                    Excel.Range range = oSheet.get_Range(columns[col - 1] + "1", collCell);

                    foreach (object temp in range.Value)
                    {
                        if (count < oRng.Row)
                        {
                            count++;
                            if (temp == null)
                                columnDatas.Add("");
                            else
                                columnDatas.Add(temp.ToString());
                        }
                        else
                            break;
                    }
                    dicSheet.Add(col, columnDatas);
                    ReleaseExcelObject(range);
                    ReleaseExcelObject(collCell);
                }
                _excelDatas.Add(n, dicSheet);
            }
        }
        /// <summary>
        /// Excel 파일에 있는 유의미한 전체 자료를 Dictionary에 저장시키는 함수
        /// </summary>
        /// <param name="fullPath">Excel 파일이름(확장자까지)</param>
        /// <returns>true면 파싱완료 false 에러</returns>
        bool ExcelFileLoad(string fullPath)
        {
            bool result = true;
            object misValue = System.Reflection.Missing.Value;
            Excel.Application oXL = new Excel.Application();
            Excel.Workbooks oWBooks = oXL.Workbooks;
            Excel.Workbook oWB = oWBooks.Open(fullPath, misValue, misValue, misValue, misValue, misValue,
                                             misValue, misValue, misValue, misValue, misValue,
                                             misValue, misValue, misValue); ;
            Excel.Sheets oSheets = oWB.Sheets;

            try
            {
                _sheetNames = new string[oSheets.Count];
                // oSheets안에 있는 자료들 _excelDatas에 저장.
                SaveExcelDatasToDictionary(oSheets);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                result = false;
            }
            finally
            {
                // 정리
                oXL.Visible = false;
                oXL.UserControl = true;
                oXL.DisplayAlerts = false;
                oXL.Quit();

                ReleaseExcelObject(oSheets);
                ReleaseExcelObject(oWB);
                ReleaseExcelObject(oWBooks);
                ReleaseExcelObject(oXL);
            }
            return result;
        }
        public void InitParse(string fileName)
        {
            string fullName = Directory.GetCurrentDirectory() + "\\" + fileName;

            _excelDatas = new Dictionary<int, Dictionary<int, List<string>>>();
            _convertDatas = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            if (ExcelFileLoad(fullName))
            {
                Console.WriteLine("엑셀데이터 파싱 종료!");
            }
            else
                Console.WriteLine("파일 로드 실패......");
        }

        public void ConvertExcelDatas()
        {
            for (int i = 1; i <= _excelDatas.Count; i++)
            {
                Dictionary<string, Dictionary<string, string>> dic = new Dictionary<string, Dictionary<string, string>>();

                for (int k = 1; k < _excelDatas[i][1].Count; k++) // row 개수만큼
                {
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    for (int j = 1; j <= _excelDatas[i].Count; j++) // 칼럼 개수만큼
                    {
                        data.Add(_excelDatas[i][j][0], _excelDatas[i][j][k]);
                    }

                    dic.Add(_excelDatas[i][1][k], data);
                }
                _convertDatas.Add(_sheetNames[i - 1], dic);
            }
        }
        /// <summary>
        /// 각 시트 이름으로 .Json 파일을 만들어 낸다.
        /// </summary>
        public void SaveJsonFile(string outputPath)
        {
            if (Directory.Exists(outputPath) == false)
                Directory.CreateDirectory(outputPath);

            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> sheet in _convertDatas)
            {
                string filePath = outputPath + "\\" + sheet.Key + ".json";

                FileStream fStream = new FileStream(filePath, FileMode.Create);
                StreamWriter sw = new StreamWriter(fStream, Encoding.Unicode);

                JsonWriter writer = new JsonWriter(sw);
                writer.WriteObjectStart();              // { : 중괄호가 열림. object의미. []는 데이터 구분점.
                writer.WritePropertyName(sheet.Key);    // 시트이름. : 까지.
                writer.WriteArrayStart();
                foreach (KeyValuePair<string, Dictionary<string, string>> record in sheet.Value)
                {
                    writer.WriteObjectStart();
                    foreach (KeyValuePair<string, string> cell in record.Value)
                    {
                        writer.WritePropertyName(cell.Key);
                        writer.Write(cell.Value);
                    }
                    writer.WriteObjectEnd();
                }

                writer.WriteArrayEnd();
                writer.WriteObjectEnd();                // } : 중괄호 닫힘.
                sw.Close();
            }
        }
    }
}
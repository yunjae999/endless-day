using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameServer
{
    /// <summary>
    /// Unity 클라이언트의 DataTable과 동일한 역할.
    /// 엑셀 → JSON 파이프라인으로 나온 같은 파일을 서버도 직접 읽어서, DB에 따로 넣지 않고도
    /// 클라이언트와 서버가 항상 같은 밸런스 데이터를 보게 한다.
    /// </summary>
    public class DataTable
    {
        Dictionary<int, Dictionary<string, string>> _rows = new Dictionary<int, Dictionary<string, string>>();

        public void LoadJson(string fullPath)
        {
            string json = File.ReadAllText(fullPath);
            JObject root = JObject.Parse(json);

            foreach (JProperty sheetProperty in root.Properties())
            {
                JArray rows = (JArray)sheetProperty.Value;
                foreach (JObject row in rows)
                {
                    JProperty firstProp = row.Properties().First();
                    int key = int.Parse(firstProp.Value.ToString());

                    Dictionary<string, string> rowData = new Dictionary<string, string>();
                    foreach (JProperty prop in row.Properties())
                        rowData[prop.Name] = prop.Value.ToString();

                    _rows[key] = rowData;
                }
            }
        }

        public IEnumerable<int> GetAllKeys()
        {
            return _rows.Keys;
        }

        public string ToS(int key, string subkey)
        {
            if (!_rows.ContainsKey(key) || !_rows[key].ContainsKey(subkey))
                return "";
            return _rows[key][subkey];
        }

        public int ToI(int key, string subkey)
        {
            return int.TryParse(ToS(key, subkey), out int result) ? result : 0;
        }

        public float ToF(int key, string subkey)
        {
            return float.TryParse(ToS(key, subkey), out float result) ? result : 0f;
        }
    }
}

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// com.unity.nuget.newtonsoft-json 설치필요

public class DataTable
{
    Dictionary<int, Dictionary<string, string>> _sheet;

    public int _recordCount { get { return _sheet.Count; } }

    public void LoadJson(string fullPath)
    {
        TextAsset json = Resources.Load<TextAsset>(fullPath);

        if (json == null)
        {
            Debug.LogError("JSON 파일을 찾을 수 없습니다: " + fullPath);
            return;
        }

        JObject root = JObject.Parse(json.text);
        JArray array = JArray.Parse(root.Properties().First().Value.ToString());

        _sheet = new Dictionary<int, Dictionary<string, string>>();

        for (int i = 0; i < array.Count; i++)
        {
            JObject obj = (JObject)array[i];

            JProperty firstProp = obj.Properties().First();
            int key = int.Parse(firstProp.Value.ToString());

            Dictionary<string, string> row =
                array[i].ToObject<Dictionary<string, string>>();

            _sheet.Add(key, row);
        }
    }
    public string ToS(int key, string subkey)
    {
        string findValue = string.Empty;
        if (!_sheet.ContainsKey(key))
            return findValue;
        _sheet[key].TryGetValue(subkey, out findValue);            

        return findValue;
    }
    public int ToI(int key, string subkey)
    {
        string findValue = string.Empty;
        int val = 0;

        if (!_sheet.ContainsKey(key))
            return val;

        _sheet[key].TryGetValue(subkey, out findValue);

        int.TryParse(findValue, out val);
        return val;
    }
    public float ToF(int key, string subkey)
    {
        string findValue = string.Empty;
        float val = 0f;

        if (!_sheet.ContainsKey(key))
            return val;

        _sheet[key].TryGetValue(subkey, out findValue);

        float.TryParse(findValue, out val);
        return val;
    }
    public IEnumerable<int> GetAllKeys()
    {
        return _sheet.Keys;
    }
}

using System.Collections.Generic;
using System.Data;
using UnityEngine;
using Defines;

public class TableDataManager : TSingleton<TableDataManager>
{
    const string path = "Tables/";
    Dictionary<TableName, DataTable> _tableDoc = new Dictionary<TableName, DataTable>();
    public void TableAllLoad()
    {
        int tableCount = (int)TableName.max;
        _tableDoc = new Dictionary<TableName, DataTable>();
        for (int i = 0; i < tableCount; i++)
        {
            TableName tableName = (TableName)i;
            DataTable dt = new DataTable();
            dt.LoadJson(path + tableName);
            _tableDoc.Add(tableName, dt);
        }
    }

    public DataTable Get(TableName name)
    {
        if (_tableDoc.ContainsKey(name))
            return _tableDoc[name];

        return null;
    }
}

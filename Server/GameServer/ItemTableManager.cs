using System;
using System.Collections.Generic;
using System.IO;

namespace GameServer
{
    /// <summary>
    /// 클라이언트의 ItemManager와 대응. EquipmentTable/ConsumableTable을 직접 읽어서
    /// ItemID → (ItemType, Price)만 서버가 필요한 만큼 캐싱해둔다.
    /// DB에 별도로 넣지 않음 - 엑셀이 유일한 원본.
    /// </summary>
    public class ItemTableManager
    {
        public static ItemTableManager _instance { get; } = new ItemTableManager();

        Dictionary<int, (int itemType, int price)> _items = new Dictionary<int, (int, int)>();

        /// <summary>tablesPath : Unity 프로젝트의 Assets/Resources/Tables 폴더 경로</summary>
        public void LoadAll(string tablesPath)
        {
            _items.Clear();

            LoadTable(Path.Combine(tablesPath, "EquipmentTable.json"), itemType: 1);
            LoadTable(Path.Combine(tablesPath, "ConsumableTable.json"), itemType: 2);

            Console.WriteLine("[ItemTableManager] 아이템 {0}개 로드 완료.", _items.Count);
        }

        void LoadTable(string path, int itemType)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("[ItemTableManager] 파일을 찾을 수 없음 : {0}", path);
                return;
            }

            DataTable table = new DataTable();
            table.LoadJson(path);

            foreach (int itemId in table.GetAllKeys())
            {
                int price = table.ToI(itemId, "Price");
                _items[itemId] = (itemType, price);
            }
        }

        public bool TryGetItem(int itemId, out int itemType, out int price)
        {
            if (_items.TryGetValue(itemId, out var info))
            {
                itemType = info.itemType;
                price = info.price;
                return true;
            }

            itemType = 0;
            price = 0;
            return false;
        }
    }
}

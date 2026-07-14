using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로그인 데이터와 인벤토리 등, 씬을 넘어서도 유지해야 하는 세션 상태.
/// 마을/던전씬마다 새로 생성되는 PlayerController 등이 Awake 시점에 여기서 값을 읽어간다.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession _instance { get; private set; }

    public int UserId { get; private set; }
    public string Nickname { get; private set; }
    public int Gold { get; private set; }
    public int TryCount { get; private set; }
    public bool IsCleared { get; private set; }
    public List<int> UnlockedWeapons { get; private set; } = new List<int>();

    public InventoryModel Inventory { get; private set; } = new InventoryModel();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>로그인 성공(LoginOK) 시점에 NetworkManager의 결과로 세션을 채운다</summary>
    public void LoadFromLoginResult(LoginResultData data)
    {
        UserId = data.UserId;
        Nickname = data.Nickname;
        Gold = data.Gold;
        TryCount = data.TryCount;
        IsCleared = data.IsCleared;

        UnlockedWeapons = ParseIntArray(data.UnlockedWeapons);

        List<int> equipped = ParseIntArray(data.EquippedEquipment);
        for (int i = 0; i < InventoryModel.EQUIP_SLOT_COUNT && i < equipped.Count; i++)
            Inventory.EquippedItemIDs[i] = equipped[i];

        // TODO: PlayerInventory(소비/장비 보유 목록)도 로그인 시 같이 받아오면 여기서 Inventory.InventorySlots 채우기
    }

    List<int> ParseIntArray(string json)
    {
        List<int> result = new List<int>();
        if (string.IsNullOrEmpty(json))
            return result;

        JArray array = JArray.Parse(json);
        foreach (JToken token in array)
            result.Add(token.ToObject<int>());

        return result;
    }

    public void AddGold(int amount)
    {
        Gold += amount;
    }
}

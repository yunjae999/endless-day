using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로그인 데이터와 인벤토리 등, 씬을 넘어서도 유지해야 하는 세션 상태.
/// 인벤토리 UI는 씬(Village/Dungeon)마다 새로 생성되는 프리팹이 스스로 등록/해제하며,
/// GameSession은 "지금 열려있는지" 상태만 갖고 등록된 컨트롤러에게 보이기/숨기기를 위임한다.
/// </summary>
public class GameSession : TSingleton<GameSession>
{
    bool _isInventoryOpen;
    UIInventoryController _inventoryUI;

    public int UserId { get; private set; }
    public string Nickname { get; private set; }
    public int Gold { get; private set; }
    public int TryCount { get; private set; }
    public bool IsCleared { get; private set; }
    public List<int> UnlockedWeapons { get; private set; } = new List<int>();

    public InventoryModel Inventory { get; private set; } = new InventoryModel();

    // ─────────────────────────────────────────────
    // 인벤토리 UI 등록 (씬마다 새로 생기는 프리팹이 자기 자신을 등록)
    // ─────────────────────────────────────────────

    public void RegisterInventoryUI(UIInventoryController controller)
    {
        _inventoryUI = controller;
        _inventoryUI.SetVisible(_isInventoryOpen);   // 씬 전환 중에도 열려있던 상태면 그대로 유지
    }

    public void UnregisterInventoryUI(UIInventoryController controller)
    {
        if (_inventoryUI == controller)
            _inventoryUI = null;
    }

    public void ToggleInventory()
    {
        _isInventoryOpen = !_isInventoryOpen;

        if (_inventoryUI != null)
            _inventoryUI.SetVisible(_isInventoryOpen);
    }

    // ─────────────────────────────────────────────
    // 로그인 데이터
    // ─────────────────────────────────────────────

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

    /// <summary>골드가 충분하면 차감하고 true, 부족하면 아무것도 안 하고 false</summary>
    public bool TrySpendGold(int amount)
    {
        if (Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }

    // ─────────────────────────────────────────────
    // 상점 - 서버 응답을 실제 상태에 반영
    // (구매/판매 버튼 클릭 → NetworkManager.SendBuyItem/SendSellItem 호출은 UI 쪽에서,
    //  서버 응답(OnBuyResult/OnSellResult)을 받으면 이 메서드들을 호출해서 반영)
    // ─────────────────────────────────────────────

    public void ApplyBuyResult(bool success, int itemId, int newGold)
    {
        Gold = newGold;   // 성공/실패 상관없이 서버가 계산한 최종 골드로 항상 동기화

        if (success)
            Inventory.AddItem(itemId, 1);
    }

    public void ApplySellResult(bool success, int itemId, int newGold)
    {
        Gold = newGold;

        if (success)
            Inventory.RemoveItem(itemId, 1);
    }
}
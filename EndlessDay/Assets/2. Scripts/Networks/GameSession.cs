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
    // 레벨 / 경험치 (던전 재도전마다 리셋되는 값 - 로그라이트 특성)
    // ─────────────────────────────────────────────

    const int EXP_PER_LEVEL_MULTIPLIER = 50;   // 임시 공식: 필요경험치 = 현재레벨 × 50
    const int PERK_CHOICE_COUNT = 3;
    const int CURRENT_WEAPON_TYPE = 1;   // TODO: 무기 선택 시스템 완성되면 실제 장착 무기 값으로 교체 (지금은 검 고정)

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp { get; private set; }

    /// <summary>PerkID → 현재 스택 수. 던전 재도전마다 리셋되는 값</summary>
    public Dictionary<int, int> ActivePerks { get; private set; } = new Dictionary<int, int>();

    public void AddExp(int amount)
    {
        CurrentExp += amount;

        int requiredExp = GetRequiredExp(CurrentLevel);
        while (CurrentExp >= requiredExp)
        {
            CurrentExp -= requiredExp;
            LevelUp();
            requiredExp = GetRequiredExp(CurrentLevel);
        }
    }

    int GetRequiredExp(int level)
    {
        return level * EXP_PER_LEVEL_MULTIPLIER;
    }

    /// <summary>HUD의 경험치 바가 진행률(현재/필요) 계산할 때 씀</summary>
    public int GetCurrentRequiredExp()
    {
        return GetRequiredExp(CurrentLevel);
    }

    void LevelUp()
    {
        CurrentLevel++;
        Debug.Log("[GameSession] 레벨업! 현재 레벨 : " + CurrentLevel);

        TriggerPerkSelection();
    }

    /// <summary>레벨업 시점에 호출. 강화 UI가 등록돼있으면 후보를 넘겨서 띄움</summary>
    void TriggerPerkSelection()
    {
        List<PerkData> candidates = new List<PerkData>();
        foreach (PerkData perk in PerkManager._instance.GetAll())
        {
            if (IsPerkEligible(perk))
                candidates.Add(perk);
        }

        List<PerkData> picked = PickRandom(candidates, PERK_CHOICE_COUNT);

        Debug.Log("[GameSession] 강화 선택지 " + picked.Count + "개:");
        foreach (PerkData perk in picked)
            Debug.Log(" - " + perk.PerkName);

        if (_perkSelectionUI != null)
            _perkSelectionUI.Show(picked);
        else
            Debug.LogWarning("[GameSession] 강화 선택 UI가 등록되어 있지 않음 (씬에 배치됐는지 확인)");
    }

    /// <summary>강화 선택 완료 시 호출(지금은 UI 대신 테스트 코드에서 직접 호출). 스택 +1 하고 스탯 재계산까지</summary>
    public void ApplyPerkChoice(int perkId)
    {
        if (!ActivePerks.ContainsKey(perkId))
            ActivePerks[perkId] = 0;
        ActivePerks[perkId]++;

        PerkData perk = PerkManager._instance.Get(perkId);
        Debug.Log("[GameSession] 강화 적용 : " + (perk != null ? perk.PerkName : perkId.ToString())
            + " (현재 " + ActivePerks[perkId] + "스택)");

        PlayerStats?.Recalculate();
    }

    // ─────────────────────────────────────────────
    // PlayerStatManager 등록 (씬마다 새로 생기는 Player 오브젝트가 자기 자신을 등록)
    // ─────────────────────────────────────────────

    public PlayerStatManager PlayerStats { get; private set; }

    public void RegisterPlayerStats(PlayerStatManager statManager)
    {
        PlayerStats = statManager;
    }

    public void UnregisterPlayerStats(PlayerStatManager statManager)
    {
        if (PlayerStats == statManager)
            PlayerStats = null;
    }

    // ─────────────────────────────────────────────
    // PlayerController 등록 (HUD가 CurrentHP, 쿨타임 등을 읽어가기 위함)
    // ─────────────────────────────────────────────

    public PlayerController Player { get; private set; }

    public void RegisterPlayer(PlayerController player)
    {
        Player = player;
    }

    public void UnregisterPlayer(PlayerController player)
    {
        if (Player == player)
            Player = null;
    }

    // ─────────────────────────────────────────────
    // 강화 선택 UI 등록 (씬마다 새로 생기는 HUD가 자기 자신을 등록)
    // ─────────────────────────────────────────────

    UIPerkSelectionController _perkSelectionUI;

    public void RegisterPerkSelectionUI(UIPerkSelectionController controller)
    {
        _perkSelectionUI = controller;
    }

    public void UnregisterPerkSelectionUI(UIPerkSelectionController controller)
    {
        if (_perkSelectionUI == controller)
            _perkSelectionUI = null;
    }

    /// <summary>무기 전용 강화는 현재 무기와 안 맞으면 제외, 이미 최대 스택이면 제외</summary>
    bool IsPerkEligible(PerkData perk)
    {
        if (perk.WeaponType != 0 && perk.WeaponType != CURRENT_WEAPON_TYPE)
            return false;

        int currentStack = ActivePerks.ContainsKey(perk.PerkID) ? ActivePerks[perk.PerkID] : 0;
        if (currentStack >= perk.MaxStack)
            return false;

        return true;
    }

    List<PerkData> PickRandom(List<PerkData> source, int count)
    {
        List<PerkData> pool = new List<PerkData>(source);
        List<PerkData> result = new List<PerkData>();

        int pickCount = Mathf.Min(count, pool.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

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
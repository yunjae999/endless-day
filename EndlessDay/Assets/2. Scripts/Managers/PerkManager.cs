using Defines;
using System.Collections.Generic;

/// <summary>
/// PerkTable(마스터) + PerkStatTable/PerkSpecialTable(자식, PerkID로 연결)을 읽어서
/// PerkID 기준으로 조립해두는 매니저. 게임 시작 시 1회 LoadAll() 호출, 이후엔 Get(perkId)로만 조회.
/// </summary>
public class PerkManager : TSingleton<PerkManager>
{
    Dictionary<int, PerkData> _perks = new Dictionary<int, PerkData>();

    public void LoadAll()
    {
        _perks = new Dictionary<int, PerkData>();

        DataTable perkTable = TableDataManager._instance.Get(TableName.PerkTable);
        DataTable statTable = TableDataManager._instance.Get(TableName.PerkStatTable);
        DataTable specialTable = TableDataManager._instance.Get(TableName.PerkSpecialTable);

        foreach (int perkId in perkTable.GetAllKeys())
        {
            PerkData data = new PerkData
            {
                PerkID = perkId,
                PerkName = perkTable.ToS(perkId, "PerkName"),
                Grade = perkTable.ToI(perkId, "Grade"),
                WeaponType = perkTable.ToI(perkId, "WeaponType"),
                MaxStack = perkTable.ToI(perkId, "MaxStack"),
                Description = perkTable.ToS(perkId, "Description"),
                IconPath = perkTable.ToS(perkId, "IconPath"),
            };

            data.StatEffects = FindStatEffects(statTable, perkId);
            data.SpecialEffect = FindSpecialEffect(specialTable, perkId);

            _perks[perkId] = data;
        }
    }

    /// <summary>PerkStatTable 전체를 순회하며 PerkID가 일치하는 행만 골라 담음 (1:N)</summary>
    List<StatEffect> FindStatEffects(DataTable statTable, int perkId)
    {
        List<StatEffect> result = new List<StatEffect>();

        foreach (int rowKey in statTable.GetAllKeys())
        {
            if (statTable.ToI(rowKey, "PerkID") != perkId)
                continue;

            result.Add(new StatEffect
            {
                StatType = statTable.ToI(rowKey, "StatType"),
                CalcType = statTable.ToI(rowKey, "CalcType"),
                Value = statTable.ToF(rowKey, "Value"),
            });
        }

        return result;
    }

    /// <summary>PerkSpecialTable에서 PerkID가 일치하는 첫 행. 특수효과 없는 강화면 null</summary>
    SpecialEffect FindSpecialEffect(DataTable specialTable, int perkId)
    {
        foreach (int rowKey in specialTable.GetAllKeys())
        {
            if (specialTable.ToI(rowKey, "PerkID") != perkId)
                continue;

            return new SpecialEffect
            {
                TriggerType = specialTable.ToI(rowKey, "TriggerType"),
                TriggerValue = specialTable.ToI(rowKey, "TriggerValue"),
                DamagePercent = specialTable.ToI(rowKey, "DamagePercent"),
                AreaRadius = specialTable.ToF(rowKey, "AreaRadius"),
                EffectFlags = specialTable.ToI(rowKey, "EffectFlags"),
            };
        }

        return null;
    }

    public PerkData Get(int perkId)
    {
        return _perks.ContainsKey(perkId) ? _perks[perkId] : null;
    }

    /// <summary>강화 선택 UI에서 후보 뽑을 때 전체 목록이 필요하므로</summary>
    public IEnumerable<PerkData> GetAll()
    {
        return _perks.Values;
    }
}
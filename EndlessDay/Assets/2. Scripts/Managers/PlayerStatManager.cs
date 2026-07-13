using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    int _baseMaxHP;
    int _baseAttackPower;
    int _baseDefense;
    int _baseMoveSpeed;
    int _baseRunSpeed;
    int _baseAttackSpeed;
    int _baseCritChance;
    int _baseCritDamage;

    public int BaseMoveSpeed { get { return _baseMoveSpeed; } }
    public int BaseRunSpeed { get { return _baseRunSpeed; } }
    public void InitBaseStats()
    {
        DataTable baseStatTable = TableDataManager._instance.Get(Defines.TableName.PlayerBaseStatTable);
        if (TableDataManager._instance == null)
            Debug.Log("null");

        _baseMaxHP = baseStatTable.ToI(1, "MaxHP");
        _baseAttackPower = baseStatTable.ToI(1, "AttackPower");
        _baseDefense = baseStatTable.ToI(1, "Defense");
        _baseMoveSpeed = baseStatTable.ToI(1, "MoveSpeed");
        _baseRunSpeed = baseStatTable.ToI(1, "RunSpeed");
        _baseAttackSpeed = baseStatTable.ToI(1, "AttackSpeed");
        _baseCritChance = baseStatTable.ToI(1, "CritChance");
        _baseCritDamage = baseStatTable.ToI(1, "CritDamage");
    }
}

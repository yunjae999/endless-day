using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD 1단계. 전부 읽기 전용 표시라 인벤토리/상점과 달리 여러 파일로 안 나누고
/// 하나로 묶었다. HUD 프리팹(씬마다 하나)에 부착.
/// 미니맵/보스체력바/방진행도는 던전 시스템 이후 2단계에서 추가 예정.
/// </summary>
public class UIHudController : MonoBehaviour
{
    [Header("스탯 패널")]
    [SerializeField] TextMeshProUGUI _attackPowerText;
    [SerializeField] TextMeshProUGUI _defenseText;
    [SerializeField] TextMeshProUGUI _critChanceText;
    [SerializeField] TextMeshProUGUI _critDamageText;
    [SerializeField] TextMeshProUGUI _attackSpeedText;
    [SerializeField] TextMeshProUGUI _moveSpeedText;

    [Header("초상화 / 레벨")]
    [SerializeField] TextMeshProUGUI _levelText;

    [Header("체력 / 경험치 바")]
    [SerializeField] Image _hpFillImage;     // Image Type = Filled 로 설정
    [SerializeField] Image _expFillImage;

    [Header("골드")]
    [SerializeField] TextMeshProUGUI _goldText;

    [Header("스킬 쿨타임 (Image Type = Filled, Fill Method = Radial 360 추천)")]
    [SerializeField] Image _rollCooldownOverlay;
    [SerializeField] Image _skillCooldownOverlay;

    [Header("강화 아이콘 (개수 바뀔 때만 다시 생성)")]
    [SerializeField] UIHudPerkIcon _perkIconPrefab;
    [SerializeField] Transform _perkIconParent;

    List<UIHudPerkIcon> _perkIcons = new List<UIHudPerkIcon>();
    int _lastPerkCount = -1;   // 강화 개수가 바뀔 때만 아이콘 목록을 다시 그리기 위한 캐시

    void Update()
    {
        RefreshStatPanel();
        RefreshLevelAndExp();
        RefreshHP();
        RefreshGold();
        RefreshCooldowns();
        RefreshPerkIcons();
    }

    void RefreshStatPanel()
    {
        PlayerStatManager stats = GameSession._instance.PlayerStats;
        if (stats == null)
            return;

        _attackPowerText.text = Mathf.RoundToInt(stats.FinalAttackPower).ToString();
        _defenseText.text = Mathf.RoundToInt(stats.FinalDefense).ToString();
        _critChanceText.text = Mathf.RoundToInt(stats.FinalCritChance) + "%";
        _critDamageText.text = Mathf.RoundToInt(stats.FinalCritDamage) + "%";
        _attackSpeedText.text = stats.FinalAttackSpeed.ToString("0.0");
        _moveSpeedText.text = Mathf.RoundToInt(stats.FinalMoveSpeed).ToString();
    }

    void RefreshLevelAndExp()
    {
        _levelText.text = "Lv." + GameSession._instance.CurrentLevel;

        int requiredExp = GameSession._instance.GetCurrentRequiredExp();
        float ratio = requiredExp > 0 ? (float)GameSession._instance.CurrentExp / requiredExp : 0f;
        _expFillImage.fillAmount = ratio;
    }

    void RefreshHP()
    {
        PlayerController player = GameSession._instance.Player;
        PlayerStatManager stats = GameSession._instance.PlayerStats;
        if (player == null || stats == null || stats.FinalMaxHP <= 0f)
            return;

        _hpFillImage.fillAmount = player.CurrentHP / stats.FinalMaxHP;
    }

    void RefreshGold()
    {
        _goldText.text = GameSession._instance.Gold.ToString();
    }

    void RefreshCooldowns()
    {
        PlayerController player = GameSession._instance.Player;
        if (player == null)
            return;

        _rollCooldownOverlay.fillAmount = player.RollCooldownRatio;
        _skillCooldownOverlay.fillAmount = player.SkillCooldownRatio;
    }

    /// <summary>강화 목록은 자주 안 바뀌니, 실제로 개수가 달라졌을 때만 다시 그림</summary>
    void RefreshPerkIcons()
    {
        Dictionary<int, int> activePerks = GameSession._instance.ActivePerks;

        if (activePerks.Count == _lastPerkCount)
            return;
        _lastPerkCount = activePerks.Count;

        foreach (UIHudPerkIcon icon in _perkIcons)
            Destroy(icon.gameObject);
        _perkIcons.Clear();

        foreach (KeyValuePair<int, int> pair in activePerks)
        {
            PerkData perk = PerkManager._instance.Get(pair.Key);
            if (perk == null)
                continue;

            UIHudPerkIcon icon = Instantiate(_perkIconPrefab, _perkIconParent);
            icon.SetContent(perk, pair.Value);
            _perkIcons.Add(icon);
        }
    }
}

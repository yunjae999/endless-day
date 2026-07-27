using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 머리 위 체력바 + 이름. World Space Canvas로 Player/Monster 프리팹의 자식에 배치.
/// IDamageable만 보고 동작하므로 플레이어/몬스터 어느 쪽이든 이 스크립트 하나로 재사용 가능.
/// 언제 보여줄지는 이 스크립트가 스스로 판단하지 않고, 소유자(MonsterController 등)가
/// ShowTemporarily()를 직접 호출해서 알려주는 방식 - 상태를 제일 잘 아는 쪽이 판단.
/// </summary>
public class UIWorldHealthBar : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] Image _hpFillImage;   // Image Type = Filled(Horizontal) 로 설정

    [Header("전투 중이거나 공격받았을 때만 표시 (몬스터용, 플레이어는 이 스크립트 자체를 안 씀)")]
    [SerializeField] CanvasGroup _canvasGroup;   // 껐다 켤 대상 - GameObject를 직접 끄면 Update가 멈춰서 다시 못 켜니 투명도로 처리
    [SerializeField] bool _showOnlyInCombat = true;
    [SerializeField] float _hideDelay = 3f;

    IDamageable _target;
    float _hideTimer;

    /// <summary>소유자(PlayerController/MonsterController)가 자기 자신을 등록할 때 호출</summary>
    public void Init(string displayName, IDamageable target)
    {
        _nameText.text = displayName;
        _target = target;

        if (_showOnlyInCombat && _canvasGroup != null)
            _canvasGroup.alpha = 0f;   // 평소엔 숨김
    }

    void Update()
    {
        if (_target == null || _target.MaxHP <= 0)
            return;

        _hpFillImage.fillAmount = (float)_target.CurrentHP / _target.MaxHP;

        if (!_showOnlyInCombat || _canvasGroup == null)
            return;

        if (_hideTimer > 0f)
        {
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f)
                _canvasGroup.alpha = 0f;
        }
    }

    /// <summary>전투 관련 상태에 들어가거나 공격받았을 때 소유자가 직접 호출 - 보여주고 숨김 타이머를 리셋</summary>
    public void ShowTemporarily()
    {
        if (!_showOnlyInCombat || _canvasGroup == null)
            return;

        _canvasGroup.alpha = 1f;
        _hideTimer = _hideDelay;
    }

    /// <summary>사망 등, 즉시 숨겨야 할 때 소유자가 직접 호출 (타이머 기다리지 않음)</summary>
    public void Hide()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 0f;
        _hideTimer = 0f;
    }
}
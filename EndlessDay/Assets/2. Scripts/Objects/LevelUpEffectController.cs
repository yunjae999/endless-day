using UnityEngine;

/// <summary>
/// 레벨업 시 VFX + 머리 위 텍스트 연출 전담. HUD 프리팹에 배치.
/// GameSession은 TSingleton(코드로 자동 생성)이라 씬에 미리 배치된 인스턴스가 없어서
/// 프리팹 참조를 인스펙터로 연결할 수 없다 - 그래서 이 컨트롤러로 분리.
/// </summary>
public class LevelUpEffectController : MonoBehaviour
{
    public static LevelUpEffectController _instance { get; private set; }

    [SerializeField] GameObject _levelUpVFXPrefab;
    [SerializeField] UIFloatingText _levelUpTextPrefab;
    [SerializeField] Vector3 _levelUpTextOffset = new Vector3(0f, 2.2f, 0f);   // 머리 위 위치

    void Awake()
    {
        _instance = this;
    }

    public void Play(Vector3 playerPosition)
    {
        if (_levelUpVFXPrefab != null)
            Instantiate(_levelUpVFXPrefab, playerPosition, Quaternion.identity);

        if (_levelUpTextPrefab != null)
        {
            UIFloatingText text = Instantiate(_levelUpTextPrefab, playerPosition + _levelUpTextOffset, Quaternion.identity);
            text.Init("Level Up!", new Color(1f, 0.84f, 0.2f));
        }
    }
}

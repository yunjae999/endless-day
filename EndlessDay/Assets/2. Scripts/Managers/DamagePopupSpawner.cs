using UnityEngine;

/// <summary>
/// 데미지 팝업 생성 전담. HUD 프리팹에 하나 배치 - 씬마다 자동으로 존재가 보장됨.
/// </summary>
public class DamagePopupSpawner : MonoBehaviour
{
    public static DamagePopupSpawner _instance { get; private set; }

    [SerializeField] UIDamagePopup _popupPrefab;

    void Awake()
    {
        _instance = this;
    }

    public void Spawn(Vector3 worldPosition, int damage, bool isCrit, bool isPlayerDamaged)
    {
        if (_popupPrefab == null)
            return;

        UIDamagePopup popup = Instantiate(_popupPrefab, worldPosition, Quaternion.identity);
        popup.Init(damage, isCrit, isPlayerDamaged);
    }
}

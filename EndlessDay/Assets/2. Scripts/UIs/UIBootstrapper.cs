using UnityEngine;

/// <summary>
/// Village/Dungeon 등 각 씬에 하나씩 배치. HUD와 Inventory UI 프리팹을 생성한다.
/// 인벤토리의 초기 표시 여부는 여기서 정하지 않음 - UIInventoryController.Awake()가
/// GameSession에 등록하면서 그 시점의 "열려있었는지" 상태를 그대로 이어받는다.
/// </summary>
public class UIBootstrapper : MonoBehaviour
{
    [SerializeField] GameObject _hudPrefab;
    [SerializeField] GameObject _inventoryPrefab;

    void Awake()
    {
        Instantiate(_hudPrefab);
        Instantiate(_inventoryPrefab);
    }
}

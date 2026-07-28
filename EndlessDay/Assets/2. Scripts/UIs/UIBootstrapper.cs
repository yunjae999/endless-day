using UnityEngine;

/// <summary>
/// Village/Dungeon 등 각 씬에 하나씩 배치. HUD/Inventory(2D UI)와 SceneEffects(3D 월드 이펙트)를
/// 씬마다 자동 생성한다. HUD는 순수 UI만 담당하도록, 레벨업 VFX 같은 3D 이펙트는 별도 프리팹으로 분리.
/// 인벤토리의 초기 표시 여부는 여기서 정하지 않음 - UIInventoryController.Awake()가
/// GameSession에 등록하면서 그 시점의 "열려있었는지" 상태를 그대로 이어받는다.
/// </summary>
public class UIBootstrapper : MonoBehaviour
{
    [SerializeField] GameObject _hudPrefab;
    [SerializeField] GameObject _inventoryPrefab;
    [SerializeField] GameObject _sceneEffectsPrefab;

    void Awake()
    {
        Instantiate(_hudPrefab);
        Instantiate(_inventoryPrefab);
        Instantiate(_sceneEffectsPrefab);
    }
}
using UnityEngine;

/// <summary>
/// 씬 시작 시 지정된 스폰 위치에 Player 프리팹을 생성. Village/Dungeon 등 각 씬에 하나씩 배치.
/// HUD/Inventory를 UIBootstrapper가 생성하는 것과 같은 방식 - Player도 씬마다 새로 생기는 게 전제라
/// (GameSession.RegisterPlayer로 자기 자신을 등록하는 구조), 씬 안에 미리 박아두는 대신 여기서 생성한다.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] GameObject _playerPrefab;
    [SerializeField] Transform _spawnPoint;   // 씬 안의 빈 오브젝트, 위치/방향 표시용 마커

    void Awake()
    {
        Vector3 position = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

        Instantiate(_playerPrefab, position, rotation);
    }
}

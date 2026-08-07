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
    [SerializeField] bool _showShadow = true;   // 이 씬(마을/던전)에서 그림자를 보여줄지 - 씬마다 배치된 스포너에서 각자 설정

    void Awake()
    {
        Vector3 position = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

        GameObject playerObject = Instantiate(_playerPrefab, position, rotation);

        if (playerObject.TryGetComponent<PlayerController>(out PlayerController player))
            player.SetShadowVisible(_showShadow);
    }
}
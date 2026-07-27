using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 방 하나. 몬스터 스폰은 MonsterSpawner에게 맡기고, 여기서는 "클리어 조건을 충족했는지"와
/// "충족하면 뭘 할지"만 책임진다.
/// </summary>
public class RoomController : MonoBehaviour
{
    [SerializeField] int _roomID;              // DungeonRoomTable/RoomSpawnTable의 RoomID와 일치해야 함
    [SerializeField] Transform[] _spawnPoints;  // 스폰 위치들, 스폰 개수가 더 많으면 순환 사용
    [SerializeField] DungeonController _dungeonController;
    [SerializeField] int _stageIndex;           // DungeonController.StageClear()에 넘길 인덱스 (0부터)
    [SerializeField] bool _isFinalRoom;         // 던전의 마지막 방(최종 보스)이면 체크 - 클리어 시 마을로 복귀

    List<MonsterController> _aliveMonsters = new List<MonsterController>();
    bool _isCleared;
    bool _hasSpawned;

    public bool IsCleared => _isCleared;

    void Start()
    {
        _aliveMonsters = MonsterSpawner.SpawnRoom(_roomID, _spawnPoints);
        _hasSpawned = true;
    }

    void Update()
    {
        if (_isCleared || !_hasSpawned)
            return;

        CheckClearCondition();
    }

    /// <summary>지금은 "스폰된 몬스터 전멸"만 판단. 나중에 조건 종류 늘어나면 여기만 확장하면 됨</summary>
    void CheckClearCondition()
    {
        // Destroy된 몬스터는 자동으로 null 취급되므로, 그것만 걸러내면 생존 여부 판단 가능
        _aliveMonsters.RemoveAll(m => m == null);

        if (_aliveMonsters.Count == 0)
            OnRoomCleared();
    }

    void OnRoomCleared()
    {
        _isCleared = true;
        Debug.Log("[RoomController] 방 클리어! RoomID : " + _roomID);

        UIClearBanner._instance?.Show("Room " + (_stageIndex + 1) + " Clear");

        // 마지막 방은 "다음 방으로 가는 길"이라는 개념 자체가 없어서(다음 방이 없으니) 호출하면 안 됨
        if (!_isFinalRoom && _dungeonController != null)
            _dungeonController.StageClear(_stageIndex);   // 다음 방으로 가는 길 열기 (벽/계단은 여기서 중앙 관리)

        if (_isFinalRoom)
            OnDungeonCleared();
    }

    void OnDungeonCleared()
    {
        Debug.Log("[RoomController] 던전 클리어!");

        // TODO: 클리어 보상(DungeonTable.ClearReward) 지급, PlayerData.IsCleared 서버 반영은
        //       결과창에서 확인 누른 시점 등으로 추후 연결

        if (UIResultController._instance != null)
            UIResultController._instance.Show(true);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");
    }
}
using Defines;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RoomSpawnTable/DungeonRoomTable을 읽어서 몬스터를 실제로 스폰하는 역할만 담당.
/// 상태를 안 가지는 정적 유틸리티라 씬에 배치할 필요 없이 어디서든 바로 호출 가능.
/// "클리어 조건 판단" 등 그 이후의 로직은 호출한 쪽(RoomController)의 책임.
/// </summary>
public static class MonsterSpawner
{
    public static List<MonsterController> SpawnRoom(int roomID, Transform[] spawnPoints)
    {
        List<MonsterController> spawned = new List<MonsterController>();

        DataTable roomTable = TableDataManager._instance.Get(TableName.DungeonRoomTable);
        DataTable spawnTable = TableDataManager._instance.Get(TableName.RoomSpawnTable);
        DataTable monsterTable = TableDataManager._instance.Get(TableName.MonsterTable);

        int totalSpawnCount = roomTable.ToI(roomID, "TotalSpawnCount");

        List<(int monsterId, int weight)> candidates = FindSpawnCandidates(spawnTable, roomID);
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] RoomID " + roomID + "에 대한 스폰 후보가 없음");
            return spawned;
        }

        for (int i = 0; i < totalSpawnCount; i++)
        {
            int monsterId = PickWeightedRandom(candidates);
            MonsterController monster = SpawnOneMonster(monsterId, monsterTable, spawnPoints[i % spawnPoints.Length]);
            if (monster != null)
                spawned.Add(monster);
        }

        return spawned;
    }

    /// <summary>RoomSpawnTable 전체를 순회해서 이 방(RoomID)에 해당하는 후보만 골라 담음</summary>
    static List<(int monsterId, int weight)> FindSpawnCandidates(DataTable spawnTable, int roomID)
    {
        List<(int monsterId, int weight)> result = new List<(int, int)>();

        foreach (int rowKey in spawnTable.GetAllKeys())
        {
            if (spawnTable.ToI(rowKey, "RoomID") != roomID)
                continue;

            int monsterId = spawnTable.ToI(rowKey, "MonsterID");
            int weight = spawnTable.ToI(rowKey, "SpawnWeight");
            result.Add((monsterId, weight));
        }

        return result;
    }

    static int PickWeightedRandom(List<(int monsterId, int weight)> candidates)
    {
        int totalWeight = 0;
        foreach (var c in candidates)
            totalWeight += c.weight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var c in candidates)
        {
            cumulative += c.weight;
            if (roll < cumulative)
                return c.monsterId;
        }

        return candidates[candidates.Count - 1].monsterId;   // 안전장치 (부동소수 오차 등)
    }

    static MonsterController SpawnOneMonster(int monsterId, DataTable monsterTable, Transform spawnPoint)
    {
        string prefabPath = monsterTable.ToS(monsterId, "PrefabPath");
        GameObject prefab = Resources.Load<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogWarning("[MonsterSpawner] 몬스터 프리팹을 찾을 수 없음 : " + prefabPath);
            return null;
        }

        GameObject monster = Object.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        monster.TryGetComponent<MonsterController>(out MonsterController controller);
        return controller;
    }
}

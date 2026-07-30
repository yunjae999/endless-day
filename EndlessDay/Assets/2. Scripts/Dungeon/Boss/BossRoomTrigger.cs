using UnityEngine;

/// <summary>
/// 보스방 입구에 배치하는 트리거. 플레이어가 이 영역에 들어오는 순간 보스를 활성화시킨다.
/// 보스는 던전 시작 시 동적으로 스폰되는 오브젝트라 인스펙터로 미리 연결해둘 수 없어서,
/// 트리거가 발동되는 그 순간(이미 스폰은 끝나있는 시점) "Boss" 태그로 씬에서 찾는다.
/// 한 번 발동하면 다시 반응할 필요 없으니 트리거 자체를 꺼버림.
/// </summary>
public class BossRoomTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[BossRoomTrigger] OnTriggerEnter : " + other.name + " (tag: " + other.tag + ")");   // 임시

        if (!other.CompareTag("Player"))
            return;

        GameObject bossObject = GameObject.FindWithTag("Boss");
        Debug.Log("[BossRoomTrigger] FindWithTag(\"Boss\") 결과 : " + (bossObject != null ? bossObject.name : "못 찾음"));   // 임시

        if (bossObject != null && bossObject.TryGetComponent<BossController>(out BossController boss))
            boss.ActivateBoss();

        gameObject.SetActive(false);   // 한 번 발동했으면 더 이상 감지 필요 없음
    }
}
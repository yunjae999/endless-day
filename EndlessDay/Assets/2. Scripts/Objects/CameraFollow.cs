using UnityEngine;

/// <summary>
/// 씬(Village/Dungeon)의 Main Camera에 부착. GameSession에 등록된 Player를 자동으로 따라간다.
/// Player는 씬마다 새로 생기지만, GameSession.Player가 항상 "지금 씬의 그 Player"를 가리키고 있어서
/// 씬마다 카메라에 참조를 따로 연결해줄 필요가 없다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Vector3 _offset = new Vector3(0f, 10f, -8f);   // 탑뷰 고정 각도, 씬에 맞게 조정

    [Header("카메라 이동 범위 제한 (던전이 좁아서 밖이 보이는 걸 방지)")]
    [SerializeField] bool _useBounds;
    [SerializeField] Vector2 _minBounds;   // X, Z
    [SerializeField] Vector2 _maxBounds;

    void LateUpdate()
    {
        PlayerController player = GameSession._instance.Player;
        if (player == null)
            return;

        Vector3 desiredPosition = player.transform.position + _offset;

        if (_useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, _minBounds.x, _maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, _minBounds.y, _maxBounds.y);
        }

        transform.position = desiredPosition;
    }
}
using UnityEngine;

/// <summary>
/// 탑다운 시점에서 타겟(플레이어)을 부드럽게 따라가는 카메라.
/// LateUpdate에서 갱신하여 타겟의 이동/물리 연산이 끝난 후 위치를 잡음(떨림 방지).
/// </summary>
public class CameraFollow : MonoBehaviour
{

    [Header("Position")]
    [Tooltip("타겟 기준 카메라 오프셋. 완전 수직 탑다운이면 Z를 0으로.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -5f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Bounds (선택)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    Transform target;
    private Vector3 velocity = Vector3.zero;

    public void SetTarget(Transform newTarget) => target = newTarget;

    void Start()
    {
        target = GameObject.FindWithTag("Player").transform;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.y, maxBounds.y);
        }

        // Lerp보다 SmoothDamp이 가속/감속이 자연스러워 카메라 움직임에 더 적합
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
using UnityEngine;

/// <summary>
/// VFX 프리팹에 붙여서 재생이 끝나면 자동으로 삭제한다.
/// Particle System이 있으면 그 재생 길이를 기준으로, 없으면 인스펙터에 지정한 고정 시간을 사용.
/// </summary>
public class VFXAutoDestroy : MonoBehaviour
{
    [SerializeField] float _fixedLifetime = 1f;   // Particle System이 없을 때만 사용

    void Start()
    {
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : _fixedLifetime;

        Destroy(gameObject, lifetime);
    }
}

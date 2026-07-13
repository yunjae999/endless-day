using UnityEngine;

public class AttackHitboxTrigger : MonoBehaviour
{
    PlayerController _controller;

    void Awake()
    {
        _controller = GetComponentInParent<PlayerController>();
    }

    void OnTriggerEnter(Collider other)
    {
        _controller.OnAttackHitboxTriggerEnter(other);
    }
}
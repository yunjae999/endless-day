using UnityEngine;
using Defines;

public class PlayerController : MonoBehaviour
{
    Animator _animator;
    PlayerActionState _currentState;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();  
    }

    public void ChangeActionState(PlayerActionState state)
    {
        _animator.SetInteger("ActionState", (int)state);
        _currentState = state;
    }
}

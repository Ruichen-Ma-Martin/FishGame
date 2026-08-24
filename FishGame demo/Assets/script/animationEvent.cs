using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationEvent : MonoBehaviour
{
    public event Action OnAttack;
    public event Action EndAttack;
    public event Action AttackFinish;
    private void AttackAnimationTrigger()
    {
        OnAttack?.Invoke();
    }
    private void EndAttackAnimationTrigger()
    {
        EndAttack?.Invoke();
    }
    private void AttackFinishTrigger()
    {
        AttackFinish?.Invoke();
    }
}

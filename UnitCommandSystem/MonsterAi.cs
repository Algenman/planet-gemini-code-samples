using UnityEngine;

/// <summary>
/// 몬스터가 현재 상황에 따라 대기, 순찰, 추적, 공격, 복귀,
/// 스포너 호출 행동을 전환하는 핵심 흐름입니다.
/// </summary>
public abstract class MonsterAi : UnitCommonAi
{
    [SerializeField] private float traceLimit = 12f;

    protected Transform target;
    protected Vector3 spawnPosition;
    protected bool isWaveState;

    protected override void UpdateAiState()
    {
        UpdateSituation();

        switch (aiState)
        {
            case AIState.Idle:
                WaitOrSelectPatrolPosition();
                break;

            case AIState.Patrol:
                PatrolAroundSpawn();
                break;

            case AIState.Trace:
                TraceTarget();
                break;

            case AIState.Attack:
                if (attackState == AttackState.Waiting)
                    CheckAttackRange();
                else
                    Attack();
                break;

            case AIState.ReturnToSpawn:
                ReturnToSpawn();
                break;

            case AIState.SpawnerCall:
                RespondToSpawnerCall();
                break;
        }
    }

    private void UpdateSituation()
    {
        if (!target)
        {
            if (aiState == AIState.Trace || aiState == AIState.Attack)
            {
                aiState = isWaveState
                    ? AIState.SpawnerCall
                    : AIState.ReturnToSpawn;
                attackState = AttackState.Waiting;
            }
            return;
        }

        targetDistance = Vector2.Distance(transform.position, target.position);
        float distanceFromSpawn = Vector2.Distance(transform.position, spawnPosition);

        if (!isWaveState && distanceFromSpawn > traceLimit)
        {
            aiState = AIState.ReturnToSpawn;
        }
        else if (targetDistance <= attackDistance)
        {
            aiState = AIState.Attack;
            attackState = AttackState.AttackStart;
        }
        else
        {
            aiState = AIState.Trace;
        }
    }

    public void CallFromSpawner(Transform callTarget)
    {
        target = callTarget;
        aiState = AIState.SpawnerCall;
    }

    private void WaitOrSelectPatrolPosition()
    {
        // ... 일정 시간 대기 후 스폰 지점 주변의 순찰 위치 선택
    }

    private void PatrolAroundSpawn()
    {
        // ... 순찰 중 타깃 발견 시 Trace 상태로 전환
    }

    private void TraceTarget()
    {
        // ... 공격 거리까지 타깃 추적
    }

    private void ReturnToSpawn()
    {
        // ... 추적 범위를 벗어나거나 타깃을 잃으면 스폰 지점으로 복귀
    }

    private void RespondToSpawnerCall()
    {
        // ... 스포너가 지정한 방어 대상 또는 웨이브 목적지로 이동
    }
}

using System.Collections.Generic;
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

    protected readonly List<WorldObj> targetList = new();
    protected WorldObj bestTarget;

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

    /// <summary>
    /// 주변에서 발견한 대상의 어그로, 거리, 공격 집중도를 점수화하여
    /// 현재 몬스터가 추적할 대상을 선택합니다.
    /// </summary>
    protected void SelectTargetByPriority()
    {
        if (aiState == AIState.ReturnToSpawn)
            return;

        var attackableCandidates =
            new List<(WorldObj obj, float distance, float aggro)>();

        WorldObj nearestUnattackable = null;
        float nearestUnattackableScore = float.MinValue;

        foreach (WorldObj candidate in targetList)
        {
            if (!candidate)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                candidate.transform.position);

            candidate.TryGet(out AggroAmount aggroComponent);
            float aggro = aggroComponent
                ? aggroComponent.GetAggroAmount()
                : 0f;

            bool isAttackable =
                candidate.Get<UnitAi>() ||
                candidate.Get<TowerAi>() ||
                candidate.Get<PlayerController>() ||
                candidate.Get<ScienceBuilding>();

            if (!isAttackable)
            {
                float score = aggro - distance * 5.5f;

                if (score > nearestUnattackableScore)
                {
                    nearestUnattackableScore = score;
                    nearestUnattackable = candidate;
                }

                continue;
            }

            attackableCandidates.Add((candidate, distance, aggro));
        }

        WorldObj selectedTarget = null;
        float bestScore = float.MinValue;

        foreach (var candidate in attackableCandidates)
        {
            float score =
                candidate.aggro - candidate.distance * 3f;

            candidate.obj.TryGet(out Structure structure);

            if (structure)
            {
                // 이미 많은 몬스터가 공격 중인 건물은 선호도를 낮춰
                // 특정 건물 하나에 모든 몬스터가 집중되는 것을 줄입니다.
                score -= structure.monsterTargetAmount * 3f;
            }

            if (bestTarget && candidate.obj == bestTarget)
            {
                // 기존 타깃에 보너스를 주어 점수가 조금 변할 때마다
                // 추적 대상이 바뀌는 현상을 줄입니다.
                score += 3f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                selectedTarget = candidate.obj;
            }
        }

        if (nearestUnattackable &&
            (selectedTarget == null ||
             nearestUnattackableScore > bestScore))
        {
            selectedTarget = nearestUnattackable;
        }

        if (!selectedTarget)
            return;

        UpdateTargetCount(selectedTarget);

        bestTarget = selectedTarget;
        target = selectedTarget.transform;
        aiState = AIState.Trace;
    }

    private void UpdateTargetCount(WorldObj nextTarget)
    {
        if (bestTarget == nextTarget)
            return;

        if (bestTarget && bestTarget.TryGet(out Structure previousStructure))
            previousStructure.monsterTargetAmount--;

        if (nextTarget.TryGet(out Structure nextStructure))
            nextStructure.monsterTargetAmount++;
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

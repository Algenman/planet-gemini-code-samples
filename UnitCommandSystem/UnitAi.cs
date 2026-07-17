using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어가 명령한 이동, 패트롤, 공격 이동 상태를 서버에서 관리합니다.
/// 이동 경로 생성과 프로젝트 의존 로직은 예시에서 제외했습니다.
/// </summary>
public abstract class UnitAi : UnitCommonAi
{
    [SerializeField] private float moveSpeed = 3f;

    private Vector3 targetPosition;
    private float arrivalRadius;
    private bool isAttackMove;

    protected override void UpdateAiState()
    {
        switch (aiState)
        {
            case AIState.Move:
                MoveToCommandPosition();
                break;

            case AIState.Patrol:
                Patrol();
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

            case AIState.Idle:
                SearchTargetWhenAttackMove();
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMoveCommandServerRpc(
        Vector3 destination,
        float groupArrivalRadius,
        bool attackMove)
    {
        targetPosition = destination;
        arrivalRadius = groupArrivalRadius;
        isAttackMove = attackMove;
        aiState = AIState.Move;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPatrolCommandServerRpc(Vector3 formationPosition)
    {
        targetPosition = formationPosition;
        aiState = AIState.Patrol;
    }

    private void MoveToCommandPosition()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime);

        // 목적지 한 점에 완전히 겹치게 하지 않고 그룹 크기에 맞춘 범위 안에서
        // 이동을 끝내 유닛끼리 길을 막아 정지하는 상황을 줄입니다.
        float stopDistance = Mathf.Max(0.05f, arrivalRadius / 2f);
        if (Vector3.Distance(transform.position, targetPosition) <= stopDistance)
            aiState = AIState.Idle;
    }

    private void Patrol()
    {
        // UnitGroupCtrl에서 전달한 포메이션별 좌표를 왕복합니다.
        // ... 패트롤 방향 전환 및 도착 처리
    }

    private void TraceTarget()
    {
        // ... 타깃 추적 및 공격 거리 확인
    }

    private void SearchTargetWhenAttackMove()
    {
        if (!isAttackMove)
            return;

        // ... 공격 이동 중 주변 타깃 검색
    }
}

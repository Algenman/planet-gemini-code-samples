using Unity.Netcode;

/// <summary>
/// 플레이어 유닛과 몬스터가 공통으로 사용하는 행동 및 공격 상태입니다.
/// 실제 프로젝트의 체력, 애니메이션, 피격 동기화 코드는 예시에서 제외했습니다.
/// </summary>
public enum AIState
{
    Idle,
    Move,
    Patrol,
    Trace,
    Attack,
    ReturnToSpawn,
    SpawnerCall,
    Die
}

public enum AttackState
{
    Waiting,
    AttackStart,
    AttackDelay
}

public abstract class UnitCommonAi : NetworkBehaviour
{
    protected AIState aiState = AIState.Idle;
    protected AttackState attackState = AttackState.Waiting;

    protected float targetDistance;
    protected float attackDistance;

    protected virtual void FixedUpdate()
    {
        // 이동, 타깃 선정, 공격 판정은 서버를 기준으로 처리합니다.
        if (!IsServer || aiState == AIState.Die)
            return;

        UpdateAiState();
    }

    protected abstract void UpdateAiState();

    protected void CheckAttackRange()
    {
        if (targetDistance > attackDistance)
        {
            aiState = AIState.Trace;
            attackState = AttackState.Waiting;
            return;
        }

        aiState = AIState.Attack;
        attackState = AttackState.AttackStart;
    }

    protected void Attack()
    {
        if (attackState != AttackState.AttackStart)
            return;

        if (ExecuteAttack())
            attackState = AttackState.AttackDelay;
        else
            attackState = AttackState.Waiting;
    }

    // 유닛 종류별 발사체, 근접 공격, 범위 공격은 하위 클래스에서 구현합니다.
    protected abstract bool ExecuteAttack();
}

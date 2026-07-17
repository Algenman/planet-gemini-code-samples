using System.Collections;
using UnityEngine;

/// <summary>
/// 공격 피해량과 공격 속도를 기준으로 추가 어그로를 누적하고,
/// 일정 시간마다 추가 어그로를 감소시킵니다.
/// </summary>
public class AggroAmount : MonoBehaviour
{
    public float baseAggroAmount;

    [SerializeField] private float aggroAmount;

    private readonly float maxAggroAmount = 20f;
    private readonly float aggroAmountPercent = 0.05f;
    private readonly float aggroDecayStep = 1f;
    private readonly float aggroDecayInterval = 4f;

    private bool isAggroActive;

    public void SetAggroAmount(float damage, float attackSpeed)
    {
        float speedAmount = attackSpeed * 2f / 10f;

        aggroAmount +=
            damage * aggroAmountPercent + speedAmount;

        if (aggroAmount > maxAggroAmount)
            aggroAmount = maxAggroAmount;

        if (!isAggroActive)
            StartCoroutine(AggroDecayTimer());
    }

    private IEnumerator AggroDecayTimer()
    {
        isAggroActive = true;

        while (aggroAmount > 0f)
        {
            yield return new WaitForSeconds(aggroDecayInterval);
            aggroAmount -= aggroDecayStep;
        }

        aggroAmount = 0f;
        isAggroActive = false;
    }

    public float GetAggroAmount()
    {
        return baseAggroAmount + aggroAmount;
    }
}

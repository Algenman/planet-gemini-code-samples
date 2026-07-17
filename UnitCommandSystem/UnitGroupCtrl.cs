using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택한 유닛의 그룹 이동과 패트롤 명령을 분배합니다.
/// </summary>
public class UnitGroupCtrl : MonoBehaviour
{
    [SerializeField] private List<UnitAi> unitList = new();

    private readonly List<Vector3> unitOffsets = new();
    private Vector3 groupCenter;

    public void SetMoveTarget(Vector3 targetPosition, bool isAttackMove)
    {
        if (unitList.Count == 0)
            return;

        // 유닛 수에 따라 도착 허용 반경을 넓힙니다.
        // 모든 유닛이 한 좌표를 차지하려다 서로 가로막는 상황을 줄입니다.
        float totalDiameter = 0.7f * unitList.Count;
        float groupRadius = totalDiameter / (2f * Mathf.PI);
        float arrivalRadius = groupRadius / 2f;

        foreach (UnitAi unit in unitList)
            unit.SetMoveCommandServerRpc(targetPosition, arrivalRadius, isAttackMove);
    }

    public void SetPatrolTarget(Vector3 patrolPosition)
    {
        if (unitList.Count == 0)
            return;

        CalculateGroupFormation();

        // 그룹 중심을 기준으로 저장한 상대 위치를 목적지에도 적용합니다.
        // 패트롤 이동 중에도 선택 당시의 배치 간격을 유지합니다.
        for (int i = 0; i < unitList.Count; i++)
        {
            Vector3 formationPosition = patrolPosition + unitOffsets[i];
            unitList[i].SetPatrolCommandServerRpc(formationPosition);
        }
    }

    private void CalculateGroupFormation()
    {
        groupCenter = Vector3.zero;

        foreach (UnitAi unit in unitList)
            groupCenter += unit.transform.position;

        groupCenter /= unitList.Count;

        unitOffsets.Clear();

        foreach (UnitAi unit in unitList)
            unitOffsets.Add(unit.transform.position - groupCenter);
    }
}

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 마우스 선택 결과와 유닛 명령을 이벤트로 전달합니다.
/// 커서, 정보창, 입력 액션 연결 등 UI 의존 코드는 예시에서 제외했습니다.
/// </summary>
public class UnitDrag : MonoBehaviour
{
    public static event Action ClearSelectionRequested;
    public static event Action<UnitAi> UnitSelected;
    public static event Action<Vector3, bool> MoveRequested;
    public static event Action<Vector3> PatrolRequested;
    public static event Action HoldRequested;
    public static event Action<NetworkObjectReference> TargetRequested;

    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private float dragThreshold = 0.1f;

    public void LeftMouseUp(Vector2 startPosition, Vector2 endPosition)
    {
        if (Vector2.Distance(startPosition, endPosition) >= dragThreshold)
            SelectUnitsInArea(startPosition, endPosition);
        else
            SelectSingleUnit(endPosition);
    }

    public void RightMouseUp(Vector2 targetPosition)
    {
        MoveRequested?.Invoke(targetPosition, false);
    }

    public void SetAttackMove(Vector2 targetPosition)
    {
        MoveRequested?.Invoke(targetPosition, true);
    }

    public void SetPatrol(Vector2 targetPosition)
    {
        PatrolRequested?.Invoke(targetPosition);
    }

    public void Hold()
    {
        HoldRequested?.Invoke();
    }

    public void SetTarget(NetworkObject target)
    {
        if (target)
            TargetRequested?.Invoke(new NetworkObjectReference(target));
    }

    private void SelectUnitsInArea(Vector2 startPosition, Vector2 endPosition)
    {
        Vector2 min = Vector2.Min(startPosition, endPosition);
        Vector2 max = Vector2.Max(startPosition, endPosition);
        Collider2D[] colliders = Physics2D.OverlapAreaAll(min, max, unitLayer);

        var selectedUnits = new HashSet<UnitAi>();

        foreach (Collider2D collider in colliders)
        {
            UnitAi unit = collider.GetComponentInParent<UnitAi>();
            if (unit)
                selectedUnits.Add(unit);
        }

        ReplaceSelection(selectedUnits);
    }

    private void SelectSingleUnit(Vector2 position)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero, 0f, unitLayer);
        UnitAi unit = hit ? hit.collider.GetComponentInParent<UnitAi>() : null;

        ClearSelectionRequested?.Invoke();

        if (unit)
            UnitSelected?.Invoke(unit);
    }

    public void SelectSameTypeOnScreen(UnitAi selectedUnit)
    {
        if (!selectedUnit || !Camera.main)
            return;

        var sameTypeUnits = new List<UnitAi>();
        UnitAi[] allUnits = FindObjectsByType<UnitAi>(FindObjectsSortMode.None);

        foreach (UnitAi unit in allUnits)
        {
            if (unit.UnitTypeIndex != selectedUnit.UnitTypeIndex ||
                unit.UnitLevel != selectedUnit.UnitLevel)
                continue;

            Vector3 viewportPosition = Camera.main.WorldToViewportPoint(
                unit.transform.position);

            bool isOnScreen = viewportPosition.z > 0f &&
                              viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
                              viewportPosition.y >= 0f && viewportPosition.y <= 1f;

            if (isOnScreen)
                sameTypeUnits.Add(unit);
        }

        ReplaceSelection(sameTypeUnits);
    }

    private static void ReplaceSelection(IEnumerable<UnitAi> units)
    {
        ClearSelectionRequested?.Invoke();

        foreach (UnitAi unit in units)
            UnitSelected?.Invoke(unit);
    }
}

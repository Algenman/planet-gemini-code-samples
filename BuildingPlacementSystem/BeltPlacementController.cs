using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원본 프로젝트의 BeltPreBuilding 핵심 로직입니다.
/// 드래그한 벨트를 설치 가능한 연속 구간 단위로 나눕니다.
/// </summary>
public class BeltPlacementController : BuildingPlacementController
{
    protected override void FixedUpdate()
    {
        if (!isEnough || !mouseHoldCheck || !isBuildingOn)
            return;

        if (buildingList.Count > canBuildCount)
            return;

        Vector3 currentPosition = transform.position;
        float deltaX = currentPosition.x - endBuildPos.x;
        float deltaY = currentPosition.y - endBuildPos.y;

        if (Mathf.Abs((int)deltaX) < 1 &&
            Mathf.Abs((int)deltaY) < 1)
        {
            return;
        }

        endBuildPos = currentPosition;
        CheckPos(endBuildPos, (int)deltaX, (int)deltaY);
        isDrag = true;
    }

    protected void ConfirmBeltPlacement()
    {
        var buildGroups = new List<List<GameObject>>();
        List<GameObject> currentGroup = null;

        for (int i = 0; i < buildingList.Count; i++)
        {
            if (GroupBuildCheck(buildingList[i], posList[i]))
            {
                if (currentGroup == null)
                {
                    currentGroup = new List<GameObject>();
                    buildGroups.Add(currentGroup);
                }

                currentGroup.Add(buildingList[i]);
            }
            else
            {
                // 설치할 수 없는 셀을 만나면 현재 연속 구간을 종료합니다.
                currentGroup = null;
            }
        }

        foreach (List<GameObject> group in buildGroups)
        {
            var positions = new Vector3[group.Count];
            var directions = new int[group.Count];

            for (int i = 0; i < group.Count; i++)
            {
                positions[i] = group[i].transform.position;
                directions[i] = (int)group[i]
                    .GetComponent<BuildingPlacementPreview>()
                    .animator.GetFloat("DirNum");
            }

            // 실제 프로젝트에서는 방향 배열을 받는 ServerRpc 오버로드로
            // 각 벨트의 위치와 방향을 함께 전달합니다.
            PlaceBeltGroupServerRpc(
                isInHostMap,
                buildingIndex,
                positions,
                directions,
                gameManager.isDebugMode);
        }
    }

    private void CheckPos(Vector3 targetPosition, int moveX, int moveY)
    {
        // 원본의 드래그 방향 계산과 꺾이는 구간 생성 로직입니다.
        // 프로젝트 의존 프리뷰 생성 부분은 생략했습니다.
        // ...
    }

    private void PlaceBeltGroupServerRpc(
        bool hostMap,
        int index,
        Vector3[] positions,
        int[] directions,
        bool debugMode)
    {
        // 원본 PreBuilding의 방향 배열 ServerRpc 호출부입니다.
        // ...
    }
}

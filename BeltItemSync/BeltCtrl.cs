using UnityEngine;

/// <summary>
/// ServerTime을 기준으로 벨트 위 아이템의 위치를 계산합니다.
///
/// 아이템의 현재 좌표를 네트워크로 계속 전달하지 않고,
/// 진입 시간과 이동 시간을 이용해 각 클라이언트가 위치를 계산합니다.
/// </summary>
public class BeltCtrl : MonoBehaviour
{
    // ... 벨트 및 아이템 관리 코드

    public void ItemMove(double serverNow)
    {
        for (int i = 0; i < itemObjList.Count; i++)
        {
            ItemProps item = itemObjList[i];

            // 서버 시간을 기준으로 벨트 진입 후 경과 시간을 계산합니다.
            double elapsed =
                serverNow - item.beltEnterTime;

            float progress;

            if (item.beltTravelDuration <= 0)
            {
                progress = 1f;
            }
            else
            {
                progress = Mathf.Clamp01(
                    (float)(
                        elapsed /
                        item.beltTravelDuration
                    )
                );
            }

            // 잘못된 시간 데이터로 인한 위치 오류를 방지합니다.
            if (float.IsNaN(progress))
                progress = 1f;

            Vector3 newPosition = Vector3.Lerp(
                item.beltStartPos,
                item.beltEndPos,
                progress
            );

            // 유효한 좌표일 때만 아이템 위치를 적용합니다.
            if (!float.IsNaN(newPosition.x) &&
                !float.IsNaN(newPosition.y) &&
                !float.IsNaN(newPosition.z))
            {
                item.transform.position =
                    newPosition;
            }
        }

        if (itemObjList.Count == 0)
            return;

        // 가장 앞에 있는 아이템이 벨트 끝에 도착했는지 확인합니다.
        ItemProps frontItem = itemObjList[0];

        double frontItemElapsed =
            serverNow - frontItem.beltEnterTime;

        isItemStop =
            frontItem.beltTravelDuration <= 0 ||
            frontItemElapsed >=
            frontItem.beltTravelDuration;

        if (isItemStop)
        {
            TryTransferToNextBelt(serverNow);
        }
    }

    // ... 아이템 전달 및 벨트 관리 코드
}

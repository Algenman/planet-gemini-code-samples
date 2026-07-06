using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 그룹에 포함된 벨트의 아이템 이동을 일괄적으로 처리합니다.
///
/// 각 BeltCtrl에서 개별적으로 FixedUpdate를 실행하지 않고,
/// 그룹 관리자가 한 번만 ServerTime을 조회한 뒤 모든 벨트에 전달합니다.
/// </summary>
public class BeltGroupMgr : NetworkBehaviour
{
    // ... 벨트 그룹 관리 코드

    private void FixedUpdate()
    {
        if (Time.timeScale == 0)
            return;

        // 같은 물리 프레임에서 처리되는 모든 벨트가
        // 동일한 시점의 서버 시간을 사용하도록 한 번만 조회합니다.
        double serverNow =
            NetworkManager.Singleton.ServerTime.Time;

        foreach (BeltCtrl belt in beltList)
        {
            if (!belt ||
                belt.destroyStart ||
                belt.isPreBuilding ||
                !belt.isGameStartItemReady)
            {
                continue;
            }

            if (belt.itemObjList.Count > 0)
            {
                // 같은 그룹의 모든 벨트에 동일한 시간 값을 전달합니다.
                belt.ItemMove(serverNow);
            }
            else if (belt.isItemStop)
            {
                belt.isItemStop = false;
            }
        }
    }

    // ... 벨트 그룹 관리 코드
}

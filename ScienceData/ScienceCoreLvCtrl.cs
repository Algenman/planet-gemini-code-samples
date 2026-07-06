using UnityEngine;

/// <summary>
/// 코어 레벨에 해당하는 연구 데이터를 조회하고
/// 정렬된 순서대로 연구 아이콘을 생성합니다.
/// </summary>
public class ScienceCoreLvCtrl : MonoBehaviour
{
    // ... 연구 UI 참조 및 초기화 코드

    private void CreateScienceIcons()
    {
        var scienceData =
            ScienceInfoGet.instance
                .GetScienceByCoreLevel(coreLevel);

        foreach (var data in scienceData.Values)
        {
            GameObject iconObject =
                Instantiate(
                    scienceTreeIcon,
                    panel.transform
                );

            SciTreeIconCtrl iconController =
                iconObject.GetComponent<SciTreeIconCtrl>();

            Item itemData =
                itemList.FindDataGetLevel(
                    data.name,
                    data.level
                );

            iconController.icon.sprite =
                itemData.icon;

            string gameName =
                InGameNameDataGet.instance.ReturnName(
                    data.level,
                    data.name
                );

            iconController.SetIcon(
                data.name,
                data.level,
                data.info.coreLv,
                data.info.time,
                gameName,
                data.info.basicScience
            );
        }
    }

    // ... 연구 UI 관리 코드
}

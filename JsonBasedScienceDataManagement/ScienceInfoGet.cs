using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 연구 JSON을 로드하고 대상·단계별 데이터를 제공합니다.
/// </summary>
public class ScienceInfoGet : MonoBehaviour
{
    private Dictionary<
        string,
        Dictionary<
            string,
            Dictionary<int, ScienceInfoData>
        >
    > _scienceInfoData;

    public static ScienceInfoGet instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 연구 대상 → 카테고리 → 단계 구조로 역직렬화합니다.
        TextAsset json =
            Resources.Load<TextAsset>("ScienceInfo");

        _scienceInfoData =
            JsonConvert.DeserializeObject<
                Dictionary<
                    string,
                    Dictionary<
                        string,
                        Dictionary<int, ScienceInfoData>
                    >
                >
            >(json.text);
    }

    /// <summary>
    /// 연구 대상 이름과 단계에 해당하는 데이터를 반환합니다.
    /// </summary>
    public ScienceInfoData GetScienceInfo(
        string scienceName,
        int level)
    {
        if (!_scienceInfoData.TryGetValue(
                scienceName,
                out var categoryData))
        {
            return null;
        }

        foreach (var levelData in categoryData.Values)
        {
            if (levelData.TryGetValue(
                    level,
                    out ScienceInfoData scienceInfo))
            {
                return scienceInfo;
            }
        }

        return null;
    }

    /// <summary>
    /// 지정한 코어 레벨에서 노출할 연구 데이터를
    /// sortIndex 순서로 반환합니다.
    /// </summary>
    public SortedDictionary<
        int,
        (string name, int level, ScienceInfoData info)
    > GetScienceByCoreLevel(int coreLevel)
    {
        var result = new SortedDictionary<
            int,
            (string, int, ScienceInfoData)
        >();

        foreach (var science in _scienceInfoData)
        {
            if (science.Key == "Core")
                continue;

            foreach (var category in science.Value.Values)
            {
                foreach (var level in category)
                {
                    ScienceInfoData info = level.Value;

                    if (info.coreLv == coreLevel)
                    {
                        result.Add(
                            info.sortIndex,
                            (
                                science.Key,
                                level.Key,
                                info
                            )
                        );
                    }
                }
            }
        }

        return result;
    }
}

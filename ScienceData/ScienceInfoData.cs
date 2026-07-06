using System.Collections.Generic;

/// <summary>
/// JSON에 저장된 하나의 연구 단계 정보를 나타냅니다.
/// </summary>
public class ScienceInfoData
{
    public int sortIndex;
    public List<string> items;
    public List<int> amounts;
    public int coreLv;
    public float time;
    public string info;
    public bool basicScience;
}

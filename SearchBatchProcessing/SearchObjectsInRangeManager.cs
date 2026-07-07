using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 유닛, 타워, 건물의 주변 오브젝트 탐색을 한 곳에서 관리하는 매니저입니다.
///
/// 오브젝트 수가 많아질수록 각 오브젝트가 동시에 탐색을 수행하면
/// 특정 프레임에 연산이 몰릴 수 있기 때문에, searchCap 기준으로
/// 여러 프레임에 나누어 탐색을 처리합니다.
/// </summary>
public class SearchObjectsInRangeManager : NetworkBehaviour
{
    private readonly List<UnitCommonAi> unitList = new();
    private readonly List<TowerAi> towerList = new();
    private readonly List<Structure> structureList = new();

    private readonly HashSet<UnitCommonAi> pendingUnitRemove = new();
    private readonly HashSet<TowerAi> pendingTowerRemove = new();

    [SerializeField] private int searchCap = 50;

    [SerializeField] private float unitSearchInterval = 0.3f;
    [SerializeField] private float towerSearchInterval = 0.9f;

    private float unitSearchTimer;
    private float towerSearchTimer;

    private int unitCurrentIndex;
    private int towerCurrentIndex;

    private bool isUnitProcessing;
    private bool isTowerProcessing;

    #region Singleton
    public static SearchObjectsInRangeManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }
    #endregion

    private void Update()
    {
        // 탐색 판정은 서버에서만 수행하여 멀티플레이 환경의 기준을 일관되게 유지합니다.
        if (!IsServer)
            return;

        ProcessUnitSearch();
        ProcessTowerSearch();
    }

    /// <summary>
    /// 유닛 주변 탐색을 searchCap 개수만큼 나누어 처리합니다.
    /// 모든 유닛이 한 프레임에 탐색하지 않도록 현재 인덱스를 저장해두고
    /// 다음 프레임에 이어서 처리합니다.
    /// </summary>
    private void ProcessUnitSearch()
    {
        if (!isUnitProcessing)
        {
            unitSearchTimer += Time.deltaTime;

            if (unitSearchTimer >= unitSearchInterval)
            {
                unitSearchTimer = 0f;
                unitCurrentIndex = 0;
                isUnitProcessing = true;
            }

            if (pendingUnitRemove.Count > 0)
                RemovePendingUnits();

            return;
        }

        if (unitCurrentIndex >= unitList.Count)
        {
            isUnitProcessing = false;
            return;
        }

        int remaining = unitList.Count - unitCurrentIndex;
        int count = Mathf.Min(searchCap, remaining);

        for (int i = 0; i < count; i++)
        {
            UnitCommonAi unit = unitList[unitCurrentIndex];

            if (unit)
                unit.SearchObjectsInRange();
            else
                pendingUnitRemove.Add(unit);

            unitCurrentIndex++;
        }

        if (unitCurrentIndex >= unitList.Count)
            isUnitProcessing = false;
    }

    /// <summary>
    /// 타워 탐색도 유닛과 동일하게 분산 처리합니다.
    /// 타워는 유닛보다 탐색 주기를 길게 두어 전체 탐색 부하를 조절했습니다.
    /// </summary>
    private void ProcessTowerSearch()
    {
        if (!isTowerProcessing)
        {
            towerSearchTimer += Time.deltaTime;

            if (towerSearchTimer >= towerSearchInterval)
            {
                towerSearchTimer = 0f;
                towerCurrentIndex = 0;
                isTowerProcessing = true;
            }

            if (pendingTowerRemove.Count > 0)
                RemovePendingTowers();

            return;
        }

        if (towerCurrentIndex >= towerList.Count)
        {
            isTowerProcessing = false;
            return;
        }

        int remaining = towerList.Count - towerCurrentIndex;
        int count = Mathf.Min(searchCap, remaining);

        for (int i = 0; i < count; i++)
        {
            TowerAi tower = towerList[towerCurrentIndex];

            if (tower)
                tower.SearchObjectsInRange();
            else
                pendingTowerRemove.Add(tower);

            towerCurrentIndex++;
        }

        if (towerCurrentIndex >= towerList.Count)
            isTowerProcessing = false;
    }

    public void UnitListAdd(UnitCommonAi unit)
    {
        if (!unitList.Contains(unit))
            unitList.Add(unit);
    }

    public void UnitListRemove(UnitCommonAi unit)
    {
        // 탐색 루프 중 리스트가 변경되지 않도록 삭제 요청을 pending 목록에 모아둡니다.
        pendingUnitRemove.Add(unit);
    }

    private void RemovePendingUnits()
    {
        unitList.RemoveAll(unit => unit == null || pendingUnitRemove.Contains(unit));
        pendingUnitRemove.Clear();
    }

    public void TowerListAdd(TowerAi tower)
    {
        if (!towerList.Contains(tower))
            towerList.Add(tower);
    }

    public void TowerListRemove(TowerAi tower)
    {
        pendingTowerRemove.Add(tower);
    }

    private void RemovePendingTowers()
    {
        towerList.RemoveAll(tower => tower == null || pendingTowerRemove.Contains(tower));
        pendingTowerRemove.Clear();
    }

    public void StructureListAdd(Structure structure)
    {
        if (!structureList.Contains(structure))
            structureList.Add(structure);
    }

    public void StructureListRemove(Structure structure)
    {
        structureList.Remove(structure);
    }

    /// <summary>
    /// 저장 데이터를 불러온 직후처럼 여러 건물의 연결 정보를 다시 탐색해야 할 때 사용합니다.
    /// 건물 탐색도 searchCap 기준으로 나누어 처리해 한 프레임에 부하가 몰리지 않게 했습니다.
    /// </summary>
    public void StructureSearchFunc()
    {
        StartCoroutine(StructureSearchCoroutine());
    }

    private IEnumerator StructureSearchCoroutine()
    {
        yield return null;

        for (int i = 0; i < structureList.Count; i++)
        {
            Structure structure = structureList[i];
            if (structure)
                structure.SearchObjectsInRange();

            if ((i + 1) % searchCap == 0)
                yield return null;
        }
    }
}

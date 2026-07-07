# SearchBatchProcessing

대량의 유닛, 타워, 건물이 주변 오브젝트를 탐색할 때 탐색 작업을 한 프레임에 몰아서 처리하지 않고, 일정 개수 단위로 나누어 처리한 예시입니다.

## 문제

게임이 진행될수록 유닛, 방어 타워, 건물 수가 증가하면서 각 오브젝트가 주변 대상을 탐색하는 비용도 함께 증가했습니다.

특히 모든 오브젝트가 같은 프레임에 `SearchObjectsInRange()`를 호출하면 특정 프레임에 연산이 몰려 프레임 드랍이 발생할 수 있었습니다.

## 해결

각 오브젝트가 개별적으로 탐색 타이밍을 관리하는 대신, `SearchObjectsInRangeManager`에서 탐색 대상 리스트를 중앙 관리하도록 구성했습니다.

유닛과 타워는 각각 다른 탐색 주기를 가지고, 탐색이 시작되면 `searchCap` 개수만큼만 처리한 뒤 다음 프레임에 이어서 처리합니다.

또한 탐색 루프 중 리스트가 변경되지 않도록 제거 요청은 `pending` 목록에 모아두었다가 안전한 시점에 한 번에 정리했습니다.

## 핵심 구현

- 유닛, 타워, 건물 탐색 리스트를 중앙 매니저에서 관리
- `searchCap`을 기준으로 한 프레임에 처리할 탐색 개수 제한
- 현재 처리 인덱스를 저장해 여러 프레임에 걸쳐 탐색을 이어서 수행
- 제거 요청은 즉시 삭제하지 않고 `pending` 목록에 모아 안전하게 처리
- 멀티플레이 환경에서 판정 기준이 어긋나지 않도록 서버에서만 탐색 수행

## 예시 코드

```csharp
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
```

## 결과

주변 탐색 작업이 특정 프레임에 몰리는 것을 줄이고, 오브젝트 수가 증가해도 탐색 부하가 급격히 튀는 상황을 완화했습니다.

또한 탐색 주기와 프레임당 처리량을 매니저에서 조절할 수 있게 되어, 이후 유닛/타워 수가 늘어나도 탐색 비용을 한 곳에서 관리할 수 있었습니다.

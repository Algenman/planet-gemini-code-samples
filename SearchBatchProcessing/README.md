# SearchBatchProcessing

대량의 유닛, 타워, 건물이 주변 오브젝트를 탐색할 때 탐색 작업을 한 프레임에 몰아서 처리하지 않고, 일정 개수 단위로 나누어 처리한 예시입니다.

## 문제

게임이 진행될수록 유닛, 방어 타워, 건물 수가 증가하면서 각 오브젝트가 주변 대상을 탐색하는 비용도 함께 증가했습니다.

특히 여러 오브젝트가 같은 프레임에 `SearchObjectsInRange()`를 호출하면 해당 프레임에 탐색 호출이 모였습니다.

## 해결

각 오브젝트가 개별적으로 탐색 타이밍을 관리하는 대신, `SearchObjectsInRangeManager`에서 탐색 대상 리스트를 중앙 관리하도록 구성했습니다.

유닛과 타워는 각각 다른 탐색 주기를 가지고, 탐색이 시작되면 `searchCap` 개수만큼만 처리한 뒤 다음 프레임에 이어서 처리합니다.

또한 탐색 루프 중 리스트가 변경되지 않도록 제거 요청은 `pending` 목록에 모아두었다가 탐색 처리 밖에서 정리했습니다.

## 핵심 구현

- 유닛, 타워, 건물 탐색 리스트를 중앙 매니저에서 관리
- `searchCap`을 기준으로 한 프레임에 처리할 탐색 개수 제한
- 현재 처리 인덱스를 저장해 여러 프레임에 걸쳐 탐색을 이어서 수행
- 제거 요청은 즉시 삭제하지 않고 `pending` 목록에 모아 처리
- Host와 Client가 서로 다른 대상을 선택하지 않도록 서버에서만 탐색 수행

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

## 확인한 내용

Unity Profiler에서 `searchCap`을 기준으로 탐색 호출이 여러 프레임에 나뉘는 것을 확인했습니다. 당시 측정값은 기록하지 않아 향상률은 적지 않았습니다.

탐색 중 유닛과 타워를 제거하는 상황도 확인했으며, `pending` 목록을 정리한 뒤 다음 탐색이 컬렉션 변경 오류 없이 이어졌습니다.

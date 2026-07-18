# WorldObj Component Cache

Unity Netcode 기반 2인 협동 게임 프로젝트에서 사용한 상속 기반 컴포넌트 캐싱 예시입니다.

## 배경

프로젝트 규모가 커지면서 건물, 유닛, 몬스터 등 여러 오브젝트가 서로의 컴포넌트를 조회하는 경우가 많아졌습니다. 특히 주변 오브젝트를 탐색하거나 건물 타입에 따라 처리하는 과정에서 컴포넌트 조회와 분기 코드가 여러 클래스에 반복되었습니다.

반복 조회를 공통 코드에서 처리하기 위해 `WorldObj` 컴포넌트 캐시를 구성했습니다. `WorldObj`는 생성 시점에 부착된 컴포넌트와 프로젝트에서 정의한 부모 클래스 타입까지 캐싱하고, 이후 `Has<T>()`, `Get<T>()`, `TryGet<T>()`로 조회합니다.

## 주요 구조

다음과 같은 상속 구조가 있다면:

```text
Miner : Production : Structure
```

하나의 `Miner` 컴포넌트를 구체 타입뿐 아니라 부모 타입으로도 조회할 수 있습니다.

```csharp
worldObj.Get<Miner>();
worldObj.TryGet(out Production production);
worldObj.Has<Structure>();
```

## 활용 예시

물류 시스템에서는 서로 다른 건물을 공통 부모 타입의 리스트 하나로 관리합니다.

```csharp
public List<Structure> outObj = new List<Structure>();
```

다음은 아이템 전달 로직 중 건물의 기능에 따라 처리하는 부분만 발췌한 예시입니다.

```csharp
protected virtual void SendItem(int itemIndex)
{
    // ... 공통 유효성 및 상태 검사

    Structure outFactory = outObj[sendItemIndex];

    if (outFactory.TryGet(out Production production))
    {
        // 생산 건물의 아이템 수용 가능 여부 확인
        Item item =
            GeminiNetworkManager.instance.GetItemSOFromIndex(itemIndex);

        if (!production.CanTakeItem(item))
            return;
    }
    else if (outFactory.TryGet(out SendUnderBeltCtrl sendUnderBelt))
    {
        // 지하 벨트의 출력 연결 상태 확인
        if (sendUnderBelt.outObj.Count == 0)
            return;
    }
    else if (!outFactory.canTakeItem)
    {
        // 그 외 건물의 기본 수용 상태 확인
        return;
    }

    // ... 아이템 전달 처리
}
```

## 코드에서 확인할 수 있는 동작

- 서로 다른 건물 타입을 `List<Structure>` 하나로 관리할 수 있습니다.
- 모든 건물의 공통 상태는 `Structure`를 통해 바로 확인할 수 있습니다.
- 타입별 세부 기능은 캐싱된 컴포넌트를 통해 간단하게 조회할 수 있습니다.
- 반복적인 `GetComponent()` 대신 Dictionary에서 타입을 조회합니다.
- 프로젝트에서 정의한 부모 클래스 타입도 같은 컴포넌트에 연결합니다.

호출부는 구체 타입을 미리 고정하지 않고 캐시에 등록된 기능 타입을 확인할 수 있습니다.

## 확인한 내용

`Miner : Production : Structure` 상속 관계에서 실제 컴포넌트 타입과 부모 클래스 타입으로 각각 조회되는지 확인했습니다. 등록되지 않은 타입은 `Has<T>()`와 `TryGet<T>()`에서 `false`, `Get<T>()`에서 `null`을 반환해 실패 결과도 함께 확인했습니다.

Unity Profiler에서도 반복되던 `GetComponent()` 호출이 캐시 조회로 바뀌며 호출 수가 감소한 것을 확인했습니다. 당시 측정 기록이 남아 있지 않아 감소율이나 GC Alloc 수치는 적지 않았습니다.

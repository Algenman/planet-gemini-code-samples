# WorldObj Component Cache

Unity Netcode 기반 2인 협동 게임 프로젝트에서 사용한 상속 기반 컴포넌트 캐싱 예시입니다.

## 배경

프로젝트 규모가 커지면서 건물, 유닛, 몬스터 등 여러 오브젝트가 서로의 컴포넌트를 조회하는 경우가 많아졌습니다. 특히 주변 오브젝트를 탐색하거나 건물 타입에 따라 처리하는 과정에서 컴포넌트 조회와 분기 코드가 여러 클래스에 반복되었습니다.

이를 개선하기 위해 월드에서 동작하는 오브젝트가 `WorldObj`를 공통 부모로 사용하도록 구성했습니다. `WorldObj`는 생성 시점에 오브젝트의 컴포넌트와 사용자 정의 부모 타입을 캐싱하고, 이후 `Has<T>()`, `Get<T>()`, `TryGet<T>()`를 통해 조회할 수 있도록 합니다.

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

## 장점

- 서로 다른 건물 타입을 `List<Structure>` 하나로 관리할 수 있습니다.
- 모든 건물의 공통 상태는 `Structure`를 통해 바로 확인할 수 있습니다.
- 타입별 세부 기능은 캐싱된 컴포넌트를 통해 간단하게 조회할 수 있습니다.
- 반복적인 컴포넌트 조회와 명시적인 타입 변환을 줄였습니다.
- 새로운 건물 타입이 추가되어도 기존 컬렉션과 조회 구조를 재사용할 수 있습니다.

이 구조를 통해 호출부가 오브젝트의 구체 타입을 미리 알지 못해도, 해당 오브젝트가 제공하는 기능을 기준으로 처리할 수 있도록 개선했습니다.

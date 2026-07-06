# ServerTime 기반 벨트 아이템 동기화

공장 자동화 게임의 벨트 위에서 이동하는 아이템을 적은 네트워크 통신으로 일관되게 표현하기 위한 구조입니다.

## 문제

플레이 시간이 길어질수록 벨트와 벨트 위를 이동하는 아이템의 수가 계속 증가했습니다.

호스트와 클라이언트에서 아이템의 위치와 이동 상태를 동일하게 유지해야 했지만, 모든 아이템의 좌표를 지속적으로 RPC로 전달하면 아이템 수에 비례해 네트워크 트래픽이 증가하는 문제가 있었습니다.

반대로 각 클라이언트가 자신의 프레임 시간을 기준으로 위치를 계산하면 PC 성능과 프레임 차이로 인해 서로 다른 이동 결과가 발생했습니다.

이러한 차이가 누적되면서 다음과 같은 문제가 나타났습니다.

- 호스트와 클라이언트에서 아이템 위치가 조금씩 어긋남
- 클라이언트에서 아이템이 벨트 사이에 멈춘 것처럼 보임
- 벨트와 건물 사이의 아이템 전달 시점이 달라짐
- 아이템 순서와 입출력 결과가 서로 다르게 처리됨

## 기존 구조

초기에는 각각의 `BeltCtrl`이 자신의 `FixedUpdate()`에서 아이템을 이동시켰습니다.

```csharp
private void FixedUpdate()
{
    ItemMove();
}
```

각 벨트가 개별적으로 이동 시간을 계산했기 때문에 벨트 수가 증가할수록 실행되는 `FixedUpdate()`도 함께 증가했습니다. 또한 클라이언트마다 서로 다른 로컬 시간과 처리 시점을 사용하면서 아이템 위치에 차이가 발생했습니다.

## 개선 구조

각 벨트에서 실행하던 `FixedUpdate()`를 제거하고, 같은 벨트 그룹을 관리하는 `BeltGroupMgr`에서 이동 처리를 일괄 실행하도록 변경했습니다.

```csharp
private void FixedUpdate()
{
    double serverNow =
        NetworkManager.Singleton.ServerTime.Time;

    foreach (BeltCtrl belt in beltList)
    {
        if (belt.itemObjList.Count > 0)
            belt.ItemMove(serverNow);
    }
}
```

`BeltGroupMgr`는 한 번 조회한 `ServerTime`을 그룹 내 모든 벨트에 전달합니다. 이를 통해 같은 물리 프레임에서 처리되는 벨트들이 동일한 시간 값을 기준으로 아이템 위치를 계산하도록 구성했습니다.

## ServerTime 기반 위치 계산

아이템의 현재 좌표를 지속적으로 동기화하는 대신 다음 데이터를 사용해 이동 진행률을 계산합니다.

- 벨트 진입 시간
- 벨트 시작 위치
- 벨트 도착 위치
- 벨트 이동 시간
- 현재 ServerTime

```csharp
double elapsed =
    serverNow - item.beltEnterTime;

float progress = Mathf.Clamp01(
    (float)(
        elapsed /
        item.beltTravelDuration
    )
);

Vector3 newPosition = Vector3.Lerp(
    item.beltStartPos,
    item.beltEndPos,
    progress
);
```

이동 진행률은 다음과 같이 계산됩니다.

```text
이동 진행률 = (현재 ServerTime - 벨트 진입 시간)
              / 전체 이동 시간
```

호스트와 클라이언트는 아이템을 각자 이동시키지만, 동기화된 서버 시간을 기준으로 계산하기 때문에 프레임 차이로 인한 위치 오차를 줄일 수 있습니다.

## 벨트 그룹 단위 관리

아이템 이동 갱신을 개별 벨트가 아닌 벨트 그룹 단위로 관리하도록 변경했습니다.

```text
BeltGroupMgr
 ├─ BeltCtrl
 ├─ BeltCtrl
 ├─ BeltCtrl
 └─ BeltCtrl
```

그룹 관리자는 다음 역할을 담당합니다.

- 그룹 내 벨트의 유효 상태 확인
- 한 번 조회한 ServerTime 공유
- 아이템이 있는 벨트만 이동 처리
- 비어 있는 벨트의 정지 상태 초기화

아이템 생성 시 호스트가 부여한 고유 Index를 사용하고, 제거 및 순서 변경도 같은 Index를 기준으로 처리하여 호스트와 클라이언트의 아이템 순서가 달라지는 문제를 줄였습니다.

## 결과

- 아이템 좌표를 매 프레임 RPC로 전송하지 않도록 개선했습니다.
- 아이템 수 증가에 따른 위치 동기화 트래픽을 줄였습니다.
- 호스트와 클라이언트가 동일한 ServerTime을 기준으로 위치를 계산하도록 구성했습니다.
- 프레임 차이로 발생하던 아이템 위치 오차를 줄였습니다.
- 여러 벨트의 `FixedUpdate()`를 그룹 관리자 하나로 통합했습니다.
- 벨트와 건물 사이의 아이템 전달 시점을 더 일관되게 유지했습니다.
- 고유 Index를 기준으로 아이템의 순서와 제거 상태를 관리했습니다.

## 핵심 코드

```csharp
double serverNow =
    NetworkManager.Singleton.ServerTime.Time;

foreach (BeltCtrl belt in beltList)
{
    if (belt.itemObjList.Count > 0)
        belt.ItemMove(serverNow);
}
```

아이템의 좌표를 직접 동기화하는 대신, 동기화된 시간과 이동에 필요한 상태를 공유하고 각 클라이언트가 동일한 방식으로 위치를 계산하는 구조입니다.

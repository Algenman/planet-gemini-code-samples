# ServerTime 기반 벨트 아이템 동기화

벨트 아이템의 좌표를 계속 전송하지 않고, 진입 시간과 ServerTime으로 위치를 계산한 예시입니다.

## 문제

플레이 시간이 길어질수록 벨트와 벨트 위를 이동하는 아이템의 수가 계속 증가했습니다.

Host와 Client에서 아이템 이동을 함께 표현해야 했지만, 모든 아이템의 좌표를 지속적으로 RPC로 전달하면 아이템 수만큼 전송 항목이 늘어납니다.

반대로 각 Client가 자신의 프레임 시간을 기준으로 위치를 계산하면 프레임 차이로 인해 서로 다른 위치를 계산했습니다.

이러한 차이가 누적되면서 다음과 같은 문제가 나타났습니다.

- Host와 Client에서 아이템 위치가 조금씩 어긋남
- Client에서 아이템이 벨트 사이에 멈춘 것처럼 보임
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

각 벨트가 개별적으로 이동 시간을 계산했기 때문에 벨트 수가 증가할수록 실행되는 `FixedUpdate()`도 함께 증가했습니다. 또한 Client마다 서로 다른 로컬 시간과 처리 시점을 사용하면서 아이템 위치에 차이가 발생했습니다.

## 개선 구조

각 벨트에서 실행하던 `FixedUpdate()`를 제거하고, 같은 벨트 그룹을 관리하는 `BeltGroupMgr`에서 이동 처리를 실행하도록 바꿨습니다.

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

Host와 Client는 아이템을 각자 이동시키지만 같은 ServerTime을 입력값으로 사용합니다. 프레임 차이가 있는 환경에서 Local Time 방식보다 위치 차이가 줄어드는 것을 확인했지만, 완전히 같은 좌표를 보장하지는 않습니다.

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

원본 프로젝트에서는 Host가 부여한 고유 Index로 아이템 제거와 순서를 처리했습니다. 현재 공개된 `BeltCtrl`과 `BeltGroupMgr` 샘플에는 Index 생성·제거 코드가 포함되어 있지 않습니다.

## 확인한 내용

- 좌표 RPC 대신 진입 시간·시작 위치·도착 위치·이동 시간으로 진행률을 계산합니다.
- `BeltGroupMgr`가 한 번 조회한 ServerTime을 그룹의 `BeltCtrl`에 전달합니다.
- 프레임 차이가 있는 Host와 Client에서 Local Time 방식보다 위치 차이가 줄어드는 것을 확인했습니다.
- 벨트에서 건물로 전달된 뒤 양쪽의 아이템 처리 결과를 비교했습니다.

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

아이템 좌표를 직접 동기화하는 대신 ServerTime과 이동 상태로 각 환경에서 위치를 계산합니다.

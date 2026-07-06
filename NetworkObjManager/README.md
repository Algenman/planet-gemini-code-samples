# NetworkObjManager

게임 진행 중 접속한 신규 클라이언트의 네트워크 오브젝트 상태를 단계적으로 동기화하기 위한 관리자입니다.

## 문제

게임이 이미 진행 중인 상태에서 새로운 클라이언트가 접속하면 일부 오브젝트의 상태가 초기화되거나 적용되지 않는 문제가 발생했습니다.

각 `NetworkObject`의 생성 시점이 서로 달라 아직 생성되지 않은 오브젝트를 대상으로 상태 동기화 RPC가 먼저 전달되는 경우가 원인이었습니다. 개별 오브젝트의 `OnNetworkSpawn()`에서 동기화를 시도할 수도 있었지만, 모든 오브젝트가 준비된 시점을 통합적으로 판단하기 어려웠습니다.

또한 다수의 오브젝트 상태를 한 번에 동기화하면 신규 클라이언트에 처리 부하가 집중될 수 있어 전송량을 조절할 구조가 필요했습니다.

## 해결 방법

`NetworkObjManager`가 서버에 존재하는 주요 네트워크 오브젝트를 종류별로 관리하도록 구성했습니다.

신규 클라이언트가 접속하면 서버가 먼저 동기화 대상 오브젝트의 수를 전달합니다. 클라이언트는 해당 수만큼 로컬 오브젝트가 생성될 때까지 기다린 후 서버에 준비 완료 응답을 보냅니다.

```text
동기화 요청
    ↓
서버가 동기화 대상 수 전달
    ↓
클라이언트가 NetworkObject 생성 대기
    ↓
준비 완료 응답
    ↓
서버가 상태 동기화 시작
```

```csharp
private IEnumerator WaitForSyncCoroutine()
{
    yield return new WaitUntil(() =>
        netStructures.Count >= _syncTargetStructureCount &&
        netBeltGroupMgrs.Count >= _syncTargetBeltGroupCount &&
        networkBelts.Count >= _syncTargetBeltCount
    );

    NotifyReadyServerRpc();
}
```

## 배치 기반 흐름 제어

상태 동기화는 한 번에 모두 처리하지 않고 일정한 배치 단위로 나누었습니다.

```csharp
const int batchSize = 100;
const int maxInFlight = 5;
```

서버는 각 배치 경계 메시지를 클라이언트에 전달합니다. 클라이언트가 해당 메시지를 수신하면 서버에 확인 응답을 보내고, 서버는 아직 확인되지 않은 배치 수가 제한을 넘지 않도록 다음 전송 시점을 조절합니다.

```csharp
while (currentBatchId - _lastConfirmedBatchId > maxInFlight)
    yield return null;
```

현재 구현은 각 오브젝트 상태의 적용 결과를 개별적으로 검증하는 방식이 아니라, 배치 경계 수신 여부를 기준으로 동기화 전송량을 제어하는 방식입니다.

## 완료 처리

서버는 마지막 배치 경계의 확인 응답까지 받은 후 신규 클라이언트의 동기화 완료 상태를 변경하고 게임 참여를 진행합니다.

```csharp
while (_lastConfirmedBatchId < currentBatchId - 1)
    yield return null;
```

## 결과

- 상태 동기화 전에 필요한 `NetworkObject`가 먼저 생성되도록 순서를 분리했습니다.
- 대량의 오브젝트 상태 전송이 한 시점에 집중되는 것을 줄였습니다.
- 배치 경계 확인 응답을 기준으로 서버의 동기화 진행 속도를 제한했습니다.
- 모든 배치 확인 후 신규 클라이언트가 게임에 참여하도록 구성했습니다.
- 게임 진행 중 접속한 클라이언트에서 상태가 누락되는 문제를 줄였습니다.

## 적용 범위

이 프로젝트는 Host와 Client가 참여하는 2인 협동 게임을 기준으로 제작되었습니다. 따라서 `NetworkObjManager`는 한 번에 한 명의 신규 클라이언트를 순차적으로 동기화하는 구조입니다.

# UnitCommandSystem

플레이어 유닛의 그룹 이동·패트롤 명령과 몬스터의 상황별 AI 패턴을 구현한 코드 예시입니다.

이 샘플은 실제 프로젝트에서 사용한 코드 중 유닛 제어와 AI 상태 전환의 핵심 흐름만 정리했습니다. 경로 생성, 애니메이션, UI, 저장 데이터 등 프로젝트 의존 로직은 공개 범위에서 제외했습니다.

## 선택과 명령 전달

`UnitDrag`는 마우스 입력을 해석하여 단일 선택, 영역 선택, 화면 내 동일 종류 선택을 처리합니다. 선택 결과와 이동·공격 이동·패트롤·정지·집중 공격 명령은 이벤트로 전달합니다.

`UnitGroupCtrl`은 이 이벤트를 구독하여 현재 선택된 유닛 목록과 선택 표시를 관리하고, 입력된 명령을 선택된 모든 유닛의 ServerRpc로 분배합니다. 입력 처리와 개별 유닛 행동 로직을 직접 연결하지 않아 선택 방식이나 명령 종류가 추가되어도 역할을 나누어 관리할 수 있도록 구성했습니다.

```text
UnitDrag
 ├─ 단일 클릭 / 드래그 영역 / 동일 종류 선택
 └─ 이동 / 공격 이동 / 패트롤 / 정지 / 집중 공격 이벤트
          ↓
UnitGroupCtrl
 ├─ 선택 유닛 목록 및 선택 표시 관리
 └─ 선택된 유닛에 그룹 명령 분배
          ↓
UnitAi ServerRpc
```

```csharp
private void OnEnable()
{
    UnitDrag.UnitSelected += AddUnit;
    UnitDrag.MoveRequested += SetMoveTarget;
    UnitDrag.PatrolRequested += SetPatrolTarget;
    UnitDrag.HoldRequested += HoldUnits;
    UnitDrag.TargetRequested += SetTarget;
}
```

## 유닛 컨트롤

선택한 유닛을 `UnitGroupCtrl`에서 하나의 그룹으로 관리하고 이동, 공격 이동, 패트롤 명령을 각 유닛에 전달합니다. 명령과 실제 행동 판정은 서버를 기준으로 처리하여 멀티플레이 환경에서 동일한 결과를 유지하도록 구성했습니다.

### 간격을 고려한 그룹 이동

여러 유닛에게 같은 좌표를 지정하면 모든 유닛이 한 점에 모이려 하면서 서로 끼거나, 앞의 유닛에 가로막혀 이동 상태가 끝나지 않을 수 있습니다.

이를 줄이기 위해 유닛 수로 그룹의 도착 허용 반경을 계산하고, 각 유닛이 목표 지점의 일정 범위 안에 들어오면 이동을 완료하도록 구현했습니다. 모든 유닛을 한 좌표에 겹치게 하지 않으면서 자연스럽게 배치 간격을 확보할 수 있습니다.

```csharp
float totalDiameter = 0.7f * unitList.Count;
float groupRadius = totalDiameter / (2f * Mathf.PI);
float arrivalRadius = groupRadius / 2f;

foreach (UnitAi unit in unitList)
    unit.SetMoveCommandServerRpc(targetPosition, arrivalRadius, isAttackMove);
```

### 포메이션을 유지하는 패트롤

패트롤 명령에서는 선택된 유닛들의 중심 좌표를 구한 뒤, 각 유닛이 중심에서 떨어진 상대 위치를 저장합니다. 패트롤 목적지에 이 오프셋을 더해 각 유닛의 목적지를 따로 지정함으로써 이동 전의 배치 형태와 간격을 유지하도록 했습니다.

```csharp
Vector3 formationPosition = patrolPosition + unitOffsets[i];
unitList[i].SetPatrolCommandServerRpc(formationPosition);
```

## 몬스터 AI

몬스터는 하나의 행동에 고정하지 않고 현재 타깃, 공격 거리, 스폰 지점과의 거리, 웨이브 상태, 스포너 호출 여부에 따라 행동을 전환합니다.

- `Idle`: 대기 후 주변 순찰 위치 선택
- `Patrol`: 스폰 지점 주변 순찰
- `Trace`: 발견한 타깃 추적
- `Attack`: 공격 범위에 진입한 타깃 공격
- `ReturnToSpawn`: 타깃을 잃거나 추적 범위를 벗어나면 복귀
- `SpawnerCall`: 스포너가 지정한 방어 대상 또는 웨이브 목적지로 이동
- `Die`: 사망 처리 후 행동 중단

```csharp
if (!isWaveState && distanceFromSpawn > traceLimit)
{
    aiState = AIState.ReturnToSpawn;
}
else if (targetDistance <= attackDistance)
{
    aiState = AIState.Attack;
}
else
{
    aiState = AIState.Trace;
}
```

## 구조

```text
UnitDrag ── 선택 및 명령 이벤트 ──> UnitGroupCtrl ── 그룹 명령 ──> UnitAi

UnitCommonAi
 ├─ UnitAi: 이동 / 패트롤 / 공격 이동 / 정지 / 집중 공격
 └─ MonsterAi: 대기 / 순찰 / 추적 / 공격 / 복귀 / 스포너 호출
```

## 핵심 구현

- 선택 유닛 목록을 기준으로 그룹 명령 일괄 전달
- 단일 클릭, 드래그 영역, 화면 내 동일 종류 유닛 선택
- 이동, 공격 이동, 패트롤, 정지, 집중 공격 입력을 이벤트로 분리
- 유닛 수에 비례한 도착 허용 반경으로 유닛 간 겹침과 이동 정체 완화
- 그룹 중심과 상대 오프셋을 이용한 패트롤 포메이션 유지
- 서버 기준의 유닛 명령 및 AI 행동 판정
- 공통 행동 상태와 공격 상태를 분리한 상태 기반 제어
- 타깃 거리와 전투 상황에 따른 몬스터 행동 패턴 전환

## 결과

유닛이 같은 목적지에 과도하게 밀집해 서로 가로막는 상황을 줄였으며, 그룹 패트롤 시 기존 배치 간격을 유지할 수 있도록 개선했습니다.

또한 몬스터가 타깃 유무와 거리, 활동 범위, 웨이브 및 스포너 호출 상황에 따라 서로 다른 행동을 수행하도록 구성하여 전투 상황에 대응하는 AI 패턴을 구현했습니다.

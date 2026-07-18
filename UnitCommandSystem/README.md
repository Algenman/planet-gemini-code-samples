# UnitCommandSystem

플레이어 유닛의 그룹 이동·패트롤 명령과 몬스터의 상황별 AI 패턴을 구현한 코드 예시입니다.

이 샘플은 실제 프로젝트에서 사용한 코드 중 유닛 제어와 AI 상태 전환의 핵심 흐름만 정리했습니다. 경로 생성, 애니메이션, UI, 저장 데이터 등 프로젝트 의존 로직은 공개 범위에서 제외했습니다.

## 선택과 명령 전달

`UnitDrag`는 마우스 입력을 해석하여 단일 선택, 영역 선택, 화면 내 동일 종류 선택을 처리합니다. 선택 결과와 이동·공격 이동·패트롤·정지·지정한 대상 공격 명령은 이벤트로 전달합니다.

`UnitGroupCtrl`은 이 이벤트를 구독하여 현재 선택된 유닛 목록과 선택 표시를 관리하고, 입력된 명령을 선택된 모든 유닛의 ServerRpc로 분배합니다. 입력 처리와 개별 유닛 행동 로직을 직접 연결하지 않아 선택 방식이나 명령 종류가 추가되어도 역할을 나누어 관리할 수 있도록 구성했습니다.

![유닛 선택, 이동 및 패트롤](../docs/media/unit-control.gif)

```text
UnitDrag
 ├─ 단일 클릭 / 드래그 영역 / 동일 종류 선택
 └─ 이동 / 공격 이동 / 패트롤 / 정지 / 지정한 대상 공격 이벤트
          ↓
UnitGroupCtrl
 ├─ 선택 유닛 목록 및 선택 표시 관리
 ├─ 도착 반경 / 패트롤 상대 위치 계산
 └─ 선택된 유닛에 그룹 명령 분배
          ↓
UnitAi ServerRpc
 └─ 목적지 / 명령 정보 / 지정한 대상 참조 저장
          ↓
서버 FixedUpdate
 └─ Move / Patrol / Idle / Trace 상태 갱신
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

선택한 유닛을 `UnitGroupCtrl`에서 하나의 그룹으로 관리하고 이동, 공격 이동, 패트롤, 정지, 지정한 대상 공격 명령을 각 유닛에 전달합니다. Client 입력은 ServerRpc로 전달하고, `UnitAi`와 `UnitCommonAi`가 서버에서 명령 상태와 행동 분기를 갱신합니다.

### 간격을 고려한 그룹 이동

여러 유닛에게 같은 좌표를 지정하면 모든 유닛이 한 점에 모이려 하면서 서로 끼거나, 앞의 유닛에 가로막혀 이동 상태가 끝나지 않을 수 있습니다.

이를 줄이기 위해 유닛 수로 그룹의 도착 허용 반경을 계산하고, 각 유닛이 목표 지점의 일정 범위 안에 들어오면 이동을 완료하도록 처리했습니다. 조작 영상과 Host와 Client 테스트에서 선택·이동·패트롤 명령이 서버 처리 결과에 따라 반영되는 것을 확인했습니다.

```csharp
float totalDiameter = 0.7f * unitList.Count;
float groupRadius = totalDiameter / (2f * Mathf.PI);
float arrivalRadius = groupRadius / 2f;

foreach (UnitAi unit in unitList)
    unit.SetMoveCommandServerRpc(targetPosition, arrivalRadius, isAttackMove);
```

### 포메이션을 유지하는 패트롤

패트롤 명령에서는 선택된 유닛들의 중심 좌표를 구한 뒤, 각 유닛이 중심에서 떨어진 상대 위치를 저장합니다. 패트롤 목적지에 이 오프셋을 더해 각 유닛의 목적지를 따로 전달합니다. 공개 샘플에는 패트롤 왕복과 도착 처리 본문이 생략되어 있습니다.

```csharp
Vector3 formationPosition = patrolPosition + unitOffsets[i];
unitList[i].SetPatrolCommandServerRpc(formationPosition);
```

## 몬스터 AI

몬스터는 하나의 행동에 고정하지 않고 현재 타깃, 공격 거리, 스폰 지점과의 거리, 웨이브 상태, 스포너 호출 여부에 따라 행동을 전환합니다.

- `Idle`: 타깃이 없고 스폰 지점 기준 활동 범위 안에 있으면 대기
- `Patrol`: 스폰 지점 주변 순찰
- `Trace`: 발견한 타깃 추적
- `Attack`: 공격 범위에 진입한 타깃 공격
- `ReturnToSpawn`: 타깃을 잃었거나 활동 범위를 벗어나면 스폰 지점으로 복귀
- `SpawnerCall`: 스포너가 지정한 방어 대상 또는 웨이브 목적지로 이동
- `Die`: 사망 처리 후 행동 중단

![스포너에서 생성된 몬스터와 주변 유닛의 전투](../docs/media/monster-combat.gif)

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

### 몬스터 타깃 우선순위

몬스터는 주변 후보를 유닛·타워·플레이어·연구 건물과 그 외 오브젝트로 나누고, 서로 다른 거리 가중치를 적용합니다.

```text
유닛·타워·플레이어·연구 건물 후보 = 총 어그로
                                   - 거리 × 3
                                   - 건물의 공격 몬스터 수 × 3
                                   + 기존 타깃이면 3

그 외 후보 = 총 어그로 - 거리 × 5.5
```

```csharp
float score = candidate.aggro - candidate.distance * 3f;

if (structure)
    score -= structure.monsterTargetAmount * 3f;

if (bestTarget && candidate.obj == bestTarget)
    score += 3f;
```

추가 어그로는 `피해량 × 0.05 + 공격 속도 × 0.2`만큼 누적되며 최대 20으로 제한합니다. 감소 코루틴이 시작되면 4초마다 1씩 감소하고, 실행 중 다시 값이 추가되어도 감소 주기는 초기화하지 않습니다.

후보 점수는 일정 주기로 다시 계산합니다. 처음 선택한 대상이 이동하거나 멀어지면 변경된 거리를 반영하고, 기존 타깃에는 3점의 보너스를 부여해 타깃이 지나치게 자주 바뀌는 현상을 줄였습니다. 타깃이 변경되거나 사라지는 경우와 몬스터가 사망하는 경우에는 해당 건물을 공격 중인 몬스터 수를 갱신했습니다.

플레이 테스트에서는 웨이브 몬스터가 한 대상만 따라가지 않고 공격 대상을 나누는 것을 확인했습니다. 재선정 호출부와 타깃 소실·사망 정리 코드는 원본 프로젝트에 있으며, 공개 샘플에서는 생략했습니다.

## 구조

```text
UnitDrag ── 선택 및 명령 이벤트 ──> UnitGroupCtrl ── 그룹 명령 ──> UnitAi

UnitCommonAi
 ├─ UnitAi: 이동 / 패트롤 / 공격 이동 / 정지 / 지정한 대상 공격
 └─ MonsterAi: 상태 전환 / 어그로 기반 타깃 선정 / 공격 분산
```

## 핵심 구현

- 선택 유닛 목록을 기준으로 그룹 명령 일괄 전달
- 단일 클릭, 드래그 영역, 화면 내 동일 종류 유닛 선택
- 이동, 공격 이동, 패트롤, 정지, 지정한 대상 공격 입력을 이벤트로 분리
- 유닛 수에 비례한 도착 허용 반경으로 유닛 간 겹침과 이동 정체 완화
- 그룹 중심과 상대 오프셋을 이용한 패트롤 포메이션 유지
- 서버 기준의 유닛 명령 및 AI 행동 판정
- 공통 행동 상태와 공격 상태를 분리한 상태 기반 제어
- 타깃 거리와 전투 상황에 따른 몬스터 행동 패턴 전환
- 어그로, 거리, 대상별 공격 몬스터 수, 기존 타깃을 반영한 타깃 점수 계산
- 추가 어그로의 최대값 제한 및 시간 기반 감소

## 확인한 내용

단일 선택과 드래그 선택 후 이동·패트롤 명령이 선택 목록을 거쳐 각 유닛의 ServerRpc로 전달되는 것을 조작 영상에서 확인했습니다. 여러 유닛은 도착 허용 반경 안에 들어오면 `Move` 상태를 종료합니다.

몬스터는 타깃 유무와 거리, 활동 범위, 웨이브 및 스포너 호출 상태에 따라 AI 상태를 변경합니다.

웨이브 테스트에서는 거리와 어그로, 건물에 집중된 몬스터 수를 다시 계산하면서 공격 대상이 한곳에만 집중되지 않는 것을 확인했습니다.

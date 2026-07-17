# Planet Gemini Code Samples

실제 출시한 2인 협동 공장 자동화 게임 **Planet Gemini**에서 구현했던 일부 시스템을 포트폴리오용으로 정리한 코드 샘플 저장소입니다.

이 저장소의 코드는 실제 프로젝트 전체 코드가 아니라, 구현 의도와 구조를 설명하기 위해 일부 로직을 발췌하거나 예시 형태로 정리한 내용입니다.

일부 구현 방식은 Unity 공식 문서와 다양한 기술 자료를 참고했으며, 프로젝트 요구사항에 맞게 직접 수정 및 적용했습니다.

## 기능 영상

| Feature | Video |
| --- | --- |
| 유닛 선택, 이동 및 패트롤 | [▶ Unit Control](./docs/media/unit-control.mp4) |
| 스포너 크립과 주변 유닛 전투 | [▶ Monster Combat](./docs/media/monster-combat.mp4) |
| 건물 배치 및 건설 진행 | [▶ Building Construction](./docs/media/building-construction.mp4) |
| 드래그 기반 연속 벨트 설치 | [▶ Belt Placement](./docs/media/belt-placement.mp4) |

## 주요 샘플

| Sample | Topic | Description |
| --- | --- | --- |
| [WorldObj](./WorldObj/README.md) | 구조 개선 / 컴포넌트 캐싱 | 모든 게임 오브젝트의 공통 부모 클래스에서 컴포넌트를 캐싱하고, 반복적인 `GetComponent()` 호출과 분기 처리를 줄인 예시입니다. |
| [NetworkObjManager](./NetworkObjManager/README.md) | 신규 클라이언트 동기화 | 게임 진행 중 접속한 클라이언트가 누락 없이 상태 데이터를 적용할 수 있도록, 범위 단위 전송과 처리 완료 응답 흐름을 구성한 예시입니다. |
| [BeltItemSync](./BeltItemSync/README.md) | ServerTime 기반 위치 동기화 | 벨트 위 아이템 위치를 매번 RPC로 보내지 않고, 서버 시간을 기준으로 각 클라이언트가 동일한 이동 결과를 계산하도록 개선한 예시입니다. |
| [JsonBasedScienceDataManagement](./JsonBasedScienceDataManagement/README.md) | JSON 기반 데이터 관리 | 연구 트리 정보를 코드에 직접 작성하지 않고 JSON 데이터로 분리하여 관리한 예시입니다. |
| [SearchBatchProcessing](./SearchBatchProcessing/README.md) | 탐색 부하 분산 처리 | 유닛, 타워, 건물의 주변 오브젝트 탐색을 중앙 매니저에서 관리하고, 일정 개수 단위로 나누어 여러 프레임에 분산 처리한 예시입니다. |
| [UnitCommandSystem](./UnitCommandSystem/README.md) | 유닛 컨트롤 / 몬스터 AI | 그룹 이동과 포메이션 패트롤, 상황별 행동 및 어그로 기반 타깃 우선순위를 구현한 예시입니다. |
| [BuildingPlacementSystem](./BuildingPlacementSystem/README.md) | 건물 배치 / 건설 상태 | 비네트워크 미리보기, 배치 조건 검사, 서버 건물 생성, 연속 벨트 배치와 건설 완료 흐름을 정리한 예시입니다. |

## 대표적으로 보여주고 싶은 부분

이 저장소에서는 아래와 같은 구현 경험을 중심으로 정리했습니다.

- 멀티플레이 환경에서의 상태 동기화 흐름 설계
- 대량 오브젝트 처리 시 프레임 부하와 네트워크 트래픽을 줄이기 위한 구조 개선
- 반복적인 컴포넌트 조회와 예외 처리를 줄이기 위한 공통 부모 클래스 설계
- 데이터와 로직을 분리하기 위한 JSON 기반 관리 방식
- 실제 플레이 중 발생한 문제를 프로파일링과 구조 변경으로 개선한 경험
- 그룹 단위 유닛 명령과 상황별 몬스터 AI 패턴 구현
- 로컬 미리보기와 서버 검증을 분리한 멀티플레이 건물 배치 구현

## 공개 범위

상용 프로젝트 전체 소스가 아닌 포트폴리오용 샘플입니다.

프로젝트 의존성이 강한 코드, 에셋 참조, 기획 데이터, 불필요한 UI 연결부 등은 제외하고 각 주제의 핵심 흐름이 드러나도록 정리했습니다.

# JSON 기반 연구 데이터 관리

Planet Gemini의 건물, 유닛 및 업그레이드 연구 정보를 JSON으로 관리한 예시입니다.

각 연구 항목의 다음 정보를 데이터로 관리했습니다.

- 필요 아이템과 수량
- 요구 코어 레벨
- 연구 시간
- 기본 해금 여부
- 연구 설명
- UI 정렬 순서

## 데이터 처리 흐름

```text
ScienceInfo.json
    ↓
ScienceInfoGet
    ↓
ScienceInfoData
    ↓
코어 레벨별 데이터 조회
    ↓
sortIndex 기준 UI 생성
```

JSON은 다음 계층으로 구성했습니다.

```text
연구 대상 → 카테고리 → 연구 단계 → 상세 정보
```

```json
{
  "Belt": {
    "Build": {
      "2": {
        "items": ["IronPlate", "Cogwheel"],
        "amounts": [25, 25],
        "coreLv": 2,
        "time": 20,
        "basicScience": false,
        "sortIndex": 4
      }
    }
  }
}
```

`ScienceInfoGet`에서 JSON을 중첩 Dictionary로 역직렬화하고, 연구 대상과 단계 또는 코어 레벨을 기준으로 필요한 데이터를 조회합니다.

```text
Dictionary<string, ...>             연구 대상 이름
Dictionary<string, ...>             Build·Battle 카테고리
Dictionary<int, ScienceInfoData>     연구 단계와 상세 데이터
```

공개 샘플에서는 코어 레벨과 `sortIndex`를 기준으로 데이터를 조회하고 연구 아이콘을 생성하는 흐름을 확인할 수 있습니다. 비용과 해금 판정의 실제 사용 코드는 이 샘플에 포함되어 있지 않습니다.

## 예시 파일

```text
JsonBasedScienceDataManagement/
├─ ScienceInfo.json
├─ ScienceInfoData.cs
├─ ScienceInfoGet.cs
├─ ScienceCoreLvCtrl.cs
└─ README.md
```

- `ScienceInfo.json`: 실제 연구 데이터 중 일부를 발췌한 예시
- `ScienceInfoData.cs`: JSON 연구 항목의 데이터 모델
- `ScienceInfoGet.cs`: JSON 역직렬화와 연구 데이터 조회
- `ScienceCoreLvCtrl.cs`: 조회한 데이터를 이용한 연구 아이콘 생성

## 확인한 내용과 데이터 제약

밸런스 작업 중 `coreLv`, `time`, `basicScience`, `sortIndex` 값을 바꾸고 연구 UI에 반영되는 것을 확인했습니다.

새로운 연구 항목을 추가할 때 JSON만 수정하는 것은 아니며, 관련 인스턴스와 ScriptableObject 목록 등록이 필요합니다. `sortIndex`가 중복되면 `SortedDictionary.Add()`에서 오류가 발생하므로 데이터마다 고유한 값을 사용했습니다.

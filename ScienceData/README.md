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

조회한 데이터는 연구 비용 표시, 해금 조건 확인, 연구 시간 설정 및 연구 아이콘 정렬에 사용했습니다.

## 예시 파일

```text
ScienceData/
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

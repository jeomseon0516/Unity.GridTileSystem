# 변경 기록

이 문서는 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따릅니다.

## [0.1.1] - 2026-07-29

- Runtime·Editor·Tests·Samples 어셈블리의 `rootNamespace`와 소스 파일 위치를 namespace에 맞게 정리했습니다.

## [Unreleased]

### 변경

- Runtime, Editor, Tests, Samples 어셈블리의 `rootNamespace`를 실제 공개 namespace와 일치시켰습니다.
- `Hex` 중간 폴더를 제거하여 파일 위치와 `Jeomseon.HexGrid` namespace가 일치하도록 정리했습니다.
- 샘플 코드를 독립적인 Sample asmdef로 분리했습니다.

## [0.1.0] - 2026-07-29

### 추가

- 육각형 축 좌표 및 큐브 좌표 자료형
- 타일 상태와 상호작용 이벤트
- URP 데칼 기반 그리드 표시
- Scene View 타일 편집 도구
- 기본 사용 예제와 런타임 테스트

### 변경

- 패키지 이름을 `com.jeomseon.unity.grid-tile-system`으로 통일
- 표준 UPM 폴더 구조와 이름 기반 어셈블리 참조 적용
- 레거시 통합 저장소 의존성을 분리된 Jeomseon 패키지 의존성으로 교체
- 외부 SerializedDictionary 의존성을 직렬화 목록과 조회 캐시로 교체
- 레거시 마우스 컴포넌트 의존성을 Unity Input System 입력으로 교체
- 비공개 메서드 이름을 C# 명명 규칙에 맞게 정리

### 수정

- `HexCoordinates`에서 `Vector3` 변환 시 S 좌표 대신 R 좌표가 중복되던 문제


## [0.1.2] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.

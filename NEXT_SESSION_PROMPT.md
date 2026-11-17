Rappel VR 프로젝트를 이어서 진행할게. 프로젝트 정보, 일정은 CLAUDE.md에 있으니 먼저 읽어 줘. 기획서(Assets/02. Docs/Rappel — 컨셉 기획서.pdf)도 읽어 줘.
단, CLAUDE.md는 11/17 밤 기준이라 아래 내용이 더 최신이야.

## 작업 규칙
* 모든 답변은 한국어로, Unity 용어는 쉽게 풀어서 설명해 줘.
* 로직 구조는 내가 짠다. 설계안을 먼저 제시하지 말고, 내가 말한 구조대로 코드를 작성해 줘. 내 구조에 없는 세부 사항을 정해야 하면 구현한 뒤에 무엇을 정했는지 짧게 알려 줘. 내 구조를 바꾸게 되면 바뀐 점을 분명히 말해 줘.
* 기존 코드와 부딪히거나 버그가 될 부분이 보이면 사실만 짧게 알려 줘.
* 스크립트는 내가 요청할 때만 만들거나 수정해 줘. "시작하자" 같은 말은 코드 작성 허락이 아니야. 내가 "다음 할 일"을 물으면 할 일만 알려 줘.
* Unity가 보통 켜져 있으니 .unity 씬 파일이나 ProjectSettings를 직접 수정하지 마. 에디터 작업은 단계별로 안내만 해 줘.
* 씬 상태(위치, 스케일, 연결)를 말하기 전에 저장된 씬 파일(Assets/00. Scene/00. TestScene.unity)을 직접 확인해 줘. CLAUDE.md나 예전 대화만 보고 말하지 마.
* 내가 코드를 이해 못 하면 쉬운 말 + 예시(표, 머메이드)로 설명해 줘.
* 커밋은 내가 할게.

## 11/18(수) 00시 기준 진행 상황
### 끝난 것
* 11/17 완료 기준 통과: 로프 선(LineRenderer) + 8자고리 + 양손 제동 Link 테스트 완료
  - 원인이었던 것: PelvisCheck, Figure8Follow의 Descent 칸이 비어 있었음 → XR Origin 연결함
* CLAUDE.md 갱신함 (LineRenderer 로프, 스크립트 구조, 몸/IK 제외)
* 줄 끼워 고정 + 고정 햅틱 구현, Link에서 확인 완료 (HandBrake.cs에 들어감)
* 몸/IK 안 함. 손 메시(Assets/PolyOne/Free VR Hands)도 손목이 이상하게 틀어져서 포기 → 기본 컨트롤러 모델 그대로. 씬에서 손 오브젝트는 빠져 있음 (PolyOne 폴더는 프로젝트에 남아 있음, 미추적)

### 스크립트 변경 (Assets/01. Scripts/Rappel/)
* HandBrake.cs: "줄 끼워 고정" 추가 (canLock 켠 손만)
  - 8자고리(lockPoint) 반경 lockRadius 0.2 안 + 그립 ≥ lockGrip 0.9 + 트리거를 누른 순간(triggerPressPoint 0.5, 누른 프레임만) → 고정/해제 토글
  - 고정 중: 실제 그립 대신 Grip = 1, IsAtBrake = true → BrakeManager가 오른손 선택 → 곡선에서 0 m/s (완전 정지). BrakeManager, RappelDescent는 안 고침
  - 고정/해제 둘 다 Right Controller의 HapticImpulsePlayer로 진동 (0.8, 0.2초). IsLocked 프로퍼티 있음
* 씬 연결 (00:21 저장 기준)
  - Brakes/RightHand: Any Position 켬 (캘리브레이션만 골반, 그 뒤엔 어디서 쥐든 제동), Can Lock 켬, Trigger = XRI Right Interaction/Activate Value, Lock Point = Black Figure Eight, Haptic = Right Controller
  - Brakes/LeftHand: Any Position 끔, Brake Point = Black Figure Eight, 반경 0.2, Can Lock 끔
* 실행 순서: Figure8Follow(-100) → HandBrake(-60) → BrakeManager(-50) → RappelDescent(0) → RopeLine(LateUpdate)
* 값 흐름: 각 스크립트가 매 프레임 계산해서 자기 변수에 저장 → 다음 스크립트가 읽어 감 (HandBrake.Grip/IsAtBrake → BrakeManager.BrakeGrip → RappelDescent)

### 알아둘 사실
* 고정 중에는 HandBrake.Grip이 실제 그립이 아니라 1임. 나중에 Grip을 읽는 기능을 붙이면 주의 (속도 비례 햅틱/마찰음은 하강 속도를 읽으면 영향 없음)
* RopeLine은 고정 중에도 줄이 오른손을 따라감 (아직 안 고침)
* CLAUDE.md에 "손만 보여 줌", "아직 확인 안 된 것: 손 표시 방법"이 남아 있음 → 손도 안 하기로 했으니 다음 갱신 때 고칠 것
* 커밋 안 된 변경: 씬, BrakeManager.cs, HandBrake.cs, OpenXRPackageSettings.asset, CLAUDE.md, Assets/PolyOne/

## 다음 할 일 (수 11/18)
1. RopeLine 고정 중 모양: 고정 중 줄을 어떻게 그릴지 정하고 수정 (짧게)
2. 측면 경계: 고정 상태에서 몸을 옆으로 틀기 (밀리면 "몸을 돌릴 수 있음" 정도로 단순화)
3. 점프: A 버튼, 벽에 붙어 있고 고정이 아닐 때만. 진자처럼 돌아옴, 수평 이동 1~1.5m
4. 하강 속도 비례 햅틱과 줄 마찰음
5. 저녁 APK 실기기 확인 + 백업 (11/17에 미룬 것 포함: 로프 선, 8자고리, 양손 제동, 고정)

수요일 완료 기준: 하강 → 고정 → 점프를 이어서 플레이 가능. 점프가 제일 크고 위험함.

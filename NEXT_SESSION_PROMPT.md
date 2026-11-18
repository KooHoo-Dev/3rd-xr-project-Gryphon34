Rappel VR 프로젝트를 이어서 진행할게. 프로젝트 정보, 일정은 CLAUDE.md에 있으니 먼저 읽어 줘. 기획서(Assets/02. Docs/Rappel — 컨셉 기획서.pdf)도 읽어 줘.
단, CLAUDE.md는 11/18(수) 12:15 기준이라 아래 내용이 더 최신이야.

## 작업 규칙
* 모든 답변은 한국어로, Unity 용어는 쉽게 풀어서 설명해 줘.
* 로직 구조는 내가 짠다. 설계안을 먼저 제시하지 말고, 내가 말한 구조대로 코드를 작성해 줘. 내 구조에 없는 세부 사항을 정해야 하면 구현한 뒤에 무엇을 정했는지 짧게 알려 줘. 내 구조를 바꾸게 되면 바뀐 점을 분명히 말해 줘.
* 기존 코드와 부딪히거나 버그가 될 부분이 보이면 사실만 짧게 알려 줘.
* 스크립트는 내가 요청할 때만 만들거나 수정해 줘. "시작하자" 같은 말은 코드 작성 허락이 아니야. 내가 "다음 할 일"을 물으면 할 일만 알려 줘.
* Unity가 보통 켜져 있으니 .unity 씬 파일이나 ProjectSettings를 직접 수정하지 마. 에디터 작업은 단계별로 안내만 해 줘.
* (11/18 추가) UI처럼 에디터에서 손으로 만들기 번거로운 건, 내가 메뉴를 클릭하면 만들어지는 에디터 툴(Editor 폴더 스크립트)로 만들어 줘.
* 씬 상태(위치, 스케일, 연결)를 말하기 전에 저장된 씬 파일(Assets/00. Scene/*.unity)을 직접 확인해 줘. CLAUDE.md나 예전 대화만 보고 말하지 마.
* 내가 코드를 이해 못 하면 쉬운 말 + 예시(표, 머메이드)로 설명해 줘.
* 커밋은 내가 할게.

## 11/18(수) 13:50 기준 진행 상황
### 끝난 것
* Link 테스트 완료: 고정 + 고정 햅틱, 고정 중 줄 모양, 상태머신, 점프(A), 하강 햅틱, 캘리브레이션 햅틱, 점프 복귀 햅틱, Device 모드 높이 → 수요일 완료 기준(하강 → 고정 → 점프) 통과
* 측면 경계: 코드 없이 단순화로 확정 (고정한 뒤 플레이어가 직접 몸을 돌림)
* XR Origin: Tracking Origin Mode = Device, Camera Y Offset = 1.6 (앉든 서든 눈 높이 1.6, 플레이 자세로 벽 보고 Meta 버튼 길게 눌러 재설정 → 캘리브레이션)
* 공통 프리팹 RappelRig (Assets/00. Scene/RappelRig.prefab): XR Origin(안에 Brakes, Figure8, EndScreenCanvas) + RopeLine + RappelState + PelvisMarker. Cylinder(옥상 고정점)는 씬에 따로, RopeLine의 Anchor는 씬 연결
* 스테이지 씬 5개 만듦 (00. TestScene → 01. WalkScene으로 이름 바뀜)
* 범위 변경 (사용자 결정): 스테이지 5개 + 메뉴 씬 + 사격까지 지금 다 만듦
* 커밋 16f6b39 (11/18 13:44)까지 전부 커밋됨. 지금 커밋 안 된 변경 없음

### 스테이지 규칙 (사용자 결정)
| 씬 | StageType | 성공 | 실패 |
|---|---|---|---|
| 01. WalkScene | Descent | 천천히 내려와서 착지 | 하강 속도가 2 m/s를 넘는 순간 바로 |
| 02. JumpScene | Jump | 점프를 한 번이라도 하고 착지 | 점프 없이 착지 (내가 정함) |
| 03. SideScene | SideWatch | 표적을 다 맞히고 착지 | 안 맞히고 착지 |
| 04. WIndowScene | Window | 창문 진입 | 창문 놓치고 착지 |
| 05. MissionScene | Mission | 창문 진입 | 놓치고 착지 |
* 총: 고정 중에만 꺼냄. 고정 중엔 오른쪽 골반 = 제동이 아니라 총 꺼내기. 총을 넣으면 고정으로 돌아감
* 기본값으로 제안해 둔 것 (사용자 확인은 1번 수정만 받음, 나머지는 아직 "기본값으로" 확답 없음)
  - 총 꺼내기/넣기: 고정 중 오른손 골반 범위 + 그립 누른 순간 토글 (PelvisCheck의 isAtPelvis를 밖에서 읽게 열어서 재사용)
  - 사격: 총 든 상태에서 오른손 트리거 누른 순간 → 총구 방향 레이 → 맞은 표적 사라짐 + 총구 연기(PolygonHeist GunShot_Smoke_FX). 총 모델 SM_Wep_PistolSwat_01
  - 창문 진입: 유리 앞 트리거 박스 + 점프 복귀 순간 머리가 안에 있으면 진입 → 유리 깨기 → 방 안으로 1m 부드럽게 이동 → StageRule.NotifyWindowEntered()
  - 메뉴 씬: XRI 기본 XR Origin + 스테이지 버튼 5개

### 스크립트 (Assets/01. Scripts/Rappel/)
* RappelStateMachine.cs (RappelState 오브젝트)
  - 상태: Ready, OnWall, Locked, GunDrawn, Jumping, Finished
  - 요청 함수(허락되면 true): Calibrate(Ready→OnWall), ToggleLock(OnWall↔Locked), DrawGun(Locked→GunDrawn), HolsterGun(GunDrawn→Locked), StartJump(OnWall→Jumping), EndJump(Jumping→OnWall), Finish(bool success)(Ready/Finished 빼고 어디서나)
  - Succeeded, IsRopeLocked(Locked 또는 GunDrawn), StateChanged(이전, 새) C# 이벤트. 종료 땐 Succeeded가 먼저 정해진 뒤 이벤트
  - DrawGun/HolsterGun은 아직 부르는 곳 없음
* RappelDescent.cs (XR Origin): OnWall/Jumping일 때만 그립대로 하강, 그 외엔 속도 즉시 0. groundY(0)에 처음 닿는 순간 Landed(닿기 직전 속도) 이벤트, IsGrounded
* PelvisCheck.cs (XR Origin): Ready일 때 오른손 골반 + 그립 1 → Calibrate() → 성공하면 haptic(0.8, 0.2초). haptic = Right Controller
* HandBrake.cs (Brakes/RightHand, LeftHand): Grip, IsAtBrake. canLock 켠 손(오른손)은 8자고리 0.2m 안 + 그립 ≥ 0.9 + 트리거 누른 순간 → ToggleLock() → 허락되면 햅틱(0.8, 0.2초). isLocked와 "고정 중 그립 1"은 삭제됨
* BrakeManager.cs: 안 바뀜
* RappelJump.cs (XR Origin): StartJump() 허락되면 1.5초 동안 XR Origin -Z로 최대 1m 나갔다 돌아옴 (사인 반 바퀴, 수평만). 끝나면 EndJump()
  - 입력: A(XRI Right Locomotion/Jump), 또는 왼손 IsAtBrake일 때 X (스크립트 안에서 직접 만든 입력 <XRController>{LeftHand}/{PrimaryButton})
  - 점프 중 상태가 Jumping이 아니게 되면 그 자리에서 멈춤
* DescentHaptics.cs (Brakes): 하강 중(속도 ≥ 0.05) IsAtBrake인 손에 진동. 세기 0.1~0.5(속도 3에서 최대), 매번 ±30% 랜덤(roughness 0.3), 0.1초마다 0.15초. 점프→벽 복귀 순간 양손 1.0, 0.1초 (그동안 하강 진동 멈춤)
* RopeLine.cs: IsRopeLocked면 줄이 오른손 대신 Pin_Out에서 바로 아래로
* Figure8Follow.cs: 상태가 Ready에서 벗어나는 순간 한 번만 위치 맞춤
* StageRule.cs (씬마다 StageRule 오브젝트, 프리팹 밖): stage 종류, failSpeed 2. Descent는 매 프레임 속도 > 2면 Finish(false), 착지 땐 닿기 직전 속도로 다시 판정. NotifyAllTargetsHit(), NotifyWindowEntered() 열어 둠 (아직 부르는 곳 없음)
* EndScreen.cs (EndScreenCanvas): Finished가 되면 panel 켜고 "성공"/"실패". Retry()(지금 씬 buildIndex 다시 로드), GoToMenu()(씬 이름 "Menu")
* Editor/EndScreenBuilder.cs: 메뉴 Tools > Rappel > 종료 화면 만들기 → XR Origin 아래 월드 캔버스(0, 1.5, 0.7), 0.8×0.5m, NotoSansKR SDF 글꼴, 버튼 2개 연결, Event System(XRUIInputModule) 없으면 생성
* 실행 순서: Figure8Follow(-100) → HandBrake(-60) → BrakeManager(-50) → RappelDescent(0), RappelJump(0) → StageRule(5) → DescentHaptics(10) → RopeLine, Figure8Follow(LateUpdate)

### 씬 연결 (저장 기준: 01 13:23, 02/03 13:24, 04 13:28, 05 13:29)
* 5개 씬 모두: RappelRig 인스턴스 1개, Event System(XRUIInputModule) 있음, StageRule stage = 0~4 순서대로 맞음
* 04, 05에 Fragged Wall Glass(Assets/Fragmented Objects) 배치됨. 위치/크기/원래 유리 끄기는 확인 안 함
* 04 WIndowScene에는 01에 있는 SM_Env_Wall_Interior_Window_03 하나가 없음 (의도인지 확인 안 함)
* XR Origin 시작 위치는 5개 씬 모두 같음 (씬별로 아직 안 바꿈)
* Build Settings에는 01. WalkScene만 등록됨 (02~05, 메뉴 씬 없음)

### 알아둘 사실
* Fragmented Objects: FraggedController.FragAll()로 유리 전체를 한 번에 깨기 (Damage 방식은 부분 파손용). 조각 48개(Rigidbody + MeshCollider)
  - 머티리얼(Glass, Glass Inside)이 빌트인 Standard, 파티클(DirtyParticle)이 Particles/Standard Unlit → URP에서 분홍색. Frag Materials에 PolygonHeist_Material_01_A_Glass를 넣거나 Render Pipeline Converter로 변환 필요
  - 프리팹 크기(Wall_05x5)가 창문 유리와 달라 Scale 조절 필요
* EndScreen GoToMenu()는 메뉴 씬("Menu")을 만들어 Build Settings에 넣기 전엔 에러
* Retry()는 buildIndex로 다시 로드 → Build Settings에 없는 씬(02~05)에서는 동작 안 함
* 종료 화면 버튼의 레이저 클릭(Near-Far Interactor의 UI 상호작용)은 확인 안 함
* 왼손 X 점프는 확인 안 함 (RappelJump의 Left Hand 칸, Left Jump Action 바인딩)
* 하강 실패/성공 판정, 종료 화면 표시는 확인 안 함
* 사격 트리거와 고정 트리거: 총 든 상태(GunDrawn)에서는 ToggleLock이 거절되므로 트리거 = 사격으로 나눌 수 있음
* 줄 마찰음, 착지음, 총소리: 소리 파일이 프로젝트에 없음 (구할지 미정, 마찰음은 미룸)
* 튜토리얼식 조작 설명은 메모로만 (메모리 rappel-tutorial-plan)
* CLAUDE.md에서 낡은 부분: 씬 이름(00. TestScene), 스테이지 5개/메뉴 씬/StageRule/EndScreen/GunDrawn 없음, 일정과 자를 순서가 범위 변경 전 기준. 다음 갱신 때 고칠 것

## 다음 할 일 (수 11/18 오후 ~ 목 11/19 오전 13시 기능 동결)
1. 01. WalkScene에서 테스트: 그립 놓아서 2 m/s 넘기면 실패, 천천히 착지하면 성공, 다시 하기 버튼
2. 창문 진입 (04, 05): 구조 확정(기본값 확인) → 스크립트 작성(FragAll 포함) → 유리 머티리얼 URP 처리, 크기 맞추기
3. 총 꺼내기/넣기 + 사격 + 표적 (03, 05): 구조 확정 → 작성
4. 메뉴 씬 + Build Settings에 6개 씬 등록 (메뉴가 0번)
5. 씬별 XR Origin 시작 위치 (창문 침투 연습은 창문 근처)
6. 줄 마찰음 (소리 파일 정하기)
7. 저녁 APK 실기기 확인 + 백업 (11/17, 11/18 미확인분 포함)
8. CLAUDE.md 갱신
* 밀리면 CLAUDE.md "자를 순서": 표적 사격 → 비네트 → 벽 걷기 발소리. (스테이지 확장 이후 자를 순서는 아직 다시 정하지 않음)
* 오늘 완료 기준: 수요일 기준(하강 → 고정 → 점프)은 통과. 저녁 APK에 그때까지 된 스테이지를 담아 백업

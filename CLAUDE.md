Rappel VR 프로젝트를 이어서 진행하려고 해. 모든 답변과 제안은 한국어로 해 줘. Unity 용어는 쉽게 풀어서 설명해 줘.

## 마감
- 발표: 2026-11-20(금) 오전 9시, 학원. 시연 영상 + Quest 3 APK 빌드
- 목요일 오후 1시 기능 동결. 이후에는 버그 수정, 빌드, 영상만 함
- 범위: 아래 일정에 있는 기능만. 새 기능을 제안하지 말고, 일정이 밀리면 "자를 순서"를 기준으로 알려 줘
  - (11/18 변경) 표적 사격: 필수 기능(창문 침투, 착지, 시작/종료 화면)을 끝낸 뒤 남는 시간에만 넣음. 목 11시까지 안 되면 자름

## 프로젝트
- Meta Quest 3용 1인 VR 체험. 그립을 쥐는 강도로 하강 속도를 조절하며 건물 외벽을 레펠로 내려감.
- 기획서: Assets/02. Docs/Rappel — 컨셉 기획서.pdf (먼저 읽어 줘)
- Unity 6.3 (6000.3.15f1), OpenXR + XR Interaction Toolkit 3.5.1, Obi Rope 7.0.5

## 설계 원칙
- 플레이어 몸(XR Origin)은 스크립트로 직접 움직이고, 로프는 그걸 따라오는 시각용으로만 사용 (멀미 방지).
- 진행 단계는 상태머신(RappelStateMachine) 하나가 들고, 단계를 바꾸는 규칙도 거기에만 둠. 다른 스크립트는 State를 읽거나 요청 함수로 "바꿔 줘"라고만 함. 하강 속도, 그립 같은 "정도" 값은 각 스크립트가 들고 있음 (상태로 만들지 않음)
- 값은 한 방향으로 흐름: HandBrake → BrakeManager → RappelDescent → DescentHaptics. 거꾸로 읽는 참조를 만들지 않음
- 로프: (11/17 변경) Obi Rope(Pinhole 방식)는 튜닝이 어려워서 포기 → LineRenderer 직선으로 대체. Obi Solver 오브젝트는 지우지 않고 꺼 둠 (여유 있으면 "늘어진 끝부분만 Obi"로 올릴 수 있게)
  - Obi 메모: Pinhole의 Target에는 ObiCollider(+ Unity Collider, 트리거)가 필요. 구멍 위치는 Target 위치가 아니라 시작 순간 Position 지점의 로프 위치. 입자 간격이 핀 간격보다 넓으면 감기는 모양이 안 나옴
- 캐릭터: (11/17 결정) 몸/팔/발과 IK는 넣지 않음. (11/18) 손 메시(Assets/PolyOne/Free VR Hands)도 손목이 틀어져서 포기 → 기본 컨트롤러 모델 그대로. PolyOne 폴더는 프로젝트에 남아 있음
- 측면 경계: (11/18 확정) 코드 없음. 고정한 뒤 플레이어가 직접 몸을 돌리는 것으로 끝 (사격이 없으면 감지해도 달라질 게 없음). 시연 영상에서 장면으로 보여 줌

## 현재 상태 (11/18 낮 기준)
스크립트는 모두 Assets/01. Scripts/Rappel/, namespace Rappel.
실행 순서: Figure8Follow(-100) → HandBrake(-60) → BrakeManager(-50) → RappelDescent(0), RappelJump(0) → DescentHaptics(10) → RopeLine, Figure8Follow(LateUpdate)

- RappelStateMachine.cs (RappelState 오브젝트, 씬 맨 위)
  - 상태: Ready(준비) → OnWall(벽) ⇄ Locked(고정), OnWall ⇄ Jumping(점프), OnWall/Jumping → Finished(종료)
  - 요청 함수(허락되면 true): Calibrate, ToggleLock(벽↔고정), StartJump(벽→점프), EndJump(점프→벽), Finish(벽/점프→종료. 아직 부르는 곳 없음)
  - 바뀔 때 StateChanged(이전, 새) C# 이벤트. Inspector의 state 칸은 읽기용 (플레이 중 바꾸면 이벤트 없이 바뀜)
- RappelDescent.cs (XR Origin)
  - 상태가 OnWall/Jumping일 때만: BrakeManager.BrakeGrip → 곡선 gripToSpeed(0→3, 0.3→2, 0.5→0.8, 0.7 이상→0 m/s) → 가감속(speedUpRate 1.5, slowDownRate 3 m/s²) → XR Origin을 아래로 이동
  - Ready/Locked/Finished면 속도를 바로 0 (고정하면 즉시 정지)
  - 최대 3 m/s, groundY(0) 아래로는 안 내려감. stateMachine이나 brakeManager가 비어 있으면 하강 안 함
  - (11/16) 물리 공식(제동력/마찰) 방식은 속도가 너무 일정하고 그립이 예민해서 폐기
  - OnDisable에서 입력을 끄지 않음 (같은 입력을 다른 스크립트도 읽음, 켜고 끄기는 Input Action Manager가 함)
- PelvisCheck.cs (XR Origin)
  - 골반 위치 = 머리 + pelvisOffset(머리 Yaw 기준). 상태가 Ready이고 오른손이 반경 안 + 그립 1 → Calibrate() 요청 → 성공하면 오른손 햅틱(0.8, 0.2초)
  - 그립은 XRI Right Interaction/Select Value를 직접 읽음. pelvisMarker(구) 색은 하강 중에도 계속 바뀜 (안 초록/밖 하늘색)
- HandBrake.cs (Brakes/RightHand, Brakes/LeftHand에 하나씩): 한 손의 그립 값(Grip) + 제동 위치에 있는지(IsAtBrake)
  - anyPosition 켜면 항상 제동 위치. 아니면 controller와 brakePoint 거리 ≤ brakeRadius
  - RightHand: anyPosition 켬, canLock 켬. LeftHand: brakePoint = Black Figure Eight, radius 0.2, canLock 끔
  - 줄 끼워 고정(canLock 켠 손만): 8자고리 반경 0.2 안 + 그립 ≥ 0.9 + 트리거를 누른 순간 → ToggleLock() 요청 → 허락되면 햅틱(0.8, 0.2초). 점프 중에는 거절됨
  - (11/18) isLocked와 "고정 중 그립 1 바꿔치기"는 삭제. Grip은 항상 실제 그립 값
  - Left/Right Controller 오브젝트에 직접 붙이면 안 됨 (손 추적으로 바뀌면 컨트롤러 오브젝트가 꺼져서 값이 멈춤)
- BrakeManager.cs (Brakes): 제동 위치에 있는 손의 그립 값을 BrakeGrip으로 내보냄. 둘 다면 더 세게 쥔 손, 같으면 지금 손 유지. 둘 다 아니면 0
- RappelJump.cs (XR Origin): StartJump() 허락되면 1.5초 동안 벽 반대쪽(XR Origin -Z)으로 최대 1m 나갔다 돌아옴 (사인 반 바퀴, 한 번 왕복). 수평만 움직이고 높이는 RappelDescent가 그립대로 처리
  - 입력: A 버튼(XRI Right Locomotion/Jump), 또는 왼손이 제동 위치(8자고리)일 때 X 버튼 (XRI에 왼손 Jump가 없어서 스크립트에서 직접 만든 입력)
  - 점프 중에 상태가 Jumping이 아니게 되면(예: 창문 진입) 그 자리에서 멈춤
- DescentHaptics.cs (Brakes): 하강 중(속도 ≥ 0.05) 제동 위치에 있는 손에 진동. 세기 0.1~0.5 (속도 비례), 매번 ±30% 랜덤. 0.1초마다 0.15초 진동을 다시 보냄
  - 점프 → 벽 복귀 순간(StateChanged 이벤트) 양손에 1.0, 0.1초. 그동안 하강 진동은 안 보냄
- RopeLine.cs (RopeLine, LineRenderer): Cylinder(옥상 고정점) → Pin 4개 → 오른손 → 그 아래 groundY까지 직선. 상태가 Locked면 오른손 대신 Pin_Out에서 바로 아래로 떨어짐
- Figure8Follow.cs (Figure8, XR Origin 자식): 상태가 Ready에서 벗어나는 순간 한 번만 위치 = 머리 + (x, y, z) 오프셋(XR Origin 축 기준). 그 뒤로 머리를 안 따라감. 씬 값 y -0.8, z 0.5
- 8자고리: Figure8 아래 Black Figure Eight 모델 + 로프가 지나갈 점 Pin_In / Pin_NeckL / Pin_NeckR / Pin_Out
- 씬: Building 스케일 1, 옥상에 Cylinder(로프 고정점), XR Origin 정면(+Z)이 건물 쪽, XR Origin 시작 Y 16.09
- XR Origin: (11/18) Tracking Origin Mode = Device, Camera Y Offset = 1.6 → 앉든 서든 눈 높이 1.6. 플레이할 자세로 벽을 보고 Meta 버튼 길게 눌러 재설정 → 골반 캘리브레이션 순서로 시작
- XR Origin의 Locomotion 오브젝트는 비활성화 (Gravity가 리그를 90 m/s까지 떨어뜨렸음). 입력(Jump 등)은 Input Action Manager가 켜서 그대로 읽힘
- 테스트 씬: Assets/00. Scene/00. TestScene.unity (XRI Starter Assets의 XR Origin (XR Rig) 프리팹 사용, Input Action Manager는 프리팹 안에 있음)
- 설정: Active Input Handling = Both (Obi 샘플용, 유지), Quest Link 테스트할 때 XR Interaction Simulator는 끔
- Quest Link: Unity의 OpenXR → PC 탭 → Play Mode OpenXR Runtime을 Oculus로 지정해서 사용
- 빌드: 11/17 Android 첫 APK 빌드 + Quest 실기기 테스트 완료 (빌드 씬 = 00. TestScene)
  - "Active Input Handling = Both" 경고는 Ignore로 진행 중. 실기기 입력 문제가 생기면 New로 전환 검토 (영향은 Obi 샘플 씬뿐)
  - Android 품질 = Mobile → Mobile_Renderer (SSAO 없음). PC_Renderer의 SSAO가 Project Validation 경고 원인
- Link 테스트 완료 (11/18): 고정 + 고정 햅틱, 상태머신, 점프(A), 하강 햅틱, 캘리브레이션 햅틱, 점프 복귀 햅틱, Device 모드

## 아직 확인 안 된 것
- 왼손 X 점프 (Left Hand 칸 연결, Left Jump Action 바인딩 확인 필요)
- APK 실기기: 11/17 것(로프 선, 8자고리, 양손 제동) + 11/18 것(고정, 고정 중 줄 모양, 점프, 햅틱, Device 모드 높이)
- 착지 높이: 바닥 메쉬 윗면이 groundY(0)와 맞는지

## 일정 (매일 저녁 동작하는 APK를 백업으로 남김)

### 월 11/16 — 하강 감각 + 첫 빌드 (완료)
1. 컴파일 확인, XR Origin 설정, Quest Link로 하강/제동 수치 조정
2. 골반 캘리브레이션 → 하강 가능 상태
3. 빌드 설정 → Quest에 첫 APK 설치

### 화 11/17 — 씬 + 로프 + 두 손 제동 (완료)
4. 벽, 창문, 바닥, 지붕 앵커로 씬 구성
5. 로프 연결 (Obi 포기 → LineRenderer)
6. 8자고리 제동(왼손) + 두 손 규칙

### 수 11/18 — 고정 + 점프 + 피드백
7. (완료) 줄 끼워 고정 + 고정 햅틱, 고정 중 줄 모양
8. (완료) 측면 경계: 코드 없이 단순화로 확정
9. (완료) 상태머신 도입, 점프(A / 왼손 X), 하강 속도 비례 햅틱, 캘리브레이션 햅틱, 점프 복귀 햅틱
10. 줄 마찰음 (소리 파일 구할지 정하는 중이라 미룸)
11. (목에서 당겨 옴) 창문 침투: 점프 후 벽으로 돌아올 때 창문 높이면 진입
12. (목에서 당겨 옴) 바닥 착지 + 착지음, 시작/종료 화면 (버튼과 텍스트만)
13. 저녁 APK 실기기 확인 + 백업
- 완료 기준: 하강 → 고정 → 점프를 이어서 플레이 가능 (Link에서 통과)

### 목 11/19 오전 — 여유 기능 (오후 1시 기능 동결)
14. 비네트(XRI Tunneling Vignette), 벽 걷기 발소리
15. 표적 사격 (11시까지 안 되면 자름)
- 메모: 튜토리얼식 조작 설명(캘리브레이션, 그립 하강, 왼손 제동, 고정, 점프)을 넣고 싶음. 시작 화면 텍스트로 넣을지, 단계별 튜토리얼로 할지 시작 화면 만들 때 정함

### 목 11/19 오후 — 빌드 + 영상
16. 버그 수정 → 최종 APK
17. 시연 영상 녹화(Quest 기본 녹화 또는 scrcpy, 여러 테이크) → 편집

### 금 11/20 ~9시 — 확인만
18. Quest 충전, APK 실행 확인, 영상 재생 확인. 최종 APK, 전날 APK, 영상 USB 백업

## 밀리면 자를 순서
표적 사격 → 비네트 → 벽 걷기 발소리
고정, 8자고리, 창문 침투, 점프, 착지, 시작/종료 화면은 지킴.

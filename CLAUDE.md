Rappel VR 프로젝트를 이어서 진행하려고 해. 모든 답변과 제안은 한국어로 해 줘. Unity 용어는 쉽게 풀어서 설명해 줘.

## 마감
- 발표: 2026-11-20(금) 오전 9시, 학원. 시연 영상 + Quest 3 APK 빌드
- 목요일 오후 1시 기능 동결. 이후에는 버그 수정, 빌드, 영상만 함
- 범위: 아래 일정에 있는 기능만. 표적 사격은 하지 않음. 새 기능을 제안하지 말고, 일정이 밀리면 "자를 순서"를 기준으로 알려 줘

## 프로젝트
- Meta Quest 3용 1인 VR 체험. 그립을 쥐는 강도로 하강 속도를 조절하며 건물 외벽을 레펠로 내려감.
- 기획서: Assets/02. Docs/Rappel — 컨셉 기획서.pdf (먼저 읽어 줘)
- Unity 6.3 (6000.3.15f1), OpenXR + XR Interaction Toolkit 3.5.1, Obi Rope 7.0.5

## 설계 원칙
- 플레이어 몸(XR Origin)은 스크립트로 직접 움직이고, 로프는 그걸 따라오는 시각용으로만 사용 (멀미 방지).
- 로프: (11/17 변경) Obi Rope(Pinhole 방식)는 튜닝이 어려워서 포기 → LineRenderer 직선으로 대체. Obi Solver 오브젝트는 지우지 않고 꺼 둠 (여유 있으면 "늘어진 끝부분만 Obi"로 올릴 수 있게)
  - Obi 메모: Pinhole의 Target에는 ObiCollider(+ Unity Collider, 트리거)가 필요. 구멍 위치는 Target 위치가 아니라 시작 순간 Position 지점의 로프 위치. 입자 간격이 핀 간격보다 넓으면 감기는 모양이 안 나옴
- 캐릭터: (11/17 결정) 몸/팔/발과 IK(Final IK VRIK)는 넣지 않음. 손만 보여 줌

## 현재 상태 (11/17 밤 기준)
스크립트는 모두 Assets/01. Scripts/Rappel/, namespace Rappel.
실행 순서: Figure8Follow(-100) → HandBrake(-60) → BrakeManager(-50) → RappelDescent(0) → RopeLine(LateUpdate)

- RappelDescent.cs (XR Origin)
  - canDescend가 true면 BrakeManager.BrakeGrip → 곡선 gripToSpeed(0→3, 0.3→2, 0.5→0.8, 0.7 이상→0 m/s) → 가감속(speedUpRate 1.5, slowDownRate 3 m/s²) → XR Origin을 아래로 이동
  - 최대 3 m/s, groundY 아래로는 안 내려감. brakeManager가 비어 있으면 하강 안 함
  - (11/16) 물리 공식(제동력/마찰) 방식은 속도가 너무 일정하고 그립이 예민해서 폐기
  - OnDisable에서 입력을 끄지 않음 (같은 입력을 다른 스크립트도 읽음, 켜고 끄기는 Input Action Manager가 함)
- PelvisCheck.cs (XR Origin)
  - 골반 위치 = 머리 + pelvisOffset(머리 Yaw 기준). 오른손이 반경 안 + 그립 1 → descent.CanDescend = true (캘리브레이션, 한 번 true면 유지)
  - 그립은 XRI Right Interaction/Select Value를 직접 읽음. pelvisMarker(구) 색은 하강 중에도 계속 바뀜 (안 초록/밖 하늘색)
  - head, rightController, descent 중 하나라도 비어 있으면 아무것도 안 함
- HandBrake.cs (Brakes/RightHand, Brakes/LeftHand에 하나씩): 한 손의 그립 값 + 제동 위치에 있는지
  - anyPosition 켜면 항상 제동 위치. 아니면 controller와 brakePoint 거리 ≤ brakeRadius
  - RightHand: anyPosition 켬 (캘리브레이션 뒤에는 어디서 쥐든 제동 — 의도한 것)
  - LeftHand: controller = Left Controller, brakePoint = Black Figure Eight 모델, radius 0.2
  - Left/Right Controller 오브젝트에 직접 붙이면 안 됨 (손 추적으로 바뀌면 컨트롤러 오브젝트가 꺼져서 값이 멈춤)
- BrakeManager.cs (Brakes): 제동 위치에 있는 손의 그립 값을 BrakeGrip으로 내보냄. 둘 다면 더 세게 쥔 손, 같으면 지금 손 유지. 둘 다 아니면 0. ActiveHand(None/Right/Left) 표시
- RopeLine.cs (RopeLine, LineRenderer): Cylinder(옥상 고정점) → Pin 4개 → 오른손 → 오른손 바로 아래 groundY까지 직선. groundY는 RappelDescent와 같은 값으로 맞춰야 함
- Figure8Follow.cs (Figure8, XR Origin 자식): 캘리브레이션 순간에 한 번만 위치 = 머리 + (x, y, z) 오프셋(XR Origin 축 기준). 그 뒤로 머리를 안 따라감. 씬 값 y -0.8, z 0.5
  - 캘리브레이션은 플레이할 자세(서서) 그대로 해야 8자고리 위치가 맞음
- 8자고리: Figure8 아래 Black Figure Eight 모델 + 로프가 지나갈 점 Pin_In / Pin_NeckL / Pin_NeckR / Pin_Out
- 씬: Building 스케일 1로 정리, 옥상에 Cylinder(로프 고정점), XR Origin 정면(+Z)이 건물 쪽
- 11/17: 하강 수치, 골반 캘리브레이션, 양손 제동 Link 테스트 완료 (화요일 완료 기준 통과)
- XR Origin의 Locomotion 오브젝트는 비활성화 (Gravity가 리그를 90 m/s까지 떨어뜨렸음, Jump/Turn도 불필요)
- 테스트 씬: Assets/00. Scene/00. TestScene.unity (XRI Starter Assets의 XR Origin (XR Rig) 프리팹 사용, Input Action Manager는 프리팹 안에 있음)
- 설정: Active Input Handling = Both (Obi 샘플용, 유지), Quest Link 테스트할 때 XR Interaction Simulator는 끔
- Quest Link: Unity의 OpenXR → PC 탭 → Play Mode OpenXR Runtime을 Oculus로 지정해서 사용
- 빌드: 11/17 Android 첫 APK 빌드 + Quest 실기기 테스트 완료 (빌드 씬 = 00. TestScene)
  - "Active Input Handling = Both" 경고는 Ignore로 진행 중. 실기기 입력 문제가 생기면 New로 전환 검토 (영향은 Obi 샘플 씬뿐)
  - Android 품질 = Mobile → Mobile_Renderer (SSAO 없음). PC_Renderer의 SSAO가 Project Validation 경고 원인

## 아직 확인 안 된 것
- 로프 선 + 8자고리 + 양손 제동이 들어간 APK 실기기 확인 (11/17 저녁 예정)
- 손 표시 방법 (프로젝트에 손 모델 없음. com.unity.xr.hands 1.8.1 패키지는 설치됨)

## 일정 (매일 저녁 동작하는 APK를 백업으로 남김)

### 월 11/16 — 하강 감각 + 첫 빌드
1. 컴파일 확인, XR Origin 설정(Y=10, Floor, 정면이 벽)
2. Quest Link로 하강/제동 느낌 테스트하고 수치 조정
3. 골반 캘리브레이션: 오른손을 골반에 두고 그립 → 골반 위치 저장 → 하강 가능 상태 (그 전에는 떨어지지 않음)
4. 빌드 설정: TestScene 등록, Android 플랫폼, 샘플 씬 제외 → Quest에 첫 APK 설치
- 완료 기준: 기기에서 캘리브레이션 후 그립으로 하강/정지가 됨

### 화 11/17 — 씬 + 로프 + 두 손 제동 (캐릭터 IK는 11/17에 제외, 손만)
5. 벽, 창문, 바닥, 지붕 앵커로 씬 구성
6. 로프 연결 (Obi 포기 → LineRenderer로 대체)
7. 8자고리 제동(왼손) + 두 손 규칙: 둘 다 제동 위치면 더 세게 쥔 쪽 값, 먼저 놓으면 남은 손이 이어받음
8. 저녁 APK로 Quest에서 로프 선 + 8자고리 + 양손 제동 확인
- 완료 기준: 로프가 보이고, 양손으로 제동하며 내려감

### 수 11/18 — 고정 + 점프 + 피드백 (가장 위험한 날)
9. 줄 끼워 고정: 오른손 그립 + 8자고리에 갖다 대기 → 손을 놓아도 정지 유지 (+ 고정 성공 햅틱)
10. 측면 경계: 고정 상태에서 몸을 옆으로 틀기
11. 점프: A 버튼, 벽에 붙어 있고 고정이 아닐 때만. 진자처럼 돌아옴, 수평 이동 1~1.5m
12. 하강 속도에 비례하는 햅틱과 줄 마찰음
- 완료 기준: 하강 → 고정 → 점프를 이어서 플레이 가능

### 목 11/19 오전 — 마무리 기능 (오후 1시 기능 동결)
13. 창문 침투: 점프 후 벽으로 돌아올 때 창문 높이면 진입
14. 바닥 착지 + 착지음, 시작/종료 화면 (버튼과 텍스트만)
15. 비네트(XRI Tunneling Vignette), 벽 걷기 발소리

### 목 11/19 오후 — 빌드 + 영상
16. 버그 수정 → 최종 APK
17. 시연 영상 녹화(Quest 기본 녹화 또는 scrcpy, 여러 테이크) → 편집

### 금 11/20 ~9시 — 확인만
18. Quest 충전, APK 실행 확인, 영상 재생 확인. 최종 APK, 전날 APK, 영상 USB 백업

## 밀리면 자를 순서
측면 경계 단순화("고정 상태에서 몸을 돌릴 수 있음" 정도) → 비네트 → 벽 걷기 발소리
고정, 8자고리, 창문 침투, 점프, 시작/종료 화면은 지킴.

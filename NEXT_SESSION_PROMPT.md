Rappel VR 프로젝트를 이어서 진행할게. 프로젝트 정보, 일정은 CLAUDE.md에 있으니 먼저 읽어 줘. 기획서(Assets/02. Docs/Rappel — 컨셉 기획서.pdf)도 읽어 줘.
단, CLAUDE.md의 "현재 상태"와 "Obi 구성 계획"은 11/17 오전 기준이라 아래 내용이 더 최신이야.

## 작업 규칙
* 모든 답변은 한국어로, Unity 용어는 쉽게 풀어서 설명해 줘.
* 로직 구조는 내가 짠다. 설계안을 먼저 제시하지 말고, 내가 말한 구조대로 코드를 작성해 줘. 내 구조에 없는 세부 사항을 정해야 하면 구현한 뒤에 무엇을 정했는지 짧게 알려 줘. 내 구조를 바꾸게 되면 바뀐 점을 분명히 말해 줘.
* 기존 코드와 부딪히거나 버그가 될 부분이 보이면 사실만 짧게 알려 줘.
* 스크립트는 내가 요청할 때만 만들거나 수정해 줘. "시작하자" 같은 말은 코드 작성 허락이 아니야. 내가 "다음 할 일"을 물으면 할 일만 알려 줘.
* Unity가 보통 켜져 있으니 .unity 씬 파일이나 ProjectSettings를 직접 수정하지 마. 에디터 작업은 단계별로 안내만 해 줘.
* 씬 상태(위치, 스케일, 연결)를 말하기 전에 저장된 씬 파일(Assets/00. Scene/00. TestScene.unity)을 직접 확인해 줘. CLAUDE.md나 예전 대화만 보고 말하지 마.
* 커밋은 내가 할게.

## 11/17(화) 진행 상황
### 끝난 것
* 씬 구성: Building 스케일 1로 정리, 옥상에 Cylinder(로프 고정점), XR Origin 정면(+Z)이 건물 쪽
* 스크립트 폴더가 Assets/01. Scripts/Rappel/, 기획서는 Assets/02. Docs/로 옮겨짐 (CLAUDE.md 경로는 수정함)
* 로프: Obi Rope(Pinhole 방식)는 튜닝이 어려워서 포기 → LineRenderer로 대체 (A안). Obi Solver 오브젝트는 지우지 않고 꺼 둠 (나중에 여유 있으면 "늘어진 끝부분만 Obi"로 올릴 수 있게)
* 8자고리: XR Origin 아래 Figure8 오브젝트 + Black Figure Eight 모델 + 로프가 지나갈 점 Pin_In / Pin_NeckL / Pin_NeckR / Pin_Out
* 두 손 제동 스크립트 작성 + 씬에 연결함 (13:49 저장 기준). **VR 테스트는 아직 안 함**

### 스크립트 (Assets/01. Scripts/Rappel/)
* RappelDescent.cs (XR Origin): canDescend가 true면 BrakeManager.BrakeGrip → gripToSpeed 곡선 → 가감속 → XR Origin을 아래로 이동. brakeManager가 비어 있으면 하강 안 함. OnDisable에서 입력을 끄는 코드는 지움 (같은 입력을 다른 스크립트도 읽음, 켜고 끄기는 Input Action Manager가 함)
* PelvisCheck.cs (XR Origin): 골반 위치 = 머리 + pelvisOffset(머리 Yaw 기준). 오른손이 반경 안 + 그립 1 → CanDescend = true (캘리브레이션, 한 번). 골반 구 색(안 초록/밖 하늘색)은 하강 중에도 계속 바뀜. 캘리브레이션은 오른손 그립 입력을 직접 읽음
* HandBrake.cs (Brakes/RightBrake, Brakes/LeftBrake에 하나씩): 한 손의 그립 값 + 제동 위치에 있는지. anyPosition 켜면 위치 상관없이 항상 제동 위치. 아니면 controller와 brakePoint 거리 ≤ brakeRadius
  - 오른손: anyPosition 켬 (캘리브레이션 뒤에는 어디서 쥐든 제동 — 의도한 것)
  - 왼손: controller = Left Controller, brakePoint = Black Figure Eight 모델, radius 0.2
* BrakeManager.cs (Brakes): 제동 위치에 있는 손의 그립 값을 BrakeGrip으로 내보냄. 둘 다 제동 위치면 더 세게 쥔 손, 값이 같으면 지금 손 유지. 둘 다 아니면 0. ActiveHand(None/Right/Left) 표시
* RopeLine.cs (RopeLine 오브젝트, LineRenderer): Cylinder → Pin 4개 → 오른손(Right Controller) → 오른손 바로 아래 groundY까지 전부 직선. LateUpdate. groundY는 RappelDescent와 따로 있으니 같은 값으로 맞춰야 함
* Figure8Follow.cs (Figure8): 캘리브레이션 순간(CanDescend가 true가 되는 프레임)에 한 번만 Figure8 위치 = 머리 위치 + (xOffset, yOffset, zOffset), XR Origin 축 기준. 그 뒤로 머리를 따라가지 않음 (고개 숙이거나 돌려도 그대로). 씬 값: y -0.8, z 0.5 (모델이 Figure8 기준 (0, 0.653, -0.2)에 있어서)
* 실행 순서: Figure8Follow(-100) → HandBrake(-60) → BrakeManager(-50) → RappelDescent(0) → RopeLine(LateUpdate)

### 알아둘 사실
* HandBrake를 Left/Right Controller 오브젝트에 직접 붙이면 안 됨 (손 추적으로 바뀌면 컨트롤러 오브젝트가 꺼져서 값이 멈춤) → 그래서 Brakes 아래 빈 오브젝트에 붙임
* 캘리브레이션은 플레이할 자세(서서/앉아서) 그대로 해야 8자고리 위치가 맞음
* Obi 메모 (나중에 Obi로 돌아갈 때): Pinhole의 Target에는 ObiCollider(+ Unity Collider, 트리거로)가 필요. 구멍 위치는 Target 위치가 아니라 시작 순간 Position 지점의 로프 위치. 입자 간격이 핀 간격보다 넓으면 감기는 모양이 안 나옴

## 다음 할 일
1. Quest Link로 양손 제동 테스트: 왼손을 8자고리에 대고 그립 → 제동, 두 손 다 쥐면 더 센 쪽, 한 손 놓으면 남은 손이 이어받는지 (BrakeManager의 Active Hand / Brake Grip 디버그 칸으로 확인)
2. (오후, 완료 기준 밖이라 밀리면 미룸) 캐릭터 IK: Final IK VRIK 검토, 캐릭터는 XR Origin의 자식
3. 저녁 APK: 로프 선 + 8자고리 + 양손 제동 실기기 확인, 백업 APK
4. CLAUDE.md "현재 상태" / 설계 원칙의 Obi 부분을 위 내용으로 갱신 (내가 요청하면)
5. 수 11/18: 줄 끼워 고정(오른손 그립 + 8자고리에 갖다 대기 → 손 놓아도 정지 + 햅틱), 측면 경계, 점프(A 버튼), 하강 속도 비례 햅틱/마찰음

화요일 완료 기준: 로프가 보이고, 양손으로 제동하며 내려감 → 1번 테스트가 통과하면 완료.

Rappel VR 프로젝트를 이어서 진행할게. 프로젝트 정보, 일정은 CLAUDE.md에 있으니 먼저 읽어 줘. 기획서(Assets/02. Docs/Rappel — 컨셉 기획서.pdf)도 읽어 줘.
단, CLAUDE.md는 11/18(수) 22:54 기준이라 아래 내용이 더 최신이야. 특히 마감이 바뀌었어 (아래 "마감" 참고).

## 작업 규칙
* 모든 답변은 한국어로, Unity 용어는 쉽게 풀어서 설명해 줘.
* 로직 구조는 내가 짠다. 설계안을 먼저 제시하지 말고, 내가 말한 구조대로 코드를 작성해 줘. 내 구조에 없는 세부 사항을 정해야 하면 구현한 뒤에 무엇을 정했는지 짧게 알려 줘. 내 구조를 바꾸게 되면 바뀐 점을 분명히 말해 줘.
* 기존 코드와 부딪히거나 버그가 될 부분이 보이면 사실만 짧게 알려 줘.
* 스크립트는 내가 요청할 때만 만들거나 수정해 줘. "시작하자" 같은 말은 코드 작성 허락이 아니야. 내가 "다음 할 일"을 물으면 할 일만 알려 줘.
* Unity가 보통 켜져 있으니 .unity 씬 파일이나 ProjectSettings를 직접 수정하지 마. 에디터 작업은 단계별로 안내만 해 줘.
* UI처럼 에디터에서 손으로 만들기 번거로운 건, 내가 메뉴를 클릭하면 만들어지는 에디터 툴(Editor 폴더 스크립트)로 만들어 줘.
* 씬 상태(위치, 스케일, 연결)를 말하기 전에 저장된 씬 파일(Assets/00. Scene/*.unity, RappelRig.prefab, City.prefab)을 직접 확인해 줘. CLAUDE.md나 예전 대화만 보고 말하지 마.
* 내가 코드를 이해 못 하면 쉬운 말 + 예시(표, 머메이드)로 설명해 줘.
* 커밋은 내가 할게.
* (11/19 추가) 지금은 기능 개발에만 집중. 빌드/정리 얘기는 내가 물을 때만.

## 마감 (11/20 새벽 사용자 확인, CLAUDE.md보다 최신)
* **1차 빌드 + 시연 영상 제출: 11/23(월). 마감은 이것 하나뿐.**
* CLAUDE.md의 "11/20(금) 9시 발표", "11/19 13시 기능 동결", 날짜별 일정은 낡음 (아직 안 고침)

## 11/20(금) 02:25 기준 진행 상황
### 끝난 것 (사용자 확인)
* 01~05 스테이지 + 메뉴, 사격(03/05), 창문 진입(04/05), 안내창, 종료 화면 (이전 세션까지)
* 11/20 빌드 테스트 완료 (커밋 a07158d7 "빌드 테스트" 02:23). 사용자: "수치만 조절하면 됨"
* 건물 교체: Synty PolygonPoliceStation으로 만든 City 프리팹을 5개 씬에 배치, 바닥 높이 18.238
* 사용자가 확인 완료라고 한 것: 창문 정면 노출(고정 거절 진동, 1.5초 뒤 발각 실패, 표적 다 맞히면 꺼짐), 실패 이유 문구, UI 안 가려짐(한글 포함), 소리(발소리, 점프, 바람, 유리, 도시), 05 유리 소리

### 스크립트 변경 (Assets/01. Scripts/Rappel/) — 이번 세션
* **WindowExposure.cs (새, 03/05 표적 창문마다, 실행 순서 -70)**
  - 머리가 창문 정면 범위(width × height × 앞뒤 depth, 창문 기준 회전) 안이면
    - A: 상태머신에 SetLockBlocked(this, true) → 벽→고정 거절, 거절 순간(LockRefused) 양손 진동 0.6 / 0.3초
    - B: (OnWall이고 하강 속도 < stopSpeed 0.3) 또는 Locked/GunDrawn 상태로 exposeLimit 1.5초 머물면 StageRule.NotifyExposed() → 실패. 머무는 동안 0.25초마다 경고 진동 0.2→0.8
  - Ready/Finished면 범위 밖 취급. 점프 중이나 빠르게 지나가면 노출 아님
  - 씬 표적을 다 맞히면(StageRule.AllTargetsHit) 스스로 꺼짐 (OnDisable에서 막음 해제)
  - Scene 창 기즈모: 주황 상자. 연결 칸 비우면 씬에서 찾음 (Haptic 칸은 직접 연결)
* **RappelStateMachine.cs (구조 변경)**: SetLockBlocked(Object, bool) + IsLockBlocked + LockRefused 이벤트 추가. 막혀 있으면 OnWall→Locked만 거절 (고정 풀기는 항상 됨)
* **StageRule.cs**: Finish 직접 호출을 End(성공, 이유)로 바꿈 (이유를 먼저 정하고 Finish, 거절되면 이유 되돌림). EndReason, AllTargetsHit 공개. NotifyExposed() 추가(SideWatch/Mission만). NotifyWindowMissed(bool tooHigh)로 바뀜
  - 실패 이유 문구: 너무 빨리 내려갔습니다 (x.x m/s) / 착지가 너무 빨랐습니다 (x.x m/s) / 점프를 한 번도 안 했습니다 / 표적을 다 못 맞혔습니다 (n / m) / 창문 정면에 너무 오래 멈춰 발각됐습니다 / 창문보다 너무 높았습니다·낮았습니다 / 창문을 지나쳐 바닥까지 내려갔습니다 / 표적이 남은 채 진입했습니다 (n / m). 성공이면 이유 없음
* **EndScreen.cs**: 결과 글자 아래 줄에 `<size=reasonFontSize(40)>이유</size>` 붙임. stageRule 칸 비우면 씬에서 찾음 (프리팹이라 연결 불가)
* **WindowEntry.cs**: glassBreakClip, glassBreakVolume(1) 추가. 진입 성공 때 AudioSource.PlayClipAtPoint(창문 위치). NotifyWindowMissed(local.y > 0) 호출
* **StageGuide.cs**: 03/05 벽 문구에 "창문 정면에선 고정 안 됨, 멈추면 발각(실패)" 추가, "목표 지점" → "표적 창문 옆 벽"
* **RappelSounds.cs (새, RappelRig의 XR Origin, 실행 순서 10)**
  - 발소리: OnWall + 하강 속도 ≥ 0.05일 때 stepDistance 0.5m마다(최소 간격 0.3초), 클립 랜덤 + 음높이 0.9~1.1
  - 점프: OnWall→Jumping = jumpClip, Jumping→OnWall = wallLandClip
  - 로프 마찰음: ropeLoopClip 반복, 음량 = 0.7 × (속도/3), 음높이 0.8→1.2
  - AudioSource 칸 비우면 시작 때 2D 스피커 2개를 만듦
* **UIAlwaysOnTop.cs + UIOverlay.shader (새)**: 캔버스에 붙이면 Awake에서 이미지 재질 → "Rappel/UI Overlay"(UI/Default + ZTest Always), TMP 글자 → "TextMeshPro/Mobile/Distance Field Overlay", Canvas sortingOrder 100. Reset에서 셰이더 칸 자동 채움
* **Editor/CityOptimizer.cs (새)**: Tools > Rappel > City 프리팹 최적화. City.prefab 모든 오브젝트 Static(Batching + Occludee, 렌더러 긴 변 ≥ 2m면 Occluder) + 그림자 지기/받기 끔 → 저장
* 실행 순서: Figure8Follow(-100) → WindowExposure(-70) → HandBrake(-60) → BrakeManager(-50) → RappelDescent(0), RappelJump(0), Gun(0) → WindowEntry(1) → GunHolster(2) → StageRule(5) → DescentHaptics(10), RappelSounds(10) → RopeLine, Figure8Follow(LateUpdate)

### 씬 연결 (저장 기준: 01 02:18, 02 00:40, 03 02:21, 04 01:32, 05 02:21, RappelRig 01:36, City 01:31, Menu 11/19 01:08)
* RappelRig: 맨 위 Y 0, XR Origin Y 약 49 (씬 덮어쓰기). groundY(RappelDescent, RopeLine)는 씬마다 덮어씀
  - 01 18.23838 / 02, 04, 05 18.2383 / **03은 RopeLine 18, RappelDescent 18.2383 (서로 다름)**
* RappelRig 안: UIAlwaysOnTop 2개(GuideCanvas, EndScreenCanvas, 셰이더 칸 채워짐), RappelSounds(발소리 10개, 점프, 벽 착지 연결, **ropeLoopClip 비어 있음**), Wind AudioSource(Wind.mp3, Loop), XR Origin에 클립 없는 AudioSource 2개(Play On Awake 켜짐)
* City.prefab: Static 적용됨, 도시 소리 AudioSource 1개(km007-city-ambience, Loop, 3D, Linear, Max 50)
* 03 WindowExposure: 2.39 × 3.67, depth 1.5, 양손 Haptic 연결
* 05 WindowExposure: 4.58 × 3.98, depth 1.5, 양손 Haptic 연결 / WindowEntry: 4.6 × 1.98, failMargin 0.48, depth 1.5, 유리 소리 연결
* 04 WindowEntry: 4.66 × 1.87, failMargin 0.54, depth 1.69, 유리 소리 연결
* 5개 씬 모두 OcclusionCullingData 있음 (오클루전 굽기 됨)

### 알아둘 사실
* 커밋 안 된 변경 없음 (02:23 커밋 이후 git status 깨끗)
* 로프 소리 없음: RappelSounds의 Rope Loop Clip 칸이 비어 있음 (소리 파일은 사용자가 찾기로 함)
* 프레임이 좀 끊기는 것 같다고 함 → 실기기에서 OVR Metrics Tool(FPS, GPU/CPU %, Stale Frames) 또는 `adb logcat -s VrApi`로 확인하기로 함. 아직 수치 없음
* CityOptimizer는 창문 유리(별도 렌더러, Glass_01)도 2m 넘으면 Occluder로 지정함 → 오클루전 굽기 후 유리 너머 방 안이 안 보일 수 있음. 사용자가 "괜찮아"로 그대로 둠 (증상 나면 City 안 Glass 오브젝트의 Occluder Static만 끄고 다시 굽기)
* 건물 그림자 없음 (CityOptimizer가 끔)
* Synty 재질(Generic_Basic 셰이더그래프)에 알파 클립이 켜져 있음 (Quest에서 조금 비쌈). 안 건드림
* WindowExposure는 05 진입 창문(WindowEntry 창문)에 두면 안 됨 (진입하려면 멈춰야 해서 발각됨)
* 창문 진입 순간 벽 착지 소리 + 유리 소리가 같이 남 (둘 다 Jumping→OnWall 순간)
* RappelSounds 발소리/점프는 같은 스피커라 음높이 바꿀 때 겹친 소리 음높이도 살짝 바뀜
* 04/05 씬에서 RappelRig "Apply All" 금지 (showDelay 2, groundY, 위치 덮어쓰기가 프리팹으로 퍼짐)
* 바닥 착지음(기획서 필수 피드백) 아직 없음. RappelDescent.Landed 이벤트 있음
* 비네트(기획서 "멀미와 안전") 아직 없음
* 사용자는 X 점프가 "아무 때나 됐다"고 했음 (원인 확인 안 함)
* Unity Inspector의 SerializedObjectNotCreatableException / MissingReferenceException은 에디터 내부 메시지 (선택한 오브젝트가 사라졌을 때). 게임 영향 없음
* CLAUDE.md에서 낡은 부분: 마감/일정/기능 동결/자를 순서, 씬 이름(00. TestScene), 스테이지 5개·메뉴·StageRule·EndScreen·StageGuide·WindowEntry·WindowExposure·사격·사운드·City 없음, 착지 높이(groundY 0)

## 다음 할 일 (11/20 ~ 11/23 제출)
1. 수치 조절 (사용자 진행 중)
2. 로프 소리 파일 구해서 Rope Loop Clip에 연결
3. 실기기 프레임 측정 (OVR Metrics Tool) → 떨어지는 곳 찾기
4. 기획서 필수 남은 것: 바닥 착지음 (구조는 사용자가 정함)
5. 여유 있으면: 비네트
6. 최종 APK 백업, 시연 영상 녹화 + 편집, CLAUDE.md 갱신
* 밀리면 자르는 순서는 CLAUDE.md "자를 순서" 기준이었으나 마감이 바뀌어서 사용자와 다시 정해야 함 (확인 안 함)
* 완료 기준: 11/23까지 끊김 없는 APK + 시연 영상 제출

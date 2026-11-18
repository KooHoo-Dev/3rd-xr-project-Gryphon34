Rappel VR 프로젝트를 이어서 진행할게. 프로젝트 정보, 일정은 CLAUDE.md에 있으니 먼저 읽어 줘. 기획서(Assets/02. Docs/Rappel — 컨셉 기획서.pdf)도 읽어 줘.
단, CLAUDE.md는 11/18(수) 12:15 기준이라 아래 내용이 더 최신이야.

## 작업 규칙
* 모든 답변은 한국어로, Unity 용어는 쉽게 풀어서 설명해 줘.
* 로직 구조는 내가 짠다. 설계안을 먼저 제시하지 말고, 내가 말한 구조대로 코드를 작성해 줘. 내 구조에 없는 세부 사항을 정해야 하면 구현한 뒤에 무엇을 정했는지 짧게 알려 줘. 내 구조를 바꾸게 되면 바뀐 점을 분명히 말해 줘.
* 기존 코드와 부딪히거나 버그가 될 부분이 보이면 사실만 짧게 알려 줘.
* 스크립트는 내가 요청할 때만 만들거나 수정해 줘. "시작하자" 같은 말은 코드 작성 허락이 아니야. 내가 "다음 할 일"을 물으면 할 일만 알려 줘.
* Unity가 보통 켜져 있으니 .unity 씬 파일이나 ProjectSettings를 직접 수정하지 마. 에디터 작업은 단계별로 안내만 해 줘.
* UI처럼 에디터에서 손으로 만들기 번거로운 건, 내가 메뉴를 클릭하면 만들어지는 에디터 툴(Editor 폴더 스크립트)로 만들어 줘.
* 씬 상태(위치, 스케일, 연결)를 말하기 전에 저장된 씬 파일(Assets/00. Scene/*.unity, RappelRig.prefab)을 직접 확인해 줘. CLAUDE.md나 예전 대화만 보고 말하지 마.
* 내가 코드를 이해 못 하면 쉬운 말 + 예시(표, 머메이드)로 설명해 줘.
* 커밋은 내가 할게.
* (11/19 추가) 지금은 기능 개발에만 집중. 빌드/정리 얘기는 내가 물을 때만.

## 11/19(목) 01:20 기준 진행 상황
### 끝난 것 (Link 테스트 완료)
* 01 하강, 02 점프: 성공/실패 판정, 종료 화면, 다시 하기
* 안내창(튜토리얼): 시작하면 바로 뜸, 왼손 Y로 켜고 끄기, 상태별 "지금 할 일" 문구
* 종료 화면 "다음 스테이지" 버튼 (성공 때만)
* 창문 진입 (04, 05): 판정 + 유리 깨짐 + 진입 순간 속도로 방 안으로 들어가 바닥에 섬 + 2초 뒤 결과 화면
* 메뉴 씬 (Menu.unity) + Build Settings 6개 등록 (Menu 0번, 01~05 = 1~5번)
* 씬별 XR Origin 시작 위치: 사용자가 끝냄 ("이대로 둠")

### 스크립트 (Assets/01. Scripts/Rappel/)
* 11/18 커밋분(상태머신, 하강, 제동, 고정, 점프, 햅틱, 로프, 8자고리)은 안 바뀜. 설명은 CLAUDE.md + 아래
  - RappelStateMachine 상태: Ready, OnWall, Locked, GunDrawn, Jumping, Finished. DrawGun/HolsterGun은 아직 부르는 곳 없음
  - RappelJump 프리팹 값: jumpDistance 2, jumpDuration 2 (사용자가 바꿈. 코드 기본값 1 / 1.5)
  - X 점프는 왼손이 8자고리 0.2m 안일 때만 (코드/프리팹 확인함. 사용자는 "아무 때나 됐다"고 함, 원인 확인 안 함)
* StageRule.cs: NotifyWindowMissed() 추가 (Window/Mission이면 Finish(false))
* EndScreen.cs
  - nextButton: 성공일 때만 켬. NextStage(): buildIndex + 1, 마지막이면 GoToMenu()
  - showDelay(기본 0): 종료 후 이 시간 뒤 패널 켬(Invoke). 프리팹 0, 04/05 씬에서만 2로 덮어씀
* StageGuide.cs (새, GuideCanvas): 시작 때 Show(). 왼손 Y(<XRController>{LeftHand}/{SecondaryButton}, 스크립트에서 직접 만든 입력)로 켜고 끔. 자동 닫힘 없음
  - 띄우는 순간 머리 기준 왼쪽 35°, 0.6m, -0.1m에 놓고 그 뒤로 안 따라감 (XR Origin 자식이라 몸과 같이 내려감)
  - 문구 = 제목 "스테이지 N · 이름"(N = StageType + 1) + 목표 한 줄(StageType별) + 지금 할 일(StageType × 상태, StateChanged로 갱신) + "왼손 Y 버튼: 이 창 켜기 / 끄기"(120%, 노란색)
  - 사용자 수정 반영: "그립 꽉" → "그립", 점프 키 "(왼손 8자고리 + X도 가능)", 점프 스테이지는 벽/점프 중 같은 전체 문구("2초 뒤 벽으로 돌아옴")
  - stageRule, stateMachine 비우면 씬에서 찾음
* WindowEntry.cs (새, 04/05 씬의 WindowEntry 오브젝트, 실행 순서 1)
  - Jumping → OnWall 순간 한 번 판정(이벤트에선 표시만, Update에서 판정). 머리를 이 오브젝트 위치/회전 기준으로 봄(스케일 무시)
  - 창문 안(width × height) → glass.FragAll() + NotifyWindowEntered() + 방 안 이동 / 위아래 failMargin 안 → NotifyWindowMissed() / 그 밖, 옆, depth 밖 → 아무 일 없음
  - 방 안 이동: 진입 순간 속도(직전 프레임 XR Origin 이동량) → 수평은 horizontalDrag로 감속(벽 쪽 속도 < minEnterSpeed면 그 값으로), 바닥보다 높으면 중력, 낮으면 climbSpeed로 올라옴 → floorPoint Y에 섬
  - Scene 창 기즈모: 초록 = 성공 범위, 빨강 = 실패 띠
* MenuScreen.cs (새, MenuCanvas): LoadStage(buildIndex)
* Editor
  - EndScreenBuilder: 버튼 3개(다시 하기 | 메뉴 | 다음 스테이지)로 만듦 + "종료 화면에 다음 스테이지 버튼 추가" 메뉴. CreateUI/Stretch/CreateButton/EnsureEventSystem/KoreanFontPath를 internal로 열어 다른 툴이 씀
  - StageGuideBuilder: Tools > Rappel > 안내창 만들기 (0.5 × 0.6m, 본문 자동 글자 크기 16~26)
  - MenuBuilder: Tools > Rappel > 메뉴 화면 만들기 (원점 앞 1.2m, 높이 1.5m, 0.8 × 0.9m, 제목 + 스테이지 버튼 5개 → LoadStage(1~5))
* 실행 순서: Figure8Follow(-100) → HandBrake(-60) → BrakeManager(-50) → RappelDescent(0), RappelJump(0) → WindowEntry(1) → StageRule(5) → DescentHaptics(10) → RopeLine, Figure8Follow(LateUpdate)

### 씬 연결 (저장 기준: 01 11/18 23:54, 02 22:54, 03 11/19 01:13, 04/05 01:15, Menu 01:08, RappelRig 11/18 23:54)
* RappelRig 프리팹에 GuideCanvas, EndScreenCanvas(NextButton 연결) 들어 있음
* 04, 05 WindowEntry: Glass, FloorPoint 연결됨. width 4.94, height 1.36, failMargin 0.81, minEnterSpeed 1, horizontalDrag 1, climbSpeed 2. EndScreen showDelay 2 (씬 덮어쓰기)
* Menu: XR Origin (XR Rig) 프리팹, Tracking Origin = Device, Camera Y Offset 1.6, 프리팹 안 오브젝트 하나 꺼짐(Locomotion으로 보임, 이름 확인 안 함), MenuCanvas, Event System 있음
* 03 SideScene: SM_Env_Wall_Interior_Window_03 하나가 지워짐 (의도인지 확인 안 함). 03/05에 "목표 지점"과 표적 아직 없음

### 알아둘 사실
* 04/05 씬에서 RappelRig "Apply All" 금지 (showDelay 2가 프리팹으로 퍼짐)
* GuideCanvas 프리팹에 예전 칸 autoHideTime 값이 남아 있음 (코드에서 지워서 무시됨, 문제 없음)
* 안내창 사격/측면 경계 문구는 확답 안 받은 사격 기본값 기준 → 사격 구조 정하면 문구도 맞춰야 함
* 고소감: "건물만 허공에 떠 있어서 안 무섭다" → 사격 끝나고 주변 채우기(옆 건물 복제, 도로/인도, 경찰차/SWAT 밴/사람, 헬기, Fog)를 하기로 함. 코드 없음. 소품은 City 프리팹 하나로 묶어 5개 씬에 넣기, Static 체크. 나무/가로등/배경 건물/스카이박스는 프로젝트에 없음
* 소리 파일 없음 (줄 마찰음, 착지음, 총소리, 바람)
* APK 실기기 확인은 11/17 이후 못 함 (메뉴, 레이저 버튼, 안내창, 창문 진입 포함)
* 커밋 안 된 변경: 스크립트 8개(새 4 + 수정 4, Editor 포함), 씬 01/03/04/05, Menu.unity(새), RappelRig.prefab, EditorBuildSettings, NotoSansKR SDF, OpenXRPackageSettings
* 메모리 rappel-tutorial-plan은 메모리 폴더에 없음 (튜토리얼은 이제 구현됨)
* CLAUDE.md에서 낡은 부분: 씬 이름(00. TestScene), 스테이지 5개/메뉴/StageRule/EndScreen/StageGuide/WindowEntry/GunDrawn 없음, 일정과 자를 순서가 범위 변경 전 기준

## 다음 할 일 (목 11/19, 13시 기능 동결)
1. 사격 구조 확정 (사용자가 아침에 정함). 정할 것:
   - 총 꺼내기/넣기 (기본값: 고정 중 오른손 골반 + 그립 토글, PelvisCheck 재사용)
   - 사격 (기본값: 총 든 상태 오른손 트리거 → 총구 레이 → 표적 사라짐 + GunShot_Smoke_FX, 총 SM_Wep_PistolSwat_01)
   - 총 위치(컨트롤러에 붙일지, 컨트롤러 모델 숨길지), 표적 개수/모양/위치, 03/05 목표 지점(표시만 / 그 높이에서 표적 등장)
   - (11/19 01:30 결정) 05 임무에도 측면 경계 중 표적 사격을 넣음. 지금 05는 StageRule 규칙상 창문 진입만 하면 성공(표적 안 봄)
   - 아직 안 정함: 05에서 표적을 다 안 맞히고 창문에 들어가면 실패로 할지 (정하면 StageRule.NotifyWindowEntered에 allTargetsHit 조건 추가)
2. 사격 스크립트 작성 + 03/05 배치 + 안내 문구 맞추기
3. 주변 채우기 (옆 건물, 바닥 소품, Fog)
4. 사운드 (파일 구하기부터)
5. APK 실기기 확인 + 백업, 시연 영상, CLAUDE.md 갱신
* CLAUDE.md 기준 사격은 "11시까지 안 되면 자름". 사용자는 사격 → 주변 채우기 → 사운드 순서로 하기로 함
* 오늘 완료 기준: 사격 동작(03에서 표적 다 맞히고 착지 → 성공) + 기능 동결 전 APK 백업

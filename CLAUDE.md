Rappel VR 프로젝트를 이어서 진행하려고 해. 모든 답변과 제안은 한국어로 해 줘. Unity 용어는 쉽게 풀어서 설명해 줘.

## 마감
- 발표: 2026-11-20(금) 오전 9시, 학원. 시연 영상 + Quest 3 APK 빌드
- 목요일 오후 1시 기능 동결. 이후에는 버그 수정, 빌드, 영상만 함
- 범위: 아래 일정에 있는 기능만. 표적 사격은 하지 않음. 새 기능을 제안하지 말고, 일정이 밀리면 "자를 순서"를 기준으로 알려 줘

## 프로젝트
- Meta Quest 3용 1인 VR 체험. 그립을 쥐는 강도로 하강 속도를 조절하며 건물 외벽을 레펠로 내려감.
- 기획서: Assets/Docs/Rappel — 컨셉 기획서.pdf (먼저 읽어 줘)
- Unity 6.3 (6000.3.15f1), OpenXR + XR Interaction Toolkit 3.5.1, Obi Rope 7.0.5

## 설계 원칙
- 플레이어 몸(XR Origin)은 스크립트로 직접 움직이고, Obi 로프는 그걸 따라오는 시각/보조 물리로 사용 (멀미 방지).
- Obi 구성 계획: ObiSolver(Burst 백엔드) + ObiRope, 옥상 고정점은 ObiParticleAttachment(Static), 8자고리와 오른손은 ObiPinhole, 벽에 ObiCollider, 렌더링은 ObiPathSmoother + ObiRopeExtrudedRenderer. Pinhole이 불안정하면 ObiRopeCursor로 대체.

## 현재 상태
- Assets/Scripts/Rappel/RappelDescent.cs (XR Origin에 붙어 있음)
  - 그립 값: XRI Right Interaction/Select Value (InputActionProperty, Use Reference)
  - (11/16 변경) 물리 공식(제동력/마찰) 방식은 속도가 너무 일정하고 그립이 예민해서 폐기
  - 그립 → 목표 속도: 인스펙터 곡선 gripToSpeed (기본: 0→3, 0.3→2, 0.5→0.8, 0.7 이상→0 m/s)
  - 실제 속도는 목표 속도를 가속도 제한으로 따라감 (speedUpRate 1.5, slowDownRate 3 m/s²)
  - 최대 하강 속도 3 m/s, groundY 아래로는 안 내려감
  - canDescend가 false면 하강 안 함 (PelvisCheck가 true로 바꿈)
- Assets/Scripts/Rappel/PelvisCheck.cs (XR Origin에 붙어 있음)
  - 골반 위치 = 머리 위치 + pelvisOffset (머리 Yaw 기준), 반경 pelvisRadius 안에 오른손 + 그립 1 → descent.CanDescend = true (한 번 true면 유지)
  - pelvisMarker(구)가 골반 위치를 따라다니고, 손이 안이면 초록/밖이면 하늘색
- 11/17: 하강 수치와 골반 그립 Link 테스트 완료
- XR Origin의 Locomotion 오브젝트는 비활성화 (Gravity가 리그를 90 m/s까지 떨어뜨렸음, Jump/Turn도 불필요)
- Building 오브젝트 스케일이 3이라 플레이어가 작게 느껴짐 → 조정 필요
- 테스트 씬: Assets/00. Scene/00. TestScene.unity (XRI Starter Assets의 XR Origin (XR Rig) 프리팹 사용)
- 설정: Active Input Handling = Both (Obi 샘플용, 유지), Quest Link 테스트할 때 XR Interaction Simulator는 끔
- Quest Link: Link 앱의 "활성 런타임 설정" 버튼이 안 눌려서, Unity의 OpenXR → PC 탭 → Play Mode OpenXR Runtime을 Oculus로 지정하는 방법으로 진행 중
- 빌드: 11/17 Android 첫 APK 빌드 + Quest 실기기 테스트 완료 (빌드 씬 = 00. TestScene)
  - 빌드 시 "Active Input Handling = Both" 경고는 Ignore로 진행 중. 실기기 입력 문제가 생기면 New로 전환 검토 (영향은 Obi 샘플 씬뿐)
  - Android 품질 = Mobile → Mobile_Renderer (SSAO 없음). PC_Renderer의 SSAO가 Project Validation 경고 원인

## 아직 확인 안 된 것
- XR Origin 정면(Z+)이 벽을 향하는지 (Tracking Origin Mode는 Floor 확인됨)

## 일정 (매일 저녁 동작하는 APK를 백업으로 남김)

### 월 11/16 — 하강 감각 + 첫 빌드
1. 컴파일 확인, XR Origin 설정(Y=10, Floor, 정면이 벽)
2. Quest Link로 하강/제동 느낌 테스트하고 수치 조정
3. 골반 캘리브레이션: 오른손을 골반에 두고 그립 → 골반 위치 저장 → 하강 가능 상태 (그 전에는 떨어지지 않음)
4. 빌드 설정: TestScene 등록, Android 플랫폼, 샘플 씬 제외 → Quest에 첫 APK 설치
- 완료 기준: 기기에서 캘리브레이션 후 그립으로 하강/정지가 됨

### 화 11/17 — 씬 + 로프 + 두 손 제동 (+ 오후에 캐릭터 IK: Final IK의 VRIK 검토, 캐릭터는 XR Origin의 자식)
5. 벽, 창문, 바닥, 지붕 앵커로 씬 구성
6. Obi 로프 연결 (옥상 Static 고정 + 8자고리 Pinhole + 오른손 Pinhole, 벽 ObiCollider)
7. 8자고리 제동(왼손) + 두 손 규칙: 둘 다 제동 위치면 더 세게 쥔 쪽 값, 먼저 놓으면 남은 손이 이어받음
8. 저녁 APK로 Quest에서 Obi 성능 확인 (무거우면 파티클 수/서브스텝 줄이기)
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

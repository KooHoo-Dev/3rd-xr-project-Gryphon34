using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rappel
{
    // 왼손 Y 버튼을 누르면 시선 왼쪽에 이 씬의 목표와 조작법을 띄움
    // 씬에 들어오면 바로 뜨고, 그 뒤로는 Y를 누를 때마다 켜고 끔 (자동으로 닫히지 않음)
    // 문구: 목표 한 줄(씬 종류 StageType별, 항상) + 지금 할 일(상태가 바뀔 때마다 그 상태에 맞게 바뀜)
    // 이 스크립트는 XR Origin 자식 캔버스(GuideCanvas)에 붙임 → 띄운 뒤에는 몸(XR Origin)을 따라 같이 내려감
    public class StageGuide : MonoBehaviour
    {
        [Header("연결")]
        // XRI 기본 입력에 왼손 Y가 따로 없어서 여기서 직접 만듦 (Use Reference 끈 상태)
        [SerializeField] private InputActionProperty toggleAction = new InputActionProperty(
            new InputAction("Guide Toggle", InputActionType.Button, "<XRController>{LeftHand}/{SecondaryButton}"));
        [SerializeField] private Transform head;            // Main Camera
        [SerializeField] private RappelStateMachine stateMachine;   // 상태가 바뀌면 "지금 할 일" 문구를 바꿈 (비우면 씬에서 찾음)
        [SerializeField] private StageRule stageRule;       // 씬에 있음. 비우면 시작할 때 씬에서 찾음 (프리팹이라 직접 연결이 안 됨)
        [SerializeField] private GameObject panel;          // 안내창 (처음엔 꺼 둠)
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        [Header("위치")]
        [SerializeField] private float distance = 0.6f;     // 머리에서 떨어진 거리 (m)
        [SerializeField] private float leftAngle = 35f;     // 시선에서 왼쪽으로 돌린 각도 (도)
        [SerializeField] private float heightOffset = -0.1f; // 눈높이 기준 위아래 (m)

        [Header("디버그 (읽기용)")]
        [SerializeField] private bool isShown;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
            if (stateMachine == null)
                stateMachine = FindFirstObjectByType<RappelStateMachine>();
        }

        void Start()
        {
            if (stageRule == null)
                stageRule = FindFirstObjectByType<StageRule>();
            if (stageRule == null)
                Debug.LogWarning("[Rappel] StageGuide: 씬에서 StageRule을 못 찾아서 하강 연습 문구를 띄워요");

            FillText();
            Show();   // 씬에 들어오면 바로 띄움
        }

        void OnEnable()
        {
            toggleAction.action?.Enable();   // 직접 만든 입력은 Input Action Manager가 안 켜 줌
            if (stateMachine != null)
                stateMachine.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (stateMachine != null)
                stateMachine.StateChanged -= OnStateChanged;
            if (!toggleAction.reference)
                toggleAction.action?.Disable();
        }

        void Update()
        {
            if (toggleAction.action != null && toggleAction.action.WasPressedThisFrame())
            {
                if (isShown) Hide();
                else Show();
            }
        }

        public void Show()
        {
            if (panel == null)
                return;

            PlaceLeftOfView();
            panel.SetActive(true);
            isShown = true;
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            isShown = false;
        }

        // 머리가 보는 방향(수평만)에서 왼쪽으로 leftAngle만큼 돌린 곳에 놓고, 머리를 향하게 함
        private void PlaceLeftOfView()
        {
            if (head == null)
                return;

            Vector3 forward = head.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.parent != null ? transform.parent.forward : Vector3.forward;
            forward.Normalize();

            Vector3 dir = Quaternion.AngleAxis(-leftAngle, Vector3.up) * forward;
            transform.position = head.position + dir * distance + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);   // 캔버스 앞면이 머리 쪽으로 보이게
        }

        // 제목 + 목표(항상) + 지금 상태에 맞는 할 일 + Y 안내
        private void FillText()
        {
            StageType stage = stageRule != null ? stageRule.Stage : StageType.Descent;
            RappelState state = stateMachine != null ? stateMachine.State : RappelState.Ready;

            if (titleText != null)
                titleText.text = $"스테이지 {(int)stage + 1} · {StageName(stage)}";

            if (bodyText == null)
                return;

            string hint = Hint(stage, state);
            bodyText.text =
                $"<size=110%><color=#FFD54F><b>목표</b></color>  {Goal(stage)}</size>\n\n" +
                (string.IsNullOrEmpty(hint) ? "" : $"<b>지금 할 일</b>\n{hint}\n\n") +
                "<size=120%><b><color=#FFD54F>왼손 Y 버튼: 이 창 켜기 / 끄기</color></b></size>";
        }

        private void OnStateChanged(RappelState prev, RappelState next)
        {
            FillText();
        }

        private static string StageName(StageType stage)
        {
            switch (stage)
            {
                case StageType.Descent:   return "하강 연습";
                case StageType.Jump:      return "점프 연습";
                case StageType.SideWatch: return "측면 경계 연습";
                case StageType.Window:    return "창문 침투 연습";
                default:                  return "임무";
            }
        }

        // 목표 한 줄 (상태와 상관없이 항상 보임)
        private static string Goal(StageType stage)
        {
            switch (stage)
            {
                case StageType.Descent:   return "천천히 내려가 착지\n(2 m/s 넘으면 실패)";
                case StageType.Jump:      return "점프 한 번 이상 하고 착지";
                case StageType.SideWatch: return "목표 지점에서 표적 모두 맞히고 착지";
                default:                  return "창문으로 진입\n(놓치고 착지하면 실패)";
            }
        }

        // 공통 문구
        private const string Calibrate = "오른손을 오른쪽 골반에 대고\n그립 끝까지 쥐기 → 진동 오면 시작";
        private const string Brake     = "그립 = 멈춤\n살짝 풀기 = 천천히\n놓기 = 빠르게\n왼손 8자고리 + 그립 = 제동";
        private const string JumpKey   = "A 버튼 = 점프\n(왼손 8자고리 + X도 가능)";
        private const string LockKey   = "오른손 8자고리 + 그립 + 트리거";
        private const string Unlock    = "고정 중엔 내려가지도, 점프하지도 않음\n" + LockKey + " = 고정 풀기";
        private const string WatchLock = "몸을 옆으로 돌려 표적 찾기\n오른손 골반 + 그립 = 총 꺼내기\n다 맞히면 " + LockKey + " = 고정 풀기";
        private const string GunDrawn  = "트리거 = 사격\n오른손 골반 + 그립 = 총 넣기";
        private const string WindowJump = "벽으로 돌아올 때 창문 앞이면 진입\n그립 = 높이 유지";

        // 지금 상태에서 할 일 (짧게 2~4줄)
        private static string Hint(StageType stage, RappelState state)
        {
            if (state == RappelState.Ready)
                return Calibrate;
            if (state == RappelState.Finished)
                return "";

            switch (stage)
            {
                case StageType.Descent:
                    return state == RappelState.Locked ? Unlock : Brake;

                case StageType.Jump:
                    switch (state)
                    {
                        case RappelState.Locked:  return Unlock;
                        // 벽, 점프 중 모두 같은 문구 (점프 설명을 한 번에 다 보여 줌)
                        default:                  return JumpKey + "\n그립으로 속도 조절\n그립 쥐면 그 높이, 놓으면 내려가며\n2초 뒤 벽으로 돌아옴";
                    }

                case StageType.SideWatch:
                    switch (state)
                    {
                        case RappelState.Locked:   return WatchLock;
                        case RappelState.GunDrawn: return GunDrawn;
                        default:                   return "목표 지점까지 내려가기\n도착하면 " + LockKey + " = 고정";
                    }

                case StageType.Window:
                    switch (state)
                    {
                        case RappelState.Jumping: return WindowJump;
                        case RappelState.Locked:  return Unlock;
                        default:                  return "창문 높이까지 내려가 그립 = 멈춤\n" + JumpKey;
                    }

                default:   // 임무: 순서
                    switch (state)
                    {
                        case RappelState.Locked:   return WatchLock;
                        case RappelState.GunDrawn: return GunDrawn;
                        case RappelState.Jumping:  return WindowJump;
                        default:
                            return "1. 목표 지점에서 고정 → 경계, 사격\n2. 고정 풀고 창문 높이까지\n3. A 점프로 창문 진입";
                    }
            }
        }
    }
}

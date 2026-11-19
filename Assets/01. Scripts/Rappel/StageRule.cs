using UnityEngine;

namespace Rappel
{
    public enum StageType
    {
        Descent,    // 하강 연습: 천천히 내려와서 착지하면 성공. 한 번이라도 failSpeed를 넘으면 바로 실패
        Jump,       // 점프 연습: 점프를 한 번이라도 하고 착지하면 성공
        SideWatch,  // 측면 경계 연습: 표적을 다 맞히고 착지하면 성공. 안 맞히고 착지하면 실패
        Window,     // 창문 침투 연습: 창문 진입하면 성공. 놓치고 착지하면 실패
        Mission     // 임무: 표적을 다 맞히고 창문 진입하면 성공. 표적이 남았거나, 창문을 놓치거나, 착지하면 실패
    }

    // 씬마다 하나 두고 Inspector에서 씬 종류를 고름 (프리팹 밖, 씬에 따로)
    // 착지/점프/창문/표적 소식을 받아서 성공/실패를 정하고 상태머신에 Finish를 요청
    // RappelDescent 뒤에 실행 (같은 프레임 하강 속도로 판정)
    [DefaultExecutionOrder(5)]
    public class StageRule : MonoBehaviour
    {
        [Header("씬 종류")]
        [SerializeField] private StageType stage = StageType.Descent;

        [Header("연결")]
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private RappelDescent descent;

        [Header("하강 연습")]
        [SerializeField] private float failSpeed = 2f;   // 이 속도(m/s)를 넘으면 바로 실패

        [Header("디버그 (읽기용)")]
        [SerializeField] private bool hasJumped;
        [SerializeField] private int targetCount;    // 씬에 있는 표적 수 (Target이 시작할 때 알려 줌)
        [SerializeField] private int hitCount;       // 맞힌 표적 수
        [SerializeField] private bool allTargetsHit;
        [SerializeField] private string endReason;   // 종료 이유 (종료 화면에 표시. 성공이면 비어 있음)

        public StageType Stage => stage;
        public string EndReason => endReason;

        void OnEnable()
        {
            if (stateMachine != null)
                stateMachine.StateChanged += OnStateChanged;
            if (descent != null)
                descent.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (stateMachine != null)
                stateMachine.StateChanged -= OnStateChanged;
            if (descent != null)
                descent.Landed -= OnLanded;
        }

        void Update()
        {
            if (stateMachine == null || descent == null)
                return;

            // 하강 연습: 한 번이라도 너무 빠르면 바로 실패
            if (stage == StageType.Descent && descent.DescentSpeed > failSpeed)
                End(false, $"너무 빨리 내려갔습니다 ({descent.DescentSpeed:0.0} m/s)");
        }

        // 이유를 정하고 종료 요청. 종료가 거절되면(이미 종료 등) 이유도 그대로 둠
        // (종료 화면이 StateChanged에서 이유를 읽으므로 Finish 전에 정해 둠)
        private void End(bool success, string reason)
        {
            string prev = endReason;
            endReason = reason;
            if (!stateMachine.Finish(success))
                endReason = prev;
        }

        private string TargetScore => $"({hitCount} / {targetCount})";

        public int TargetCount => targetCount;
        public int HitCount => hitCount;
        public bool AllTargetsHit => allTargetsHit;

        // 표적이 시작할 때 하나씩 부름 (표적 개수 세기)
        public void RegisterTarget()
        {
            targetCount++;
            allTargetsHit = false;
        }

        // 표적이 처음 맞았을 때 표적 쪽에서 부름. 다 맞히면 allTargetsHit (측면 경계 연습, 임무)
        public void NotifyTargetHit()
        {
            hitCount++;
            allTargetsHit = targetCount > 0 && hitCount >= targetCount;
            Debug.Log($"[Rappel] 표적 {hitCount} / {targetCount}");
        }

        // 창문에 들어갔을 때 창문 쪽에서 부름 (창문 침투 연습, 임무)
        // 임무는 표적을 다 맞혔을 때만 성공, 하나라도 남았으면 실패
        public void NotifyWindowEntered()
        {
            if (stateMachine == null)
                return;

            if (stage == StageType.Window)
                End(true, "");
            else if (stage == StageType.Mission)
                End(allTargetsHit, allTargetsHit ? "" : $"표적이 남은 채 진입했습니다 {TargetScore}");
        }

        // 창문 바로 위/아래로 돌아왔을 때 창문 쪽에서 부름 (너무 높거나 낮음 → 실패)
        public void NotifyWindowMissed(bool tooHigh)
        {
            if (stateMachine == null)
                return;

            if (stage == StageType.Window || stage == StageType.Mission)
                End(false, tooHigh ? "창문보다 너무 높았습니다" : "창문보다 너무 낮았습니다");
        }

        // 창문 정면에 오래 머물렀을 때 창문 쪽(WindowExposure)에서 부름 (발각 → 실패)
        public void NotifyExposed()
        {
            if (stateMachine == null)
                return;

            if (stage == StageType.SideWatch || stage == StageType.Mission)
                End(false, "창문 정면에 너무 오래 멈춰 발각됐습니다");
        }

        private void OnStateChanged(RappelState prev, RappelState next)
        {
            if (next == RappelState.Jumping)
                hasJumped = true;
        }

        private void OnLanded(float impactSpeed)
        {
            if (stateMachine == null)
                return;

            bool success;
            string reason;
            switch (stage)
            {
                case StageType.Descent:
                    success = impactSpeed <= failSpeed;
                    reason = $"착지가 너무 빨랐습니다 ({impactSpeed:0.0} m/s)";
                    break;
                case StageType.Jump:
                    success = hasJumped;
                    reason = "점프를 한 번도 안 했습니다";
                    break;
                case StageType.SideWatch:
                    success = allTargetsHit;
                    reason = $"표적을 다 못 맞혔습니다 {TargetScore}";
                    break;
                default:   // 창문, 임무: 착지 = 창문을 놓침
                    success = false;
                    reason = "창문을 지나쳐 바닥까지 내려갔습니다";
                    break;
            }

            End(success, success ? "" : reason);
        }
    }
}

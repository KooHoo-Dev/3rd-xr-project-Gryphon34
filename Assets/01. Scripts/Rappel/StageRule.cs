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

        public StageType Stage => stage;

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
                stateMachine.Finish(false);
        }

        public int TargetCount => targetCount;
        public int HitCount => hitCount;

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
                stateMachine.Finish(true);
            else if (stage == StageType.Mission)
                stateMachine.Finish(allTargetsHit);
        }

        // 창문 바로 위/아래로 돌아왔을 때 창문 쪽에서 부름 (너무 높거나 낮음 → 실패)
        public void NotifyWindowMissed()
        {
            if (stateMachine == null)
                return;

            if (stage == StageType.Window || stage == StageType.Mission)
                stateMachine.Finish(false);
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
            switch (stage)
            {
                case StageType.Descent:   success = impactSpeed <= failSpeed; break;
                case StageType.Jump:      success = hasJumped;                break;
                case StageType.SideWatch: success = allTargetsHit;            break;
                default:                  success = false;                    break;   // 창문, 임무: 착지 = 창문을 놓침
            }

            stateMachine.Finish(success);
        }
    }
}

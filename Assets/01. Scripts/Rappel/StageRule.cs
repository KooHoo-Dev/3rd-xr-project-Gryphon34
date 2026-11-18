using UnityEngine;

namespace Rappel
{
    public enum StageType
    {
        Descent,    // 하강 연습: 천천히 내려와서 착지하면 성공. 한 번이라도 failSpeed를 넘으면 바로 실패
        Jump,       // 점프 연습: 점프를 한 번이라도 하고 착지하면 성공
        SideWatch,  // 측면 경계 연습: 표적을 다 맞히고 착지하면 성공. 안 맞히고 착지하면 실패
        Window,     // 창문 침투 연습: 창문 진입하면 성공. 놓치고 착지하면 실패
        Mission     // 임무: 창문 진입하면 성공. 놓치고 착지하면 실패
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

        // 표적을 다 맞혔을 때 표적 쪽에서 부름 (측면 경계 연습)
        public void NotifyAllTargetsHit()
        {
            allTargetsHit = true;
        }

        // 창문에 들어갔을 때 창문 쪽에서 부름 (창문 침투 연습, 임무)
        public void NotifyWindowEntered()
        {
            if (stateMachine == null)
                return;

            if (stage == StageType.Window || stage == StageType.Mission)
                stateMachine.Finish(true);
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

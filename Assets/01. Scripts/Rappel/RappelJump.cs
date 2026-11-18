using UnityEngine;
using UnityEngine.InputSystem;

namespace Rappel
{
    // A 버튼, 또는 왼손이 8자고리(제동 위치)에 있을 때 X 버튼으로 벽을 차고 나갔다가 진자처럼 돌아옴 (한 번 왕복하고 끝)
    // 수평(벽에서 멀어지는 방향)만 움직임. 위아래는 RappelDescent가 그립대로 처리
    //   → 그립을 쥐고 있으면 제자리 높이에서 왕복, 놓으면 내려가면서 왕복
    // 점프 가능 여부(벽 상태인지, 고정 중이 아닌지)는 상태머신이 판단
    public class RappelJump : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private InputActionProperty jumpAction;      // XRI Right Locomotion/Jump (A 버튼)

        // XRI 기본 입력에는 왼손 Jump가 없어서 X 버튼 바인딩을 여기서 직접 만듦 (Use Reference 끈 상태)
        [SerializeField] private InputActionProperty leftJumpAction = new InputActionProperty(
            new InputAction("Left Jump", InputActionType.Button, "<XRController>{LeftHand}/{PrimaryButton}"));
        [SerializeField] private HandBrake leftHand;                  // Brakes/LeftHand. 제동 위치(8자고리)일 때만 X 점프
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private Transform rig;                       // XR Origin (비우면 이 오브젝트)

        [Header("진자")]
        [SerializeField] private float jumpDistance = 1f;            // 벽에서 가장 멀어지는 거리 (m)
        [SerializeField] private float jumpDuration = 1.5f;          // 나갔다 돌아오는 시간 (초)

        [Header("디버그 (읽기용)")]
        [SerializeField] private bool isJumping;
        [SerializeField] private float elapsed;                      // 점프 시작 후 지난 시간 (초)
        [SerializeField] private float offset;                       // 지금 벽에서 떨어진 거리 (m)

        private Vector3 awayDir;   // 벽에서 멀어지는 방향 (점프 시작 때 정함)

        void Awake()
        {
            if (rig == null)
                rig = transform;
        }

        void OnEnable()
        {
            jumpAction.action?.Enable();
            leftJumpAction.action?.Enable();   // 직접 만든 입력은 Input Action Manager가 안 켜 줌
        }

        void OnDisable()
        {
            // 직접 만든 왼손 입력만 끔 (A 버튼 입력은 다른 곳과 같이 쓰니 Input Action Manager에 맡김)
            if (!leftJumpAction.reference)
                leftJumpAction.action?.Disable();
        }

        void Update()
        {
            if (stateMachine == null)
                return;

            if (isJumping)
            {
                UpdateJump();
                return;
            }

            bool rightPressed = jumpAction.action != null && jumpAction.action.WasPressedThisFrame();
            bool leftPressed = leftJumpAction.action != null && leftJumpAction.action.WasPressedThisFrame()
                               && leftHand != null && leftHand.IsAtBrake;

            if (!rightPressed && !leftPressed)
                return;

            if (!stateMachine.StartJump())
                return;

            // XR Origin 정면(+Z)이 벽 쪽이므로 반대 방향으로 나감 (높이 성분은 뺌)
            awayDir = -rig.forward;
            awayDir.y = 0f;
            awayDir.Normalize();

            isJumping = true;
            elapsed = 0f;
            offset = 0f;
        }

        void UpdateJump()
        {
            // 다른 이유로 점프 상태가 끝났으면 (예: 창문 진입으로 종료) 그 자리에서 멈춤
            if (stateMachine.State != RappelState.Jumping)
            {
                isJumping = false;
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            // 0 → 최대 거리 → 0 (반 바퀴 사인 곡선: 출발과 복귀 때 빠르고, 가장 먼 곳에서 느림)
            float nextOffset = jumpDistance * Mathf.Sin(t * Mathf.PI);
            if (t >= 1f)
                nextOffset = 0f;

            // 이번 프레임에 바뀐 거리만큼만 더함 (RappelDescent가 바꾼 높이를 덮어쓰지 않게)
            rig.position += awayDir * (nextOffset - offset);
            offset = nextOffset;

            if (t >= 1f)
            {
                isJumping = false;
                stateMachine.EndJump();
            }
        }
    }
}

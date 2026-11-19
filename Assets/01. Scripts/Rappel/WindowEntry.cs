using UnityEngine;
using UnluckSoftware;

namespace Rappel
{
    // 창문 침투 판정. 점프하고 벽으로 돌아오는 순간(Jumping → OnWall) 머리 위치를 한 번 봄
    //   - 머리가 창문 범위 안 → 유리 깨기(FragAll) + 성공 → 진입 순간 속도로 방 안으로 들어가 floorPoint 높이에 섬
    //   - 창문 범위 바로 위/아래 failMargin 안 → 실패 (너무 높거나 너무 낮음)
    // 이 오브젝트를 창문 유리 가운데에 두고, 파란 화살표(Z)가 벽 면과 수직이 되게 돌려 둠
    [DefaultExecutionOrder(1)]
    public class WindowEntry : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private StageRule stageRule;
        [SerializeField] private Transform head;
        [SerializeField] private FraggedController glass;

        [Header("범위 (m)")]
        [SerializeField] private float width = 1.2f;
        [SerializeField] private float height = 1.2f;
        [SerializeField] private float failMargin = 1f;
        [SerializeField] private float depth = 1.5f;

        [Header("방 안으로 들어가기")]
        [SerializeField] private Transform rig;
        [SerializeField] private Transform floorPoint;
        [SerializeField] private float minEnterSpeed = 1f;
        [SerializeField] private float horizontalDrag = 3f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float climbSpeed = 2f;

        [Header("유리 깨지는 소리 (비우면 안 남)")]
        [SerializeField] private AudioClip glassBreakClip;
        [SerializeField] private float glassBreakVolume = 1f;

        [Header("디버그 (읽기용)")]
        [SerializeField] private Vector3 lastHeadLocal;
        [SerializeField] private string lastResult;
        [SerializeField] private bool entered;
        [SerializeField] private Vector3 velocity;
        [SerializeField] private bool moving;

        private bool checkPending;
        private Vector3 lastRigPos;

        void Awake()
        {
            if (stateMachine == null)
                stateMachine = FindFirstObjectByType<RappelStateMachine>();
            if (stageRule == null)
                stageRule = FindFirstObjectByType<StageRule>();
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
            if (glass == null)
                Debug.LogWarning($"[Rappel] WindowEntry({name}): Glass 칸이 비어 있어서 진입해도 유리가 안 깨져요");
            if (rig == null)
            {
                Unity.XR.CoreUtils.XROrigin origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null)
                    rig = origin.transform;
            }
            if (floorPoint == null)
                Debug.LogWarning($"[Rappel] WindowEntry({name}): Floor Point 칸이 비어 있어서 진입해도 방 안으로 안 들어가요");
            if (rig != null)
                lastRigPos = rig.position;
        }

        void OnEnable()
        {
            if (stateMachine != null)
                stateMachine.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (stateMachine != null)
                stateMachine.StateChanged -= OnStateChanged;
        }

        // 이벤트 안에서 바로 Finish를 부르지 않고, 표시만 해 두고 Update에서 판정 (상태 바뀌는 도중에 또 바꾸지 않게)
        private void OnStateChanged(RappelState prev, RappelState next)
        {
            if (prev == RappelState.Jumping && next == RappelState.OnWall)
                checkPending = true;
        }

        void Update()
        {
            if (moving)
            {
                MoveIntoRoom();
                return;
            }

            // 진입 순간 속도 = 이번 프레임에 RappelDescent, RappelJump가 움직인 양 (둘 다 이 스크립트보다 먼저 실행)
            Vector3 frameVelocity = Vector3.zero;
            if (rig != null)
            {
                if (Time.deltaTime > 0f)
                    frameVelocity = (rig.position - lastRigPos) / Time.deltaTime;
                lastRigPos = rig.position;
            }

            if (!checkPending)
                return;

            checkPending = false;

            if (entered || head == null || stageRule == null)
                return;

            // 창문 기준 머리 위치 (회전만 반영, 스케일 무시)
            Vector3 local = Quaternion.Inverse(transform.rotation) * (head.position - transform.position);
            lastHeadLocal = local;

            bool inWidth = Mathf.Abs(local.x) <= width * 0.5f;
            bool inDepth = Mathf.Abs(local.z) <= depth;
            float absY = Mathf.Abs(local.y);

            if (!inWidth || !inDepth || absY > height * 0.5f + failMargin)
            {
                lastResult = "범위 밖 (계속)";
                return;
            }

            if (absY <= height * 0.5f)
            {
                // 바로 성공 → 종료 상태라 하강이 멈춤. 결과 화면은 EndScreen의 showDelay만큼 늦게 뜸
                entered = true;
                lastResult = "진입 성공";
                if (glass != null)
                    glass.FragAll();
                // 창문 가운데(이 오브젝트 위치)에서 3D 소리로 한 번
                if (glassBreakClip != null)
                    AudioSource.PlayClipAtPoint(glassBreakClip, transform.position, glassBreakVolume);
                stageRule.NotifyWindowEntered();
                StartMove(frameVelocity);
            }
            else
            {
                lastResult = local.y > 0f ? "너무 높음 (실패)" : "너무 낮음 (실패)";
                stageRule.NotifyWindowMissed(local.y > 0f);
            }
        }

        // 진입 순간 속도로 방 안으로 출발. 벽 쪽(XR Origin 정면) 수평 속도가 너무 느리면 minEnterSpeed로 맞춤
        private void StartMove(Vector3 startVelocity)
        {
            if (rig == null || floorPoint == null)
                return;

            Vector3 inward = rig.forward;
            inward.y = 0f;
            inward.Normalize();

            Vector3 horizontal = new Vector3(startVelocity.x, 0f, startVelocity.z);
            float inwardSpeed = Vector3.Dot(horizontal, inward);
            if (inwardSpeed < minEnterSpeed)
                horizontal += inward * (minEnterSpeed - inwardSpeed);

            velocity = new Vector3(horizontal.x, startVelocity.y, horizontal.z);
            moving = true;
        }

        // 수평: 점점 느려지다 멈춤. 위아래: 바닥보다 높으면 중력으로 떨어지고, 낮으면 climbSpeed로 올라와 바닥 높이에 섬
        private void MoveIntoRoom()
        {
            float dt = Time.deltaTime;
            Vector3 pos = rig.position;
            float floorY = floorPoint.position.y;

            // 수평 감속
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float speed = Mathf.Max(0f, horizontal.magnitude - horizontalDrag * dt);
            horizontal = horizontal.sqrMagnitude > 0f ? horizontal.normalized * speed : Vector3.zero;
            pos += horizontal * dt;

            // 위아래
            bool onFloor = false;
            if (pos.y > floorY)
            {
                velocity.y -= gravity * dt;
                pos.y += velocity.y * dt;
                if (pos.y <= floorY)
                {
                    pos.y = floorY;
                    onFloor = true;
                }
            }
            else
            {
                pos.y = Mathf.MoveTowards(pos.y, floorY, climbSpeed * dt);
                onFloor = Mathf.Approximately(pos.y, floorY);
            }

            if (onFloor)
                velocity.y = 0f;

            velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
            rig.position = pos;

            if (onFloor && speed <= 0f)
                moving = false;
        }

        // Scene 창에 범위 표시: 초록 = 성공, 빨강 = 실패
        void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, depth * 2f));

            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            float bandY = height * 0.5f + failMargin * 0.5f;
            Vector3 bandSize = new Vector3(width, failMargin, depth * 2f);
            Gizmos.DrawWireCube(new Vector3(0f, bandY, 0f), bandSize);
            Gizmos.DrawWireCube(new Vector3(0f, -bandY, 0f), bandSize);
        }
    }
}

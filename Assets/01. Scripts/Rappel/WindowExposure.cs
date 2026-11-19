using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Rappel
{
    // 창문 정면 노출 (표적이 있는 창문마다 하나. 03 측면 경계 연습, 05 임무)
    // 머리가 창문 정면 범위(width × height × 앞뒤 depth) 안이면:
    //   A. 고정 거절: 상태머신에 "고정 막음"을 알림 → 그 안에서 고정을 시도하면 거절 + 경고 진동
    //   B. 노출 실패: 멈춰 있거나(벽에서 거의 안 내려감) 고정/총 든 상태로 exposeLimit초 머물면 StageRule에 알림 → 실패
    //      머무는 동안 양손에 점점 세지는 경고 진동. 범위를 벗어나거나 움직이면 시간이 0으로 돌아감
    //   점프 중이거나 빠르게 지나가면 노출로 안 침
    //   씬의 표적을 다 맞히면 이 컴포넌트가 스스로 꺼짐 (고정 막음, 노출 실패 모두 끝)
    // 이 오브젝트를 창문 가운데에 두고, 파란 화살표(Z)가 벽 면과 수직이 되게 돌려 둠 (WindowEntry와 같은 방식)
    // HandBrake(-60)가 고정을 요청하기 전에 이번 프레임 "막음"을 알려 주려고 먼저 실행
    [DefaultExecutionOrder(-70)]
    public class WindowExposure : MonoBehaviour
    {
        [Header("연결 (비우면 씬에서 찾음)")]
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private StageRule stageRule;
        [SerializeField] private RappelDescent descent;
        [SerializeField] private Transform head;

        [Header("범위 (m)")]
        [SerializeField] private float width = 2f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float depth = 1.5f;          // 창문 면에서 앞뒤로 이 거리 안

        [Header("노출 실패")]
        [SerializeField] private float exposeLimit = 1.5f;    // 이 시간(초) 머물면 실패
        [SerializeField] private float stopSpeed = 0.3f;      // 벽에서 이 속도(m/s)보다 느리면 "멈춰 있음"

        [Header("경고 진동 (비우면 안 함)")]
        [SerializeField] private HapticImpulsePlayer leftHaptic;
        [SerializeField] private HapticImpulsePlayer rightHaptic;
        [SerializeField] private float warnInterval = 0.25f;  // 노출 중 진동 간격 (초)
        [SerializeField] private Vector2 warnAmplitude = new Vector2(0.2f, 0.8f);   // 노출 시작 → 실패 직전
        [SerializeField] private float refuseAmplitude = 0.6f; // 고정 거절 진동
        [SerializeField] private float refuseDuration = 0.3f;

        [Header("디버그 (읽기용)")]
        [SerializeField] private bool inside;
        [SerializeField] private bool exposed;
        [SerializeField] private float exposedTime;

        private float warnTimer;
        private bool reported;

        void Awake()
        {
            if (stateMachine == null)
                stateMachine = FindFirstObjectByType<RappelStateMachine>();
            if (stageRule == null)
                stageRule = FindFirstObjectByType<StageRule>();
            if (descent == null)
                descent = FindFirstObjectByType<RappelDescent>();
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
        }

        void OnEnable()
        {
            if (stateMachine != null)
                stateMachine.LockRefused += OnLockRefused;
        }

        void OnDisable()
        {
            if (stateMachine != null)
            {
                stateMachine.LockRefused -= OnLockRefused;
                stateMachine.SetLockBlocked(this, false);
            }
        }

        void Update()
        {
            if (stateMachine == null || head == null)
                return;

            // 표적을 다 맞히면 이 창문은 더 위험하지 않음 → 스스로 꺼짐 (OnDisable에서 고정 막음도 풀림)
            if (stageRule != null && stageRule.AllTargetsHit)
            {
                enabled = false;
                return;
            }

            RappelState state = stateMachine.State;

            // 창문 기준 머리 위치 (회전만 반영, 스케일 무시)
            Vector3 local = Quaternion.Inverse(transform.rotation) * (head.position - transform.position);
            inside = Mathf.Abs(local.x) <= width * 0.5f
                  && Mathf.Abs(local.y) <= height * 0.5f
                  && Mathf.Abs(local.z) <= depth
                  && state != RappelState.Ready && state != RappelState.Finished;

            // A. 고정 막기
            stateMachine.SetLockBlocked(this, inside);

            // B. 노출 시간
            float speed = descent != null ? descent.DescentSpeed : 0f;
            exposed = inside && ((state == RappelState.OnWall && speed < stopSpeed)
                                 || state == RappelState.Locked
                                 || state == RappelState.GunDrawn);

            if (!exposed)
            {
                exposedTime = 0f;
                warnTimer = 0f;
                return;
            }

            exposedTime += Time.deltaTime;

            // 경고 진동: 노출 시작 순간 바로 한 번, 그 뒤 warnInterval마다 (점점 세게)
            warnTimer -= Time.deltaTime;
            if (warnTimer <= 0f)
            {
                warnTimer = warnInterval;
                float t = exposeLimit > 0f ? Mathf.Clamp01(exposedTime / exposeLimit) : 1f;
                SendHaptic(Mathf.Lerp(warnAmplitude.x, warnAmplitude.y, t), warnInterval * 0.6f);
            }

            if (exposedTime >= exposeLimit && !reported && stageRule != null)
            {
                reported = true;
                Debug.Log($"[Rappel] 창문 정면 노출({name}) {exposeLimit}초 → 발각");
                stageRule.NotifyExposed();
            }
        }

        private void OnLockRefused()
        {
            // 이 창문 때문에 막힌 경우만 진동
            if (inside)
                SendHaptic(refuseAmplitude, refuseDuration);
        }

        private void SendHaptic(float amplitude, float duration)
        {
            if (leftHaptic != null)
                leftHaptic.SendHapticImpulse(amplitude, duration);
            if (rightHaptic != null)
                rightHaptic.SendHapticImpulse(amplitude, duration);
        }

        // Scene 창에 범위 표시: 주황 = 창문 정면 (고정 금지, 오래 머물면 실패)
        void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, depth * 2f));
        }
    }
}

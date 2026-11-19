using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Rappel
{
    // 권총 격발 (권총 오브젝트에 붙임. 총 든 상태일 때만 켜져 있음 → GunHolster가 켜고 끔)
    //   총 든 상태 + 오른손 트리거를 누른 순간 → 총구에서 총열 방향(muzzle의 파란 화살표, +Z)으로 레이
    //   → 처음 맞은 물체가 표적(Target)이면 Hit() → 빨간색 + 맞는 소리 (Target이 처리)
    //   쏠 때마다 총구 연기 이펙트, 격발음, 햅틱, 반동, 슬라이드 (애니메이션 없이 Transform으로)
    //   방아쇠 모델은 트리거를 당긴 만큼 매 프레임 따라 움직임
    public class Gun : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private InputActionProperty triggerAction;   // XRI Right Interaction/Activate Value
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private Transform muzzle;                    // 총구 위치. 파란 화살표(+Z)가 총열 방향

        [Header("레이")]
        [SerializeField] private float maxDistance = 100f;           // 이 거리까지만 맞음 (m)
        [SerializeField] private LayerMask hitLayers = ~0;           // 레이가 부딪힐 레이어 (기본: 전부)
        [SerializeField] private float triggerPressPoint = 0.5f;     // 트리거를 이만큼 넘게 당기면 "누름"

        [Header("총구 이펙트")]
        [SerializeField] private GameObject muzzleFx;                 // GunShot_Smoke_FX
        [SerializeField] private float muzzleFxLifetime = 3f;         // 이 시간 뒤 이펙트 오브젝트 삭제 (초)

        [Header("격발음 (파일은 나중에. 비우면 안 남. 맞는 소리는 Target에서)")]
        [SerializeField] private AudioSource audioSource;             // 권총에 붙인 AudioSource
        [SerializeField] private AudioClip shotClip;                  // 총소리

        [Header("격발 햅틱")]
        [SerializeField] private HapticImpulsePlayer haptic;          // Right Controller의 Haptic Impulse Player
        [SerializeField] private float hapticAmplitude = 0.7f;
        [SerializeField] private float hapticDuration = 0.08f;

        // 아래 움직임은 모두 각 오브젝트의 "로컬" 기준 (Scene 창 Local 모드에서 보이는 축)
        // 축 방향은 모델마다 달라서 플레이 중 Inspector에서 값을 바꿔 보며 맞춤
        [Header("반동 (쏠 때 뒤로 밀리고 총구가 들렸다 돌아옴)")]
        [SerializeField] private Transform recoilModel;               // 움직일 권총 모델 (비우면 이 오브젝트)
        [SerializeField] private Vector3 recoilOffset = new Vector3(0f, 0f, -0.03f);  // 가장 많이 밀린 위치 (m)
        [SerializeField] private Vector3 recoilEuler = new Vector3(-8f, 0f, 0f);      // 가장 많이 들린 각도 (도)
        [SerializeField] private float recoilDuration = 0.15f;        // 원래 자리로 돌아오는 시간 (초)

        [Header("슬라이드 (쏠 때 뒤로 갔다 돌아옴)")]
        [SerializeField] private Transform slide;                     // SM_Wep_PistolSwat_Slide_01
        [SerializeField] private Vector3 slideOffset = new Vector3(0f, 0f, -0.02f);   // 가장 많이 밀린 위치 (m)
        [SerializeField] private float slideDuration = 0.08f;         // 돌아오는 시간 (초)

        [Header("방아쇠 (트리거 당긴 만큼 따라 움직임)")]
        [SerializeField] private Transform triggerModel;              // SM_Wep_PistolSwat_Trigger_01
        [SerializeField] private Vector3 triggerPullOffset = new Vector3(0f, 0f, -0.005f); // 끝까지 당겼을 때 위치 (m)
        [SerializeField] private Vector3 triggerPullEuler = Vector3.zero;                  // 끝까지 당겼을 때 회전 (도)

        [Header("디버그 (읽기용)")]
        [SerializeField] private float triggerValue;
        [SerializeField] private string lastHitName;                  // 마지막으로 레이가 맞은 물체 이름

        private bool wasPressed;   // 지난 프레임에 트리거가 눌려 있었는지

        // 움직이기 전 원래 자리 (처음 켜질 때 한 번 기억)
        private bool hasBasePose;
        private Vector3 recoilBasePos, slideBasePos, triggerBasePos;
        private Quaternion recoilBaseRot, triggerBaseRot;
        private float shotElapsed = float.MaxValue;   // 마지막으로 쏜 뒤 지난 시간 (초)

        void Awake()
        {
            if (recoilModel == null)
                recoilModel = transform;
        }

        void OnEnable()
        {
            triggerAction.action?.Enable();

            // 총을 꺼내는 순간 이미 트리거를 당기고 있었으면 바로 쏘지 않음
            wasPressed = ReadTrigger() >= triggerPressPoint;

            if (!hasBasePose)
            {
                SaveBasePose();
                hasBasePose = true;
            }
        }

        void OnDisable()
        {
            // 총을 넣을 때 반동 중이었으면 원래 자리로
            shotElapsed = float.MaxValue;
            if (hasBasePose)
                ApplyPose();
        }

        void Update()
        {
            triggerValue = ReadTrigger();
            bool pressed = triggerValue >= triggerPressPoint;
            bool pressedThisFrame = pressed && !wasPressed;
            wasPressed = pressed;

            if (pressedThisFrame && stateMachine != null && stateMachine.State == RappelState.GunDrawn)
                Fire();

            shotElapsed += Time.deltaTime;
            ApplyPose();
        }

        private void SaveBasePose()
        {
            recoilBasePos = recoilModel.localPosition;
            recoilBaseRot = recoilModel.localRotation;
            if (slide != null)
                slideBasePos = slide.localPosition;
            if (triggerModel != null)
            {
                triggerBasePos = triggerModel.localPosition;
                triggerBaseRot = triggerModel.localRotation;
            }
        }

        // 반동, 슬라이드, 방아쇠 위치를 이번 프레임 값으로 맞춤
        private void ApplyPose()
        {
            // 쏜 순간 1 → 시간이 지나며 0 (처음엔 빨리, 끝에선 천천히 돌아옴)
            float recoil = Kick(shotElapsed, recoilDuration);
            recoilModel.localPosition = recoilBasePos + recoilOffset * recoil;
            recoilModel.localRotation = recoilBaseRot * Quaternion.Euler(recoilEuler * recoil);

            if (slide != null)
                slide.localPosition = slideBasePos + slideOffset * Kick(shotElapsed, slideDuration);

            if (triggerModel != null)
            {
                float pull = Mathf.Clamp01(triggerValue);
                triggerModel.localPosition = triggerBasePos + triggerPullOffset * pull;
                triggerModel.localRotation = triggerBaseRot * Quaternion.Euler(triggerPullEuler * pull);
            }
        }

        private static float Kick(float elapsed, float duration)
        {
            if (duration <= 0f || elapsed >= duration)
                return 0f;
            float t = 1f - elapsed / duration;
            return t * t;
        }

        private void Fire()
        {
            if (muzzle == null)
                return;

            shotElapsed = 0f;   // 반동, 슬라이드 시작

            // 총구 연기 (총을 따라가지 않고 그 자리에 남음)
            if (muzzleFx != null)
            {
                GameObject fx = Instantiate(muzzleFx, muzzle.position, muzzle.rotation);
                Destroy(fx, muzzleFxLifetime);
            }

            if (audioSource != null && shotClip != null)
                audioSource.PlayOneShot(shotClip);

            if (haptic != null)
                haptic.SendHapticImpulse(hapticAmplitude, hapticDuration);

            // 총구에서 총열 방향으로 레이. 트리거 콜라이더는 무시
            if (!Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
            {
                lastHitName = "(없음)";
                Debug.DrawRay(muzzle.position, muzzle.forward * maxDistance, Color.yellow, 1f);
                return;
            }

            lastHitName = hit.collider.name;
            Debug.DrawLine(muzzle.position, hit.point, Color.red, 1f);

            Target target = hit.collider.GetComponentInParent<Target>();
            if (target != null)
                target.Hit();
        }

        private float ReadTrigger()
        {
            return triggerAction.action != null ? triggerAction.action.ReadValue<float>() : 0f;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Rappel
{
    // 총 꺼내기 / 넣기 (오른손)
    //   고정 중 + 오른손 골반 안 + 그립을 새로 쥐는 순간 → DrawGun() 요청 (고정 → 총 든 상태)
    //   총 든 상태 + 오른손 골반 안 + 그립을 놓는 순간  → HolsterGun() 요청 (총 든 상태 → 고정)
    //   골반 밖에서 그립을 놓으면 아무 일 없음 (총은 계속 들고 있음)
    // 총 모델 보이기 / 컨트롤러 모델 숨기기는 상태만 보고 정함 (총 든 상태일 때만 총이 보이고 컨트롤러가 숨음)
    // PelvisCheck가 골반 안인지 먼저 계산한 뒤에 읽도록 뒤에 실행
    [DefaultExecutionOrder(2)]
    public class GunHolster : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private InputActionProperty gripAction;      // XRI Right Interaction/Select Value
        [SerializeField] private PelvisCheck pelvis;                  // 오른손이 골반 안인지
        [SerializeField] private RappelStateMachine stateMachine;

        [Header("보이기 / 숨기기")]
        [SerializeField] private GameObject gun;                      // Right Controller 자식으로 둔 권총 (Gun 스크립트 붙은 것)
        [SerializeField] private GameObject controllerVisual;         // Right Controller Visual (총 든 동안 숨김)

        [Header("그립")]
        [SerializeField] private float gripPressPoint = 0.5f;        // 이 값 이상이면 "쥠", 미만이면 "놓음"

        [Header("꺼내기 / 넣기 햅틱")]
        [SerializeField] private HapticImpulsePlayer haptic;          // Right Controller의 Haptic Impulse Player
        [SerializeField] private float hapticAmplitude = 0.5f;
        [SerializeField] private float hapticDuration = 0.1f;

        [Header("디버그 (읽기용)")]
        [SerializeField] private float gripValue;
        [SerializeField] private bool isGripping;

        private bool wasGripping;   // 지난 프레임에 쥐고 있었는지 (쥔 순간 / 놓은 순간만 잡으려고)

        void OnEnable()
        {
            gripAction.action?.Enable();
            if (stateMachine != null)
                stateMachine.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (stateMachine != null)
                stateMachine.StateChanged -= OnStateChanged;
        }

        void Start()
        {
            // 시작 상태에 맞게 총 / 컨트롤러 모양 맞춤
            ApplyVisual(stateMachine != null ? stateMachine.State : RappelState.Ready);

            // 시작할 때 이미 쥐고 있었으면 "쥔 순간"으로 치지 않음
            gripValue = ReadGrip();
            wasGripping = gripValue >= gripPressPoint;
        }

        void Update()
        {
            gripValue = ReadGrip();
            isGripping = gripValue >= gripPressPoint;
            bool pressedThisFrame = isGripping && !wasGripping;
            bool releasedThisFrame = !isGripping && wasGripping;
            wasGripping = isGripping;

            if (stateMachine == null || pelvis == null || !pelvis.IsAtPelvis)
                return;

            bool changed = false;
            if (pressedThisFrame && stateMachine.State == RappelState.Locked)
                changed = stateMachine.DrawGun();
            else if (releasedThisFrame && stateMachine.State == RappelState.GunDrawn)
                changed = stateMachine.HolsterGun();

            if (changed && haptic != null)
                haptic.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }

        private float ReadGrip()
        {
            return gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;
        }

        private void OnStateChanged(RappelState prev, RappelState next)
        {
            ApplyVisual(next);
        }

        private void ApplyVisual(RappelState state)
        {
            bool gunDrawn = state == RappelState.GunDrawn;
            if (gun != null)
                gun.SetActive(gunDrawn);
            if (controllerVisual != null)
                controllerVisual.SetActive(!gunDrawn);
        }
    }
}

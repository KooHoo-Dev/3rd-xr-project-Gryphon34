using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Rappel
{
    // 한 손의 제동 상태: 그립 값 + 제동 위치에 있는지
    // 오른손용, 왼손용으로 컴포넌트를 2개 붙여서 인스펙터 값만 다르게 씀
    // 오른손은 canLock을 켜서 "줄 끼워 고정"도 담당
    //   8자고리 반경 안 + 그립 + 트리거 누른 순간 → 고정/해제 전환
    //   고정 중에는 실제 그립과 상관없이 그립 1 + 제동 위치로 내보냄 (= 완전 제동)
    // BrakeManager보다 먼저 실행되도록 순서를 앞당김 (같은 프레임 값으로 손을 고르게)
    [DefaultExecutionOrder(-60)]
    public class HandBrake : MonoBehaviour
    {
        [Header("입력")]
        [SerializeField] private InputActionProperty gripAction;  // XRI Right/Left Interaction/Select Value

        [Header("제동 위치")]
        [SerializeField] private bool anyPosition;                // 켜면 위치 상관없이 항상 제동 위치
        [SerializeField] private Transform controller;            // Right/Left Controller
        [SerializeField] private Transform brakePoint;            // 제동 위치 기준점
        [SerializeField] private float brakeRadius = 0.2f;        // 이 거리 안이면 제동 위치 (m)

        [Header("줄 끼워 고정 (오른손만 켬)")]
        [SerializeField] private bool canLock;                    // 켜면 이 손으로 고정/해제 가능
        [SerializeField] private InputActionProperty triggerAction; // XRI Right Interaction/Activate Value
        [SerializeField] private Transform lockPoint;             // 8자고리 모델 (Black Figure Eight)
        [SerializeField] private float lockRadius = 0.2f;         // 이 거리 안이면 8자고리에 대고 있다고 봄 (m)
        [SerializeField] private float lockGrip = 0.9f;           // 고정/해제할 때 필요한 그립 값
        [SerializeField] private float triggerPressPoint = 0.5f;  // 트리거를 이만큼 넘게 당기면 "누름"

        [Header("고정 햅틱")]
        [SerializeField] private HapticImpulsePlayer haptic;      // Right Controller의 Haptic Impulse Player
        [SerializeField] private float hapticAmplitude = 0.8f;    // 진동 세기 0~1
        [SerializeField] private float hapticDuration = 0.2f;     // 진동 길이 (초)

        [Header("디버그 (읽기용)")]
        [SerializeField] private float gripValue;
        [SerializeField] private float distance;
        [SerializeField] private bool isAtBrake;
        [SerializeField] private float triggerValue;
        [SerializeField] private float lockDistance;
        [SerializeField] private bool isLocked;

        private bool wasTriggerPressed;   // 지난 프레임에 트리거가 눌려 있었는지 (누른 순간만 잡으려고)

        public float Grip => gripValue;
        public bool IsAtBrake => isAtBrake;
        public bool IsLocked => isLocked;

        void OnEnable()
        {
            gripAction.action?.Enable();
            if (canLock)
                triggerAction.action?.Enable();
        }

        void Update()
        {
            gripValue = gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;

            // 고정/해제 확인 (실제 그립 값으로 판단)
            if (canLock)
                UpdateLock();

            // 고정 중이면 그립과 위치 상관없이 완전 제동
            if (isLocked)
            {
                gripValue = 1f;
                isAtBrake = true;
                return;
            }

            if (anyPosition)
            {
                isAtBrake = true;
                return;
            }

            if (controller == null || brakePoint == null)
            {
                isAtBrake = false;
                return;
            }

            distance = Vector3.Distance(controller.position, brakePoint.position);
            isAtBrake = distance <= brakeRadius;
        }

        void UpdateLock()
        {
            triggerValue = triggerAction.action != null ? triggerAction.action.ReadValue<float>() : 0f;
            bool triggerPressed = triggerValue >= triggerPressPoint;
            bool pressedThisFrame = triggerPressed && (wasTriggerPressed == false);
            wasTriggerPressed = triggerPressed;

            if (controller == null || lockPoint == null)
                return;

            lockDistance = Vector3.Distance(controller.position, lockPoint.position);

            // 8자고리 반경 안 + 그립 + 트리거를 누른 순간에만 전환
            if (!pressedThisFrame || lockDistance > lockRadius || gripValue < lockGrip)
                return;

            isLocked = !isLocked;
            Debug.Log(isLocked ? "[Rappel] 줄 고정" : "[Rappel] 줄 고정 해제");

            if (haptic != null)
                haptic.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }
    }
}

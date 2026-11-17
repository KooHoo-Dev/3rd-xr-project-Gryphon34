using UnityEngine;
using UnityEngine.InputSystem;

namespace Rappel
{
    // 한 손의 제동 상태: 그립 값 + 제동 위치에 있는지
    // 오른손용, 왼손용으로 컴포넌트를 2개 붙여서 인스펙터 값만 다르게 씀
    // BrakeManager보다 먼저 실행되도록 순서를 앞당김 (같은 프레임 값으로 손을 고르게)
    [DefaultExecutionOrder(-60)]
    public class HandBrake : MonoBehaviour
    {
        [Header("입력")]
        [SerializeField] private InputActionProperty gripAction;  // XRI Right/Left Interaction/Select Value

        [Header("제동 위치")]
        [SerializeField] private bool anyPosition;                // 켜면 위치 상관없이 항상 제동 위치
        [SerializeField] private Transform controller;            // Right/Left Controller
        [SerializeField] private Transform brakePoint;            // 제동 위치 기준점 (예: 8자고리 모델, PelvisMarker)
        [SerializeField] private float brakeRadius = 0.2f;        // 이 거리 안이면 제동 위치 (m)

        [Header("디버그 (읽기용)")]
        [SerializeField] private float gripValue;
        [SerializeField] private float distance;
        [SerializeField] private bool isAtBrake;

        public float Grip => gripValue;
        public bool IsAtBrake => isAtBrake;

        void OnEnable()
        {
            gripAction.action?.Enable();
        }

        void Update()
        {
            gripValue = gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;

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
    }
}

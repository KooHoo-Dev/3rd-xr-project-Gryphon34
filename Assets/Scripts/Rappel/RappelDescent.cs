using UnityEngine;
using UnityEngine.InputSystem;

namespace Rappel
{
    /// <summary>
    /// 그립 값으로 하강/제동을 계산해 리그(XR Origin)를 아래로 움직인다.
    /// - gripValue가 임계값 이하: 제동 0 (자유 하강)
    /// - 임계값 초과: 초과분에 비례해 제동력 증가
    /// - 제동력 = 쥔 정도 x (기본 제동력 + 속도 마찰 x 속도)
    /// - 가속도 = 중력 - 제동력 / 몸무게
    /// </summary>
    public class RappelDescent : MonoBehaviour
    {
        [Header("입력")]
        [SerializeField] InputActionProperty gripAction;

        [Header("움직일 대상")]
        [SerializeField] Transform rig;

        [Header("물리 값")]
        [SerializeField] float bodyMass = 70f;          // kg
        [SerializeField] float gravity = 9.81f;         // m/s^2
        [SerializeField] float baseBrakeForce = 900f;   // N, 속도와 상관없이 걸리는 제동
        [SerializeField] float speedFriction = 470f;    // N per m/s, 빠를수록 더 걸리는 제동

        [Header("그립")]
        [Range(0f, 1f)]
        [SerializeField] float gripThreshold = 0.2f;

        [Header("안전")]
        [SerializeField] float maxDescentSpeed = 3f;
        [SerializeField] float groundY = 0f;

        [Header("디버그 (읽기용)")]
        [SerializeField] float gripValue;     // 현재 그립 값 0~1
        [SerializeField] float descentSpeed;  // 현재 하강 속도 m/s (아래 방향이 +)
        [SerializeField] float brakeForce;    // 현재 제동력 N

        public float GripValue => gripValue;
        public float DescentSpeed => descentSpeed;

        void Awake()
        {
            if (rig == null)
                rig = transform;
        }

        void OnEnable()
        {
            gripAction.action?.Enable();
        }

        void OnDisable()
        {
            gripAction.action?.Disable();
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // 1. 그립 값 읽기
            gripValue = gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;

            // 2. 쥔 정도: 임계값 이하면 0, 끝까지 쥐면 1
            //    예) 임계값 0.2일 때 그립 0.2 → 0, 0.6 → 0.5, 1.0 → 1
            float gripRatio = 0f;
            if (gripValue > gripThreshold)
                gripRatio = (gripValue - gripThreshold) / (1f - gripThreshold);

            // 3. 제동력 = 쥔 정도 x (기본 제동력 + 속도 마찰 x 현재 속도)
            //    빨리 내려갈수록 마찰이 커져서, 덜 쥐면 일정한 속도로 슬슬 내려감
            brakeForce = gripRatio * (baseBrakeForce + speedFriction * descentSpeed);

            // 4. 가속도 = (몸무게 x 중력 - 제동력) / 몸무게   (힘 = 질량 x 가속도)
            float acceleration = (bodyMass * gravity - brakeForce) / bodyMass;

            // 5. 이번 프레임 동안 속도가 바뀌는 양 = 가속도 x 프레임 시간
            descentSpeed += acceleration * dt;

            // 제동은 속도를 0까지만 줄일 수 있음 (위로 끌어올리지 않음)
            descentSpeed = Mathf.Clamp(descentSpeed, 0f, maxDescentSpeed);

            // 6. 리그 이동: 이번 프레임에 내려가는 거리 = 속도 x 프레임 시간
            Vector3 pos = rig.position;
            pos.y -= descentSpeed * dt;
            if (pos.y <= groundY)
            {
                pos.y = groundY;
                descentSpeed = 0f;
            }
            rig.position = pos;
        }
    }
}

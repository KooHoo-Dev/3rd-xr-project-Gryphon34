using UnityEngine;

namespace Rappel
{
    public class RappelDescent : MonoBehaviour
    {
        [Header("입력")]
        [SerializeField] private BrakeManager brakeManager;   // 두 손 중 제동에 쓸 그립 값을 받아 옴

        [Header("움직일 대상")]
        [SerializeField] private Transform rig;

        [Header("그립 → 목표 속도 (가로: 그립 0~1, 세로: m/s)")]
        [SerializeField] private AnimationCurve gripToSpeed = new AnimationCurve(
            new Keyframe(0f, 3f),
            new Keyframe(0.3f, 2f),
            new Keyframe(0.5f, 0.8f),
            new Keyframe(0.7f, 0f),
            new Keyframe(1f, 0f));

        [Header("가감속 (m/s^2)")]
        [SerializeField] private float speedUpRate = 1.5f;   // 풀었을 때 속도가 붙는 빠르기
        [SerializeField] private float slowDownRate = 3f;    // 쥐었을 때 멈추는 빠르기

        [Header("안전")]
        [SerializeField] private float maxDescentSpeed = 3f;
        [SerializeField] private float groundY = 0f;

        [Header("상태")]
        [SerializeField] private bool canDescend;     // PelvisCheck가 true로 바꿔 줌. false면 하강 안 함

        [Header("디버그 (읽기용)")]
        [SerializeField] private float gripValue;     // 현재 그립 값 0~1
        [SerializeField] private float targetSpeed;   // 곡선에서 읽은 목표 속도 m/s
        [SerializeField] private float descentSpeed;  // 현재 하강 속도 m/s (아래 방향이 +)

        public float GripValue => gripValue;
        public float DescentSpeed => descentSpeed;

        public bool CanDescend
        {
            get => canDescend;
            set => canDescend = value;
        }

        void Awake()
        {
            if (rig == null)
                rig = transform;
        }

        void Update()
        {
            // 0. 골반 그립 확인 전에는 하강하지 않음
            if (!canDescend)
                return;

            // BrakeManager가 연결 안 돼 있으면 그립 값을 알 수 없으니 하강하지 않음 (그립 0 = 최고 속도로 떨어지는 것 방지)
            if (brakeManager == null)
                return;

            float dt = Time.deltaTime;

            // 1. 그립 값 읽기: BrakeManager가 고른 손(제동 위치 + 더 세게 쥔 손)의 값
            gripValue = brakeManager.BrakeGrip;

            // 2. 곡선에서 목표 속도 읽기 (곡선이 0 아래나 최대 속도 위로 튀어도 잘라냄)
            targetSpeed = Mathf.Clamp(gripToSpeed.Evaluate(gripValue), 0f, maxDescentSpeed);

            // 3. 현재 속도를 목표 속도 쪽으로 가속도 제한을 두고 이동
            //    빨라질 때와 느려질 때 빠르기를 따로 씀
            float rate = targetSpeed > descentSpeed ? speedUpRate : slowDownRate;
            descentSpeed = Mathf.MoveTowards(descentSpeed, targetSpeed, rate * dt);

            // 4. 리그 이동: 이번 프레임에 내려가는 거리 = 속도 x 프레임 시간
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

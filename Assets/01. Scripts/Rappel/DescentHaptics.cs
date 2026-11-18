using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace Rappel
{
    // 하강하는 동안 제동 위치에 있는 손에 진동 (그립과 상관없음, 줄이 손에서 미끄러지는 느낌)
    // 오른손은 Any Position이라 항상 제동 위치 → 하강 중 항상 진동. 왼손은 8자고리 근처일 때만
    // 멈춰 있으면(속도 0, 고정 포함) 진동 없음
    // 세기: 조금이라도 내려가면 minAmplitude, 최대 속도에서 maxAmplitude
    //   보낼 때마다 ±roughness 비율만큼 랜덤으로 흔들어서 줄이 거칠게 긁히는 느낌
    // 점프 → 벽으로 돌아오는 순간(상태머신 이벤트) 양손에 짧고 강한 진동
    // 값을 읽기만 함 (HandBrake, RappelDescent → 여기). 그래서 RappelDescent 뒤에 실행
    // Quest 햅틱은 "세기 + 길이"를 한 번씩 보내는 방식 → sendInterval마다 조금 더 긴 진동을 다시 보내서 이어지게 함
    [DefaultExecutionOrder(10)]
    public class DescentHaptics : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private RappelDescent descent;
        [SerializeField] private RappelStateMachine stateMachine;     // 점프 → 벽 복귀 순간을 받음
        [SerializeField] private HandBrake rightHand;                 // Brakes/RightHand
        [SerializeField] private HandBrake leftHand;                  // Brakes/LeftHand
        [SerializeField] private HapticImpulsePlayer rightHaptic;     // Right Controller의 Haptic Impulse Player
        [SerializeField] private HapticImpulsePlayer leftHaptic;      // Left Controller의 Haptic Impulse Player

        [Header("하강 세기 (0~1)")]
        [SerializeField] private float minAmplitude = 0.1f;          // 조금이라도 내려갈 때
        [SerializeField] private float maxAmplitude = 0.5f;          // 최대 속도일 때
        [Range(0f, 1f)]
        [SerializeField] private float roughness = 0.3f;             // 보낼 때마다 세기를 ±이 비율만큼 랜덤으로 (0이면 일정)

        [Header("속도 (m/s)")]
        [SerializeField] private float minSpeed = 0.05f;             // 이보다 느리면 정지로 보고 진동 안 함
        [SerializeField] private float speedForMax = 3f;             // 이 속도에서 maxAmplitude (RappelDescent 최대 속도와 맞춤)

        [Header("보내는 간격 (초)")]
        [SerializeField] private float sendInterval = 0.1f;          // 이 간격마다 다시 보냄
        [SerializeField] private float pulseDuration = 0.15f;        // 한 번 보낼 때 길이 (간격보다 길게 → 끊김 없음)

        [Header("점프 복귀 (벽에 닿는 순간)")]
        [SerializeField] private float impactAmplitude = 1f;         // 세기 0~1
        [SerializeField] private float impactDuration = 0.1f;        // 길이 (초). 이 동안은 하강 진동을 안 보냄 (덮어쓰지 않게)

        [Header("디버그 (읽기용)")]
        [SerializeField] private float amplitude;
        [SerializeField] private bool rightAtBrake;
        [SerializeField] private bool leftAtBrake;

        private float timer;        // 다음 하강 진동까지 남은 시간
        private float blockTimer;   // 복귀 진동이 끝날 때까지 남은 시간

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

        void Update()
        {
            if (blockTimer > 0f)
                blockTimer -= Time.deltaTime;

            if (descent == null)
                return;

            rightAtBrake = rightHand != null && rightHand.IsAtBrake;
            leftAtBrake = leftHand != null && leftHand.IsAtBrake;

            float speed = descent.DescentSpeed;

            // 정지(고정 포함)거나 제동 위치에 손이 없으면 보내지 않음 → 마지막 진동이 끝나면 멈춤
            if (speed < minSpeed || (!rightAtBrake && !leftAtBrake))
            {
                amplitude = 0f;
                timer = 0f;   // 다시 내려가기 시작하면 바로 보내게
                return;
            }

            float t = Mathf.Clamp01(speed / speedForMax);
            amplitude = Mathf.Lerp(minAmplitude, maxAmplitude, t);

            // 복귀 진동 중에는 하강 진동을 보내지 않음
            if (blockTimer > 0f)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            timer = sendInterval;

            // 거칠게: 이번에 보낼 세기만 랜덤으로 흔듦
            float rough = Mathf.Clamp01(amplitude * Random.Range(1f - roughness, 1f + roughness));

            if (rightAtBrake && rightHaptic != null)
                rightHaptic.SendHapticImpulse(rough, pulseDuration);

            if (leftAtBrake && leftHaptic != null)
                leftHaptic.SendHapticImpulse(rough, pulseDuration);
        }

        // 점프 → 벽 (왕복이 끝나 벽에 닿는 순간) 양손에 한 번
        private void OnStateChanged(RappelState prev, RappelState next)
        {
            if (prev != RappelState.Jumping || next != RappelState.OnWall)
                return;

            if (rightHaptic != null)
                rightHaptic.SendHapticImpulse(impactAmplitude, impactDuration);

            if (leftHaptic != null)
                leftHaptic.SendHapticImpulse(impactAmplitude, impactDuration);

            blockTimer = impactDuration;
            timer = 0f;   // 복귀 진동이 끝나면 바로 하강 진동 이어서
        }
    }
}

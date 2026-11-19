using UnityEngine;

namespace Rappel
{
    // 플레이어 몸에서 나는 소리 (XR Origin에 붙임. AudioSource는 비우면 이 오브젝트에 알아서 만듦)
    //   - 발소리: 벽(OnWall)에서 내려가는 중에만. stepDistance만큼 내려갈 때마다 한 번 (너무 빠르면 minStepInterval 간격)
    //   - 점프 소리: 벽 → 점프 순간(점프 버튼), 점프 → 벽 순간(벽에 다시 닿음)
    //   - 로프 마찰음: 계속 반복 재생, 하강 속도에 따라 음량/음높이 (멈추면 무음)
    // 바람, 도시 소리는 코드 없이 씬의 AudioSource(Loop)로 함
    // RappelDescent가 이번 프레임 속도를 정한 뒤에 읽도록 뒤에 실행
    [DefaultExecutionOrder(10)]
    public class RappelSounds : MonoBehaviour
    {
        [Header("연결 (비우면 씬에서 찾음)")]
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private RappelDescent descent;

        [Header("스피커 (비우면 이 오브젝트에 만듦)")]
        [SerializeField] private AudioSource oneShotSource;   // 발소리, 점프 소리
        [SerializeField] private AudioSource ropeSource;      // 로프 마찰음 (반복)

        [Header("발소리")]
        [SerializeField] private AudioClip[] footstepClips;   // 여러 개 넣으면 랜덤으로 골라 씀
        [SerializeField] private float footstepVolume = 0.8f;
        [SerializeField] private float stepDistance = 0.5f;   // 이만큼 내려갈 때마다 한 걸음 (m)
        [SerializeField] private float minStepInterval = 0.3f; // 걸음 사이 최소 시간 (초)
        [SerializeField] private float minWalkSpeed = 0.05f;  // 이 속도 아래면 안 걷는 걸로 봄 (m/s)
        [SerializeField] private Vector2 footstepPitch = new Vector2(0.9f, 1.1f);  // 걸음마다 음높이 랜덤 범위

        [Header("점프")]
        [SerializeField] private AudioClip jumpClip;          // 점프 버튼 (벽 차고 나감)
        [SerializeField] private float jumpVolume = 1f;
        [SerializeField] private AudioClip wallLandClip;      // 벽에 다시 닿음
        [SerializeField] private float wallLandVolume = 1f;

        [Header("로프 마찰음 (반복 재생)")]
        [SerializeField] private AudioClip ropeLoopClip;
        [SerializeField] private float ropeMaxVolume = 0.7f;
        [SerializeField] private float ropeFullSpeed = 3f;     // 이 속도에서 최대 음량 (m/s)
        [SerializeField] private Vector2 ropePitch = new Vector2(0.8f, 1.2f);       // 느릴 때 → 빠를 때

        [Header("디버그 (읽기용)")]
        [SerializeField] private float speed;
        [SerializeField] private float stepAccum;             // 지난 걸음 뒤로 내려간 거리

        private float timeSinceStep;

        void Awake()
        {
            if (stateMachine == null)
                stateMachine = FindFirstObjectByType<RappelStateMachine>();
            if (descent == null)
                descent = FindFirstObjectByType<RappelDescent>();

            if (oneShotSource == null)
                oneShotSource = CreateSource(false);
            if (ropeSource == null)
                ropeSource = CreateSource(true);
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

        void Start()
        {
            if (ropeLoopClip != null)
            {
                ropeSource.clip = ropeLoopClip;
                ropeSource.loop = true;
                ropeSource.volume = 0f;
                ropeSource.Play();
            }
        }

        void Update()
        {
            speed = descent != null ? descent.DescentSpeed : 0f;
            UpdateFootsteps();
            UpdateRope();
        }

        private void UpdateFootsteps()
        {
            timeSinceStep += Time.deltaTime;

            bool walking = stateMachine != null && stateMachine.State == RappelState.OnWall && speed >= minWalkSpeed;
            if (!walking)
            {
                // 멈췄다가 다시 내려가기 시작하면 곧바로 한 걸음 나게
                stepAccum = stepDistance;
                return;
            }

            stepAccum += speed * Time.deltaTime;
            if (stepAccum >= stepDistance && timeSinceStep >= minStepInterval)
            {
                stepAccum = 0f;
                timeSinceStep = 0f;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0)
                return;

            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            if (clip == null)
                return;

            oneShotSource.pitch = Random.Range(footstepPitch.x, footstepPitch.y);
            oneShotSource.PlayOneShot(clip, footstepVolume);
        }

        private void UpdateRope()
        {
            if (ropeLoopClip == null)
                return;

            float t = ropeFullSpeed > 0f ? Mathf.Clamp01(speed / ropeFullSpeed) : 0f;
            ropeSource.volume = ropeMaxVolume * t;
            ropeSource.pitch = Mathf.Lerp(ropePitch.x, ropePitch.y, t);
        }

        private void OnStateChanged(RappelState prev, RappelState next)
        {
            if (prev == RappelState.OnWall && next == RappelState.Jumping)
                PlayOneShot(jumpClip, jumpVolume);
            else if (prev == RappelState.Jumping && next == RappelState.OnWall)
                PlayOneShot(wallLandClip, wallLandVolume);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null)
                return;
            oneShotSource.pitch = 1f;
            oneShotSource.PlayOneShot(clip, volume);
        }

        // 몸에서 나는 소리라 2D (머리 방향과 상관없이 같은 크기)
        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}

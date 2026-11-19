using UnityEngine;

namespace Rappel
{
    // 표적 하나 (Target_1 ~ 5에 하나씩. 콜라이더가 있어야 레이에 맞음, Is Trigger는 끔)
    //   시작할 때 StageRule에 "표적 하나 있음"을 알림 → StageRule이 표적 개수를 셈
    //   Gun이 맞히면 Hit() → 한 번만 빨간색 + 맞는 소리, StageRule에 "맞음"을 알림
    public class Target : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private StageRule stageRule;      // 비우면 씬에서 찾음
        [SerializeField] private Renderer targetRenderer;  // 비우면 이 오브젝트(없으면 자식)에서 찾음

        [Header("맞았을 때")]
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private AudioSource audioSource;  // 표적에 붙인 AudioSource (비우면 이 오브젝트에서 찾음)
        [SerializeField] private AudioClip hitClip;        // 맞는 소리 (파일은 나중에. 비우면 안 남)

        [Header("디버그 (읽기용)")]
        [SerializeField] private bool isHit;

        public bool IsHit => isHit;

        void Awake()
        {
            if (stageRule == null)
                stageRule = FindFirstObjectByType<StageRule>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        void Start()
        {
            if (stageRule != null)
                stageRule.RegisterTarget();
        }

        // 처음 맞았으면 true, 이미 맞은 표적이면 false
        public bool Hit()
        {
            if (isHit)
                return false;

            isHit = true;

            if (targetRenderer != null)
                targetRenderer.material.color = hitColor;

            if (audioSource != null && hitClip != null)
                audioSource.PlayOneShot(hitClip);

            if (stageRule != null)
                stageRule.NotifyTargetHit();

            return true;
        }
    }
}

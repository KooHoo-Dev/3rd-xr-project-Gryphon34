using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rappel
{
    // 종료가 되면 결과 패널을 켜고 "성공"/"실패" 글자 + 그 아래 종료 이유(StageRule.EndReason, 실패일 때)를 보여 줌
    public class EndScreen : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private StageRule stageRule;              // 종료 이유를 읽음 (비우면 씬에서 찾음)
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private GameObject nextButton;

        [Header("문구")]
        [SerializeField] private string successMessage = "성공";
        [SerializeField] private string failMessage = "실패";
        [SerializeField] private float reasonFontSize = 40f;   // 결과 아래 이유 줄 글자 크기

        [Header("표시")]
        [SerializeField] private float showDelay = 0f;

        [Header("씬 이동")]
        [SerializeField] private string menuSceneName = "Menu";

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
            if (stageRule == null)
                stageRule = FindFirstObjectByType<StageRule>();
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

        private void OnStateChanged(RappelState prev, RappelState next)
        {
            if (next != RappelState.Finished)
                return;

            if (resultText != null)
            {
                string text = stateMachine.Succeeded ? successMessage : failMessage;
                string reason = stageRule != null ? stageRule.EndReason : "";
                if (!string.IsNullOrEmpty(reason))
                    text += $"\n<size={reasonFontSize}>{reason}</size>";
                resultText.text = text;
            }

            if (nextButton != null)
                nextButton.SetActive(stateMachine.Succeeded);

            if (showDelay > 0f)
                Invoke(nameof(ShowPanel), showDelay);
            else
                ShowPanel();
        }

        private void ShowPanel()
        {
            if (panel != null)
                panel.SetActive(true);
        }

        // 버튼: 다음 스테이지 (Build Settings 다음 번호 씬). 마지막 씬이면 메뉴로
        public void NextStage()
        {
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next <= 0 || next >= SceneManager.sceneCountInBuildSettings)
            {
                GoToMenu();
                return;
            }

            SceneManager.LoadScene(next);
        }

        // 버튼: 지금 씬 다시 시작
        public void Retry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // 버튼: 메뉴 씬으로
        public void GoToMenu()
        {
            if (string.IsNullOrEmpty(menuSceneName))
                return;

            SceneManager.LoadScene(menuSceneName);
        }
    }
}

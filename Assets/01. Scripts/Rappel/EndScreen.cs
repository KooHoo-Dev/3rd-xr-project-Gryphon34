using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rappel
{
    // 종료가 되면 결과 패널을 켜고 "성공"/"실패" 글자를 보여 줌
    // 버튼은 Inspector에서 Button의 On Click에 Retry(), GoToMenu()를 연결
    // 이 스크립트는 켜져 있는 오브젝트(예: 캔버스)에 붙이고, panel은 처음에 꺼 둠
    public class EndScreen : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private RappelStateMachine stateMachine;
        [SerializeField] private GameObject panel;          // 결과 화면 (처음엔 꺼 둠)
        [SerializeField] private TMP_Text resultText;       // "성공"/"실패"를 쓸 글자

        [Header("문구")]
        [SerializeField] private string successMessage = "성공";
        [SerializeField] private string failMessage = "실패";

        [Header("씬 이동")]
        [SerializeField] private string menuSceneName = "Menu";   // Build Settings에 등록된 메뉴 씬 이름

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);
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
                resultText.text = stateMachine.Succeeded ? successMessage : failMessage;

            if (panel != null)
                panel.SetActive(true);
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

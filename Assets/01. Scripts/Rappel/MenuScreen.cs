using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rappel
{
    // 메뉴 씬의 스테이지 버튼이 부름. Build Settings 번호로 씬을 불러옴 (메뉴 = 0번, 스테이지 1~5 = 1~5번)
    // 버튼 On Click에 LoadStage를 연결하고 숫자 칸에 씬 번호를 넣음 (Tools > Rappel > 메뉴 화면 만들기가 자동으로 해 줌)
    public class MenuScreen : MonoBehaviour
    {
        public void LoadStage(int buildIndex)
        {
            if (buildIndex <= 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning($"[Rappel] Build Settings에 {buildIndex}번 씬이 없어요");
                return;
            }

            SceneManager.LoadScene(buildIndex);
        }
    }
}

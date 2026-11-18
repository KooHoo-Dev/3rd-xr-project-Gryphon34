using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Rappel.EditorTools
{
    // 메뉴 Tools > Rappel > 메뉴 화면 만들기
    // 지금 열린 씬(메뉴 씬)에 메뉴 UI를 만들고 버튼 연결까지 해 줌
    //   - 월드 공간 캔버스 (원점 기준 앞 1.2m, 높이 1.5m, 크기 0.8 x 0.9m)
    //   - 제목 "Rappel" + 스테이지 버튼 5개 (Build Settings 1~5번 씬을 부름)
    //   - 씬에 Event System이 없으면 XR용으로 하나 만듦
    // XR Origin은 이 툴이 만들지 않음 → XRI Starter Assets의 XR Origin (XR Rig) 프리팹을 직접 넣음
    public static class MenuBuilder
    {
        private const string CanvasName = "MenuCanvas";

        private static readonly string[] StageNames =
        {
            "스테이지 1 · 하강 연습",
            "스테이지 2 · 점프 연습",
            "스테이지 3 · 측면 경계 연습",
            "스테이지 4 · 창문 침투 연습",
            "스테이지 5 · 임무",
        };

        [MenuItem("Tools/Rappel/메뉴 화면 만들기")]
        public static void Build()
        {
            const string title = "메뉴 화면 만들기";

            if (Object.FindFirstObjectByType<MenuScreen>() != null)
            {
                EditorUtility.DisplayDialog(title, "씬에 메뉴 화면(MenuScreen)이 이미 있어요. 다시 만들려면 먼저 지워 주세요.", "확인");
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EndScreenBuilder.KoreanFontPath);
            if (font == null)
                Debug.LogWarning($"[Rappel] 한글 글꼴을 못 찾았어요: {EndScreenBuilder.KoreanFontPath} → 글자가 네모로 나올 수 있어요");

            // 1. 캔버스 (월드 공간, XR 레이저로 누를 수 있게)
            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, title);
            canvasGo.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (Camera.main != null)
                canvas.worldCamera = Camera.main;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800f, 900f);            // 픽셀 단위 크기
            canvasRect.localScale = Vector3.one * 0.001f;               // 800 x 900 → 0.8 x 0.9 m
            canvasRect.position = new Vector3(0f, 1.5f, 1.2f);          // 원점에 선 사람 눈앞 1.2m
            canvasRect.rotation = Quaternion.identity;                  // +Z를 보는 사람에게 보임

            MenuScreen menu = canvasGo.AddComponent<MenuScreen>();

            // 2. 배경
            GameObject panel = EndScreenBuilder.CreateUI("Panel", canvasGo.transform);
            EndScreenBuilder.Stretch(panel.GetComponent<RectTransform>());
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            // 3. 제목
            GameObject titleGo = EndScreenBuilder.CreateUI("TitleText", panel.transform);
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.83f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
            if (font != null) titleText.font = font;
            titleText.text = "Rappel";
            titleText.fontSize = 96f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // 4. 스테이지 버튼 5개 (위에서 아래로, 씬 번호 1~5)
            const float top = 0.8f, bottom = 0.04f, gap = 0.02f;
            float rowHeight = (top - bottom - gap * (StageNames.Length - 1)) / StageNames.Length;
            for (int i = 0; i < StageNames.Length; i++)
            {
                float yMax = top - i * (rowHeight + gap);
                Button button = EndScreenBuilder.CreateButton($"Stage{i + 1}Button", panel.transform, StageNames[i], font,
                    new Vector2(0.08f, yMax - rowHeight), new Vector2(0.92f, yMax));
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.fontSize = 44f;

                UnityEventTools.AddIntPersistentListener(button.onClick, menu.LoadStage, i + 1);
            }

            // 5. Event System (씬에 없을 때만)
            EndScreenBuilder.EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Selection.activeGameObject = canvasGo;

            if (Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>() == null)
                Debug.LogWarning("[Rappel] 씬에 XR Origin이 없어요 → XRI Starter Assets의 XR Origin (XR Rig) 프리팹을 넣어 주세요");

            Debug.Log("[Rappel] 메뉴 화면을 만들었어요. 씬을 'Menu' 이름으로 저장하고 Build Settings 맨 위(0번)에 넣으세요.");
        }
    }
}

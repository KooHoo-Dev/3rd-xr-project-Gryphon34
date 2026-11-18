using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Rappel.EditorTools
{
    // 메뉴 Tools > Rappel > 종료 화면 만들기
    // 지금 열린 씬에 종료 화면 UI를 만들고 EndScreen 연결, 버튼 연결까지 해 줌
    //   - XR Origin 자식으로 월드 공간 캔버스 (눈앞 0.7m, 높이 1.5m, 크기 0.8 x 0.5m)
    //   - Panel(처음엔 꺼짐) 안에 결과 글자 + "다시 하기"/"메뉴" 버튼
    //   - 씬에 Event System이 없으면 XR용으로 하나 만듦
    // 만든 뒤 RappelRig에서 Overrides > Apply All 하면 캔버스가 프리팹에 저장됨 (Event System은 씬에만)
    public static class EndScreenBuilder
    {
        private const string CanvasName = "EndScreenCanvas";
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/NotoSansKR-Regular SDF.asset";

        [MenuItem("Tools/Rappel/종료 화면 만들기")]
        public static void Build()
        {
            XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
            {
                EditorUtility.DisplayDialog("종료 화면 만들기", "씬에서 XR Origin을 찾지 못했어요.", "확인");
                return;
            }

            if (origin.transform.Find(CanvasName) != null)
            {
                EditorUtility.DisplayDialog("종료 화면 만들기", $"XR Origin 아래에 {CanvasName}가 이미 있어요. 다시 만들려면 먼저 지워 주세요.", "확인");
                return;
            }

            RappelStateMachine stateMachine = Object.FindFirstObjectByType<RappelStateMachine>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null)
                Debug.LogWarning($"[Rappel] 한글 글꼴을 못 찾았어요: {KoreanFontPath} → 글자가 네모로 나올 수 있어요");

            // 1. 캔버스 (월드 공간, XR 레이저로 누를 수 있게)
            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "종료 화면 만들기");
            canvasGo.transform.SetParent(origin.transform, false);
            canvasGo.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = origin.Camera;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800f, 500f);           // 픽셀 단위 크기
            canvasRect.localScale = Vector3.one * 0.001f;              // 800 x 500 → 0.8 x 0.5 m
            canvasRect.localPosition = new Vector3(0f, 1.5f, 0.7f);    // XR Origin 기준: 눈앞 0.7m, 높이 1.5m
            canvasRect.localRotation = Quaternion.identity;            // XR Origin 정면(벽 쪽)을 보는 사람에게 보임

            // 2. 패널 (배경 + 내용물, 처음엔 꺼 둠)
            GameObject panel = CreateUI("Panel", canvasGo.transform);
            Stretch(panel.GetComponent<RectTransform>());
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            // 3. 결과 글자
            GameObject textGo = CreateUI("ResultText", panel.transform);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.45f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI resultText = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) resultText.font = font;
            resultText.text = "성공";
            resultText.fontSize = 120f;
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.color = Color.white;

            // 4. 버튼 두 개
            Button retryButton = CreateButton("RetryButton", panel.transform, "다시 하기", font, new Vector2(0.08f, 0.1f), new Vector2(0.46f, 0.38f));
            Button menuButton = CreateButton("MenuButton", panel.transform, "메뉴", font, new Vector2(0.54f, 0.1f), new Vector2(0.92f, 0.38f));

            // 5. EndScreen 붙이고 연결 (private 칸이라 SerializedObject로 넣음)
            EndScreen endScreen = canvasGo.AddComponent<EndScreen>();
            SerializedObject so = new SerializedObject(endScreen);
            so.FindProperty("stateMachine").objectReferenceValue = stateMachine;
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("resultText").objectReferenceValue = resultText;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 6. 버튼 On Click 연결 (Inspector에서 연결한 것과 같음)
            UnityEventTools.AddPersistentListener(retryButton.onClick, new UnityAction(endScreen.Retry));
            UnityEventTools.AddPersistentListener(menuButton.onClick, new UnityAction(endScreen.GoToMenu));

            // 에디터에서는 결과를 보면서 위치를 맞출 수 있게 켜 두고, 실행하면 EndScreen.Awake가 끔
            panel.SetActive(true);

            // 7. Event System (씬에 없을 때만)
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Selection.activeGameObject = canvasGo;

            if (stateMachine == null)
                Debug.LogWarning("[Rappel] 씬에서 RappelStateMachine을 못 찾았어요 → EndScreen의 State Machine 칸을 직접 연결해 주세요");

            Debug.Log("[Rappel] 종료 화면을 만들었어요. 위치를 확인하고 RappelRig에서 Overrides > Apply All 한 뒤 씬을 저장하세요.");
        }

        private static GameObject CreateUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = CreateUI(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject labelGo = CreateUI("Label", go.transform);
            Stretch(labelGo.GetComponent<RectTransform>());
            TextMeshProUGUI text = labelGo.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = label;
            text.fontSize = 56f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;

            return button;
        }

        private static void EnsureEventSystem()
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                if (existing.GetComponent<XRUIInputModule>() == null)
                    Debug.LogWarning($"[Rappel] 씬의 Event System({existing.name})에 XR UI Input Module이 없어요 → 컨트롤러 레이저로 버튼이 안 눌릴 수 있어요");
                return;
            }

            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "종료 화면 만들기");
            Debug.Log("[Rappel] XR용 Event System을 만들었어요");
        }
    }
}

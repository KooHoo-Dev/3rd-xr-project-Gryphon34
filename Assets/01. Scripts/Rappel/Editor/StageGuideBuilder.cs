using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Rappel.EditorTools
{
    // 메뉴 Tools > Rappel > 안내창 만들기
    // 지금 열린 씬의 XR Origin 아래에 안내창(GuideCanvas)을 만들고 StageGuide 연결까지 해 줌
    //   - 월드 공간 캔버스 0.5 x 0.6m, Panel(처음엔 꺼짐) 안에 제목 + 본문 글자
    //   - 위치는 실행 중에 Y를 누를 때 StageGuide가 시선 왼쪽으로 옮김 (에디터 위치는 보기용)
    //   - 문구는 실행할 때 StageRule의 씬 종류로 채워짐 (에디터에서는 예시 문구)
    // 만든 뒤 RappelRig에서 Overrides > Apply All 하면 다른 씬에도 들어감
    public static class StageGuideBuilder
    {
        private const string CanvasName = "GuideCanvas";

        [MenuItem("Tools/Rappel/안내창 만들기")]
        public static void Build()
        {
            const string title = "안내창 만들기";

            XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
            {
                EditorUtility.DisplayDialog(title, "씬에서 XR Origin을 찾지 못했어요.", "확인");
                return;
            }

            if (origin.transform.Find(CanvasName) != null)
            {
                EditorUtility.DisplayDialog(title, $"XR Origin 아래에 {CanvasName}가 이미 있어요. 다시 만들려면 먼저 지워 주세요.", "확인");
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EndScreenBuilder.KoreanFontPath);
            if (font == null)
                Debug.LogWarning($"[Rappel] 한글 글꼴을 못 찾았어요: {EndScreenBuilder.KoreanFontPath} → 글자가 네모로 나올 수 있어요");

            // 1. 캔버스 (월드 공간, 누를 버튼은 없으니 레이캐스터 없음)
            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(canvasGo, title);
            canvasGo.transform.SetParent(origin.transform, false);
            canvasGo.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = origin.Camera;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(500f, 600f);              // 픽셀 단위 크기
            canvasRect.localScale = Vector3.one * 0.001f;                 // 500 x 600 → 0.5 x 0.6 m
            canvasRect.localPosition = new Vector3(-0.35f, 1.5f, 0.5f);   // 보기용 (실행 중엔 Y 누를 때 옮겨짐)
            canvasRect.localRotation = Quaternion.Euler(0f, -35f, 0f);

            // 2. 패널 (배경, 처음엔 꺼 둠)
            GameObject panel = EndScreenBuilder.CreateUI("Panel", canvasGo.transform);
            EndScreenBuilder.Stretch(panel.GetComponent<RectTransform>());
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            // 3. 제목 (위쪽 띠)
            TextMeshProUGUI titleText = CreateText("TitleText", panel.transform, font,
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), 36f, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.text = "스테이지 1 · 하강 연습";

            // 4. 본문 (길이가 씬마다 달라서 글자 크기 자동 조절)
            TextMeshProUGUI bodyText = CreateText("BodyText", panel.transform, font,
                new Vector2(0f, 0f), new Vector2(1f, 0.88f), 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 16f;
            bodyText.fontSizeMax = 26f;
            bodyText.margin = new Vector4(24f, 8f, 24f, 20f);
            bodyText.text = "<b>목표</b>\n천천히 내려가서 바닥에 착지\n(실행하면 씬 종류에 맞는 문구로 바뀜)";

            // 5. StageGuide 붙이고 연결 (StageRule은 씬에 있어서 프리팹에 저장 안 되니 비워 둠 → 실행 때 찾음)
            StageGuide guide = canvasGo.AddComponent<StageGuide>();
            SerializedObject so = new SerializedObject(guide);
            so.FindProperty("head").objectReferenceValue = origin.Camera != null ? origin.Camera.transform : null;
            so.FindProperty("stateMachine").objectReferenceValue = Object.FindFirstObjectByType<RappelStateMachine>();
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("bodyText").objectReferenceValue = bodyText;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 에디터에서는 보이게 켜 두고, 실행하면 StageGuide.Awake가 끔
            panel.SetActive(true);

            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Selection.activeGameObject = canvasGo;
            Debug.Log("[Rappel] 안내창을 만들었어요. RappelRig에서 Overrides > Apply All 한 뒤 씬을 저장하세요.");
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font,
            Vector2 anchorMin, Vector2 anchorMax, float size, FontStyles style, TextAlignmentOptions align)
        {
            GameObject go = EndScreenBuilder.CreateUI(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = align;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
    }
}

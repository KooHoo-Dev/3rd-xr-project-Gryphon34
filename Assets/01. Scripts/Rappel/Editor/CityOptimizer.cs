using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rappel.EditorTools
{
    // 메뉴 Tools > Rappel > City 프리팹 최적화
    // Assets/00. Scene/City.prefab 안의 모든 오브젝트에 아래를 한 번에 적용하고 프리팹을 저장함
    //   1. Static (Batching + Occludee, 큰 물체는 Occluder도)
    //      → 빌드할 때 같은 재질끼리 미리 합쳐 그려서 드로우콜이 크게 줄어듦
    //      → 나중에 씬에서 오클루전 컬링을 구우면 가려진 물체를 안 그림
    //   2. 그림자 끄기 (Cast Shadows Off, Receive Shadows 끔)
    //      → 그림자용으로 한 번 더 그리는 일이 없어짐
    // 움직이는 물체(유리, 표적, RappelRig 등)는 City 밖에 있어서 영향 없음
    // 여러 번 눌러도 결과는 같음
    public static class CityOptimizer
    {
        private const string PrefabPath = "Assets/00. Scene/City.prefab";
        private const string Title = "City 프리팹 최적화";

        // 렌더러 크기(가장 긴 변)가 이보다 작으면 Occluder(다른 걸 가리는 물체)로 안 씀 (작은 소품, 풀, 꽃)
        private const float MinOccluderSize = 2f;

        [MenuItem("Tools/Rappel/City 프리팹 최적화")]
        public static void Optimize()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                EditorUtility.DisplayDialog(Title, $"{PrefabPath} 를 찾지 못했어요.", "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(Title,
                    "City 프리팹 안의 모든 오브젝트에\n- Static 켜기\n- 그림자 끄기\n를 적용하고 저장해요.\n\n" +
                    "City 프리팹을 Prefab 모드로 열어 두었다면 먼저 닫아 주세요.",
                    "적용", "취소"))
                return;

            int objectCount = 0, rendererCount = 0, occluderCount = 0;

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                EditorUtility.DisplayProgressBar(Title, "적용 중...", 0.3f);

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    objectCount++;

                    StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;

                    Renderer r = go.GetComponent<Renderer>();
                    if (r != null)
                    {
                        rendererCount++;
                        r.shadowCastingMode = ShadowCastingMode.Off;
                        r.receiveShadows = false;

                        Vector3 size = r.bounds.size;
                        if (Mathf.Max(size.x, Mathf.Max(size.y, size.z)) >= MinOccluderSize)
                        {
                            flags |= StaticEditorFlags.OccluderStatic;
                            occluderCount++;
                        }
                    }

                    GameObjectUtility.SetStaticEditorFlags(go, flags);
                }

                EditorUtility.DisplayProgressBar(Title, "저장 중...", 0.8f);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                EditorUtility.ClearProgressBar();
            }

            string msg = $"오브젝트 {objectCount}개 Static 켬\n렌더러 {rendererCount}개 그림자 끔\n그중 큰 물체 {occluderCount}개는 Occluder로 지정";
            Debug.Log($"[Rappel] {Title}: {msg.Replace("\n", " / ")}");
            EditorUtility.DisplayDialog(Title, msg, "확인");
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rappel
{
    // 이 캔버스의 UI를 건물 같은 다른 물체에 가려지지 않게 항상 위에 그림 (캔버스 오브젝트에 붙임)
    //   시작할 때 자식의 이미지/버튼 재질 → Rappel/UI Overlay, 글자(TMP) 재질 → TMP Mobile Overlay로 바꿈
    //   둘 다 깊이 비교를 안 하는(ZTest Always) 셰이더라 앞에 물체가 있어도 UI가 보임
    //   캔버스 Sorting Order도 올려서 다른 반투명 물체(유리 등)보다 나중에 그림
    // 셰이더 칸은 컴포넌트를 붙일 때 자동으로 채워짐 (비어 있으면 그 종류는 안 바꿈)
    [RequireComponent(typeof(Canvas))]
    public class UIAlwaysOnTop : MonoBehaviour
    {
        [SerializeField] private Shader uiShader;     // Rappel/UI Overlay
        [SerializeField] private Shader textShader;   // TextMeshPro/Mobile/Distance Field Overlay
        [SerializeField] private int sortingOrder = 100;

        void Awake()
        {
            GetComponent<Canvas>().sortingOrder = sortingOrder;

            Material uiMaterial = uiShader != null ? new Material(uiShader) : null;
            var textMaterials = new Dictionary<Material, Material>();   // 원래 글자 재질 → 바꾼 재질 (같은 폰트끼리 공유)

            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (graphic is TMP_Text text)
                {
                    Material source = text.fontSharedMaterial;
                    if (textShader == null || source == null)
                        continue;

                    if (!textMaterials.TryGetValue(source, out Material overlay))
                    {
                        overlay = new Material(source) { shader = textShader };
                        textMaterials.Add(source, overlay);
                    }
                    text.fontSharedMaterial = overlay;
                }
                else if (uiMaterial != null && graphic.material == graphic.defaultMaterial)
                {
                    // 따로 재질을 넣은 이미지는 건드리지 않음
                    graphic.material = uiMaterial;
                }
            }
        }

#if UNITY_EDITOR
        void Reset()
        {
            uiShader = Shader.Find("Rappel/UI Overlay");
            textShader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");
        }
#endif
    }
}

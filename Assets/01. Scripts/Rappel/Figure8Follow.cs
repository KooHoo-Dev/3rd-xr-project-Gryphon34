using UnityEngine;

namespace Rappel
{
    // 캘리브레이션(하강 가능 상태가 되는 순간)에 한 번만 8자고리 위치를 머리 위치 + 오프셋으로 맞춤
    // 오프셋은 부모(XR Origin) 기준 축: X 오른쪽, Y 위, Z 앞(벽 쪽)
    // 그 뒤로는 다시 맞추지 않음 → 고개를 숙이거나 돌려도 8자고리는 그대로
    // 하강할 때는 XR Origin의 자식이라 몸과 같이 내려감
    // 회전은 건드리지 않음
    // RopeLine보다 먼저 실행되도록 순서를 앞당김 (같은 프레임에 옮긴 위치로 로프를 그리게)
    [DefaultExecutionOrder(-100)]
    public class Figure8Follow : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private Transform head;          // Main Camera
        [SerializeField] private RappelDescent descent;   // CanDescend가 true가 되는 순간을 확인

        [Header("머리 기준 위치 (m)")]
        [SerializeField] private float xOffset = 0f;      // + 오른쪽
        [SerializeField] private float yOffset = -0.55f;  // - 아래
        [SerializeField] private float zOffset = 0.25f;   // + 앞(벽 쪽)

        [Header("디버그 (읽기용)")]
        [SerializeField] private bool isLocked;           // 캘리브레이션 때 위치를 맞췄는지

        // PelvisCheck(Update)가 CanDescend를 바꾼 같은 프레임에 바로 맞춤
        void LateUpdate()
        {
            if (isLocked || head == null || descent == null)
                return;

            if (!descent.CanDescend)
                return;

            Transform parent = transform.parent;

            // 머리 위치를 부모(XR Origin) 기준 좌표로 바꿔서 오프셋을 더함
            Vector3 headLocal = parent != null ? parent.InverseTransformPoint(head.position) : head.position;
            Vector3 target = headLocal + new Vector3(xOffset, yOffset, zOffset);

            if (parent != null)
                transform.localPosition = target;
            else
                transform.position = target;

            isLocked = true;
            Debug.Log("[Rappel] 8자고리 위치 고정");
        }
    }
}

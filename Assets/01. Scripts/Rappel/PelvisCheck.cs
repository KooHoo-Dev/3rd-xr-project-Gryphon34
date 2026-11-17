using UnityEngine;
using UnityEngine.InputSystem;

namespace Rappel
{
    public class PelvisCheck : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private InputActionProperty gripAction;   // XRI Right Interaction/Select Value
        [SerializeField] private Transform head;                   // Main Camera
        [SerializeField] private Transform rightController;        // Right Controller
        [SerializeField] private RappelDescent descent;

        [Header("골반 위치 (머리 기준)")]
        [SerializeField] private Vector3 pelvisOffset = new Vector3(0.15f, -0.65f, 0f); // x 오른쪽, y 위, z 앞 (m)
        [SerializeField] private float pelvisRadius = 0.2f;                             // 이 거리 안이면 골반에 있다고 봄 (m)

        [Header("게임 안 표시")]
        [SerializeField] private Transform pelvisMarker;   // 골반 위치를 따라다닐 오브젝트
        [SerializeField] private Color outsideColor = new Color(0f, 1f, 1f, 0.5f);  // 손이 밖에 있을 때 (하늘색)
        [SerializeField] private Color insideColor = new Color(0f, 1f, 0f, 0.5f);   // 손이 안에 있을 때 (초록색)

        private Renderer markerRenderer;

        [Header("그립")]
        [Range(0f, 1f)]
        [SerializeField] private float requiredGrip = 1f;

        [Header("디버그 (읽기용)")]
        [SerializeField] private float gripValue;
        [SerializeField] private float distanceToPelvis;
        [SerializeField] private bool isAtPelvis;

        void Awake()
        {
            if (pelvisMarker != null)
                markerRenderer = pelvisMarker.GetComponent<Renderer>();
        }

        void OnEnable()
        {
            gripAction.action?.Enable();
        }

        void Update()
        {
            if (head == null || rightController == null || descent == null)
                return;

            // 표시용 오브젝트를 골반 위치로 옮김
            if (pelvisMarker != null)
                pelvisMarker.position = GetPelvisPosition();

            // 손이 골반 안인지 확인 (하강 가능 상태가 된 뒤에도 매 프레임)
            distanceToPelvis = Vector3.Distance(rightController.position, GetPelvisPosition());
            isAtPelvis = distanceToPelvis <= pelvisRadius;

            // 손이 골반 안에 있으면 초록색, 밖이면 하늘색
            if (markerRenderer != null)
                markerRenderer.material.color = isAtPelvis ? insideColor : outsideColor;

            // 이미 하강 가능 상태면 캘리브레이션 확인은 더 필요 없음
            if (descent.CanDescend)
                return;

            gripValue = gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;

            if (isAtPelvis && gripValue >= requiredGrip)
            {
                descent.CanDescend = true;
                Debug.Log("[Rappel] 골반 그립 확인 → 하강 가능");
            }
        }

        Vector3 GetPelvisPosition()
        {
            Quaternion yaw = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
            return head.position + yaw * pelvisOffset;
        }
    }
}

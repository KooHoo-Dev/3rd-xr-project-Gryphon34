using UnityEngine;

namespace Rappel
{
    // 로프를 LineRenderer로 그림 (물리 없음, 보이기만)
    // 순서: 옥상 고정점 → 8자고리 점들 → 오른손 → 바로 아래 바닥 (모두 직선)
    [RequireComponent(typeof(LineRenderer))]
    public class RopeLine : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private Transform anchor;            // 옥상 Cylinder
        [SerializeField] private Transform[] figure8Points;   // Pin_In, Pin_NeckL, Pin_NeckR, Pin_Out 순서
        [SerializeField] private Transform rightHand;         // Right Controller

        [Header("끝부분")]
        [SerializeField] private float groundY = 0f;          // 늘어진 끝이 닿는 바닥 높이

        private LineRenderer line;
        private Vector3[] points = new Vector3[0];

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
        }

        // 몸 이동(RappelDescent.Update)과 손 추적이 끝난 뒤에 그림
        void LateUpdate()
        {
            if (anchor == null || rightHand == null)
                return;

            int pinCount = figure8Points != null ? figure8Points.Length : 0;

            // 점 개수: 고정점 1 + 8자고리 점들 + 오른손 1 + 바닥 1
            int count = 1 + pinCount + 1 + 1;
            if (points.Length != count)
            {
                points = new Vector3[count];
                line.positionCount = count;
            }

            int i = 0;

            // 1. 옥상 고정점
            points[i++] = anchor.position;

            // 2. 8자고리 점들 (비어 있는 칸은 바로 앞 점 위치로 채움)
            for (int p = 0; p < pinCount; p++)
            {
                Transform pin = figure8Points[p];
                points[i] = pin != null ? pin.position : points[i - 1];
                i++;
            }

            // 3. 오른손
            Vector3 hand = rightHand.position;
            points[i++] = hand;

            // 4. 오른손 바로 아래 바닥 (손이 바닥보다 낮으면 손 위치)
            points[i++] = new Vector3(hand.x, Mathf.Min(groundY, hand.y), hand.z);

            line.SetPositions(points);
        }
    }
}

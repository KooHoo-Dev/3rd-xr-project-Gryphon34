using UnityEngine;

namespace Rappel
{
    // 로프를 LineRenderer로 그림 (물리 없음, 보이기만)
    // 순서: 옥상 고정점 → 8자고리 점들 → 오른손 → 바로 아래 바닥 (모두 직선)
    // 줄 끼워 고정 중에는 오른손 대신 마지막 핀(Pin_Out)에서 바로 아래로 떨어짐
    [RequireComponent(typeof(LineRenderer))]
    public class RopeLine : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private Transform anchor;            // 옥상 Cylinder
        [SerializeField] private Transform[] figure8Points;   // Pin_In, Pin_NeckL, Pin_NeckR, Pin_Out 순서
        [SerializeField] private Transform rightHand;         // Right Controller
        [SerializeField] private RappelStateMachine stateMachine;   // 고정(총 든 상태 포함)이면 줄이 8자고리에서 떨어짐

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

            // 3. 끝점: 평소엔 오른손, 고정 중이면 마지막 핀(Pin_Out) 위치
            bool locked = stateMachine != null && stateMachine.IsRopeLocked;   // 고정 + 총 든 상태
            Vector3 end = locked ? points[i - 1] : rightHand.position;
            points[i++] = end;

            // 4. 끝점 바로 아래 바닥 (끝점이 바닥보다 낮으면 끝점 위치)
            points[i++] = new Vector3(end.x, Mathf.Min(groundY, end.y), end.z);

            line.SetPositions(points);
        }
    }
}

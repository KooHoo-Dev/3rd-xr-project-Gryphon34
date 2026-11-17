using UnityEngine;

namespace Rappel
{
    public enum BrakeHand
    {
        None,
        Right,
        Left
    }

    // 두 HandBrake 중 제동 위치에 있는 손의 그립 값을 내보냄
    // 둘 다 제동 위치에 있으면 더 세게 쥔 손의 값
    // HandBrake 뒤, RappelDescent 앞에 실행되도록 순서를 정함
    [DefaultExecutionOrder(-50)]
    public class BrakeManager : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private HandBrake rightHand;
        [SerializeField] private HandBrake leftHand;

        [Header("디버그 (읽기용)")]
        [SerializeField] private BrakeHand activeHand;
        [SerializeField] private float brakeGrip;   // 지금 제동에 쓰는 그립 값 0~1

        public BrakeHand ActiveHand => activeHand;
        public float BrakeGrip => brakeGrip;

        void Update()
        {
            bool rightAtBrake = rightHand != null && rightHand.IsAtBrake;
            bool leftAtBrake = leftHand != null && leftHand.IsAtBrake;

            if (rightAtBrake && leftAtBrake)
            {
                // 더 세게 쥔 손. 값이 같으면 지금 제동 중인 손 유지
                if (rightHand.Grip > leftHand.Grip) activeHand = BrakeHand.Right;
                else if (leftHand.Grip > rightHand.Grip) activeHand = BrakeHand.Left;
                else if (activeHand == BrakeHand.None) activeHand = BrakeHand.Right;
            }
            else if (rightAtBrake) activeHand = BrakeHand.Right;
            else if (leftAtBrake) activeHand = BrakeHand.Left;
            else activeHand = BrakeHand.None;

            switch (activeHand)
            {
                case BrakeHand.Right: brakeGrip = rightHand.Grip; break;
                case BrakeHand.Left:  brakeGrip = leftHand.Grip;  break;
                default:              brakeGrip = 0f;             break;
            }
        }
    }
}

using System;
using UnityEngine;

namespace Rappel
{
    // 레펠 진행 단계. 한 번에 하나만 될 수 있는 것만 넣음
    // (하강 속도, 그립 같은 "정도" 값은 각 스크립트가 들고 있음)
    public enum RappelState
    {
        Ready,      // 준비: 골반 캘리브레이션 전. 하강 안 함
        OnWall,     // 벽: 그립대로 하강. 고정, 점프 가능
        Locked,     // 고정: 줄을 8자고리에 끼움. 하강 정지. 해제, 총 꺼내기 가능
        GunDrawn,   // 총 든 상태: 고정 중에 총을 꺼냄. 하강 정지. 총 넣기만 가능 (넣으면 고정으로)
        Jumping,    // 점프: 수평 진자 + 그립대로 하강. 끝나면 자동으로 벽
        Finished    // 종료: 성공 또는 실패. 하강 안 함
    }

    // 현재 단계를 들고 있고, 단계를 바꾸는 규칙을 한곳에 모음
    // 다른 스크립트는 State를 읽거나, 요청 함수로 "바꿔 줘"라고만 함
    // 요청 함수는 지금 단계에서 허락되면 바꾸고 true, 아니면 아무것도 안 하고 false
    // 단계가 바뀌면 StateChanged 이벤트(이전 단계, 새 단계)를 보냄
    public class RappelStateMachine : MonoBehaviour
    {
        [Header("디버그 (읽기용)")]
        [SerializeField] private RappelState state = RappelState.Ready;
        [SerializeField] private bool succeeded;      // 종료됐을 때 성공인지

        public RappelState State => state;
        public bool Succeeded => succeeded;

        // 줄이 8자고리에 끼워져 있는지 (고정, 총 든 상태 둘 다)
        public bool IsRopeLocked => state == RappelState.Locked || state == RappelState.GunDrawn;

        // 단계가 바뀌는 순간 한 번 (이전 단계, 새 단계). 사운드, 햅틱, 화면 등을 붙일 때 사용
        // 종료로 바뀔 때는 Succeeded가 먼저 정해진 뒤에 보냄
        public event Action<RappelState, RappelState> StateChanged;

        // 준비 → 벽 (PelvisCheck)
        public bool Calibrate()
        {
            if (state != RappelState.Ready)
                return false;

            ChangeState(RappelState.OnWall);
            return true;
        }

        // 벽 ↔ 고정 (HandBrake). 점프 중, 총 든 상태, 준비, 종료에는 안 됨
        public bool ToggleLock()
        {
            if (state == RappelState.OnWall)
            {
                ChangeState(RappelState.Locked);
                return true;
            }

            if (state == RappelState.Locked)
            {
                ChangeState(RappelState.OnWall);
                return true;
            }

            return false;
        }

        // 고정 → 총 든 상태 (고정 중에만)
        public bool DrawGun()
        {
            if (state != RappelState.Locked)
                return false;

            ChangeState(RappelState.GunDrawn);
            return true;
        }

        // 총 든 상태 → 고정
        public bool HolsterGun()
        {
            if (state != RappelState.GunDrawn)
                return false;

            ChangeState(RappelState.Locked);
            return true;
        }

        // 벽 → 점프 (RappelJump). 고정 중에는 안 됨
        public bool StartJump()
        {
            if (state != RappelState.OnWall)
                return false;

            ChangeState(RappelState.Jumping);
            return true;
        }

        // 점프 → 벽 (RappelJump, 왕복이 끝났을 때)
        public bool EndJump()
        {
            if (state != RappelState.Jumping)
                return false;

            ChangeState(RappelState.OnWall);
            return true;
        }

        // 아무 단계 → 종료 (StageRule이 성공/실패를 정해서 부름). 준비 중이거나 이미 종료면 안 됨
        public bool Finish(bool success)
        {
            if (state == RappelState.Ready || state == RappelState.Finished)
                return false;

            succeeded = success;
            ChangeState(RappelState.Finished);
            return true;
        }

        private void ChangeState(RappelState next)
        {
            RappelState prev = state;
            state = next;

            if (next == RappelState.Finished)
                Debug.Log($"[Rappel] 상태: {prev} → {next} ({(succeeded ? "성공" : "실패")})");
            else
                Debug.Log($"[Rappel] 상태: {prev} → {next}");

            StateChanged?.Invoke(prev, next);
        }
    }
}

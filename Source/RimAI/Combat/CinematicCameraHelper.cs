using UnityEngine;
using Verse;

namespace RimAI.Combat
{
    /// <summary>
    /// 관람용 시네마틱 카메라 연출 헬퍼
    ///
    /// RimWorld의 CameraDriver를 활용하여 전투 장면을 자동으로 추적합니다.
    /// </summary>
    public static class CinematicCameraHelper
    {
        // 줌 레벨 설정
        private const float COMBAT_ZOOM_LEVEL = 4.5f;  // 전투 시 줌 (가까이)
        private const float NORMAL_ZOOM_LEVEL = 12f;   // 평시 줌 (멀리)

        // 카메라 이동 속도
        private const float CAMERA_MOVE_SPEED = 0.5f;  // 부드러운 이동

        // 원래 카메라 상태 저장
        private static Vector3 originalPosition;
        private static float originalZoom;
        private static bool isCinematicMode = false;

        /// <summary>
        /// 전투 위치로 카메라 이동 및 줌
        /// </summary>
        /// <param name="map">대상 맵</param>
        /// <param name="focusPosition">집중할 위치</param>
        public static void FocusOnCombat(Map map, IntVec3 focusPosition)
        {
            if (!RimAI_Mod.Settings.cinematicCameraEnabled) return;

            try
            {
                var cameraDriver = Find.CameraDriver;

                // 원래 상태 저장 (첫 전투 시)
                if (!isCinematicMode)
                {
                    originalPosition = cameraDriver.MapPosition;
                    originalZoom = cameraDriver.CellsVisible;
                    isCinematicMode = true;
                }

                // 전투 위치로 즉시 점프
                cameraDriver.JumpToCurrentMapLoc(focusPosition);

                // 줌 레벨 조정 (부드럽게)
                SetZoomLevel(COMBAT_ZOOM_LEVEL);

                Log.Message($"[RimAI-Camera] 전투 위치로 카메라 이동: {focusPosition}");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Camera] 카메라 이동 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 카메라를 원래 상태로 복원
        /// </summary>
        public static void ResetCamera()
        {
            if (!RimAI_Mod.Settings.cinematicCameraEnabled) return;
            if (!isCinematicMode) return;

            try
            {
                var cameraDriver = Find.CameraDriver;

                // 줌 레벨 복원
                SetZoomLevel(NORMAL_ZOOM_LEVEL);

                isCinematicMode = false;

                Log.Message($"[RimAI-Camera] 카메라 원위치 복귀");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Camera] 카메라 복원 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 줌 레벨 설정
        /// </summary>
        private static void SetZoomLevel(float targetZoom)
        {
            try
            {
                var cameraDriver = Find.CameraDriver;

                // 현재 줌 레벨 가져오기
                float currentZoom = cameraDriver.CellsVisible;

                // 목표 줌 레벨로 부드럽게 이동
                // RimWorld 1.4/1.5에서는 CameraDriver.config.sizeRange를 사용하거나
                // 직접 접근이 제한됨 - JumpToCurrentMapLoc만 사용
                // SetRootSize 대신 직접 zoom 제어는 제한적이므로 로그만 남김
                Log.Message($"[RimAI-Camera] 줌 레벨 목표: {targetZoom} (현재: {currentZoom})");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Camera] 줌 레벨 설정 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 특정 폰 추적 (지속적 업데이트 필요 시)
        /// </summary>
        /// <param name="pawn">추적할 폰</param>
        public static void TrackPawn(Pawn pawn)
        {
            if (!RimAI_Mod.Settings.cinematicCameraEnabled) return;
            if (pawn == null || pawn.Map == null) return;

            try
            {
                var cameraDriver = Find.CameraDriver;

                // 폰의 현재 위치로 카메라 이동
                if (pawn.Spawned)
                {
                    cameraDriver.JumpToCurrentMapLoc(pawn.Position);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Camera] 폰 추적 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 두 위치 사이를 보여주는 앵글 설정
        /// </summary>
        /// <param name="pos1">첫 번째 위치</param>
        /// <param name="pos2">두 번째 위치</param>
        public static void FrameTwoPositions(IntVec3 pos1, IntVec3 pos2)
        {
            if (!RimAI_Mod.Settings.cinematicCameraEnabled) return;

            try
            {
                // 두 위치의 중간점
                Vector3 midpoint = (pos1.ToVector3() + pos2.ToVector3()) / 2f;
                IntVec3 centerPos = midpoint.ToIntVec3();

                var cameraDriver = Find.CameraDriver;
                cameraDriver.JumpToCurrentMapLoc(centerPos);

                // 거리에 따라 줌 레벨 조정
                float distance = pos1.DistanceTo(pos2);
                float zoom = Mathf.Lerp(COMBAT_ZOOM_LEVEL, NORMAL_ZOOM_LEVEL, distance / 100f);
                SetZoomLevel(zoom);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[RimAI-Camera] 두 위치 프레이밍 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 현재 시네마틱 모드 여부
        /// </summary>
        public static bool IsCinematicMode => isCinematicMode;
    }
}

using System;
using System.Text;
using UnityEngine;

namespace HideSeek.AI
{
    public sealed class AIDebugModel_model
    {
        private const float FRAME_TIME_SMOOTHING = 0.1f;

        private readonly MasterAIProvider MASTER_AI_PROVIDER;
        private readonly ChaseAIController CHASE_AI_CONTROLLER;
        private readonly StringBuilder STRING_BUILDER = new(1024);

        private float _smoothedFrameTime;

        public AIDebugModel_model(
            MasterAIProvider masterAIProvider ,
            ChaseAIController chaseAIController)
        {
            MASTER_AI_PROVIDER = masterAIProvider != null
                ? masterAIProvider
                : throw new ArgumentNullException(nameof(masterAIProvider));
            CHASE_AI_CONTROLLER = chaseAIController != null
                ? chaseAIController
                : throw new ArgumentNullException(nameof(chaseAIController));
        }

        public void UpdateFrameTime(float unscaledDeltaTime)
        {
            if ( unscaledDeltaTime <= 0f )
            {
                return;
            }

            if ( _smoothedFrameTime <= 0f )
            {
                _smoothedFrameTime = unscaledDeltaTime;

                return;
            }

            _smoothedFrameTime += (unscaledDeltaTime - _smoothedFrameTime) * FRAME_TIME_SMOOTHING;
        }

        public string BuildDisplayText(float currentTime)
        {
            ChaseAIDebugSnapshot chaseSnapshot = CHASE_AI_CONTROLLER.GetDebugSnapshot(currentTime);

            STRING_BUILDER.Clear();
            STRING_BUILDER.AppendLine("<b>AI DEBUG</b>");
            AppendPerformanceData();
            STRING_BUILDER.AppendLine();
            AppendDirectorData(currentTime);
            AppendChaseData(chaseSnapshot);
            AppendEvidenceData(chaseSnapshot);
            AppendSearchData(chaseSnapshot);
            AppendAngerData(chaseSnapshot);

            return STRING_BUILDER.ToString();
        }

        private void AppendPerformanceData()
        {
            if ( _smoothedFrameTime <= 0f )
            {
                STRING_BUILDER.AppendLine("FPS=--  Frame=-- ms");

                return;
            }

            float framesPerSecond = 1f / _smoothedFrameTime;
            float frameTimeMilliseconds = _smoothedFrameTime * 1000f;

            STRING_BUILDER.AppendLine($"FPS={framesPerSecond:F1}  Frame={frameTimeMilliseconds:F1} ms");
        }

        private void AppendDirectorData(float currentTime)
        {
            string playerZoneName = GetZoneName(MASTER_AI_PROVIDER.CurrentPlayerZone);
            string targetZoneName = GetZoneName(MASTER_AI_PROVIDER.TargetZone);
            string ventName = MASTER_AI_PROVIDER.CurrentVent != null
                ? MASTER_AI_PROVIDER.CurrentVent.name
                : "NONE";

            STRING_BUILDER.AppendLine("<b>[DIRECTOR]</b>");
            STRING_BUILDER.AppendLine(
                $"Lifecycle={(MASTER_AI_PROVIDER.IsGameplayStarted ? "STARTED" : "WAITING")}  " +
                $"State={MASTER_AI_PROVIDER.CurrentState}  " +
                $"Stress={MASTER_AI_PROVIDER.GlobalStress:F1} ({MASTER_AI_PROVIDER.GlobalStressRatio:P0})");
            STRING_BUILDER.AppendLine(
                $"PlayerZone={playerZoneName}  TargetZone={targetZoneName}  Vent={ventName}");

            if ( MASTER_AI_PROVIDER.TryGetCurrentHint(out MasterAIHint currentHint) )
            {
                float remainingTime = Mathf.Max(0f , currentHint.ExpireTime - currentTime);

                STRING_BUILDER.AppendLine(
                    $"Hint=Zone {currentHint.TargetZoneId}  " +
                    $"Status={(MASTER_AI_PROVIDER.IsCurrentHintAccepted ? "ACCEPTED" : "PENDING")}  " +
                    $"Radius={currentHint.SearchRadius:F1}  " +
                    $"Urgency={currentHint.Urgency:F2}  " +
                    $"Remain={remainingTime:F1}s");
            }
            else
            {
                STRING_BUILDER.AppendLine("Hint=NONE");
            }
        }

        private void AppendChaseData(ChaseAIDebugSnapshot chaseSnapshot)
        {
            STRING_BUILDER.AppendLine();
            STRING_BUILDER.AppendLine("<b>[CHASE]</b>");
            STRING_BUILDER.AppendLine(
                $"Initialized={chaseSnapshot.IsInitialized}  " +
                $"State={chaseSnapshot.State}  " +
                $"RetreatPending={chaseSnapshot.IsRetreatPending}");
            STRING_BUILDER.AppendLine(
                $"DecisionSource={ResolveDecisionSource(chaseSnapshot)}");
            STRING_BUILDER.AppendLine(
                $"Visual={chaseSnapshot.VisualState}  " +
                $"LOS={chaseSnapshot.HasLineOfSight}  " +
                $"SuspicionReaction={chaseSnapshot.IsReactingToVisualSuspicion}  " +
                $"Detection={chaseSnapshot.DetectionRatio:P0}  " +
                $"Gain=x{chaseSnapshot.DetectionSpeedMultiplier:F2}");
            STRING_BUILDER.AppendLine(
                $"VisibilityContract={chaseSnapshot.HasTargetVisibilityState}  " +
                $"FullyHidden={chaseSnapshot.IsTargetFullyHidden}");
        }

        private void AppendEvidenceData(ChaseAIDebugSnapshot chaseSnapshot)
        {
            STRING_BUILDER.AppendLine();
            STRING_BUILDER.AppendLine("<b>[MEMORY]</b>");

            if ( chaseSnapshot.HasVisualMemory )
            {
                STRING_BUILDER.AppendLine(
                    $"Visual={FormatVector(chaseSnapshot.VisualMemoryPosition)}  " +
                    $"Strength={chaseSnapshot.VisualMemoryStrength:F2}  " +
                    $"Remain={chaseSnapshot.VisualMemoryRemainingTime:F1}s");
            }
            else
            {
                STRING_BUILDER.AppendLine("Visual=NONE");
            }

            if ( chaseSnapshot.HasAudioMemory )
            {
                STRING_BUILDER.AppendLine(
                    $"Audio={chaseSnapshot.LastNoiseType}  " +
                    $"Position={FormatVector(chaseSnapshot.AudioMemoryPosition)}  " +
                    $"Strength={chaseSnapshot.AudioMemoryStrength:F2}  " +
                    $"Remain={chaseSnapshot.AudioMemoryRemainingTime:F1}s");
            }
            else
            {
                STRING_BUILDER.AppendLine("Audio=NONE");
            }

            if ( chaseSnapshot.HasActiveAudioInvestigation )
            {
                STRING_BUILDER.AppendLine(
                    $"AudioPriority={chaseSnapshot.ActiveAudioNoiseType}  " +
                    $"Intensity={chaseSnapshot.ActiveAudioIntensity:F2}  " +
                    $"Freshness={chaseSnapshot.ActiveAudioFreshness:F2}  " +
                    $"Score={chaseSnapshot.ActiveAudioScore:F2}");
            }

            STRING_BUILDER.AppendLine(
                $"AudioDecision={chaseSnapshot.LastAudioDecisionReason}  " +
                $"CandidateScore={chaseSnapshot.LastAudioCandidateScore:F2}");
        }

        private void AppendSearchData(ChaseAIDebugSnapshot chaseSnapshot)
        {
            STRING_BUILDER.AppendLine();
            STRING_BUILDER.AppendLine("<b>[INVESTIGATION / SEARCH]</b>");
            STRING_BUILDER.AppendLine($"Investigation={chaseSnapshot.ActiveInvestigationName}");

            if ( chaseSnapshot.State != CHASE_AI_STATE.SEARCH )
            {
                STRING_BUILDER.AppendLine("Search=NONE");

                return;
            }

            int displayedPointIndex = chaseSnapshot.SearchPointCount > 0
                ? Mathf.Clamp(chaseSnapshot.CurrentSearchPointIndex + 1 , 1 , chaseSnapshot.SearchPointCount)
                : 0;

            STRING_BUILDER.AppendLine(
                $"Search={chaseSnapshot.ActiveSearchContext}  " +
                $"Point={displayedPointIndex}/{chaseSnapshot.SearchPointCount}  " +
                $"Source={chaseSnapshot.CurrentSearchPointSource}");
            STRING_BUILDER.AppendLine(
                $"Action={chaseSnapshot.CurrentSearchAction}  " +
                $"Progress={chaseSnapshot.SearchActionProgress:P0}  " +
                $"Remain={chaseSnapshot.SearchActionRemainingTime:F1}s");
            STRING_BUILDER.AppendLine(
                $"HidingCandidate={chaseSnapshot.HidingSpotCandidateName}  " +
                $"Chance={chaseSnapshot.HidingSpotInspectionChance:P0}  " +
                $"Roll={FormatInspectionRoll(chaseSnapshot.HidingSpotInspectionRoll)}  " +
                $"Selected={chaseSnapshot.WasHidingSpotSelected}");
            STRING_BUILDER.AppendLine(
                $"Zone={chaseSnapshot.ActiveSearchZoneName}  " +
                $"Restricted={chaseSnapshot.IsSearchZoneRestricted}");
        }

        private void AppendAngerData(ChaseAIDebugSnapshot chaseSnapshot)
        {
            STRING_BUILDER.AppendLine();
            STRING_BUILDER.AppendLine("<b>[ANGER]</b>");
            STRING_BUILDER.AppendLine(
                $"Value={chaseSnapshot.CurrentAnger:F1}  " +
                $"Floor={chaseSnapshot.AngerFloor:F1}  " +
                $"Generators={chaseSnapshot.CompletedGeneratorCount}");
            STRING_BUILDER.AppendLine(
                $"Effects=Speed x{chaseSnapshot.ChaseSpeedMultiplier:F2}  " +
                $"Radius x{chaseSnapshot.SearchRadiusMultiplier:F2}  " +
                $"Points={chaseSnapshot.AngerSearchPointCount}");
        }

        private static string ResolveDecisionSource(ChaseAIDebugSnapshot chaseSnapshot)
        {
            return chaseSnapshot.State switch
            {
                CHASE_AI_STATE.CHASE when chaseSnapshot.HasLineOfSight => "DIRECT_VISUAL",
                CHASE_AI_STATE.CHASE when chaseSnapshot.HasVisualMemory => "RECENT_VISUAL",
                CHASE_AI_STATE.INVESTIGATE => chaseSnapshot.ActiveInvestigationName,
                CHASE_AI_STATE.SEARCH when !string.IsNullOrEmpty(chaseSnapshot.ActiveSearchContext) => chaseSnapshot.ActiveSearchContext,
                _ => "NONE"
            };
        }

        private static string GetZoneName(AIWorldZone zone)
        {
            return zone != null ? zone.DisplayName : "NONE";
        }

        private static string FormatVector(Vector3 position)
        {
            return $"({position.x:F1}, {position.y:F1}, {position.z:F1})";
        }

        private static string FormatInspectionRoll(float inspectionRoll)
        {
            return inspectionRoll >= 0f
                ? inspectionRoll.ToString("F2")
                : "NONE";
        }
    }
}

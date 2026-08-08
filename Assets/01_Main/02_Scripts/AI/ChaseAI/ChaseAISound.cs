using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace HideSeek.AI
{
    /// <summary>
    /// Chase AI의 표현용 3D 효과음을 재생한다.
    /// 이 컴포넌트는 NoiseProvider에 게임플레이 소음을 발행하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChaseAISound : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChaseAIController _chaseAIController;
        [SerializeField] private ChaseAIAnimator _chaseAIAnimator;
        [SerializeField] private AudioMixerGroup _outputMixerGroup;

        [Header("Footstep Clips")]
        [FormerlySerializedAs("_footstepClips")]
        [SerializeField] private AudioClip[] _walkFootstepClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _jogFootstepClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _sprintFootstepClips = Array.Empty<AudioClip>();

        [Header("Action Contact Clips")]
        [SerializeField] private AudioClip[] _searchContactClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _hidingSpotContactClips = Array.Empty<AudioClip>();

        [Header("Voice Clips")]
        [SerializeField] private AudioClip[] _patrolVocalClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _investigationVocalClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _suspicionVocalClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _searchActionVocalClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _hidingSpotVocalClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _chaseStartVocalClips = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _chaseVocalClips = Array.Empty<AudioClip>();

        [Header("Footstep Volume")]
        [SerializeField, Range(0f, 1f)] private float _walkFootstepVolume = 0.7f;
        [FormerlySerializedAs("_evidenceApproachFootstepVolume")]
        [SerializeField, Range(0f, 1f)] private float _jogFootstepVolume = 0.85f;
        [FormerlySerializedAs("_chaseFootstepVolume")]
        [SerializeField, Range(0f, 1f)] private float _sprintFootstepVolume = 1f;

        [Header("Footstep Pitch")]
        [SerializeField] private Vector2 _walkFootstepPitchRange = new(0.92f , 1.02f);
        [SerializeField] private Vector2 _jogFootstepPitchRange = new(0.96f , 1.06f);
        [SerializeField] private Vector2 _sprintFootstepPitchRange = new(0.98f , 1.08f);

        [Header("Other Volume")]
        [SerializeField, Range(0f, 1f)] private float _contactVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _voiceVolume = 1f;

        [Header("Randomization")]
        [SerializeField, Range(0.5f, 1.5f)] private float _minimumPitch = 0.95f;
        [SerializeField, Range(0.5f, 1.5f)] private float _maximumPitch = 1.05f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _footstepCooldown = 0.08f;
        [SerializeField, Min(0f)] private float _actionVocalCooldown = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _searchContactFallbackProgress = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _hidingSpotContactFallbackProgress = 0.65f;
        [SerializeField] private Vector2 _patrolVocalInterval = new(8f , 15f);
        [SerializeField] private Vector2 _investigationVocalInterval = new(5f , 10f);
        [SerializeField] private Vector2 _chaseVocalInterval = new(3f , 6f);

        [Header("3D Audio")]
        [SerializeField, Min(0f)] private float _minimumDistance = 2f;
        [SerializeField, Min(0f)] private float _maximumDistance = 35f;

        private AudioSource _movementSource;
        private AudioSource _voiceSource;
        private float _lastFootstepTime = float.NegativeInfinity;
        private float _lastActionVocalTime = float.NegativeInfinity;
        private float _nextVocalTime = float.PositiveInfinity;
        private CHASE_AI_ANIMATION_ACTION _contactAction;
        private bool _hasPlayedCurrentActionContact;

        private void Awake()
        {
            if ( _chaseAIController == null )
            {
                _chaseAIController = GetComponent<ChaseAIController>();
            }

            if ( _chaseAIAnimator == null )
            {
                _chaseAIAnimator = GetComponent<ChaseAIAnimator>();
            }

            _movementSource = CreateAudioSource();
            _voiceSource = CreateAudioSource();
        }

        private void OnEnable()
        {
            BindEvents();
            ScheduleNextVocal();
        }

        private void Update()
        {
            UpdateAmbientVocal();
            UpdateActionContactFallback();
        }

        private void OnDisable()
        {
            UnbindEvents();
            _movementSource?.Stop();
            _voiceSource?.Stop();
            _nextVocalTime = float.PositiveInfinity;
        }

        private void BindEvents()
        {
            if ( _chaseAIController != null )
            {
                _chaseAIController.StateChanged -= OnStateChangedActioned;
                _chaseAIController.StateChanged += OnStateChangedActioned;
            }

            if ( _chaseAIAnimator != null )
            {
                _chaseAIAnimator.AnimationActionChanged -= OnAnimationActionChangedActioned;
                _chaseAIAnimator.AnimationActionChanged += OnAnimationActionChangedActioned;
                _chaseAIAnimator.FootstepActioned -= OnFootstepActioned;
                _chaseAIAnimator.FootstepActioned += OnFootstepActioned;
                _chaseAIAnimator.SearchContactActioned -= OnSearchContactActioned;
                _chaseAIAnimator.SearchContactActioned += OnSearchContactActioned;
                _chaseAIAnimator.HidingSpotContactActioned -= OnHidingSpotContactActioned;
                _chaseAIAnimator.HidingSpotContactActioned += OnHidingSpotContactActioned;
            }
        }

        private void UnbindEvents()
        {
            if ( _chaseAIController != null )
            {
                _chaseAIController.StateChanged -= OnStateChangedActioned;
            }

            if ( _chaseAIAnimator != null )
            {
                _chaseAIAnimator.AnimationActionChanged -= OnAnimationActionChangedActioned;
                _chaseAIAnimator.FootstepActioned -= OnFootstepActioned;
                _chaseAIAnimator.SearchContactActioned -= OnSearchContactActioned;
                _chaseAIAnimator.HidingSpotContactActioned -= OnHidingSpotContactActioned;
            }
        }

        private void OnStateChangedActioned(
            CHASE_AI_STATE previousState ,
            CHASE_AI_STATE currentState)
        {
            if ( currentState == CHASE_AI_STATE.CHASE )
            {
                PlayRandomClip(_voiceSource , _chaseStartVocalClips , _voiceVolume , true);
            }

            if ( currentState == CHASE_AI_STATE.DORMANT )
            {
                _movementSource?.Stop();
                _voiceSource?.Stop();
            }

            ScheduleNextVocal();
        }

        private void OnAnimationActionChangedActioned(
            CHASE_AI_ANIMATION_ACTION previousAction ,
            CHASE_AI_ANIMATION_ACTION currentAction)
        {
            if ( currentAction == CHASE_AI_ANIMATION_ACTION.NONE ||
                Time.time < _lastActionVocalTime + _actionVocalCooldown )
            {
                ResetActionContact(currentAction);

                return;
            }

            ResetActionContact(currentAction);

            AudioClip[] clips = currentAction switch
            {
                CHASE_AI_ANIMATION_ACTION.SUSPICION => _suspicionVocalClips,
                CHASE_AI_ANIMATION_ACTION.LOOK_AROUND => _searchActionVocalClips,
                CHASE_AI_ANIMATION_ACTION.INSPECT_HIDING_SPOT => _hidingSpotVocalClips,
                _ => Array.Empty<AudioClip>()
            };

            if ( PlayRandomClip(_voiceSource , clips , _voiceVolume , false) )
            {
                _lastActionVocalTime = Time.time;
            }
        }

        private void OnFootstepActioned(CHASE_AI_FOOT foot)
        {
            if ( Time.time < _lastFootstepTime + _footstepCooldown )
            {
                return;
            }

            AudioClip[] clips = ResolveFootstepClips();
            float volume = ResolveFootstepVolume();
            Vector2 pitchRange = ResolveFootstepPitchRange();

            if ( PlayRandomClip(
                    _movementSource ,
                    clips ,
                    volume ,
                    false ,
                    pitchRange.x ,
                    pitchRange.y) )
            {
                _lastFootstepTime = Time.time;
            }
        }

        private void OnSearchContactActioned()
        {
            if ( _contactAction != CHASE_AI_ANIMATION_ACTION.LOOK_AROUND ||
                _hasPlayedCurrentActionContact )
            {
                return;
            }

            _hasPlayedCurrentActionContact =
                PlayRandomClip(_movementSource , _searchContactClips , _contactVolume , false);
        }

        private void OnHidingSpotContactActioned()
        {
            if ( _contactAction != CHASE_AI_ANIMATION_ACTION.INSPECT_HIDING_SPOT ||
                _hasPlayedCurrentActionContact )
            {
                return;
            }

            _hasPlayedCurrentActionContact =
                PlayRandomClip(_movementSource , _hidingSpotContactClips , _contactVolume , false);
        }

        private void UpdateActionContactFallback()
        {
            if ( _chaseAIAnimator == null ||
                _chaseAIController == null ||
                _hasPlayedCurrentActionContact )
            {
                return;
            }

            float actionProgress = _chaseAIController.SearchActionProgress;

            switch ( _chaseAIAnimator.CurrentAnimationAction )
            {
                case CHASE_AI_ANIMATION_ACTION.LOOK_AROUND:
                    if ( actionProgress >= _searchContactFallbackProgress )
                    {
                        OnSearchContactActioned();
                    }
                    break;

                case CHASE_AI_ANIMATION_ACTION.INSPECT_HIDING_SPOT:
                    if ( actionProgress >= _hidingSpotContactFallbackProgress )
                    {
                        OnHidingSpotContactActioned();
                    }
                    break;
            }
        }

        private void ResetActionContact(CHASE_AI_ANIMATION_ACTION currentAction)
        {
            _contactAction = currentAction;
            _hasPlayedCurrentActionContact = false;
        }

        private void UpdateAmbientVocal()
        {
            if ( _chaseAIController == null ||
                !_chaseAIController.IsInitialized ||
                Time.time < _nextVocalTime )
            {
                return;
            }

            AudioClip[] clips = ResolveAmbientVocalClips();
            PlayRandomClip(_voiceSource , clips , _voiceVolume , false);
            ScheduleNextVocal();
        }

        private AudioClip[] ResolveAmbientVocalClips()
        {
            return _chaseAIController.CurrentState switch
            {
                CHASE_AI_STATE.PATROL => _patrolVocalClips,
                CHASE_AI_STATE.INVESTIGATE => _investigationVocalClips,
                CHASE_AI_STATE.SEARCH => _investigationVocalClips,
                CHASE_AI_STATE.CHASE => _chaseVocalClips,
                _ => Array.Empty<AudioClip>()
            };
        }

        private void ScheduleNextVocal()
        {
            if ( _chaseAIController == null )
            {
                _nextVocalTime = float.PositiveInfinity;

                return;
            }

            Vector2 interval = _chaseAIController.CurrentState switch
            {
                CHASE_AI_STATE.PATROL => _patrolVocalInterval,
                CHASE_AI_STATE.INVESTIGATE => _investigationVocalInterval,
                CHASE_AI_STATE.SEARCH => _investigationVocalInterval,
                CHASE_AI_STATE.CHASE => _chaseVocalInterval,
                _ => new Vector2(float.PositiveInfinity , float.PositiveInfinity)
            };

            _nextVocalTime = float.IsPositiveInfinity(interval.x)
                ? float.PositiveInfinity
                : Time.time + UnityEngine.Random.Range(interval.x , interval.y);
        }

        private AudioClip[] ResolveFootstepClips()
        {
            if ( _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE )
            {
                return _sprintFootstepClips;
            }

            return _chaseAIController.IsUsingEvidenceApproachSpeed
                ? _jogFootstepClips
                : _walkFootstepClips;
        }

        private float ResolveFootstepVolume()
        {
            if ( _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE )
            {
                return _sprintFootstepVolume;
            }

            return _chaseAIController.IsUsingEvidenceApproachSpeed
                ? _jogFootstepVolume
                : _walkFootstepVolume;
        }

        private Vector2 ResolveFootstepPitchRange()
        {
            if ( _chaseAIController.CurrentState == CHASE_AI_STATE.CHASE )
            {
                return _sprintFootstepPitchRange;
            }

            return _chaseAIController.IsUsingEvidenceApproachSpeed
                ? _jogFootstepPitchRange
                : _walkFootstepPitchRange;
        }

        private bool PlayRandomClip(
            AudioSource audioSource ,
            AudioClip[] clips ,
            float volume ,
            bool shouldInterrupt)
        {
            return PlayRandomClip(
                audioSource ,
                clips ,
                volume ,
                shouldInterrupt ,
                _minimumPitch ,
                _maximumPitch);
        }

        private static bool PlayRandomClip(
            AudioSource audioSource ,
            AudioClip[] clips ,
            float volume ,
            bool shouldInterrupt ,
            float minimumPitch ,
            float maximumPitch)
        {
            if ( audioSource == null || clips == null || clips.Length == 0 )
            {
                return false;
            }

            int startIndex = UnityEngine.Random.Range(0 , clips.Length);

            for ( int offset = 0; offset < clips.Length; offset++ )
            {
                AudioClip clip = clips[ (startIndex + offset) % clips.Length ];

                if ( clip == null )
                {
                    continue;
                }

                if ( shouldInterrupt )
                {
                    audioSource.Stop();
                }

                audioSource.pitch = UnityEngine.Random.Range(minimumPitch , maximumPitch);
                audioSource.PlayOneShot(clip , Mathf.Clamp01(volume));

                return true;
            }

            return false;
        }

        private AudioSource CreateAudioSource()
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = _minimumDistance;
            audioSource.maxDistance = Mathf.Max(_minimumDistance , _maximumDistance);
            audioSource.outputAudioMixerGroup = _outputMixerGroup;

            return audioSource;
        }

        private void OnValidate()
        {
            _minimumPitch = Mathf.Max(0.5f , _minimumPitch);
            _maximumPitch = Mathf.Max(_minimumPitch , _maximumPitch);
            NormalizePitchRange(ref _walkFootstepPitchRange);
            NormalizePitchRange(ref _jogFootstepPitchRange);
            NormalizePitchRange(ref _sprintFootstepPitchRange);
            _footstepCooldown = Mathf.Max(0f , _footstepCooldown);
            _actionVocalCooldown = Mathf.Max(0f , _actionVocalCooldown);
            _searchContactFallbackProgress = Mathf.Clamp01(_searchContactFallbackProgress);
            _hidingSpotContactFallbackProgress = Mathf.Clamp01(_hidingSpotContactFallbackProgress);
            _minimumDistance = Mathf.Max(0f , _minimumDistance);
            _maximumDistance = Mathf.Max(_minimumDistance , _maximumDistance);
            NormalizeInterval(ref _patrolVocalInterval);
            NormalizeInterval(ref _investigationVocalInterval);
            NormalizeInterval(ref _chaseVocalInterval);
        }

        private static void NormalizeInterval(ref Vector2 interval)
        {
            interval.x = Mathf.Max(0f , interval.x);
            interval.y = Mathf.Max(interval.x , interval.y);
        }

        private static void NormalizePitchRange(ref Vector2 pitchRange)
        {
            pitchRange.x = Mathf.Clamp(pitchRange.x , 0.5f , 1.5f);
            pitchRange.y = Mathf.Clamp(pitchRange.y , pitchRange.x , 1.5f);
        }

        private void Reset()
        {
            _chaseAIController = GetComponent<ChaseAIController>();
            _chaseAIAnimator = GetComponent<ChaseAIAnimator>();
        }
    }
}

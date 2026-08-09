using System.Collections;
using UnityEngine;

namespace HideSeek.Sound
{
    public enum BGM_FADE_CURVE
    {
        // 교차 지점에서 음량이 유지된다. 두 곡이 겹치는 구간이 파이지 않는다.
        EQUAL_POWER,

        // 진폭을 직선으로 섞는다. 교차 지점에서 살짝 작아지게 들린다.
        LINEAR
    }

    /// <summary>
    /// AudioSource 두 개를 번갈아 쓰며 배경 음악을 교차 페이드한다. GDD 14.1
    ///
    /// 페이드 중에 다른 곡을 요청해도 현재 볼륨에서 이어서 전환한다.
    /// A에서 B로 가는 도중 다시 A를 요청하면 남아 있는 A 소리를 그대로 살려 되돌린다.
    ///
    /// 인스펙터: 두 AudioSource 모두 Spatial Blend 0(2D), Output은 BGM 믹서 그룹.
    /// 교차 페이드 중에는 클립 두 개가 동시에 열리므로 임포트 설정이 메모리에 그대로 영향을 준다.
    /// 웹 빌드는 Streaming을 못 쓴다. 권장값은 Docs/SOUND_IMPLEMENTATION_STATUS.md 참고.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BgmPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _sourceA;
        [SerializeField] private AudioSource _sourceB;

        [SerializeField, Min(0f)] private float _defaultFadeDuration = 2f;
        [SerializeField] private BGM_FADE_CURVE _fadeCurve = BGM_FADE_CURVE.EQUAL_POWER;

        private AudioSource _activeSource;
        private AudioSource _idleSource;
        private Coroutine _fadeCor;

        public AudioClip CurrentClip => _activeSource != null ? _activeSource.clip : null;

        private void Awake()
        {
            if (_sourceA == null || _sourceB == null)
            {
                Debug.LogError($"[{nameof(BgmPlayer)}] AudioSource 두 개가 모두 필요합니다.", this);
                return;
            }

            SetUpSource(_sourceA);
            SetUpSource(_sourceB);

            _activeSource = _sourceA;
            _idleSource = _sourceB;
        }

        public void Play(AudioClip clip)
        {
            Play(clip, _defaultFadeDuration);
        }

        public void Play(AudioClip clip, float fadeDuration)
        {
            if (_activeSource == null || ReferenceEquals(clip, CurrentClip))
            {
                return;
            }

            if (_fadeCor != null)
            {
                StopCoroutine(_fadeCor);
                _fadeCor = null;
            }

            // 역할을 맞바꾼다. 울리던 쪽이 물러나고 반대쪽이 새 곡을 맡는다.
            AudioSource tFromSource = _activeSource;
            _activeSource = _idleSource;
            _idleSource = tFromSource;

            PrepareActiveSource(clip);

            _fadeCor = StartCoroutine(Fade_cor(tFromSource, _activeSource, Mathf.Max(0f, fadeDuration)));
        }

        // clip을 null로 넘기면 현재 곡만 페이드 아웃한다.
        public void Stop(float fadeDuration)
        {
            Play(null, fadeDuration);
        }

        private void PrepareActiveSource(AudioClip clip)
        {
            // 되돌리는 경우 이미 그 클립이 남아 울리고 있으므로 볼륨을 건드리지 않는다.
            if (ReferenceEquals(_activeSource.clip, clip))
            {
                if (clip != null && _activeSource.isPlaying == false)
                {
                    _activeSource.Play();
                }

                return;
            }

            _activeSource.Stop();
            _activeSource.clip = clip;
            _activeSource.volume = 0f;

            if (clip != null)
            {
                _activeSource.Play();
            }
        }

        private IEnumerator Fade_cor(AudioSource fromSource, AudioSource toSource, float fadeDuration)
        {
            float tFromStartVolume = fromSource.volume;
            float tToStartVolume = toSource.volume;
            float tToTargetVolume = toSource.clip != null ? 1f : 0f;
            float tElapsed = 0f;

            // 일시 정지 중에도 음악은 흘러야 하므로 unscaled를 쓴다.
            while (tElapsed < fadeDuration)
            {
                tElapsed += Time.unscaledDeltaTime;

                float tRatio = Mathf.Clamp01(tElapsed / fadeDuration);
                fromSource.volume = tFromStartVolume * GetFadeOutFactor(tRatio);
                toSource.volume = Mathf.Lerp(tToStartVolume, tToTargetVolume, GetFadeInFactor(tRatio));

                yield return null;
            }

            fromSource.Stop();
            fromSource.clip = null;
            fromSource.volume = 0f;
            toSource.volume = tToTargetVolume;

            _fadeCor = null;
        }

        private float GetFadeOutFactor(float ratio01)
        {
            if (_fadeCurve == BGM_FADE_CURVE.LINEAR)
            {
                return 1f - ratio01;
            }

            return Mathf.Cos(ratio01 * Mathf.PI * 0.5f);
        }

        private float GetFadeInFactor(float ratio01)
        {
            if (_fadeCurve == BGM_FADE_CURVE.LINEAR)
            {
                return ratio01;
            }

            return Mathf.Sin(ratio01 * Mathf.PI * 0.5f);
        }

        private static void SetUpSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }
    }
}

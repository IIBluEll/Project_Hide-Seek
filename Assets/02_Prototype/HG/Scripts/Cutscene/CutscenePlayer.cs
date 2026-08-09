using System;
using System.Collections;
using HideSeek.Sound;
using UnityEngine;
using UnityEngine.Playables;

namespace HideSeek.Cutscene
{
    /// <summary>
    /// Timeline 컷신을 재생한다. 사망 컷신과 탈출 컷신이 같은 컴포넌트를 쓰고, 차이는 전부 인스펙터 설정이다.
    ///
    /// 연출의 타이밍과 배치는 Timeline 에셋이 담당하고 이 클래스는 재생과 완료 통보만 처리한다.
    /// 플레이어 컴포넌트는 건드리지 않는다. 화면은 컷신 카메라의 Priority가 더 높고
    /// 배경을 불투명하게 지우는 것으로 덮는다. 조작 정지와 은닉은 <see cref="Started"/> 구독자가 처리한다.
    ///
    /// 월드 오브젝트를 Timeline에 바인딩하는 컷신은 프리팹으로 만들면 안 된다.
    /// 바인딩이 Director 인스턴스에 저장되어 프리팹 에셋이 씬 오브젝트를 참조할 수 없기 때문이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutscenePlayer : MonoBehaviour
    {
        [Header("Cutscene")]
        [SerializeField] private PlayableDirector _director;

        [Tooltip("컷신 전용 오브젝트를 묶은 루트. 평소에는 비활성으로 둔다. " +
                 "이 컴포넌트가 붙은 오브젝트여서는 안 된다. 켜 둘 것이 없으면 비워도 된다.")]
        [SerializeField] private GameObject _stageRootObj;

        [Header("Sound")]
        [Tooltip("연결하면 컷신 시작 시 재생 중인 BGM을 페이드 아웃한다.")]
        [SerializeField] private BgmPlayer _bgmPlayer;

        [SerializeField , Min(0f)] private float _bgmFadeOutDuration = 0.2f;

        [Tooltip("연결하면 컷신 시작과 함께 반복 재생한다. 결과 화면까지 이어지도록 " +
                 "Timeline 트랙이 아니라 여기서 재생한다. 진행 중인 BGM을 이어갈 때는 비워 둔다.")]
        [SerializeField] private AudioSource _loopBgmSource;

        [Header("Fade")]
        [Tooltip("선택 항목. 연결하면 컷신 종료 후 결과 화면이 뜰 때까지 화면을 덮은 상태로 유지한다. " +
                 "마지막 프레임에서 바로 결과 화면으로 넘길 때는 비워 둔다.")]
        [SerializeField] private CanvasGroup _fadeCanvasGroup;

        /// <summary>
        /// 재생 시작. 플레이어 은닉이나 조작 정지가 필요한 담당자가 이 이벤트를 구독한다.
        /// </summary>
        public event Action Started;

        public event Action Finished;

        public bool IsPlaying => _isPlaying;

        private bool _isPlaying;
        private Coroutine _playRoutine;

        private void Awake()
        {
            if (_stageRootObj != null)
            {
                _stageRootObj.SetActive(false);
            }

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
            }
        }

        public bool TryPlay()
        {
            if (_isPlaying)
            {
                return false;
            }

            if (!ValidateReferences())
            {
                return false;
            }

            _isPlaying = true;

            PrepareStage();
            SwitchBgm();

            // 배우 배치가 끝난 뒤에 알린다. 구독자가 플레이어를 숨기는 시점이 여기다.
            Started?.Invoke();

            // 일시 정지 등으로 timeScale이 바뀌어도 연출 길이가 흔들리지 않게 한다.
            _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

            // 마지막 프레임을 유지한다. None으로 두면 재생이 끝나는 순간 Animator가
            // 그래프를 놓아 배우가 시작 포즈로 되돌아가는 것이 그대로 보인다.
            _director.extrapolationMode = DirectorWrapMode.Hold;

            _director.Play();

            _playRoutine = StartCoroutine(WaitCutscene_cor());

            return true;
        }

        /// <summary>
        /// 재생을 중단하고 되돌린다. 재시작이 씬 재로드라면 호출할 필요가 없다.
        /// </summary>
        public void Stop()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (_director != null)
            {
                _director.Stop();
            }

            if (_loopBgmSource != null)
            {
                _loopBgmSource.Stop();
            }

            if (_stageRootObj != null)
            {
                _stageRootObj.SetActive(false);
            }

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
            }

            _isPlaying = false;
        }

        private void PrepareStage()
        {
            if (_stageRootObj != null)
            {
                _stageRootObj.SetActive(true);
            }

            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
            }
        }

        private void SwitchBgm()
        {
            // 컷신 사운드가 진행 중인 BGM에 묻히지 않게 먼저 비운다.
            if (_bgmPlayer != null)
            {
                _bgmPlayer.Stop(_bgmFadeOutDuration);
            }

            if (_loopBgmSource == null)
            {
                return;
            }

            // Timeline 트랙에 얹으면 재생이 끝나는 순간 소리가 끊긴다.
            // 결과 화면까지 루프가 이어져야 하므로 Director와 무관하게 재생한다.
            // 인스펙터에서 Loop 체크를 빠뜨려도 한 번만 울리고 끝나지 않게 한다.
            _loopBgmSource.loop = true;
            _loopBgmSource.Play();
        }

        private bool ValidateReferences()
        {
            if (_director == null)
            {
                Debug.LogError("[CutscenePlayer] PlayableDirector가 없습니다." , this);

                return false;
            }

            if (_director.playableAsset == null)
            {
                Debug.LogError("[CutscenePlayer] PlayableDirector에 Timeline 에셋이 연결되지 않았습니다." , this);

                return false;
            }

            // 루트를 끄면 이 컴포넌트와 Director까지 함께 꺼져 완료 통보가 오지 않는다.
            if (_stageRootObj == gameObject)
            {
                Debug.LogError(
                    "[CutscenePlayer] 스테이지 루트는 이 컴포넌트가 붙은 오브젝트가 아니라 자식이어야 합니다." ,
                    this);

                return false;
            }

            return true;
        }

        // extrapolationMode가 Hold라 재생이 끝나도 stopped가 오지 않으므로 길이로 완료를 잡는다.
        // Director와 이 대기 모두 실시간 기준이라 어긋나지 않는다.
        private IEnumerator WaitCutscene_cor()
        {
            yield return new WaitForSecondsRealtime((float)_director.duration);

            _playRoutine = null;
            _isPlaying = false;

            if (_fadeCanvasGroup != null)
            {
                // Timeline이 알파를 끝까지 올리지 못한 경우까지 포함해 암전을 코드로 확정한다.
                _fadeCanvasGroup.alpha = 1f;
            }

            Finished?.Invoke();
        }
    }
}

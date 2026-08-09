using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace HideSeek.Cutscene
{
    /// <summary>
    /// Timeline 컷신의 공통 재생 절차다. 사망 컷신과 탈출 컷신이 공유한다.
    /// 연출의 타이밍과 배치는 전부 Timeline 에셋이 담당하고, 이 클래스는 재생과 완료 통보만 처리한다.
    ///
    /// 플레이어 컴포넌트는 건드리지 않는다. 화면은 컷신 카메라의 Priority가 더 높고
    /// 배경을 불투명하게 지우는 것으로 덮는다. 조작 정지는 진우님 쪽에서 처리한다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ACutscenePlayer : MonoBehaviour
    {
        [Header("Cutscene")]
        [SerializeField] protected PlayableDirector _director;

        public event Action Finished;

        public bool IsPlaying => _isPlaying;

        private bool _isPlaying;
        private Coroutine _playRoutine;

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

            OnCutsceneBegin();

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
        /// 재생을 중단한다. 재시작이 씬 재로드라면 호출할 필요가 없다.
        /// </summary>
        public virtual void Stop()
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

            _isPlaying = false;
        }

        protected virtual void OnCutsceneBegin()
        {
        }

        protected virtual void OnCutsceneEnd()
        {
        }

        protected virtual bool ValidateReferences()
        {
            if (_director == null)
            {
                Debug.LogError($"[{GetType().Name}] PlayableDirector가 없습니다." , this);

                return false;
            }

            if (_director.playableAsset == null)
            {
                Debug.LogError($"[{GetType().Name}] PlayableDirector에 Timeline 에셋이 연결되지 않았습니다." , this);

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

            OnCutsceneEnd();

            Finished?.Invoke();
        }
    }
}

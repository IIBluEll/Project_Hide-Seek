using System;
using UnityEngine;

namespace HideSeek.Gameplay
{
    public sealed class GameProgressProvider : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _requiredGeneratorCount = 3;

        private bool _wereAllGeneratorsCompleted;

        public event Action<int> CompletedGeneratorCountChanged;
        public event Action AllGeneratorsCompleted;

        public int CompletedGeneratorCount { get; private set; }
        public int RequiredGeneratorCount => _requiredGeneratorCount;
        public bool AreAllGeneratorsCompleted => CompletedGeneratorCount >= _requiredGeneratorCount;

        public void NotifyGeneratorCompleted()
        {
            if ( AreAllGeneratorsCompleted )
            {
                return;
            }

            CompletedGeneratorCount = Mathf.Min(CompletedGeneratorCount + 1 , _requiredGeneratorCount);
            CompletedGeneratorCountChanged?.Invoke(CompletedGeneratorCount);

            Debug.Log($"[GameProgressProvider] 발전기 완료: {CompletedGeneratorCount}/{_requiredGeneratorCount}" , this);

            UpdateAllGeneratorsCompletedState();
        }

        public void SetRequiredGeneratorCount(int requiredGeneratorCount)
        {
            int previousCompletedGeneratorCount = CompletedGeneratorCount;

            _requiredGeneratorCount = Mathf.Max(1 , requiredGeneratorCount);
            CompletedGeneratorCount = Mathf.Min(CompletedGeneratorCount , _requiredGeneratorCount);

            if ( previousCompletedGeneratorCount != CompletedGeneratorCount )
            {
                CompletedGeneratorCountChanged?.Invoke(CompletedGeneratorCount);
            }

            UpdateAllGeneratorsCompletedState();
        }

        public void ResetProgress()
        {
            CompletedGeneratorCount = 0;
            _wereAllGeneratorsCompleted = false;

            CompletedGeneratorCountChanged?.Invoke(CompletedGeneratorCount);

            Debug.Log("[GameProgressProvider] 게임 진행도가 초기화되었습니다." , this);
        }

        [ContextMenu("Debug/Complete Generator")]
        private void CompleteGeneratorForDebug()
        {
            NotifyGeneratorCompleted();
        }

        [ContextMenu("Debug/Reset Progress")]
        private void ResetProgressForDebug()
        {
            ResetProgress();
        }

        private void UpdateAllGeneratorsCompletedState()
        {
            if ( !AreAllGeneratorsCompleted )
            {
                _wereAllGeneratorsCompleted = false;

                return;
            }

            if ( _wereAllGeneratorsCompleted )
            {
                return;
            }

            _wereAllGeneratorsCompleted = true;
            AllGeneratorsCompleted?.Invoke();

            Debug.Log("[GameProgressProvider] 모든 발전기가 완료되었습니다." , this);
        }

        private void OnValidate()
        {
            _requiredGeneratorCount = Mathf.Max(1 , _requiredGeneratorCount);
            CompletedGeneratorCount = Mathf.Clamp(CompletedGeneratorCount , 0 , _requiredGeneratorCount);
        }
    }
}

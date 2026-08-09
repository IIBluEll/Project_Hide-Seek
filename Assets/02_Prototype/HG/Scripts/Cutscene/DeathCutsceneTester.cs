using UnityEngine;
using UnityEngine.InputSystem;

namespace HideSeek.Cutscene
{
    /// <summary>
    /// 프로토타입 검증용 임시 입력이다.
    /// AI와 플레이어 없이 컷신만 단독으로 확인할 때 쓰고, 통합 후에는 삭제한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeathCutsceneTester : MonoBehaviour
    {
        [SerializeField] private DeathCutsceneStage _deathCutsceneStage;
        [SerializeField] private Camera _playerCamera;

        [SerializeField] private Key _playKey = Key.K;
        [SerializeField] private Key _resetKey = Key.L;

        private void Update()
        {
            if (_deathCutsceneStage == null)
            {
                return;
            }

            Keyboard tKeyboard = Keyboard.current;

            if (tKeyboard == null)
            {
                return;
            }

            if (tKeyboard[_playKey].wasPressedThisFrame)
            {
                _deathCutsceneStage.TryPlay(_playerCamera);

                return;
            }

            if (tKeyboard[_resetKey].wasPressedThisFrame)
            {
                _deathCutsceneStage.Stop();
            }
        }
    }
}

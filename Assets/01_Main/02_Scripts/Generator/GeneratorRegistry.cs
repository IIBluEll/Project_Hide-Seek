using System;
using System.Collections.Generic;
using UnityEngine;

namespace HideSeek.Generators
{
    /// <summary>
    /// 발전기 등록 수명만 맡는다. 구독, 초기 훑기, 중복 방지, 정리를 한곳에 모아
    /// 발전기를 지켜보는 쪽마다 같은 배관을 복사하지 않게 한다.
    ///
    /// MonoBehaviour가 아니라서 이 클래스만 따로 검증할 수 있다. <see cref="QteRunner"/>와 같은 이유다.
    ///
    /// 초기 훑기가 필요한 이유는 <see cref="Generator.Enabled"/>가 OnEnable에서 발행되기 때문이다.
    /// 구독하는 쪽보다 먼저 켜진 발전기는 이벤트를 이미 놓쳤다.
    /// </summary>
    public sealed class GeneratorRegistry
    {
        private readonly List<Generator> LIST_GENERATOR = new();

        private Action<Generator> _registeredCallback;
        private Action<Generator> _unregisteredCallback;
        private bool _isAttached;

        public IReadOnlyList<Generator> Generators => LIST_GENERATOR;

        /// <summary>
        /// 구독을 시작하고 이미 활성화된 발전기에 대해 즉시 콜백을 돌린다. Awake에서 부른다.
        /// 콜백은 null과 중복이 걸러진 뒤에 불리므로 받는 쪽이 그 검사를 다시 하지 않아도 된다.
        /// </summary>
        public void Attach(Action<Generator> registeredCallback , Action<Generator> unregisteredCallback)
        {
            if (_isAttached)
            {
                return;
            }

            _isAttached = true;
            _registeredCallback = registeredCallback;
            _unregisteredCallback = unregisteredCallback;

            Generator.Enabled += OnGeneratorEnabledActioned;
            Generator.Disabled += OnGeneratorDisabledActioned;

            // MonoBehaviour가 아니라 FindObjectsByType을 물려받지 못한다. System.Object와 겹쳐 이름을 다 적는다.
            Generator[] tArr_generator = UnityEngine.Object.FindObjectsByType<Generator>(FindObjectsSortMode.None);
            for (int i = 0; i < tArr_generator.Length; i++)
            {
                Register(tArr_generator[i]);
            }
        }

        /// <summary>
        /// 구독을 끊고 등록된 발전기를 역순으로 전부 해제한다. OnDestroy에서 부른다.
        /// 구독을 먼저 끊어야 남은 발전기의 OnDisable이 사라지는 중인 쪽을 다시 건드리지 않는다.
        /// </summary>
        public void Detach()
        {
            if (_isAttached == false)
            {
                return;
            }

            Generator.Enabled -= OnGeneratorEnabledActioned;
            Generator.Disabled -= OnGeneratorDisabledActioned;

            for (int i = LIST_GENERATOR.Count - 1; i >= 0; i--)
            {
                Unregister(LIST_GENERATOR[i]);
            }

            LIST_GENERATOR.Clear();

            _registeredCallback = null;
            _unregisteredCallback = null;
            _isAttached = false;
        }

        private void OnGeneratorEnabledActioned(Generator generator)
        {
            Register(generator);
        }

        private void OnGeneratorDisabledActioned(Generator generator)
        {
            Unregister(generator);
        }

        private void Register(Generator generator)
        {
            if (generator == null || LIST_GENERATOR.Contains(generator))
            {
                return;
            }

            LIST_GENERATOR.Add(generator);
            _registeredCallback?.Invoke(generator);
        }

        private void Unregister(Generator generator)
        {
            if (generator == null || LIST_GENERATOR.Remove(generator) == false)
            {
                return;
            }

            _unregisteredCallback?.Invoke(generator);
        }
    }
}

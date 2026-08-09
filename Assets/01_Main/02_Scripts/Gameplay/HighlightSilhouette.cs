using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HideSeek.Gameplay
{
    /// <summary>
    /// 붙은 오브젝트의 모델을 벽 너머까지 보이는 단색 실루엣으로 표시한다.
    /// 표시 대상을 가리지 않는다. 발전기와 탈출 문이 같은 컴포넌트를 쓴다.
    ///
    /// 원본 Renderer의 머티리얼은 건드리지 않는다. 같은 메시를 쓰는 표시 전용 오브젝트를 자식으로 만들어 켜고 끄므로
    /// 원본의 머티리얼 교체나 셰이더 설정과 충돌하지 않는다.
    ///
    /// 언제 표시할지는 이 컴포넌트가 판단하지 않는다. 표시를 관리하는 쪽이
    /// <see cref="SetMaterial"/>과 <see cref="SetVisible"/>을 호출한다. 그 상대가 누구인지는 알지 않는다.
    /// 발전기는 소모성 능력으로, 탈출 문은 발전기 완료 시점부터 상시로 켜는 식이다.
    ///
    /// 자기 존재를 알리지 않는다. 관리하는 쪽이 대상을 등록할 때 같은 오브젝트에서 이 컴포넌트를 찾아간다.
    /// 대상 1개당 등록 경로가 하나여야 누구의 실루엣인지 알 수 있기 때문이다.
    ///
    /// 머티리얼도 인스펙터에 두지 않는다. 색은 관리하는 쪽이 정할 문제라 등록하는 쪽이 하나를 넣어준다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighlightSilhouette : MonoBehaviour
    {
        [Tooltip("비워 두면 이 오브젝트와 자식의 MeshRenderer, SkinnedMeshRenderer를 모두 찾아 쓴다. 일부만 표시하려면 직접 지정한다.")]
        [SerializeField] private Renderer[] _sourceRenderers;

        private const string SILHOUETTE_SUFFIX = " (Highlight)";

        private readonly List<Renderer> LIST_SILHOUETTE = new();

        private Material _highlightMaterial;
        private bool _isBuilt;
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        private void OnDisable()
        {
            // 컴포넌트만 꺼지면 실루엣은 계속 보인다. 다시 켜질 때 관리하는 쪽이 현재 상태를 다시 넣어준다.
            SetVisible(false);
        }

        private void OnDestroy()
        {
            // 실루엣을 다른 오브젝트 아래에 만들었을 수 있으므로 직접 지운다.
            for (int i = 0; i < LIST_SILHOUETTE.Count; i++)
            {
                if (LIST_SILHOUETTE[i] != null)
                {
                    Destroy(LIST_SILHOUETTE[i].gameObject);
                }
            }

            LIST_SILHOUETTE.Clear();
        }

        public void SetMaterial(Material highlightMaterial)
        {
            if (highlightMaterial == null)
            {
                return;
            }

            _highlightMaterial = highlightMaterial;

            for (int i = 0; i < LIST_SILHOUETTE.Count; i++)
            {
                if (LIST_SILHOUETTE[i] != null)
                {
                    ApplyMaterial(LIST_SILHOUETTE[i] , LIST_SILHOUETTE[i].sharedMaterials.Length);
                }
            }
        }

        public void SetVisible(bool isVisible)
        {
            if (isVisible && _isBuilt == false)
            {
                if (_highlightMaterial == null)
                {
                    Debug.LogError($"[{nameof(HighlightSilhouette)}] 머티리얼이 없어 표시할 수 없습니다. 등록하는 쪽이 SetMaterial을 먼저 호출해야 합니다." , this);
                    return;
                }

                Build();
            }

            _isVisible = isVisible;

            for (int i = 0; i < LIST_SILHOUETTE.Count; i++)
            {
                if (LIST_SILHOUETTE[i] != null)
                {
                    LIST_SILHOUETTE[i].gameObject.SetActive(isVisible);
                }
            }
        }

        // 실루엣은 처음 표시할 때 한 번만 만든다. 표시하지 않는 대상은 오브젝트를 늘리지 않는다.
        private void Build()
        {
            _isBuilt = true;

            Renderer[] tArr_source = _sourceRenderers != null && _sourceRenderers.Length > 0
                ? _sourceRenderers
                : GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < tArr_source.Length; i++)
            {
                CreateSilhouette(tArr_source[i]);
            }

            if (LIST_SILHOUETTE.Count == 0)
            {
                Debug.LogError($"[{nameof(HighlightSilhouette)}] 표시할 메시를 찾지 못했습니다. 모델보다 상위에 두거나 인스펙터에서 Renderer를 지정해야 합니다." , this);
            }
        }

        private void CreateSilhouette(Renderer source)
        {
            if (source == null)
            {
                return;
            }

            SkinnedMeshRenderer tSourceSkinned = source as SkinnedMeshRenderer;
            Mesh tMesh = tSourceSkinned != null ? tSourceSkinned.sharedMesh : GetMesh(source);

            // ParticleSystemRenderer처럼 메시가 없는 Renderer는 실루엣을 만들 수 없다.
            if (tMesh == null)
            {
                return;
            }

            GameObject tObj = new GameObject(source.name + SILHOUETTE_SUFFIX);
            tObj.transform.SetParent(source.transform , false);
            tObj.layer = source.gameObject.layer;
            tObj.SetActive(false);

            Renderer tSilhouette;

            if (tSourceSkinned != null)
            {
                SkinnedMeshRenderer tSkinned = tObj.AddComponent<SkinnedMeshRenderer>();
                tSkinned.sharedMesh = tMesh;
                tSkinned.bones = tSourceSkinned.bones;
                tSkinned.rootBone = tSourceSkinned.rootBone;
                tSkinned.localBounds = tSourceSkinned.localBounds;
                tSkinned.updateWhenOffscreen = tSourceSkinned.updateWhenOffscreen;

                tSilhouette = tSkinned;
            }
            else
            {
                tObj.AddComponent<MeshFilter>().sharedMesh = tMesh;
                tSilhouette = tObj.AddComponent<MeshRenderer>();
            }

            tSilhouette.shadowCastingMode = ShadowCastingMode.Off;
            tSilhouette.receiveShadows = false;
            tSilhouette.lightProbeUsage = LightProbeUsage.Off;
            tSilhouette.reflectionProbeUsage = ReflectionProbeUsage.Off;
            tSilhouette.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            ApplyMaterial(tSilhouette , tMesh.subMeshCount);

            LIST_SILHOUETTE.Add(tSilhouette);
        }

        private static Mesh GetMesh(Renderer source)
        {
            return source.TryGetComponent(out MeshFilter tFilter) ? tFilter.sharedMesh : null;
        }

        // 서브메시마다 슬롯이 필요하다. 슬롯 수가 모자라면 나머지 서브메시가 그려지지 않는다.
        private void ApplyMaterial(Renderer silhouette , int slotCount)
        {
            if (silhouette == null || _highlightMaterial == null)
            {
                return;
            }

            Material[] tArr_material = new Material[Mathf.Max(1 , slotCount)];
            for (int i = 0; i < tArr_material.Length; i++)
            {
                tArr_material[i] = _highlightMaterial;
            }

            silhouette.sharedMaterials = tArr_material;
        }
    }
}

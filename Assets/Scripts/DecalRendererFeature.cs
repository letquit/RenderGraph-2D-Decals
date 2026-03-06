using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class DecalRendererFeature : ScriptableRendererFeature
{
    [SerializeField] DecalRendererFeatureSettings settings;

    [Serializable]
    public class DecalSettings
    {
        public Material DecalMaterial;
        public Mesh DecalMesh;
        public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public DecalSettings Settings = new DecalSettings();
    DecalRendererPass _decalPass;

    public override void Create()
    {
        _decalPass = new DecalRendererPass("2D Decal Pass");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (Settings.DecalMaterial == null || Settings.DecalMesh == null)
        {
            return;
        }

        if (renderingData.cameraData.cameraType == CameraType.SceneView)
        {
            return;
        }
        
        _decalPass.Setup(Settings);
        
        renderer.EnqueuePass(_decalPass);
    }

    [Serializable]
    public class DecalRendererFeatureSettings
    {
        
    }

    class DecalRendererPass : ScriptableRenderPass
    {
        readonly DecalRendererFeatureSettings settings;
        private DecalSettings _settings;
        private ProfilingSampler _profilingSampler;

        public DecalRendererPass(string profilerTag)
        {
            _profilingSampler = new ProfilingSampler(profilerTag);
        }
        
        public DecalRendererPass(DecalRendererFeatureSettings settings)
        {
            this.settings = settings;
        }

        private class PassData
        {
            public Material DecalMaterial;
            public Mesh DecalMesh;
            public List<Matrix4x4> DecalMatrices;
        }

        public void Setup(DecalSettings settings)
        {
            _settings = settings;
            renderPassEvent = settings.RenderPassEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Render Custom Pass";

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, _profilingSampler))
            {
                if (DecalManager.Instance == null || DecalManager.Instance.ActiveDecals.Count == 0)
                {
                    return;
                }
                
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                
                passData.DecalMaterial = _settings.DecalMaterial;
                passData.DecalMesh = _settings.DecalMesh;
                passData.DecalMatrices = new List<Matrix4x4>(DecalManager.Instance.ActiveDecals.Count);
                foreach (var decal in DecalManager.Instance.ActiveDecals)
                {
                    passData.DecalMatrices.Add(Matrix4x4.TRS(decal.Position, decal.Rotation, decal.Size));
                }

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                builder.SetRenderFunc<PassData>(ExecutePass);
            }
        }
        
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            context.cmd.DrawMeshInstanced(data.DecalMesh, 0, data.DecalMaterial, 0, data.DecalMatrices.ToArray());
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// 贴花渲染器功能类，用于在URP中添加自定义贴花渲染通道
/// </summary>
public class DecalRendererFeature : ScriptableRendererFeature
{
    [SerializeField] DecalRendererFeatureSettings settings;

    /// <summary>
    /// 贴花设置类，包含贴花渲染所需的基本配置
    /// </summary>
    [Serializable]
    public class DecalSettings
    {
        public Material DecalMaterial; // 贴花材质
        public Mesh DecalMesh; // 贴花网格
        public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingTransparents; // 渲染通道事件
    }

    public DecalSettings Settings = new DecalSettings();
    DecalRendererPass _decalPass;

    /// <summary>
    /// 创建渲染通道
    /// </summary>
    public override void Create()
    {
        _decalPass = new DecalRendererPass("2D Decal Pass");
    }

    /// <summary>
    /// 添加渲染通道到渲染队列
    /// </summary>
    /// <param name="renderer">可脚本化渲染器</param>
    /// <param name="renderingData">渲染数据</param>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 检查贴花材质和网格是否为空
        if (Settings.DecalMaterial == null || Settings.DecalMesh == null)
        {
            return;
        }

        // 不在场景视图中渲染
        if (renderingData.cameraData.cameraType == CameraType.SceneView)
        {
            return;
        }
        
        _decalPass.Setup(Settings);
        
        renderer.EnqueuePass(_decalPass);
    }

    /// <summary>
    /// 贴花渲染器功能设置类
    /// </summary>
    [Serializable]
    public class DecalRendererFeatureSettings
    {
        
    }

    /// <summary>
    /// 贴花渲染通道类，负责具体的贴花渲染逻辑
    /// </summary>
    class DecalRendererPass : ScriptableRenderPass
    {
        readonly DecalRendererFeatureSettings settings;
        private DecalSettings _settings;
        private ProfilingSampler _profilingSampler;

        /// <summary>
        /// 构造函数，使用性能分析采样器初始化
        /// </summary>
        /// <param name="profilerTag">性能分析标签</param>
        public DecalRendererPass(string profilerTag)
        {
            _profilingSampler = new ProfilingSampler(profilerTag);
        }
        
        /// <summary>
        /// 构造函数，使用设置初始化
        /// </summary>
        /// <param name="settings">贴花渲染器功能设置</param>
        public DecalRendererPass(DecalRendererFeatureSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// 传递数据类，包含渲染所需的数据
        /// </summary>
        private class PassData
        {
            public Material DecalMaterial;
            public Mesh DecalMesh;
            public List<Matrix4x4> DecalMatrices;
        }

        /// <summary>
        /// 设置贴花渲染通道参数
        /// </summary>
        /// <param name="settings">贴花设置</param>
        public void Setup(DecalSettings settings)
        {
            _settings = settings;
            renderPassEvent = settings.RenderPassEvent;
        }

        /// <summary>
        /// 记录渲染图，构建贴花渲染通道
        /// </summary>
        /// <param name="renderGraph">渲染图</param>
        /// <param name="frameData">帧数据容器</param>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Render Custom Pass";

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, _profilingSampler))
            {
                // 检查贴花管理器是否存在以及是否有活跃的贴花
                if (DecalManager.Instance == null || DecalManager.Instance.ActiveDecals.Count == 0)
                {
                    return;
                }
                
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                
                passData.DecalMaterial = _settings.DecalMaterial;
                passData.DecalMesh = _settings.DecalMesh;
                passData.DecalMatrices = new List<Matrix4x4>(DecalManager.Instance.ActiveDecals.Count);
                
                // 遍历所有活跃的贴花，计算其变换矩阵
                foreach (var decal in DecalManager.Instance.ActiveDecals)
                {
                    passData.DecalMatrices.Add(Matrix4x4.TRS(decal.Position, decal.Rotation, decal.Size));
                }

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                builder.SetRenderFunc<PassData>(ExecutePass);
            }
        }
        
        /// <summary>
        /// 执行贴花渲染通道
        /// </summary>
        /// <param name="data">传递数据</param>
        /// <param name="context">光栅图上下文</param>
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            context.cmd.DrawMeshInstanced(data.DecalMesh, 0, data.DecalMaterial, 0, data.DecalMatrices.ToArray());
        }
    }
}

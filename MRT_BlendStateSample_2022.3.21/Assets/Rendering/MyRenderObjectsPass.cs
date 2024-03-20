using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace MyRendering
{
    public class MyRenderObjectsPass : ScriptableRenderPass
    {
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        ProfilingSampler m_ProfilingSampler;

        public Material overrideMaterial { get; set; }
        public int overrideMaterialPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        RenderStateBlock m_RenderStateBlock;
        NativeArray<RenderStateBlock> m_RenderStateBlocks;

        private const int TextureCount = 2;
        private static readonly string DepthTextureName = "_CameraDepthTexture";
        private static readonly string[] ColorTextureNames = new string[TextureCount]
        {
            "_ColorTexture1",
            "_ColorTexture2"
        };
        private RTHandle[] _colorAttachmentHandles = new RTHandle[TextureCount];

        public MyRenderObjectsPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags,
            RenderQueueType renderQueueType, int layerMask)
        {
            base.profilingSampler = new ProfilingSampler(nameof(MyRenderObjectsPass));

            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            }

            m_RenderStateBlock.mask |= RenderStateMask.Blend;
            m_RenderStateBlock.blendState = new BlendState(separateMRTBlend: true)
            {
                // RT0 : シェーダー側のブレンドモードを使用
                blendState0 = RenderTargetBlendState.defaultValue,

                // RT1 : Blend One One で上書き
                blendState1 = new RenderTargetBlendState(
                    sourceAlphaBlendMode: BlendMode.One,
                    destinationColorBlendMode: BlendMode.One)
            };
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            // Then using RTHandles, the color and the depth properties must be separate
            desc.colorFormat = RenderTextureFormat.ARGB32;
            desc.depthBufferBits = 0;
            
            // カラーテクスチャ確保
            for (int i = 0; i < TextureCount; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(
                    ref _colorAttachmentHandles[i],
                    desc, 
                    FilterMode.Point,
                    TextureWrapMode.Clamp,
                    name: ColorTextureNames[i]);
            }
        }

        public void Dispose()
        {
            Debug.Log("Dispose");
            for (int i = 0; i < _colorAttachmentHandles.Length; i++)
            {
                _colorAttachmentHandles[i]?.Release();
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            
            // レンダーターゲット指定
            ConfigureTarget(_colorAttachmentHandles);
            
            // クリア
            ConfigureClear(ClearFlag.Color, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings =
                CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings,
                    ref m_RenderStateBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}

using System.Collections.Generic;
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

        private const int TextureCount = 2;
        private static readonly string DepthTextureName = "_CameraDepthTexture";
        private static readonly string[] ColorTextureNames = new string[TextureCount]
        {
            "_ColorTexture1",
            "_ColorTexture2"
        };

        private RenderTargetIdentifier[] ColorAttachments;
        private RenderTargetIdentifier DepthAttachment;
        private int[] ColorPropertyIDs;

        public MyRenderObjectsPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags,
            RenderQueueType renderQueueType, int layerMask)
        {
            base.profilingSampler = new ProfilingSampler(nameof(MyRenderObjectsPass));

            CreateRenderTargets(); // MRT用のRenderTargetIdentifier・ShaderPropertyID作成

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
                // RT0 : Blend One Zero
                blendState0 = RenderTargetBlendState.defaultValue,

                // RT1 : Blend One One
                blendState1 = new RenderTargetBlendState(
                    sourceColorBlendMode: BlendMode.One,
                    destinationColorBlendMode: BlendMode.One)
            };
        }

        private void CreateRenderTargets()
        {
            ColorAttachments = new RenderTargetIdentifier[TextureCount];
            ColorPropertyIDs = new int[TextureCount];
            for (int i = 0; i < TextureCount; i++)
            {
                var textureName = ColorTextureNames[i];
                ColorAttachments[i] = new RenderTargetIdentifier(textureName);
                ColorPropertyIDs[i] = Shader.PropertyToID(textureName);
            }

            DepthAttachment = new RenderTargetIdentifier(DepthTextureName);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            var desc = cameraTextureDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGB32;

            // テクスチャ確保
            foreach (var nameID in ColorPropertyIDs)
            {
                cmd.GetTemporaryRT(nameID, desc);
            }

            // レンダーターゲット指定
            ConfigureTarget(ColorAttachments, DepthAttachment);

            // クリア
            ConfigureClear(ClearFlag.Color, Color.clear);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            base.OnCameraCleanup(cmd);

            // テクスチャ解放
            foreach (var nameID in ColorPropertyIDs)
            {
                cmd.ReleaseTemporaryRT(nameID);
            }
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
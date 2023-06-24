using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public static class ShaderPropertyId
    {
        public static readonly int ColorTex = Shader.PropertyToID("_ColorTex");
        public static readonly int NormalTex = Shader.PropertyToID("_NormalTex");
    }
    
    public class MyRenderTargetBuffer
    {
        private static bool isInitialize = false;
        
        static RTHandle m_MyColorTexture;
        static RTHandle m_MyDepthTexture;
        static RTHandle m_MyNormalTexture;

        public static RTHandle MyColorTexture => m_MyColorTexture;
        public static RTHandle MyDepthTexture => m_MyDepthTexture;
        public static RTHandle MyNormalTexture => m_MyNormalTexture;
        
        public static RTHandle[] ColorAttachments { get; private set; }
        public static RTHandle DepthAttachment { get; private set; }

        public static void Setup(RenderTextureDescriptor desc)
        {
            if (isInitialize)
            {
                return;
            }
            isInitialize = true;
            
            Debug.Log("Alloc RTHandle");
            
            var colorDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGB32, 0);
            var depthDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.Depth, 8);
            var normalDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGB32, 0);

            RenderingUtils.ReAllocateIfNeeded(ref m_MyColorTexture, colorDesc, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_MyColorTexture");
            RenderingUtils.ReAllocateIfNeeded(ref m_MyDepthTexture, depthDesc, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_MyDepthTexture");
            RenderingUtils.ReAllocateIfNeeded(ref m_MyNormalTexture, normalDesc, FilterMode.Point, TextureWrapMode.Clamp,
                name: "_MyNormalTexture");
            
            ColorAttachments = new[] { MyColorTexture, MyNormalTexture };
            DepthAttachment = MyDepthTexture;
        }

        public static void Dispose()
        {
            if (!isInitialize)
            {
                return;
            }
            Debug.Log("Release RTHandle");
            
            DepthAttachment = null;
            ColorAttachments = null;
            
            RTHandles.Release(m_MyNormalTexture);
            m_MyNormalTexture = null;

            RTHandles.Release(MyDepthTexture);
            m_MyDepthTexture = null;

            RTHandles.Release(MyColorTexture);
            m_MyColorTexture = null;

            isInitialize = false;
        }
    }
    
    public class MyRenderObjectsFeature : ScriptableRendererFeature
    {
        public RenderObjects.RenderObjectsSettings settings = new RenderObjects.RenderObjectsSettings();

        RenderObjectsPass renderObjectsPass;

        /// <inheritdoc/>
        public override void Create()
        {
            RenderObjects.FilterSettings filter = settings.filterSettings;

            if (settings.Event < RenderPassEvent.BeforeRenderingPrePasses)
                settings.Event = RenderPassEvent.BeforeRenderingPrePasses;

            renderObjectsPass = new RenderObjectsPass(settings.passTag, settings.Event, filter.PassNames,
                filter.RenderQueueType, filter.LayerMask, settings.cameraSettings);

            switch (settings.overrideMode)
            {
                case RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None:
                    renderObjectsPass.overrideMaterial = null;
                    renderObjectsPass.overrideShader = null;
                    break;
                case RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Material:
                    renderObjectsPass.overrideMaterial = settings.overrideMaterial;
                    renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;
                    renderObjectsPass.overrideShader = null;
                    break;
                case RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Shader:
                    renderObjectsPass.overrideMaterial = null;
                    renderObjectsPass.overrideShader = settings.overrideShader;
                    renderObjectsPass.overrideShaderPassIndex = settings.overrideShaderPassIndex;
                    break;
            }

            if (settings.overrideDepthState)
                renderObjectsPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

            if (settings.stencilSettings.overrideStencilState)
                renderObjectsPass.SetStencilState(settings.stencilSettings.stencilReference,
                    settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                    settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            MyRenderTargetBuffer.Setup(renderingData.cameraData.cameraTargetDescriptor);

            renderObjectsPass.ConfigureTarget(MyRenderTargetBuffer.ColorAttachments, MyRenderTargetBuffer.DepthAttachment);
            renderObjectsPass.ConfigureClear(ClearFlag.All, Color.clear);

            renderer.EnqueuePass(renderObjectsPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            MyRenderTargetBuffer.Dispose();
        }
    }
}

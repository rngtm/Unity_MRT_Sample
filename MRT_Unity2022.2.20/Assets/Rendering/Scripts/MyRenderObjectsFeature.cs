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
        
        public static RTHandle MyColorTexture { get; private set; }
        public static RTHandle MyDepthTexture { get; private set; }
        public static RTHandle MyNormalTexture { get; private set; }
        
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
            MyColorTexture = RTHandles.Alloc(colorDesc, name: "_MyColorTexture");
            MyDepthTexture = RTHandles.Alloc(depthDesc, name: "_MyDepthTexture");
            MyNormalTexture = RTHandles.Alloc(normalDesc, name: "_MyNormalTexture");
            
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
            
            RTHandles.Release(MyNormalTexture);
            MyNormalTexture = null;

            RTHandles.Release(MyDepthTexture);
            MyDepthTexture = null;

            RTHandles.Release(MyColorTexture);
            MyColorTexture = null;

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

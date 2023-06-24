using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    static class ShaderPropertyId
    {
        public static readonly int MyColorTexture = Shader.PropertyToID("_MyColorTexture");
        public static readonly int MyDepthTexture = Shader.PropertyToID("_MyDepthTexture");
        public static readonly int MyNormalTexture = Shader.PropertyToID("_MyNormalTexture");
        public static readonly int ColorTex = Shader.PropertyToID("_ColorTex");
        public static readonly int NormalTex = Shader.PropertyToID("_NormalTex");
    }

    public static class MyRenderTargetBuffer
    {
        private static bool isInitialize = false;
        
        public static RenderTargetIdentifier[] ColorAttachments;
        public static RenderTargetIdentifier MyDepthTexture;
        public static RenderTargetIdentifier MyColorTexture;
        public static RenderTargetIdentifier MyNormalTexture;
  
        public static void Initialize()
        {
            if (isInitialize)
            {
                return;
            }

            isInitialize = true;

            MyColorTexture = new RenderTargetIdentifier(ShaderPropertyId.MyColorTexture);
            MyNormalTexture = new RenderTargetIdentifier(ShaderPropertyId.MyNormalTexture);
            MyDepthTexture = new RenderTargetIdentifier(ShaderPropertyId.MyDepthTexture);

            ColorAttachments = new RenderTargetIdentifier[]
            {
                MyColorTexture,
                MyNormalTexture,
            };

        }

        public static void Dispose()
        {
            ColorAttachments = null;
            isInitialize = false;
        }
    }
    
    public class SetupRenderPass : ScriptableRenderPass
    {
        public SetupRenderPass(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor desc)
        {
            base.Configure(cmd, desc);

            var colorDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGB32, 0);
            var depthDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.Depth, 8);
            var normalDesc = new RenderTextureDescriptor(desc.width, desc.height, RenderTextureFormat.ARGB32, 0);
            cmd.GetTemporaryRT(ShaderPropertyId.MyColorTexture, colorDesc);
            cmd.GetTemporaryRT(ShaderPropertyId.MyDepthTexture, depthDesc);
            cmd.GetTemporaryRT(ShaderPropertyId.MyNormalTexture, normalDesc);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            base.OnCameraCleanup(cmd);
            
            cmd.ReleaseTemporaryRT(ShaderPropertyId.MyColorTexture);
            cmd.ReleaseTemporaryRT(ShaderPropertyId.MyDepthTexture);
            cmd.ReleaseTemporaryRT(ShaderPropertyId.MyNormalTexture);
        }
    }

    public class MyDrawObjectsFeature : ScriptableRendererFeature
    {
        public RenderObjects.RenderObjectsSettings settings = new RenderObjects.RenderObjectsSettings();
        private RenderObjectsPass renderObjectsPass;
        private SetupRenderPass setupRenderPass;
        
        public override void Create()
        {
            MyRenderTargetBuffer.Initialize();
            RenderObjects.FilterSettings filter = settings.filterSettings;

            // Render Objects pass doesn't support events before rendering prepasses.
            // The camera is not setup before this point and all rendering is monoscopic.
            // Events before BeforeRenderingPrepasses should be used for input texture passes (shadow map, LUT, etc) that doesn't depend on the camera.
            // These events are filtering in the UI, but we still should prevent users from changing it from code or
            // by changing the serialized data.
            if (settings.Event < RenderPassEvent.BeforeRenderingPrepasses)
                settings.Event = RenderPassEvent.BeforeRenderingPrepasses;

            renderObjectsPass = new RenderObjectsPass(settings.passTag, settings.Event, filter.PassNames,
                filter.RenderQueueType, filter.LayerMask, settings.cameraSettings);

            setupRenderPass = new SetupRenderPass(RenderPassEvent.BeforeRenderingPrepasses);

            renderObjectsPass.overrideMaterial = settings.overrideMaterial;
            renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;

            if (settings.overrideDepthState)
                renderObjectsPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

            if (settings.stencilSettings.overrideStencilState)
                renderObjectsPass.SetStencilState(settings.stencilSettings.stencilReference,
                    settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                    settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderObjectsPass.ConfigureTarget(MyRenderTargetBuffer.ColorAttachments, MyRenderTargetBuffer.MyDepthTexture);
            renderObjectsPass.ConfigureClear(ClearFlag.All, Color.clear);

            renderer.EnqueuePass(setupRenderPass);
            renderer.EnqueuePass(renderObjectsPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            MyRenderTargetBuffer.Dispose();
        }
    }
}
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyDeferredLightingFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material _material;

        public void Setup(Material material)
        {
            _material = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("My Deferred Lighting");
            var cameraColorTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
            
            // シェーダーの _ColorTex へ、テクスチャ _MyColorTexture を設定する
            cmd.SetGlobalTexture(ShaderPropertyId.ColorTex, MyRenderTargetBuffer.MyColorTexture);
            
            // シェーダーの _NormalTex へ、テクスチャ _MyNormalTexture を設定する
            cmd.SetGlobalTexture(ShaderPropertyId.NormalTex, MyRenderTargetBuffer.MyNormalTexture);
                
            cmd.Blit(null, cameraColorTargetHandle, _material);
            context.ExecuteCommandBuffer(cmd);
            
            CommandBufferPool.Release(cmd);
        }
    }

    Material _material;
    CustomRenderPass m_ScriptablePass;


    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        if (_material == null)
        {
            _material = CoreUtils.CreateEngineMaterial("Hidden/MyDeferredLighting");
        }
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.Setup(_material);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_material != null)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }
    }
}



using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyRendering
{
    public class MyRenderObjects : ScriptableRendererFeature
    {
        MyRenderObjectsPass renderObjectsPass;
        
        private RenderTargetIdentifier[] ColorAttachments;

        public override void Create()
        {
            var lightModeTags = new string[]
            {
                "MyTag"
            };
            renderObjectsPass = new MyRenderObjectsPass(
                "MyTag",
                RenderPassEvent.AfterRenderingTransparents,
                lightModeTags,
                RenderQueueType.Opaque,
                -1);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(renderObjectsPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            renderObjectsPass.Dispose();
        }
    }
}
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;

namespace MyRendering
{
    public class MyRenderObjects : ScriptableRendererFeature
    {
        MyRenderObjectsPass renderObjectsPass;

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
    }
}
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class BlurSettings
    {
        public Material pixelateMaterial;
        [Range(1f, 20f)]
        public float pixelSize = 6f;
        public bool isActive = false;
    }

    public BlurSettings settings = new BlurSettings();
    private PixelateRenderPass pixelatePass;

    public override void Create()
    {
        pixelatePass = new PixelateRenderPass(settings);
        pixelatePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.isActive || settings.pixelateMaterial == null) return;
        pixelatePass.Setup(settings);
        renderer.EnqueuePass(pixelatePass);
    }

    protected override void Dispose(bool disposing)
    {
        pixelatePass?.Dispose();
    }

    class PixelateRenderPass : ScriptableRenderPass
    {
        private BlurSettings settings;
        private RTHandle tempTexture;

        public PixelateRenderPass(BlurSettings settings)
        {
            this.settings = settings;
        }

        public void Setup(BlurSettings s) { settings = s; }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, name: "_TempPixelateTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.pixelateMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("PixelateEffect");
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            settings.pixelateMaterial.SetFloat("_PixelSize", settings.pixelSize);

            Blit(cmd, source, tempTexture, settings.pixelateMaterial);
            Blit(cmd, tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose() { tempTexture?.Release(); }
    }
}
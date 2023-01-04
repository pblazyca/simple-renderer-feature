using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GradientFog : ScriptableRendererFeature
{
    [System.Serializable]
    public class GradientFogSettings
    {
        [field: Header("Base material")]
        [field: SerializeField]
        public Material FogMaterial { get; private set; }

        [field: Header("Distance")]
        [field: SerializeField]
        public float StartDistance { get; private set; } = 0;
        [field: SerializeField]
        public float EndDistance { get; private set; } = 50;

        [field: Header("Skybox visability")]
        [field: SerializeField, Range(0.0f, 1.0f)]
        public float TransparentFactor { get; private set; } = 1.0f;

        [field: Header("Colors")]
        [field: SerializeField]
        public Color NearColor { get; private set; } = new(0, 0.2f, 0.35f, 1);
        [field: SerializeField]
        public Color MiddleColor { get; private set; } = new(0.62f, 0.86f, 1, 1);
        [field: SerializeField]
        public Color FarColor { get; private set; } = new(0.85f, 0.96f, 1, 1);
    }

    public GradientFogSettings settings = new();
    private GradientFogPass m_GradientFogPass;
    private bool isInitialized = false;

    public override void Create()
    {
        if (settings.FogMaterial == null)
        {
            return;
        }

        settings.FogMaterial.SetFloat("_StartDist", settings.StartDistance);
        settings.FogMaterial.SetFloat("_EndDist", settings.EndDistance);
        settings.FogMaterial.SetFloat("_TransparentFactor", settings.TransparentFactor);
        settings.FogMaterial.SetColor("_NearCol", settings.NearColor);
        settings.FogMaterial.SetColor("_MidCol", settings.MiddleColor);
        settings.FogMaterial.SetColor("_FarCol", settings.FarColor);

        m_GradientFogPass = new GradientFogPass(settings.FogMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
        };

        isInitialized = true;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (isInitialized == false) return;
        renderer.EnqueuePass(m_GradientFogPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (isInitialized == false) return;
        m_GradientFogPass.Setup(renderer.cameraColorTargetHandle, settings);
    }

    private class GradientFogPass : ScriptableRenderPass
    {
        private Material fogMaterial;
        public GradientFogSettings settings;


        private RTHandle source { get; set; }
        private RTHandle m_TempTex;

        void Dispose()
        {
            m_TempTex?.Release();
        }

        public GradientFogPass(Material material)
        {
            fogMaterial = new Material(Shader.Find("Shader Graphs/GradientFogGraph"));
        }

        public void Setup(RTHandle destinationColor, GradientFogSettings s)
        {
            settings = s;
            source = destinationColor;

            if (fogMaterial == null)
            {
                fogMaterial = new Material(Shader.Find("Shader Graphs/GradientFogGraph"));
            }

            fogMaterial.SetFloat("_StartDist", settings.StartDistance);
            fogMaterial.SetFloat("_EndDist", settings.EndDistance);
            fogMaterial.SetFloat("_TransparentFactor", settings.TransparentFactor);
            fogMaterial.SetColor("_NearCol", settings.NearColor);
            fogMaterial.SetColor("_MidCol", settings.MiddleColor);
            fogMaterial.SetColor("_FarCol", settings.FarColor);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            source = null;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (fogMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Gradient Fog");

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            opaqueDesc.depthBufferBits = 0;

            //cmd.GetTemporaryRT(m_TempTex.id, opaqueDesc);
            RenderingUtils.ReAllocateIfNeeded(ref m_TempTex, opaqueDesc);
            //RTHandles.Alloc(opaqueDesc, name: "_TempTex");

            //Blitter.BlitCameraTexture(cmd, source, m_TempTex, fogMaterial, 0);
            //Blitter.BlitCameraTexture(cmd, m_TempTex, source);

            Blit(cmd, source, m_TempTex, fogMaterial);
            Blit(cmd, m_TempTex, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            //cmd.ReleaseTemporaryRT(m_TempTex.id);
        }
    }
}



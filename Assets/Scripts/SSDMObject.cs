using UnityEngine;

namespace GeoTetra.SSDM
{
    [ExecuteInEditMode]
    public class SSDMObject : MonoBehaviour
    {
        [Range(0, 1)] 
        public float m_Intensity = 0.5f;
        
        [SerializeField] 
        RenderTexture m_ColorBuffer;
        
        [SerializeField] 
        RenderTexture m_WorldPosBuffer;
        
        [SerializeField] 
        Shader m_WorldPosShader;
        
        [SerializeField] 
        Material m_WorldPosBlit;
        
        [SerializeField] 
        Camera m_Camera;

        void OnValidate()
        {
            Initialize();
        }

        void OnEnable()
        {
            Initialize();
        }

        void Initialize()
        {
            m_Camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
            m_Camera.targetTexture = m_ColorBuffer;
            // m_Camera.SetReplacementShader(m_WorldPosShader, "");
            // m_Camera.ResetReplacementShader();
        }
        
        void OnRenderImage(RenderTexture src, RenderTexture dest) 
        {
            Graphics.Blit(src, m_ColorBuffer);

            SSDMUtility.AddMatricesToMaterial(m_WorldPosBlit, m_Camera);
            
            Graphics.Blit(src, m_WorldPosBuffer, m_WorldPosBlit);
        }
    }
}

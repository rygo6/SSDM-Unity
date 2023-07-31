using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace GeoTetra.SSDM
{
    // [ExecuteInEditMode]
    public class SSDMCamera : MonoBehaviour
    {
        [SerializeField] 
        Material m_DisplayMaterial;
        
        [SerializeField] 
        Material m_DisplacedDisplayMaterial;
        
        [SerializeField] 
        Material m_CameraBlitMaterial;
        
        [SerializeField] 
        RenderTexture m_RenderTarget;
        
        [FormerlySerializedAs("m_DepthNormalTarget")] [SerializeField] 
        RenderTexture m_NormalDepthTarget;
        
        [SerializeField] 
        RenderTexture m_DisplacedRenderTargetSampler;

        [SerializeField] 
        RenderTexture m_DisplacedRenderTarget;

        [SerializeField] 
        ComputeShader m_Compute;
        
        [SerializeField] 
        ComputeShader m_ComputeBlit;
        
        [SerializeField] 
        Camera m_Camera;
        
        [SerializeField] 
        int m_Width = 512;
        
        [SerializeField] 
        int m_Height = 512;
        
        [SerializeField] 
        int m_MipCount = 4;
        
        const int k_ThreadSize = 32;
        int m_FirstMipKernelId;
        int m_SubsequentMipKernelId;
        int m_BlitKernelId;

        // void OnValidate()
        // {
        //     m_Camera.targetTexture = null;
        //     
        //     if (!OnValidateRenderTexture(m_RenderTarget))
        //     {
        //         m_RenderTarget = null;
        //     }
        //     
        //     if (!OnValidateRenderTexture(m_DisplacedRenderTargetSampler))
        //     {
        //         m_DisplacedRenderTargetSampler = null;
        //     }
        //
        //     if (!OnValidateRenderTexture(m_DisplacedRenderTarget))
        //     {
        //         m_DisplacedRenderTarget = null;
        //     }
        //     
        //     Initialize();
        // }
        //
        // bool OnValidateRenderTexture(RenderTexture renderTexture)
        // {
        //     if (renderTexture == null || renderTexture.mipmapCount == m_MipCount) 
        //         return true;
        //     DestroyImmediate(renderTexture);
        //     return false;
        // }

        void OnEnable()
        {
            m_RenderTarget = null;
            m_DisplacedRenderTarget = null;
            m_DisplacedRenderTargetSampler = null;
            Initialize();
        }

        void Initialize()
        {
            m_Camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
            m_FirstMipKernelId = m_Compute.FindKernel("FirstMip");
            m_SubsequentMipKernelId = m_Compute.FindKernel("SubsequentMip");
            m_BlitKernelId = m_ComputeBlit.FindKernel("Blit");
            
            if (m_RenderTarget == null)
            {
                m_RenderTarget = new RenderTexture(m_Width, m_Height, 24, GraphicsFormat.R16G16B16A16_SFloat, m_MipCount)
                {
                    // enableRandomWrite = true,
                    autoGenerateMips = true,
                    useMipMap = true,
                    filterMode = FilterMode.Bilinear
                };
                m_RenderTarget.Create();
            }

            m_Camera.targetTexture = m_RenderTarget;
            m_DisplayMaterial.mainTexture = m_RenderTarget;
            
            if (m_NormalDepthTarget == null)
            {
                m_NormalDepthTarget = new RenderTexture(m_Width, m_Height, 0, GraphicsFormat.R16G16B16A16_SFloat, m_MipCount)
                {
                    // enableRandomWrite = true,
                    autoGenerateMips = true,
                    useMipMap = true,
                    filterMode = FilterMode.Bilinear
                };
                m_NormalDepthTarget.Create();
            }
            
            if (m_DisplacedRenderTargetSampler == null)
            {
                m_DisplacedRenderTargetSampler = new RenderTexture(m_Width, m_Height, 0, GraphicsFormat.R16G16B16A16_SFloat, m_MipCount)
                {
                    enableRandomWrite = true,
                    autoGenerateMips = false,
                    useMipMap = true,
                    filterMode = FilterMode.Bilinear
                };
                m_DisplacedRenderTargetSampler.Create();
            }

            if (m_DisplacedRenderTarget == null)
            {
                m_DisplacedRenderTarget = new RenderTexture(m_Width, m_Height, 0, GraphicsFormat.R16G16B16A16_SFloat, m_MipCount)
                    {
                        enableRandomWrite = true,
                        autoGenerateMips = false,
                        useMipMap = true,
                        filterMode = FilterMode.Bilinear
                    };
                m_DisplacedRenderTarget.Create();
            }
            
            m_DisplacedDisplayMaterial.mainTexture = m_DisplacedRenderTarget;
        }
        
        void OnRenderImage(RenderTexture src, RenderTexture dest) 
        {
            SSDMUtility.AddMatricesToMaterial(m_CameraBlitMaterial, m_Camera);
            Graphics.Blit(src, dest, m_CameraBlitMaterial);

            DispatchMipCompute(dest, m_MipCount, m_FirstMipKernelId);
            for (int mip = m_MipCount - 1; mip > 0; mip--)
            {
                DispatchMipCompute(dest, mip, m_SubsequentMipKernelId);
            }
        }

        void DispatchMipCompute(RenderTexture src, int mip, int kernelId)
        {
            var viewMatrix = m_Camera.worldToCameraMatrix;
            var projectionMatrix = m_Camera.projectionMatrix;
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var viewProjectMatrix = projectionMatrix * viewMatrix;
            m_Compute.SetMatrix("VP_WorldToClip", viewProjectMatrix);       
            m_Compute.SetMatrix("invVP_ClipToWorld", viewProjectMatrix.inverse);
            m_Compute.SetMatrix("V_WorldToObject", viewMatrix);
            m_Compute.SetMatrix("invV_ObjectToWorld", viewMatrix.inverse);
            
            var mipIndex = mip - 1;
            m_Compute.SetInt("_MipIndex", mipIndex);
            
            m_Compute.SetTexture(kernelId,"_WorldPosSampler", src);
            m_Compute.SetTexture(kernelId,"_ResultSampler", m_DisplacedRenderTargetSampler);
            m_Compute.SetTexture(kernelId,"_Result", m_DisplacedRenderTarget, mipIndex);
            
            var sizeX = m_Width >> mipIndex;
            var sizeY = m_Height >> mipIndex;
            m_Compute.SetFloat("_SizeX", sizeX);
            m_Compute.SetFloat("_SizeY", sizeY);
            var texelSizeX = 1.0f / sizeX;
            var texelSizeY = 1.0f / sizeY;
            m_Compute.SetFloat("_TexelSizeX", texelSizeX);
            m_Compute.SetFloat("_TexelSizeY", texelSizeY);
            // Debug.Log($"x {sizeX}  y {sizeY} - x {texelSizeX}  y {texelSizeY}");
            
            var threadSizeX = sizeX / k_ThreadSize;
            threadSizeX = threadSizeX < 1 ? 1 : threadSizeX;
            var threadSizeY = sizeY / k_ThreadSize;
            threadSizeY = threadSizeY < 1 ? 1 : threadSizeY;
            m_Compute.Dispatch(kernelId, threadSizeX, threadSizeY, 1);

            // m_ComputeBlit.SetInt("_MipIndex", mipIndex);
            m_ComputeBlit.SetTexture(m_BlitKernelId,"_ResultSampler", m_DisplacedRenderTarget, mipIndex);
            m_ComputeBlit.SetTexture(m_BlitKernelId,"_Result", m_DisplacedRenderTargetSampler, mipIndex);
            m_ComputeBlit.Dispatch(m_BlitKernelId, threadSizeX, threadSizeY, 1);
        }
        
        void DispatchMipClear(int mip, int kernelId)
        {
            var mipIndex = mip - 1;
            m_Compute.SetTexture(kernelId,"_Result", m_DisplacedRenderTarget, mipIndex);
            var sizeX = (m_Width >> mipIndex) / k_ThreadSize;
            var sizeY = (m_Height >> mipIndex) / k_ThreadSize;
            m_Compute.Dispatch(kernelId,sizeX < 1 ? 1 : sizeX, sizeY < 1 ? 1 : sizeY, 1);
        }
    }
}

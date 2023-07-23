using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace GeoTetra.SSDM
{
    // [ExecuteInEditMode]
    public class SSDMCamera : MonoBehaviour
    {
        [SerializeField] 
        Material m_DisplayMaterial;
        
        [SerializeField] 
        Material m_CameraBlitMaterial;
        
        [SerializeField] 
        RenderTexture m_RenderTarget;
        
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
        int m_ClearKernelId;
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
            m_Camera.depthTextureMode |= DepthTextureMode.DepthNormals;
            m_ClearKernelId = m_Compute.FindKernel("Clear");
            m_FirstMipKernelId = m_Compute.FindKernel("FirstMip");
            m_SubsequentMipKernelId = m_Compute.FindKernel("SubsequentMip");
            m_BlitKernelId = m_ComputeBlit.FindKernel("Blit");
            
            if (m_RenderTarget == null)
            {
                m_RenderTarget = new RenderTexture(m_Width, m_Height, 0, GraphicsFormat.R16G16B16A16_SFloat, m_MipCount)
                {
                    // enableRandomWrite = true,
                    autoGenerateMips = true,
                    useMipMap = true
                };
                m_RenderTarget.Create();
            }

            m_Camera.targetTexture = m_RenderTarget;
            
            if (m_DisplacedRenderTargetSampler == null)
            {
                m_DisplacedRenderTargetSampler = new RenderTexture(m_Width, m_Height, 0, GraphicsFormat.R16G16B16A16_SFloat, m_MipCount)
                {
                    enableRandomWrite = true,
                    autoGenerateMips = false,
                    useMipMap = true
                };
                m_DisplacedRenderTargetSampler.Create();
            }

            if (m_DisplacedRenderTarget == null)
            {
                m_DisplacedRenderTarget = new RenderTexture(m_Width, m_Height, 0, GraphicsFormat.R16G16B16A16_SFloat, m_MipCount)
                    {
                        enableRandomWrite = true,
                        autoGenerateMips = false,
                        useMipMap = true
                    };
                m_DisplacedRenderTarget.Create();
            }
            
            m_DisplayMaterial.mainTexture = m_DisplacedRenderTarget;
        }
        
        void OnRenderImage(RenderTexture src, RenderTexture dest) 
        {
            // Graphics.Blit(src, dest, m_CameraBlitMaterial);
            Graphics.Blit(src, dest);
            
            for (int mip = m_MipCount; mip > 0; mip--)
            {
                DispatchMipClear(mip, m_ClearKernelId);
            }
   
            DispatchMipCompute(src, m_MipCount, m_FirstMipKernelId);
            for (int mip = m_MipCount - 1; mip > 0; mip--)
            {
                DispatchMipCompute(src, mip, m_SubsequentMipKernelId);
            }
        }

        void DispatchMipCompute(RenderTexture src, int mip, int kernelId)
        {
            var viewMatrix = m_Camera.worldToCameraMatrix;
            var projectionMatrix = m_Camera.projectionMatrix;
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var viewProjectMatrix_worldPosToClip = projectionMatrix * viewMatrix;
            var inverseViewProjectionMatrix_clipToWorldPos = viewProjectMatrix_worldPosToClip.inverse;
            m_Compute.SetMatrix("invVP_clipToWorld", inverseViewProjectionMatrix_clipToWorldPos);
            m_Compute.SetMatrix("VP_worldToClip", viewProjectMatrix_worldPosToClip);
            
            var mipIndex = mip - 1;
            m_Compute.SetInt("_MipLevel", mip);
            
            m_Compute.SetTexture(kernelId,"_WorldPosSampler", m_RenderTarget);
            m_Compute.SetTexture(kernelId,"_ResultSampler", m_DisplacedRenderTargetSampler);
            m_Compute.SetTexture(kernelId,"_Result", m_DisplacedRenderTarget, mipIndex);
            
            var sizeX = m_Width >> mipIndex;
            var sizeY = m_Height >> mipIndex;
            m_Compute.SetFloat("_SizeX", sizeX);
            m_Compute.SetFloat("_SizeY", sizeY);
            var texelSizeX = sizeX / 1.0f;
            var texelSizeY = sizeY / 1.0f;
            m_Compute.SetFloat("_TexelSizeX", texelSizeX);
            m_Compute.SetFloat("_TexelSizeY", texelSizeY);
            Debug.Log($"x {sizeX}  y {sizeY} - x {texelSizeX}  y {texelSizeY}");
            
            var threadSizeX = sizeX / k_ThreadSize;
            threadSizeX = threadSizeX < 1 ? 1 : threadSizeX;
            var threadSizeY = sizeY / k_ThreadSize;
            threadSizeY = threadSizeY < 1 ? 1 : threadSizeY;
            m_Compute.Dispatch(kernelId, threadSizeX, threadSizeY, 1);

            m_ComputeBlit.SetInt("_MipLevel", mip);
            m_ComputeBlit.SetTexture(m_BlitKernelId,"_ResultSampler", m_DisplacedRenderTarget);
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

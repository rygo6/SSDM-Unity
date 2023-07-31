using UnityEngine;

namespace GeoTetra.SSDM
{
    public class SSDMUtility
    {
        public static void AddMatricesToMaterial(Material material, Camera camera)
        {
            var viewMatrix = camera.worldToCameraMatrix;
            var projectionMatrix = camera.projectionMatrix;
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var viewProjectMatrix = projectionMatrix * viewMatrix;
            material.SetMatrix("VP_WorldToClip", viewProjectMatrix);
            material.SetMatrix("invVP_ClipToWorld", viewProjectMatrix.inverse);
            material.SetMatrix("V_WorldToObject", viewMatrix);
            material.SetMatrix("invV_ObjectToWorld", viewMatrix.inverse);
        }
    }
}
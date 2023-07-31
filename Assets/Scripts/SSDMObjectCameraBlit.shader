Shader "GeoTetra/SSDMObjectCameraBlit"
{
    Properties
    {
		_MainTex ("_MainTex", 2D) = "white" {}    
    }
    SubShader
    {
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4x4 invVP_ClipToWorld;
			float4x4 VP_WorldToClip;
			float4x4 invV_ObjectToWorld;
			float4x4 V_WorldToObject;
			sampler2D_float _CameraDepthTexture;
			sampler2D_float _CameraDepthNormalsTexture;
			

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 ndcPosAndDepthToClipSpacePos(float2 ndcPos, float depth)
			{
				float4 clipsSpacePos = float4(ndcPos * 2.0 - 1.0, depth, 1.0);
				return clipsSpacePos;
			}

			float3 ndcPosAndDepthToWorldPos(float2 ndcPos, float depth)
			{
				float4 clipSpacePos = ndcPosAndDepthToClipSpacePos(ndcPos, depth);
				float4 homogenizedWorldPos = mul(invVP_ClipToWorld, clipSpacePos);
				return homogenizedWorldPos.xyz / homogenizedWorldPos.w;
			}

			float2 ndcToUv(float4 ndcPosition)
			{
			    float2 uv;
			    uv.x = ndcPosition.x;
			    uv.y = 1.0 - ndcPosition.y;
			    return uv;
			}

			float4 worldPosToNdcPos(float3 worldPos)
			{
			    // Apply the world-to-clip transformation
			    float4 clipPosition = mul(VP_WorldToClip, float4(worldPos, 1.0));

			    // Divide by the w component to get homogeneous coordinates
			    clipPosition /= clipPosition.w;

			    // Map the clip space coordinates to NDC space
			    float2 ndcPosition;
			    ndcPosition.x = (clipPosition.x + 1.0) * 0.5;
			    ndcPosition.y = (1.0 - clipPosition.y) * 0.5;

			    // Combine the NDC position with the original z and w components
			    float4 ndcPositionWithZW = float4(ndcPosition, clipPosition.zw);

			    return ndcPositionWithZW;
			}

			float4 frag (v2f i) : SV_Target
			{
				float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
				// float sceneZ = LinearEyeDepth(depthSample);

				float2 ndcPos = i.uv.xy;
				float3 worldPos = ndcPosAndDepthToWorldPos(ndcPos, depthSample);
				// float4 ndcPosWithZWFromWorldPos = worldPosToNdcPos(worldPos);
				// float2 uvFromWorldPos = ndcToUv(ndcPosWithZWFromWorldPos);
				
				// float4 depthNormalSample = tex2D(_CameraDepthNormalsTexture, i.uv.xy);
				// float3 viewNormal;
				// float viewDepth;
				// DecodeDepthNormal(depthNormalSample, viewDepth, viewNormal);
				//
				// float3 worldNormal = mul((float3x3)invV_ObjectToWorld, float4(viewNormal, 0.0));
				
				// float3 renormal = mul((float3x3)V_WorldToObject, float4(worldNormal, 0.0));
				
				return float4(worldPos, 1);
			}
			ENDCG
		}
	}
}
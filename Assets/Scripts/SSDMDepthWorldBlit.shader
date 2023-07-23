Shader "GeoTetra/SSDMDepthWorldBlit"
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
			float4x4 invVP_clipToWorld;
			float4x4 VP_worldToClip;
			sampler2D_float _CameraDepthTexture;
			float4 _CameraDepthTexture_ST;

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
				float4 homogenizedWorldPos = mul(invVP_clipToWorld, clipSpacePos);
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
			    float4 clipPosition = mul(VP_worldToClip, float4(worldPos, 1.0));

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
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

				float2 ndcPos = i.uv.xy;
				float3 worldPos = ndcPosAndDepthToWorldPos(ndcPos, depth);
				float4 ndcPosWithZWFromWorldPos = worldPosToNdcPos(worldPos);
				float2 uvFromWorldPos = ndcToUv(ndcPosWithZWFromWorldPos);

				float4 color = tex2D(_MainTex, i.uv);
				// worldPos *= color.a;
				
				// return float4(ndcPosWithZWFromWorldPos.xy, 0.0, 1.0);
				// return float4(ndcPos, 0.0, 1.0);
				// return float4(uv, 0.0, 1.0);
				// return float4(uvFromWorldPos, 0.0, 1.0);
				return float4(worldPos, color.a);
				// return float4(worldPos, 1);
			}
			ENDCG
		}
	}
}
Shader "GeoTetra/SSDM"
{
	Properties
	{
		_MainTex ("_MainTex", 2D) = "white" {}
		_NormalDepth ("_NormalDepth", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off 
		ZWrite On 

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
			sampler2D _NormalDepth;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 color = tex2D(_MainTex, i.uv);
				if (color.a == 0)
					discard;
				
				float4 normalDepth = tex2D(_NormalDepth, i.uv);
				return float4(normalDepth.rgb, color.a);
			}
			ENDCG
		}
		
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
// Add by yaukey at 2018-06-13.

Shader "Hidden/ObjectLightmapViewer/Unlit/FullQuad"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Overlay" }

        Cull Off 
        ZWrite Off 
        ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
                v.vertex.y = 1.0 - v.vertex.y;
				o.vertex = float4((v.vertex.xy - 0.5) * 2.0, 1.0, 1.0);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = fixed4(DecodeLightmap(tex2D(_MainTex, i.uv)), 1.0);
				return col;
			}
			ENDCG
		}
	}
}

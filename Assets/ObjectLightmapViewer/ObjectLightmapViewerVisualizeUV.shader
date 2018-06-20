// Add by yaukey at 2018-06-13.

Shader "Hidden/ObjectLightmapViewer/Unlit/VisualizeUV"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
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
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			fixed4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
                v.uv2.y = 1.0 - v.uv2.y;
				o.vertex = float4((v.uv2.xy - 0.5) * 2.0, 1.0, 1.0);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return _Color;
			}

			ENDCG
		}
	}
}

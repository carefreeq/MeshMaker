Shader "QQ/Editor/GL"
{
	Properties
	{
	}
	SubShader
	{
		Tags{ "RenderType" = "Overlay"
		"Queue" = "Overlay" }

		Pass
		{
			Zwrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct a2v
			{
				float4 vertex : POSITION;
				float4 color :Color;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color :TEXCOORD0;
			};
			v2f vert(a2v v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}
}

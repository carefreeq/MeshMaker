// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "QQ/Editor/Axis"
{
	Properties
	{
		_Color("Color",Color) = (1,1,1,0.5)
		_RimColor("Rim Color",Color) = (0.2,0.2,0.2,0.5)
		_Alpha("alpha",Range(0,1)) = 0.5
	}
		SubShader
	{
		Tags{
		"RenderType" = "Overlay"
		"Queue" = "Overlay" }
		Zwrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGINCLUDE
		#include "UnityCG.cginc"
		fixed4 _Color;
		fixed4 _RimColor;
		float _Alpha;
		struct a2v
		{
			float4 vertex : POSITION;
			float3 normal:NORMAL;
			float4 color:Color;
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float3 normal:NORMAL;
			float3 viewDir : TEXCOORD0;
			float4 color:TEXCOORD1;
		};
		v2f vert(a2v v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.normal = UnityObjectToWorldDir(v.normal);
			o.viewDir = UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex));
			o.color = v.color;
			return o;
		}
		fixed4 _frag(v2f i)
		{
			float NdotV = saturate(dot(normalize(i.normal), normalize(i.viewDir)));
			return lerp(_RimColor, _Color, NdotV)*i.color.a;
		}
		ENDCG
			Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			fixed4 frag(v2f i) : SV_Target
			{
				return _frag(i);
			}
			ENDCG
		}
		Pass
		{
			Ztest Always
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = _frag(i);
				col.a = min(col.a, _Alpha);
				return col;
			}
			ENDCG
		}
	}
}

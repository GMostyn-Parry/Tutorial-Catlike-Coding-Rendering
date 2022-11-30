Shader "Custom/Textured With Detail"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_DetailTex("Detail Texture", 2D) = "gray" {}
		_Tint("Tint", Color) = (1, 1, 1, 1)
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex MyVertexProgram
			#pragma fragment MyFragmentProgram

			#include "UnityCG.cginc"

			struct VertexData
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Interpolators
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
				float2 uvDetail : TEXCOORD1;
			};

			sampler2D _MainTex, _DetailTex;
			float4 _MainTex_ST, _DetailTex_ST;
			float4 _Tint;

			Interpolators MyVertexProgram(VertexData v)
			{
				Interpolators i;
				i.position = UnityObjectToClipPos(v.position);
				i.uv = TRANSFORM_TEX(v.uv, _MainTex);
				i.uvDetail = TRANSFORM_TEX(v.uv, _DetailTex);

				return i;
			}

			float4 MyFragmentProgram(Interpolators i) : SV_TARGET
			{
				float4 colour = tex2D(_MainTex, i.uv) * _Tint;
				colour *= tex2D(_DetailTex, i.uvDetail) * unity_ColorSpaceDouble;

				return colour;
			}

			ENDCG
		}
	}
}
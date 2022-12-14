Shader "Custom/First Shader"
{
	Properties
	{
		_Tint("Tint", Color) = (1, 1, 1, 1)
		_MainTex("Texture", 2D) = "white" {}
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
				float3 uv : TEXCOORD0;
			};

			struct Interpolators
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			float4 _Tint;
			sampler2D _MainTex;
			float4 _MainTex_ST;

			Interpolators MyVertexProgram(VertexData v)
			{
				Interpolators i;
				i.position = UnityObjectToClipPos(v.position);
				i.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return i;
			}

			float4 MyFragmentProgram(Interpolators i) : SV_TARGET
			{
				return tex2D(_MainTex, i.uv) * _Tint;
			}

			ENDCG
		}
	}
}
Shader "BillboardGeomShader"
{
	Properties
	{
		_Sprite ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_Size("Size", Vector) = (1,1,0,0)
	}

	SubShader
	{
		Tags { "Queue" = "Overlay+100" "RenderType" = "Transparent" }

		LOD 100
		Blend One One

		Cull off
		ZWrite off

		Pass
		{
			CGPROGRAM
			//#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _Sprite;
			float4 _Color = float4(1, 0.5f, 0.0f, 1);
			float2 _Size = float2(1, 1);
			float3 _worldPos;

			struct data {
				float3 pos;
				float size;
			};
			StructuredBuffer<data> buf_points;

			struct geomInput
			{
				float4 pos : SV_POSITION;
				float size : PSIZE0;
				float2 uv : TEXCOORD0;
			};

			struct fragInput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			geomInput vert(uint id : SV_VertexID)
			{
				geomInput o;
				o.pos = float4(buf_points[id].pos + _worldPos, 1.0f);
				o.size = buf_points[id].size;
				o.uv = float2(0, 0);
				return o;
			}

			float4 RotPoint(float4 p, float3 offset, float3 sideVector, float3 upVector)
			{
				float3 finalPos = p.xyz;
				finalPos += offset.x * sideVector;
				finalPos += offset.y * upVector;
				return float4(finalPos, 1);
			}

			[maxvertexcount(4)]
			void geom(point geomInput p[1], inout TriangleStream<fragInput> triStream)
			{
				float2 halfS = p[0].size;
				float4 v[4];

				float3 up = float3(0, 1, 0);
				float3 look = normalize(_WorldSpaceCameraPos - p[0].pos.xyz);
				float3 right = normalize(cross(look, up));
				up = normalize(cross(right, look));
				v[0] = RotPoint(p[0].pos, float3(-halfS.x, -halfS.y, 0), right, up);
				v[1] = RotPoint(p[0].pos, float3(halfS.x, -halfS.y, 0), right, up);
				v[2] = RotPoint(p[0].pos, float3(-halfS.x, halfS.y, 0), right, up);
				v[3] = RotPoint(p[0].pos, float3(halfS.x, halfS.y, 0), right, up);

				fragInput pIn;
				pIn.pos = mul(UNITY_MATRIX_VP, v[0]);
				pIn.uv = float2(0, 0);
				triStream.Append(pIn);

				pIn.pos = mul(UNITY_MATRIX_VP, v[1]);
				pIn.uv = float2(1, 0);
				triStream.Append(pIn);

				pIn.pos = mul(UNITY_MATRIX_VP, v[2]);
				pIn.uv = float2(0, 1);
				triStream.Append(pIn);

				pIn.pos = mul(UNITY_MATRIX_VP, v[3]);
				pIn.uv = float2(1, 1);
				triStream.Append(pIn);
			}

			float4 frag(fragInput i) : COLOR
			{
				fixed4 col = tex2D(_Sprite, i.uv) * _Color;
				return col;
			}

			ENDCG
		}
	}
	Fallback Off
}

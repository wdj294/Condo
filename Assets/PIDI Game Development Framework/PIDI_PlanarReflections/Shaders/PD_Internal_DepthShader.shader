Shader "PIDI Shaders Collection/Internal/Reflections Depth Shader"
{
	Properties{
		_DepthPlaneOrigin("Plane Origin", Vector ) = (0,0,0,0)
		_DepthPlaneNormal("Plane Normal", Vector ) = (0,-1,0,0)
		_MainTex("Main Texture", 2D ) = "white"{}
		_Cutoff("Cutoff", Range(0,1) ) = 0.85
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZWrite On
		Cull Back

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag		
			#include "UnityCG.cginc"


			struct v2f{
				float2 screenUV : TEXCOORD0;
                float2 uv : TEXCOORD1;
                fixed4 color : COLOR;
                float4 pos : SV_POSITION;
			};

			float4 _DepthPlaneOrigin;
			float4 _DepthPlaneNormal;
			sampler2D _MainTex;
			half _Cutoff;
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, v.vertex));
				o.uv = v.texcoord;
                o.color = _DepthPlaneOrigin*_DepthPlaneNormal-mul(unity_ObjectToWorld, v.vertex)*_DepthPlaneNormal;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = i.color+(_Cutoff-tex2D(_MainTex,i.uv).a);
				clip(tex2D(_MainTex,i.uv).a-_Cutoff);
				return col;
			}
			ENDCG
		}

		
	}
}

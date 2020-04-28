Shader "Framework/Glass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BumpTex("Normal Texture", 2D) = "white" {}
		_Magnitude("Magnitude", float) = 1
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
		}
		ZWrite On
		Lighting Off
		Cull Off
		Fog{ Mode Off }
		Blend One Zero

		LOD 100     // Level of detail: VertexLit kind of shaders = 100

		// 自动生成_GrabTexture
		GrabPass { }
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata
			{
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
			};
			struct v2f
			{
				float4 uvgrab : TEXCOORD0;
				float4 vertex : SV_POSITION;
				half2 uv : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _GrabTexture;
			sampler2D _BumpTex;
			float _Magnitude;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvgrab = ComputeGrabScreenPos(o.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 mainColor = tex2D(_MainTex, i.uv);

				half4 bump = tex2D(_BumpTex, i.uv);
				half2 distortion = UnpackNormal(bump).rg;
				i.uvgrab.xy += distortion * _Magnitude;
				fixed4 col = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.uvgrab));
				return col * mainColor;
			}
			ENDCG
		}
	}
}
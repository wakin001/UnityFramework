Shader "Framework/CleanGlass"
{
	Properties
	{
		//_MainTex ("Texture", 2D) = "white" {}
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
			};
			struct v2f
			{
				float4 uvgrab : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
			sampler2D _GrabTexture;
			
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvgrab = ComputeGrabScreenPos(o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2Dproj(_GrabTexture,  UNITY_PROJ_COORD(i.uvgrab));
				return col + half4(0.5, 0, 0, 0);
			}
			ENDCG
		}
	}
}
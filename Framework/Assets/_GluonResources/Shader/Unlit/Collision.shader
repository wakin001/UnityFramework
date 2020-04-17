// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Gluon/Unlit/Collision" 
{
    Properties
    {
		_Color("Main Color", Color) = (1,0,0,0.5)
	}

	SubShader {
		Tags { "Queue" = "Transparent+1" "RenderType"="Transparent" "Gluon"="Collision" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {  
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				
				#include "UnityCG.cginc"
	
				struct appdata_t {
					float4 vertex : POSITION;
				};
	
				struct v2f {
					float4 vertex : SV_POSITION;
					UNITY_FOG_COORDS(0)
				};
	
				fixed4 _Color;
				
				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}
				
				fixed4 frag (v2f i) : COLOR
				{
					fixed4 col = _Color;
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
			ENDCG
		}
	}
}
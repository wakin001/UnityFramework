// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Framework/TransparentAdd"
{
    Properties
    {
		_MainTex("MainTex", 2D) = "white"{}
		_Color("Color", Color) = (1,1,1,1)
		_ColorPower("Color Power" , Range(0,5)) = 1
		[Enum(None,0,Front,1,Back,2)] _Cull("Culling" , Int) = 2
		_VertexColorImpact("Vertex Color Impact" , Range(0,2)) = 1
		_ProgrammableColor("Programmable Color", Color) = (1,1,1,1)
		[Space][Header(Detail Parameter)] _OffsetFactor("Depth Offset Factor" , Float) = 0
		_OffsetUnits("Depth Offset Units" , Float) = 0
    }
    SubShader
    {
        Tags 
		{ 
			"Queue" = "Transparent"
		}
        LOD 100

        Pass
        {
			Name "TRANSPARENTADD"
			Tags
			{
				"LightMode" = "ForwardBase"
			}

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull[_Cull]
			Offset[_OffsetFactor],[_OffsetUnits]
			Fog
			{
				Mode Off
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
			#pragma multi_compile_fwdbase

            #include "UnityCG.cginc"

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _Color;
			float _ColorPower;

            v2f vert (a2v v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				col = col * _Color * _ColorPower;
                return col;
            }
            ENDCG
        }

    }
}

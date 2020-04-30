Shader "Framework/NormalMapping"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_NormalTex("Normal Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				fixed4 tangent : TANGENT;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				fixed3 worldNormal : TEXCOORD2;
				fixed3 lightDir : TEXCOORD3;
				fixed3 viewDir : TEXCOORD4;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _NormalTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// 因为法线纹理是基于tangent space，所以光照向量也必须转换到tangent space中去计算。
				//Create a rotation matrix for tangent space
				TANGENT_SPACE_ROTATION;

				o.lightDir = mul(rotation, ObjSpaceLightDir(v.vertex));
				o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex));

				// pass lighting information to pixel shader
				TRANSFER_VERTEX_TO_FRAGMENT(o);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// normal vector
				fixed3 normalMapping = UnpackNormal(tex2D(_NormalTex, i.uv));

				//// ambient
				//fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;

				//// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);

				//fixed3 diffuse = _LightColor0.xyz * col.xyz * saturate(dot(normalMapping, i.lightDir));

				//fixed4 color = fixed4(ambient + diffuse, col.a);

				//return color;

				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				
				fixed3 lightDir = normalize(fixed3(0.5, 0.4, 0.3));
				col.xyz = col.xyz * saturate(dot(normalMapping, lightDir));
				return col;
			}

			ENDCG
		}
	}
}

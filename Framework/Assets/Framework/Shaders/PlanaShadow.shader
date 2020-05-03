// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Framework/PlanaShadow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ShadowInvLen ("ShadowInvLen", float) = 1.0 //0.4449261
		//_ShadowFalloff ("ShadowFalloff", float) = 0.1
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+10" }
		LOD 100
		
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			
			ENDCG
		}

		Pass
		{		
			Blend SrcAlpha  OneMinusSrcAlpha
			ZWrite Off
			Cull Back
			ColorMask RGB
			
			Stencil
			{
				Ref 0					//参考值
				ReadMask 255			//读取掩码，取值范围也是0-255的整数，默认值为255，二进制位11111111，即读取的时候不对referenceValue和stencilBufferValue产生效果，读取的还是原始值
				WriteMask 255			//输出掩码，当写入模板缓冲时进行掩码操作（按位与【&】），writeMask取值范围是0-255的整数，默认值也是255，即当修改stencilBufferValue值时，写入的仍然是原始值
				Comp Equal				//条件，关键字有，Greater（>），GEqual（>=），Less（<），LEqual（<=），Equal（=），NotEqual（!=），Always（总是满足），Never（总是不满足）
				//Pass IncrSat
				Pass Invert				//条件满足后的处理
				Fail Keep				//条件不满足后的处理
				ZFail Keep				//深度测试失败后的处理
			}
			
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag

			float4 _ShadowPlane;
			float4 _ShadowProjDir;
			float4 _WorldPos;
			float _ShadowInvLen;
			float4 _ShadowFadeParams;
			float _ShadowFalloff;
			
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 clipPos : SV_POSITION;
				// 物体本身的世界坐标
				float3 objWorldPos : TEXCOORD0;
				// 阴影的世界坐标
				float3 objShadowWorldPos: TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				v2f o;

				float3 lightdir = normalize(_ShadowProjDir);
				// 物体的世界坐标
				float3 worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;
				// _ShadowPlane.xyz是阴影平面的法线，_ShadowPlane.w 就是平面方程p * n = t中的t
				float distance = (_ShadowPlane.w - dot(_ShadowPlane.xyz, worldpos)) / dot(_ShadowPlane.xyz, lightdir.xyz);
				// 计算阴影的世界坐标
				worldpos = worldpos + distance * lightdir.xyz;
				// 阴影的世界坐标变换到齐次坐标
				o.clipPos = mul(unity_MatrixVP, float4(worldpos, 1.0));
				o.objWorldPos = _WorldPos.xyz;
				o.objShadowWorldPos = worldpos;

				return o;
			}
			
			float4 frag(v2f i) : SV_Target
			{
				// 计算阴影的世界坐标和物体本身世界的差，用于阴影衰减
				float3 attenDis = (i.objWorldPos - i.objShadowWorldPos);
				float4 color = float4(0.0, 0.0, 0.0, 1.0);
				
				// 下面两种阴影衰减公式都可以使用(当然也可以自己写衰减公式)
				// 1. 王者荣耀的衰减公式
				//color.w = (pow((1.0 - clamp(((sqrt(dot(attenDis, attenDis)) * _ShadowInvLen) - _ShadowFadeParams.x), 0.0, 1.0)), _ShadowFadeParams.y) * _ShadowFadeParams.z);

				// 2. https://zhuanlan.zhihu.com/p/31504088 这篇文章介绍的另外的阴影衰减公式
				color.w = 1.0 - saturate(distance(i.objWorldPos, i.objShadowWorldPos) * _ShadowFalloff);

				return color;
			}
			
			ENDCG
		}
	}
}

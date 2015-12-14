// Unlit alpha-cutout shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Sports/Cutout Transparent" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	_Color("Color", Color) = (0.5,0.5,0.5,0.5)
}
SubShader {
	Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
	LOD 100

	Lighting Off

	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			fixed4 _Color;

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _Cutoff;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.texcoord) * _Color;
				clip((col.a - _Cutoff));
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
		ENDCG
	}

	Pass{
		Name "ShadowCaster"
		Tags{ "LightMode" = "ShadowCaster" }

		ZWrite On ZTest LEqual

		CGPROGRAM
		#pragma target 3.0
		// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
		#pragma exclude_renderers gles

		// -------------------------------------


		#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
		#pragma multi_compile_shadowcaster

		#pragma vertex vertShadowCaster
		#pragma fragment fragShadowCaster

		#include "UnityStandardShadow.cginc"

		ENDCG
	}
}

}

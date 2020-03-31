Shader "Custom/LightGradient"
{
	Properties{
		_Color1("Base (RGB) 1", Color) = (0,0,0,0)
		_Color2("Base (RGB) 2", Color) = (0,0,0,0)
		_TransparentColor("Base (RGB) transparent", Color) = (0,0,0,0)

		_BlendThreshold("Blend Distance", Float) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Glossiness("Smoothness", Range(0,1)) = 0.5
	}

		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 200
		
			CGPROGRAM

			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows vertex:vert

			//Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			half4 _Color1;
			half4 _Color2;
			half4 _TransparentColor;
			
			float _ChangePoint;
			float4 _CenterPoint;
			float _MaxDistance;
			float _BlendThreshold;
			half _Metallic;
			half _Glossiness;

			struct Input {
				float3 worldPos;
			};

			void vert(inout appdata_full v, out Input o) {
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.worldPos = v.vertex.xyz;
			}

			void surf(Input IN, inout SurfaceOutputStandard o) {

				float startBlending = _ChangePoint - _BlendThreshold;
				float endBlending = _ChangePoint + _BlendThreshold;

				float curDistance = distance(_CenterPoint.xyz, IN.worldPos);
				float changeFactor = saturate((curDistance - startBlending) / (_BlendThreshold * 2));

				if (curDistance >= _MaxDistance) {
					o.Albedo = _TransparentColor.rgb;
					o.Alpha = _TransparentColor.a;
				}
				else {
					half4 c = lerp(_Color1, _Color2, changeFactor);

					o.Albedo = c.rgb;
					o.Alpha = c.a;
					o.Metallic = _Metallic;
					o.Smoothness = _Glossiness;
				}
			}

			ENDCG
	}
		FallBack "Diffuse"
}
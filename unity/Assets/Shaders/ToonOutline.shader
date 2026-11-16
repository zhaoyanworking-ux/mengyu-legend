Shader "ChibiARPG/ToonOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.72, 0.78, 0.92, 1)
        _RimColor ("Rim Color", Color) = (0.75, 0.9, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.2
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.35
        _OutlineColor ("Outline Color", Color) = (0.28, 0.22, 0.32, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.08)) = 0.018
        _RampThreshold ("Light Threshold", Range(0,1)) = 0.42
        _RampSmooth ("Light Smooth", Range(0.001, 0.5)) = 0.12
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _Gloss ("Gloss", Range(8, 256)) = 48
        _SpecularThreshold ("Specular Threshold", Range(0,1)) = 0.78
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            float _OutlineWidth;
            fixed4 _OutlineColor;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz + normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(pos, 1));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        Pass
        {
            Name "TOON"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color;
            fixed4 _ShadowColor;
            fixed4 _RimColor;
            float _RimPower;
            float _RimStrength;
            float _RampThreshold;
            float _RampSmooth;
            fixed4 _SpecularColor;
            float _Gloss;
            float _SpecularThreshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldN : TEXCOORD0;
                float3 worldP : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldN = UnityObjectToWorldNormal(v.normal);
                o.worldP = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldN);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - i.worldP);
                float ndl = dot(n, l);
                float ramp = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, ndl);
                fixed3 lit = lerp(_ShadowColor.rgb * _Color.rgb, _Color.rgb, ramp);
                lit *= _LightColor0.rgb;
                lit += ShadeSH9(half4(n, 1)) * _Color.rgb * 0.65;

                float3 h = normalize(l + v);
                float spec = pow(saturate(dot(n, h)), _Gloss);
                spec = smoothstep(_SpecularThreshold, _SpecularThreshold + 0.08, spec);
                lit += _SpecularColor.rgb * spec * ramp * 0.35;

                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimStrength;
                // 只在受光侧给一点清新的边缘光
                rim *= saturate(ndl + 0.35);
                lit += _RimColor.rgb * rim;

                return fixed4(lit, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Fraktalia/Core/Sky/Burning" {
    Properties{
        _Tint("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure("Exposure", Range(0, 8)) = 1.0
        _Rotation("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _Tex("Cubemap   (HDR)", Cube) = "grey" {}

        _BlendMap("Blend Map", CUBE) = "white" {}
        _BlendMap_Size("Blend Size", float) = 1.0


            //Animation
            _RotationSpeed("Rotation Speed", float) = 1
            _RotationOffset("Rotation Offset", float) = 1

            _MRStepsX("Steps X", Range(0,20)) = 1
            _MRStepsY("Steps Y", Range(0,20)) = 1
            _MRDistX("Dist X", int) = 1
            _MRDistY("Dist Y", int) = 1
            _MRExponent("Outer Exponent", int) = 1
            _MRExponent_Back("Inner Exponent", int) = 1
    }

        SubShader{

            Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
            Cull Off ZWrite Off

             Pass {

             CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            half4 _Tex_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation;

            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
                    float3 viewDir : TEXCOORD2;
            };


            samplerCUBE _BlendMap;
            float _BlendMap_Size;

            uniform int _MRStepsX;
            uniform int _MRStepsY;
            uniform float _MRDistX;
            uniform float	_MRDistY;
            uniform float _MRExponent_Back;
            uniform float _RotationSpeed;
            uniform float _RotationOffset;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                float4x4 modelMatrix = unity_ObjectToWorld;
                o.viewDir = mul(modelMatrix, v.vertex).xyz - _WorldSpaceCameraPos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                     half4 tex = texCUBE(_Tex, i.texcoord);
            half3 c = DecodeHDR(tex, _Tex_HDR);
            c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            c *= _Exposure;
            
                
                half3 M = float3(0, 0, 0);

                for (int r = 0; r < _MRStepsX; r++)
                {
                    float sin45 = sin(r * 0.1 * _MRDistX);
                    float cos45 = cos(r * 0.1 * _MRDistX);

                    float3x3 rot_45 = float3x3
                        (cos45, 0, sin45,
                            0, 1, 0,
                            -sin45, 0, cos45);

                    for (int k = 0; k < _MRStepsY; k++)
                    {
                        float sinX = sin(_RotationSpeed * -_Time + k * 0.1 * _MRDistY + _RotationOffset);
                        float cosX = cos(_RotationSpeed * -_Time + k * 0.1 * _MRDistY + _RotationOffset);
                        float sinY = sin(_RotationSpeed * -_Time + k * 0.1 * _MRDistY + _RotationOffset);
                        float3x3 rot_A = float3x3
                            (1, 0, 0,
                                0, cosX, -sinY,
                                0, sinX, cosX);

                        float3x3 rotation = mul(rot_45, rot_A);
                        float3 scroll = i.viewDir;
                        M += texCUBE(_BlendMap, mul(scroll, rotation));
                    }

                }

                //scroll = float2(0.3f + -_Time.x*0.3f,0.5f + -_Time.x*0.2f) + i.texcoord.xy;
                //M += tex2D(_BlendMap, scroll * _BlendMap_Size);
                //
                //scroll = float2(-0.2f + _Time.x * 0.2f,-0.5f + -_Time.x * 0.2f) + i.texcoord.xy;
                //M += tex2D(_BlendMap, scroll * _BlendMap_Size);


                //M /= 2;
                M = M * _BlendMap_Size;
                half3 I = c;



                c = (I) * (I + (2 * M) * (1 - I));
                

                return half4(c, 1);
            }
            ENDCG
            }
        }
Fallback Off
}

// Composites the two VR eye images (OpenVR mirror textures) into a red/cyan anaglyph for the desktop window.
// _LeftRect / _RightRect: xy = uv offset, zw = uv scale of the crop taken from each eye image.
Shader "Hidden/VRoxel/Anaglyph"
{
    Properties
    {
        _MainTex ("Unused", 2D) = "black" {}
        _LeftTex ("Left Eye", 2D) = "black" {}
        _RightTex ("Right Eye", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _LeftTex;
            sampler2D _RightTex;
            float4 _LeftRect;
            float4 _RightRect;
            float _FlipY;
            int _Mode; // 0 Dubois, 1 Color, 2 Half color, 3 Gray

            float3 SampleEye(sampler2D tex, float4 rect, float2 uv)
            {
                float2 e = rect.xy + uv * rect.zw;
                if (_FlipY > 0.5) e.y = 1 - e.y;
                if (any(e < 0) || any(e > 1)) return 0;
                // Anaglyph mixing is defined on display (gamma) values; the eye textures are read as linear.
                return LinearToGammaSpace(tex2D(tex, e).rgb);
            }

            float Luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 l = SampleEye(_LeftTex, _LeftRect, i.uv);
                float3 r = SampleEye(_RightTex, _RightRect, i.uv);
                float3 c;
                if (_Mode == 0)
                {
                    // Dubois least-squares red/cyan matrices
                    c.r = dot(l, float3( 0.456,  0.500,  0.176)) + dot(r, float3(-0.043, -0.088, -0.002));
                    c.g = dot(l, float3(-0.040, -0.038, -0.016)) + dot(r, float3( 0.378,  0.734, -0.018));
                    c.b = dot(l, float3(-0.015, -0.021, -0.005)) + dot(r, float3(-0.072, -0.113,  1.226));
                }
                else if (_Mode == 1) c = float3(l.r, r.g, r.b);
                else if (_Mode == 2) c = float3(Luma(l), r.g, r.b);
                else c = float3(Luma(l), Luma(r), Luma(r));
                return fixed4(GammaToLinearSpace(saturate(c)), 1);
            }
            ENDCG
        }
    }
}

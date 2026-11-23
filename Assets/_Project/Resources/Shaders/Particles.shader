// Hạt hiệu ứng = các ô vuông 1-2 pixel nghệ thuật, màu lấy từ bảng màu 16 ô.
Shader "Horde/Particles"
{
    Properties
    {
        _Palette ("Palette", 2D) = "white" {}
        _PPU ("Pixels per unit", Float) = 10
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest+10" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "HordeCommon.cginc"

            StructuredBuffer<float4> _Particles;   // xy = vị trí mặt đất, z = độ cao, w = màu + 16 * (cỡ - 1)
            sampler2D _Palette;
            float _PPU;

            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR0;
            };

            v2f vert (uint vid : SV_VertexID, uint iid : SV_InstanceID)
            {
                float4 p = _Particles[iid];
                float size = floor(p.w / 16.0) + 1.0;
                float colorIdx = p.w - (size - 1.0) * 16.0;
                float2 corner = kCorners[vid] + 0.5;                    // 0..1, căn đúng mép pixel
                float2 basePos = SnapToPixel(p.xy + float2(0, p.z), _PPU);

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(basePos + corner * size / _PPU, DepthFromY(p.y) - 0.02, 1.0));
                o.color = tex2Dlod(_Palette, float4((colorIdx + 0.5) / 16.0, 0.5, 0, 0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return i.color; }
            ENDCG
        }
    }
}

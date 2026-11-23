// Mũi tên + bóng của nó. Cùng 1 shader, 2 material: _Shadow = 0 (mũi tên) / 1 (bóng trên mặt đất).
Shader "Horde/Arrows"
{
    Properties
    {
        _MainTex ("Arrow sprite", 2D) = "white" {}
        _Shadow ("Shadow pass", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 0
        _ZWrite ("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        Pass
        {
            Cull Off
            ZWrite [_ZWrite]
            ZTest LEqual
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "HordeCommon.cginc"

            struct ArrowGpu { float4 a; float4 b; };   // a: xy mặt đất, z độ cao, w phe | b: x góc bay, y góc bóng
            StructuredBuffer<ArrowGpu> _Arrows;
            sampler2D _MainTex;
            float _Shadow;
            float4 _ArrowSize;                         // x = dài, y = rộng, z = PPU

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (uint vid : SV_VertexID, uint iid : SV_InstanceID)
            {
                ArrowGpu g = _Arrows[iid];
                float2 c = kCorners[vid];
                bool shadow = _Shadow > 0.5;

                float s, co;
                sincos(shadow ? g.b.y : g.b.x, s, co);
                float2 local = float2(c.x * _ArrowSize.x, c.y * _ArrowSize.y);
                float2 rotated = float2(local.x * co - local.y * s, local.x * s + local.y * co);

                // Mũi tên được nâng lên theo độ cao; bóng nằm đúng chỗ trên mặt đất
                float2 basePos = shadow ? g.a.xy : g.a.xy + float2(0, g.a.z);
                float z = shadow ? 40.0 : -20.0;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(SnapToPixel(basePos, _ArrowSize.z) + rotated, z, 1.0));
                o.uv = float2(c.x + 0.5, ((c.y + 0.5) + (1.0 - g.a.w)) * 0.5);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.5);
                if (_Shadow > 0.5) return fixed4(0.08, 0.05, 0.04, 0.35);
                return col;
            }
            ENDCG
        }
    }
}

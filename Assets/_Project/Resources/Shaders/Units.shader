// Vẽ TẤT CẢ lính bằng 1 lệnh vẽ duy nhất (GPU instancing qua Graphics.RenderPrimitives).
Shader "Horde/Units"
{
    Properties
    {
        _MainTex ("Unit atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
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

            StructuredBuffer<float4> _Units;   // xy = vị trí, z = ô trong atlas, w = cờ (1 = quay trái, 2 = nháy trắng, 4 = đang bị làm chậm, 8 = khổng lồ 2x2 ô, bit 4.. = số pixel nâng hình)
            sampler2D _MainTex;
            float4 _Atlas;                     // x = số cột, y = số hàng, z = kích thước quad, w = PPU

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float2 fx    : TEXCOORD1;   // x = nháy trắng, y = bị làm chậm
            };

            v2f vert (uint vid : SV_VertexID, uint iid : SV_InstanceID)
            {
                float4 u = _Units[iid];                 // dòng dữ liệu của lính thứ iid
                float2 c = kCorners[vid];
                uint flags = (uint)u.w;

                float k = (flags & 8u) ? 2.0 : 1.0;        // khổng lồ: quad và vùng atlas to gấp đôi
                float lift = (float)(flags >> 4) / _Atlas.w; // vị trí là chân: nâng hình lên cho chân chạm đúng chỗ
                float2 p = SnapToPixel(u.xy, _Atlas.w) + c * _Atlas.z * k + float2(0.0, lift);

                float row = floor(u.z / _Atlas.x);
                float col = u.z - row * _Atlas.x;
                float2 local = c + 0.5;
                if (flags & 1u) local.x = 1.0 - local.x;   // lật ngang khi quay sang trái

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(p, DepthFromY(u.y), 1.0));
                o.uv = float2((col + local.x * k) / _Atlas.x, 1.0 - (row + k * (1.0 - local.y)) / _Atlas.y);
                o.fx = float2((flags & 2u) ? 1.0 : 0.0, (flags & 4u) ? 1.0 : 0.0);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.5);          // cutout: không cần sắp xếp thứ tự vẽ
                col.rgb = lerp(col.rgb, fixed3(0.45, 0.7, 1.0), i.fx.y * 0.4);   // ám xanh khi bị làm chậm
                col.rgb = lerp(col.rgb, fixed3(1, 1, 1), i.fx.x * 0.85);
                return col;
            }
            ENDCG
        }
    }
}

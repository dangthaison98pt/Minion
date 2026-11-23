// "Con dấu": vẽ xác chết, vũng máu, tên cắm đất... THẲNG VÀO texture mặt đất.
// Sau khi in, lính chết bị xoá khỏi mô phỏng → hàng nghìn xác nằm la liệt mà không tốn gì.
Shader "Horde/Stamp"
{
    Properties
    {
        _MainTex ("Stamp atlas", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "UnityCG.cginc"
            #include "HordeCommon.cginc"

            StructuredBuffer<float4> _Stamps;  // xy = vị trí thế giới, z = ô trong atlas, w = cờ (bit 0-1 xoay 90°, bit 2 lật ngang)
            sampler2D _MainTex;
            float4 _Field;                     // xy = góc dưới trái, zw = kích thước chiến trường
            float4 _StampAtlas;                // x = số cột, y = số hàng, z = cỡ quad (đơn vị thế giới), w = PPU

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (uint vid : SV_VertexID, uint iid : SV_InstanceID)
            {
                float4 s = _Stamps[iid];
                uint flags = (uint)s.w;
                float2 c = kCorners[vid];

                float2 world = SnapToPixel(s.xy, _StampAtlas.w) + c * _StampAtlas.z;
                float2 ndc = (world - _Field.xy) / _Field.zw * 2.0 - 1.0;
            #if UNITY_UV_STARTS_AT_TOP
                ndc.y = -ndc.y;   // D3D/Vulkan/Metal: Unity lật ảnh khi vẽ vào RenderTexture; ở đây không dùng ma trận chiếu nên tự lật
            #endif

                float2 local = c + 0.5;
                uint rot = flags & 3u;
                if (rot == 1u) local = float2(local.y, 1.0 - local.x);
                else if (rot == 2u) local = 1.0 - local;
                else if (rot == 3u) local = float2(1.0 - local.y, local.x);
                if (flags & 4u) local.x = 1.0 - local.x;

                float row = floor(s.z / _StampAtlas.x);
                float col = s.z - row * _StampAtlas.x;

                v2f o;
                o.pos = float4(ndc, 0.5, 1.0);
                o.uv = float2((col + local.x) / _StampAtlas.x, 1.0 - (row + 1.0 - local.y) / _StampAtlas.y);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
}

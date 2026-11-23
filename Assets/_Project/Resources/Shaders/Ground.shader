// Mặt đất = 1 hình chữ nhật dán RenderTexture. Máu và xác được "in" thẳng vào texture này.
Shader "Horde/Ground"
{
    Properties
    {
        _MainTex ("Ground", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
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

            sampler2D _MainTex;
            float4 _Field;   // xy = góc dưới trái của chiến trường, zw = kích thước

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (uint vid : SV_VertexID)
            {
                float2 c = kCorners[vid] + 0.5;
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(_Field.xy + c * _Field.zw, 50.0, 1.0));
                o.uv = c;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.uv); }
            ENDCG
        }
    }
}

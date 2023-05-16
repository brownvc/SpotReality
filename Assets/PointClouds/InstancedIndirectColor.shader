Shader "Custom/InstancedIndirectColor" {
    SubShader{
        Tags { "RenderType" = "Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
            };

            struct MeshProperties {
                float4 pos;
                float4 color;
            };


            StructuredBuffer<MeshProperties> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                float4x4 mat = 	 {1.0,0.0,0.0,_Properties[instanceID].pos.x,
							    0.0,1.0,0.0,_Properties[instanceID].pos.y,
							    0.0,0.0,1.0,_Properties[instanceID].pos.z,
							    0.0,0.0,0.0,1.0 };

                float4 pos = mul(mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);
                o.color = _Properties[instanceID].color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                return i.color;
            }

            ENDCG
        }
    }
}
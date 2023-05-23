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
                float2 uv : TEXCOORD0;
            };

            struct MeshProperties {
                float4 pos;
                float4 color;
            };

            sampler2D _colorMap;

            int w;  // Here is the real width but hidden under a pseudonym
            float width; float height; //   1 / width actually but don't tell anyone

            float a;

            float4 _colorMap_ST;

            StructuredBuffer<MeshProperties> _Properties;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {
                v2f o;

                //float scalarProd = -(spotPos.z - _Properties[instanceID].pos.z) / sqrt(pow(spotPos.x - _Properties[instanceID].pos.x,2) + pow(spotPos.z - _Properties[instanceID].pos.z,2)); 
                //float sina = sin(acos(scalarProd));
                
                float d = _Properties[instanceID].pos.w;

                float4x4 mat = 	 {cos(a) * d,0.0,sin(a) * d,_Properties[instanceID].pos.x,
							      0.0,d,0.0,_Properties[instanceID].pos.y,
							      - sin(a) * d,0.0,cos(a) * d,_Properties[instanceID].pos.z,
							      0.0,0.0,0.0,1.0 };
                                  
                /*
                float4x4 mat = 	 {1.0,0.0,0.0,_Properties[instanceID].pos.x,
							      0.0,1.0,0.0,_Properties[instanceID].pos.y,
							      0.0,0.0,1.0,_Properties[instanceID].pos.z,
							      0.0,0.0,0.0,1.0 }; */

                float4 pos = mul(mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);

                //o.color = _Properties[instanceID].color;
                //o.color.r = instanceID * 0.000003;
                
                float id = float(instanceID);

                float4 coor = {(_Properties[instanceID].color.x) * width, _Properties[instanceID].color.y * height, 0.0, 0.0};
                
                //float4 coor = {floor(instanceID * width), floor(instanceID * width), 0.0, 0.0};
                float2 uv = TRANSFORM_TEX(coor.xy, _colorMap);
                coor.x = uv.y; coor.y = uv.x;
                //float4 coor = {0.2,0.5,0.0,0.0};
                o.color = tex2Dlod(_colorMap, coor);

                //o.uv = TRANSFORM_TEX (i.texcoord, _colorMap);
                //o.color.r = _Properties[instanceID].pos.w * 100000;
                //o.color.g = _Properties[instanceID].pos.w / 64;
                //o.color.b = _Properties[instanceID].pos.w / 64;
                //o.color.b = coor.y;
                //o.color.r = coor.y;
                //o.color.g = coor.y;
                //o.color.g = 0.0;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                return i.color;
                //return tex2D(_colorMap,i.uv);
            }

            ENDCG
        }
    }
}
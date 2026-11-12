Shader "Antchemy/Soft Toy"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o);
                o.vertex=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color=UNITY_ACCESS_INSTANCED_PROP(Props,_Color);
                float diffuse=saturate(dot(normalize(i.normal),normalize(float3(-.35,.6,-1))));
                float shine=pow(diffuse,24)*.055;
                return fixed4(saturate(color.rgb*(.78+.22*diffuse)+shine),color.a);
            }
            ENDCG
        }
    }
}

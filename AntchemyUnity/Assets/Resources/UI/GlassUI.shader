Shader "UI/AntchemyGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _Frost ("White frost",Range(0,1))=.82
        _StencilComp ("Stencil Comparison",Float)=8
        _Stencil ("Stencil ID",Float)=0
        _StencilOp ("Stencil Operation",Float)=0
        _StencilWriteMask ("Stencil Write Mask",Float)=255
        _StencilReadMask ("Stencil Read Mask",Float)=255
        _ColorMask ("Color Mask",Float)=15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 screen:TEXCOORD1; float4 world:TEXCOORD2; };
            sampler2D _MainTex,_ColonyFrostedBackdrop;
            fixed4 _Color; float _Frost; float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.screen=ComputeScreenPos(o.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed alpha=tex2D(_MainTex,i.uv).a*i.color.a;
                float2 uv=i.screen.xy/i.screen.w;
                fixed3 paper=tex2D(_ColonyFrostedBackdrop,uv).rgb;
                fixed3 glass=lerp(paper,fixed3(1,1,1),saturate(_Frost+.035*i.uv.y));
                #ifdef UNITY_UI_CLIP_RECT
                alpha*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                return fixed4(glass*i.color.rgb,alpha);
            }
            ENDCG
        }
    }
}

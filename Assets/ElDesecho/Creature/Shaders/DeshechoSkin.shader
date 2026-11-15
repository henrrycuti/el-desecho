Shader "Umbra/DeshechoSkin" {
 Properties { [MainColor] _BaseColor("Skin",Color)=(.025,.034,.035,1) _Smoothness("Wetness",Range(0,1))=.88 _Metallic("Metallic",Range(0,1))=.25 _Fury("Fury",Range(0,1))=0 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;half _Smoothness,_Metallic,_Fury;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half fog:TEXCOORD2;float3 local:TEXCOORD3;};
   V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);o.local=a.positionOS.xyz;return o;}
   half4 frag(V v):SV_Target{
    float3 p=v.local*85;float grain=sin(p.x+sin(p.z*.6))*sin(p.y+sin(p.x*.7))*sin(p.z*.8);
    half3 n=normalize(v.normal+float3(sin(p.y*1.2),sin(p.z),sin(p.x))*.09);
    SurfaceData s=(SurfaceData)0;s.albedo=_BaseColor.rgb*(.85+grain*.15);s.metallic=_Metallic;s.smoothness=saturate(_Smoothness+grain*.03);s.occlusion=1;s.alpha=1;s.normalTS=half3(0,0,1);
    InputData d=(InputData)0;d.positionWS=v.world;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(v.world);d.shadowCoord=TransformWorldToShadowCoord(v.world);d.bakedGI=SampleSH(n);d.shadowMask=half4(1,1,1,1);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(v.positionCS);
    half4 color=UniversalFragmentPBR(d,s);color.rgb=MixFog(color.rgb,v.fog);return color;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}

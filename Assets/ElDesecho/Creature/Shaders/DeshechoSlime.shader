Shader "Umbra/DeshechoSlime" {
 Properties {[MainColor] _BaseColor("Slime",Color)=(.17,.22,.18,.75) _Fade("Fade",Range(0,1))=1}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest LEqual
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;half _Fade;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V {float4 pos:SV_POSITION;half3 normal:TEXCOORD0;float3 world:TEXCOORD1;half4 color:COLOR;half fog:TEXCOORD2;};
   V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.pos=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);o.color=a.color;o.fog=ComputeFogFactor(o.pos.z);return o;}
   half4 frag(V v):SV_Target{half3 n=normalize(v.normal);Light light=GetMainLight();half3 view=GetWorldSpaceNormalizeViewDir(v.world);half shine=pow(saturate(dot(n,normalize(light.direction+view))),48);half3 rgb=_BaseColor.rgb*(max(SampleSH(n),.12)+light.color*(.2+.5*abs(dot(n,light.direction))))+shine*light.color*.8;return half4(MixFog(rgb,v.fog),_BaseColor.a*_Fade*max(v.color.a,.35));}
   ENDHLSL
  }
 }
}

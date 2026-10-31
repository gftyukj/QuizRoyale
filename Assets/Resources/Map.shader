Shader "Royale/Map" {
 Properties { _Color ("Color", Color) = (1,1,1,1) }
 SubShader {
  Tags { "RenderType"="Opaque" }
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct input { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; };
   struct output { float4 pos:SV_POSITION; fixed4 color:COLOR; float light:TEXCOORD0; };
   output vert(input v) { output o; o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color; o.light=.63+.37*max(0,dot(UnityObjectToWorldNormal(v.normal),normalize(float3(-.4,.8,-.3))));return o; }
   fixed4 frag(output i):SV_Target { return fixed4(_Color.rgb*i.color.rgb*i.light,_Color.a); }
   ENDCG
  }
 }
}

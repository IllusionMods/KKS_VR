Shader "Hidden/Sprite Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    Subshader
    {
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }      
            CGPROGRAM
			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            
		    #include "UnityCG.cginc"

	        sampler2D _MainTex;
	        float4 _MainTex_ST;
	        float4 _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
 
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

			#define _f 12.2f;

            fixed4 frag (v2f i) : SV_Target
		    {
                fixed4 col = tex2D(_MainTex, i.uv);
 
 				float2 texel = _MainTex_TexelSize * _f;
				fixed leftPixel = tex2D(_MainTex, i.uv + float2(-texel.x, 0)).a;
				fixed upPixel = tex2D(_MainTex, i.uv + float2(0, texel.y)).a;
				fixed rightPixel = tex2D(_MainTex, i.uv + float2(texel.x, 0)).a;
				fixed bottomPixel = tex2D(_MainTex, i.uv + float2(0, -texel.y)).a;
 
				fixed sum = leftPixel + upPixel + rightPixel + bottomPixel;
				fixed outline = step(1, sum) * step(sum, 4);
 				clip(outline + col.a - 0.5);
                return lerp(fixed4(0,0,0,1), col, col.a);
		    }

            ENDCG
        }
    }
}
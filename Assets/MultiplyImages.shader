Shader "UI/Multiply" {
Properties {
      _MainTex ("Texture", 2D) = "white" {}
      _BlendTex ("Texture", 2D) = "white" {}

       _StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
    }
 
    SubShader {
      Tags { "RenderType" = "Opaque" }

       Stencil
         {
             Ref [_Stencil]
             Comp [_StencilComp]
             Pass [_StencilOp] 
             ReadMask [_StencilReadMask]
             WriteMask [_StencilWriteMask]
         }
          ColorMask [_ColorMask]

      CGPROGRAM
      #pragma surface surf Lambert
      struct Input {
          float2 uv_MainTex;
 
      };
      sampler2D _MainTex;
          sampler2D _BlendTex;
 
      void surf (Input IN, inout SurfaceOutput o) {
        o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
        half3 blend = tex2D (_BlendTex, IN.uv_MainTex).rgb;
               
                //I think this can be optimized a bit
                o.Albedo.r = o.Albedo.r * blend.r;
                o.Albedo.g = o.Albedo.g * blend.g;
                o.Albedo.b = o.Albedo.b * blend.b;
      }
      ENDCG
    }
    Fallback "Diffuse"
  }
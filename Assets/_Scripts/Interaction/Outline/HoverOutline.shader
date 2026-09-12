// Thin, constant-width silhouette outline for hovered interactables.
//
// Technique: inverted hull. The hovered mesh is drawn a second time with
// front faces culled, so only its BACK faces reach the rasteriser. Those
// back faces are pushed outwards in CLIP space along the view-space normal,
// which leaves a thin ring of colour peeking out around the silhouette while
// the object's own depth hides everything inside it.
//
// Why clip-space and not object-space extrusion:
//   Offsetting in clip space and multiplying by w makes the perspective
//   divide cancel out, so the outline stays exactly _OutlineWidth pixels
//   thick no matter how far away the object is or how it is scaled. An
//   object-space extrusion would balloon on small props and vanish on large
//   ones, which is exactly the look we do not want here.
//
// Paired with Bufe/HoverOutlineMask, which runs first and stamps the object's
// silhouette into stencil bit 3. Without that mask a hull traces the contour of
// every SHELL it is handed, so an object built from several overlapping parts
// (the sucuk pile is six sausages) draws contours inside itself as well. The
// stencil test below rejects those: the outline can only ever appear on pixels
// the object itself did not cover.
//
// Cost: two draw calls per hovered mesh — one stencil-only, one thin ring of
// colour. No render target, no full-screen pass, no renderer feature. Nothing
// is drawn at all while the player is not hovering anything.
Shader "Bufe/HoverOutline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0.03, 0.03, 0.05, 1)

        _OutlineWidth ("Outline Width (pixels)", Range(0.25, 8)) = 2

        // LEqual (4) hides the outline behind anything standing in front of the
        // object, which is what you normally want. Set it to Always (8) for an
        // outline that reads through walls and counters.
        [Enum(UnityEngine.Rendering.CompareFunction)]
        _ZTest ("Z Test", Float) = 4
    }

    SubShader
    {
        // Geometry+100 keeps the outline inside the opaque pass but sorted
        // after the objects themselves, so the depth buffer it tests against
        // is already complete.
        Tags
        {
            "RenderType"        = "Opaque"
            "RenderPipeline"    = "UniversalPipeline"
            "Queue"             = "Geometry+100"
            "IgnoreProjector"   = "True"
        }

        Pass
        {
            Name "HoverOutline"
            Tags { "LightMode" = "UniversalForward" }

            Cull    Front
            ZWrite  Off
            ZTest   [_ZTest]
            Blend   SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            // Draw only where Bufe/HoverOutlineMask did NOT stamp bit 3, i.e.
            // strictly outside the object's own silhouette. WriteMask 0 so this
            // pass reads the mask without disturbing it.
            Stencil
            {
                Ref       8
                ReadMask  8
                WriteMask 0
                Comp      NotEqual
            }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Single CBUFFER so the outline material stays SRP Batcher compatible.
            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _ZTest;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                float4 positionCS = TransformWorldToHClip(positionWS);
                float3 normalCS   = TransformWorldToHClipDir(normalWS);

                // Normals pointing straight at (or away from) the camera project
                // to a near-zero 2D direction — leave those vertices alone
                // instead of normalising garbage.
                float2 dir    = normalCS.xy;
                float  dirLen = length(dir);
                dir = dirLen > 1e-5 ? dir / dirLen : float2(0.0, 0.0);

                // * positionCS.w cancels the upcoming perspective divide;
                // * 2 / _ScreenParams.xy converts pixels to NDC units.
                float2 offset = dir * (_OutlineWidth * 2.0 / _ScreenParams.xy) * positionCS.w;
                positionCS.xy += offset;

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

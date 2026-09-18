// Stencil mask for the hover outline. Draws nothing visible.
//
// Runs BEFORE Bufe/HoverOutline (queue Geometry+50 vs Geometry+100, and URP
// sorts opaque draws by render queue first) and stamps the outlined object's
// silhouette into stencil bit 3. The outline pass then refuses to draw on any
// pixel carrying that bit.
//
// Why this is needed:
//   An inverted hull traces the silhouette of every SHELL it is given, not the
//   silhouette of the object as a whole. On something built from several
//   overlapping parts — the sucuk pile is six sausages — each sausage draws its
//   own contour inside the pile, and the parts that interpenetrate add more.
//   The mask turns "outline of each shell" into "outline of the union", which
//   is what the player actually wants to see, and it does so regardless of the
//   mesh's topology, normals or winding.
//
// ZTest Always is deliberate: the mask marks where the object WOULD be, even
// where other geometry hides it. Occlusion is still handled correctly, by the
// outline pass's own depth test. Testing here instead would mean comparing the
// mesh against its own depth values, which is exactly the tie that z-fights.
Shader "Bufe/HoverOutlineMask"
{
    Properties
    {
        // Which stencil bit this mask stamps. URP reserves bits [0,3] for user
        // code (see StencilUsage in the URP package) and clears the buffer every
        // frame, so nothing in the pipeline collides with them — but two outline
        // systems drawing at once WOULD collide with each other. The hover
        // outline stamps bit 3 (8); the tutorial highlight stamps bit 2 (4), so a
        // glowing target and a hovered object never eat into each other's
        // silhouette where they overlap on screen. Must match the _StencilRef on
        // the Bufe/HoverOutline material it is paired with.
        _StencilRef  ("Stencil Ref",  Float) = 8
        _StencilMask ("Stencil Mask", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Geometry+50"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HoverOutlineMask"
            Tags { "LightMode" = "UniversalForward" }

            // Cull Off so open or inside-out meshes still stamp their footprint.
            // With no colour and no depth writes this costs stencil traffic only.
            Cull      Off
            ZWrite    Off
            ZTest     Always
            ColorMask 0

            // The bit comes from the material — see the Properties block above.
            Stencil
            {
                Ref       [_StencilRef]
                ReadMask  [_StencilMask]
                WriteMask [_StencilMask]
                Comp      Always
                Pass      Replace
            }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Stencil state is shaderlab-level, but the properties still have to be
            // declared here or the material drops out of the SRP Batcher.
            CBUFFER_START(UnityPerMaterial)
                float _StencilRef;
                float _StencilMask;
            CBUFFER_END

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

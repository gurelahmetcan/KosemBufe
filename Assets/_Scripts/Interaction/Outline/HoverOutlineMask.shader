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

            // Bit 3. URP reserves stencil bits [0,3] for user code (see
            // StencilUsage in the URP package), and the buffer is cleared every
            // frame, so nothing else in the pipeline can collide with it.
            Stencil
            {
                Ref       8
                ReadMask  8
                WriteMask 8
                Comp      Always
                Pass      Replace
            }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // The mask has no material properties, but declaring the block keeps it
            // unambiguously SRP Batcher compatible.
            CBUFFER_START(UnityPerMaterial)
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

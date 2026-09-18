using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Bufe
{
    /// <summary>
    /// Shared lookups for scripts that move the first-person arms procedurally
    /// (HandBob, HandReach), so they all find the same IK targets and move the
    /// same Transforms.
    ///
    /// Why a "pivot" instead of the IK target itself: the Idle/Grip clips keyframe
    /// the right IK target (HandDisplayGrip) and its parent, so a script writing to
    /// them is overridden by the clip before the IK reads it. The pivot is the
    /// target's topmost ancestor directly under the Animator (e.g. RightHand),
    /// which no clip animates; the clip-driven children ride on top of it.
    ///
    /// The Rig Builder only copies scene values into the animation stream for a
    /// constraint's own target — the target's parents come from the Animator. The
    /// pivot therefore needs a <see cref="RigTransform"/> marker so the IK sees it
    /// move. It is added at runtime when missing; the caller must then rebuild the
    /// rig once (<see cref="RebuildIfNeeded"/>).
    /// </summary>
    public static class ArmsRig
    {
        /// <summary>
        /// Fills empty left/right target references from the Two Bone IK constraints
        /// (told apart by the tip bone's name), and warns once per side if an assigned
        /// target is not what any constraint actually uses.
        /// </summary>
        public static void ResolveTargets(TwoBoneIKConstraint[] constraints,
                                          ref Transform left, ref Transform right, Object context)
        {
            foreach (TwoBoneIKConstraint c in constraints)
            {
                Transform tip    = c.data.tip;
                Transform target = c.data.target;
                if (tip == null || target == null) continue;

                if (left  == null && tip.name.Contains("Left"))  left  = target;
                if (right == null && tip.name.Contains("Right")) right = target;
            }

            WarnIfNotIKTarget(left,  "Left",  constraints, context);
            WarnIfNotIKTarget(right, "Right", constraints, context);
        }

        /// <summary>
        /// The Transform to move for an IK target: its topmost ancestor directly under
        /// the Animator, marked with RigTransform. Falls back to the target itself when
        /// that ancestor would also carry the arm bones (moving it would drag the whole
        /// skeleton) or no Animator is given. Sets addedMarker when a RigTransform was
        /// added, meaning the rig must be rebuilt.
        /// </summary>
        public static Transform ChoosePivot(Transform target, Animator animator,
                                            TwoBoneIKConstraint[] constraints, ref bool addedMarker)
        {
            if (target == null || animator == null) return target;

            Transform root = animator.transform;
            if (!target.IsChildOf(root) || target == root) return target;

            Transform top = target;
            while (top.parent != null && top.parent != root)
                top = top.parent;

            if (top == target) return target;   // directly under the Animator — synced as the IK target

            foreach (TwoBoneIKConstraint c in constraints)
            {
                Transform tip = c.data.tip;
                if (tip != null && tip.IsChildOf(top)) return target;
            }

            if (top.GetComponent<RigTransform>() == null)
            {
                top.gameObject.AddComponent<RigTransform>();
                addedMarker = true;
            }

            return top;
        }

        /// <summary>
        /// Rebuilds the rig graph after a RigTransform was added (the sync list is fixed
        /// at build time). Whichever script adds the marker first rebuilds; later ones
        /// find it present and skip.
        /// </summary>
        public static void RebuildIfNeeded(Animator animator, bool addedMarker)
        {
            if (!addedMarker || animator == null) return;

            RigBuilder rigBuilder = animator.GetComponent<RigBuilder>();
            if (rigBuilder != null && rigBuilder.isActiveAndEnabled)
                rigBuilder.Build();
        }

        /// <summary>Converts a world-space offset into the space t.localPosition lives in.</summary>
        public static Vector3 ToParentSpace(Transform t, Vector3 worldOffset)
            => t.parent != null ? t.parent.InverseTransformVector(worldOffset) : worldOffset;

        private static void WarnIfNotIKTarget(Transform assigned, string side,
                                              TwoBoneIKConstraint[] constraints, Object context)
        {
            if (assigned == null || constraints.Length == 0) return;

            foreach (TwoBoneIKConstraint c in constraints)
                if (c.data.target == assigned) return;

            var actual = new System.Text.StringBuilder();
            foreach (TwoBoneIKConstraint c in constraints)
                actual.Append($"\n  '{c.name}' → target '{(c.data.target != null ? c.data.target.name : "none")}'");

            Debug.LogWarning($"[{context.GetType().Name}] {side} target '{assigned.name}' is not the Target of any " +
                             $"Two Bone IK constraint, so moving it won't move the arm. Constraints found:{actual}", context);
        }
    }
}

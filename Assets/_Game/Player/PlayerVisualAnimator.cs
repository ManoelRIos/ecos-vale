using UnityEngine;

namespace Ecos
{
    /// <summary>Procedural two-bone poses for the temporary low-poly traveler.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        sealed class Limb
        {
            public Transform pivot, hinge, tip;
            public Quaternion restRotation;
        }

        Transform visualRoot, torso;
        Limb leftLeg, rightLeg, leftArm, rightArm;
        float phase, breathTime, motion, running, crouch, airborne, turn, landing;
        float restFootHeight;

        public void Initialize(Transform actor)
        {
            if (visualRoot)
                return;
            int count = actor.childCount;
            visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.gameObject.layer = actor.gameObject.layer;
            visualRoot.SetParent(actor, false);
            for (int i = count - 1; i >= 0; i--)
                actor.GetChild(i).SetParent(visualRoot, false);
            torso = new GameObject("TorsoPivot").transform;
            torso.gameObject.layer = actor.gameObject.layer;
            torso.SetParent(visualRoot, false);
            torso.localPosition = Vector3.up * .80f;
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                var part = visualRoot.GetChild(i);
                if (part == torso || part.name == "Leg-1" || part.name == "Leg1")
                    continue;
                part.SetParent(torso, true);
            }
            leftLeg = BuildLimb(visualRoot.Find("Leg-1"), true);
            rightLeg = BuildLimb(visualRoot.Find("Leg1"), true);
            leftArm = BuildLimb(torso.Find("Arm-1"), false);
            rightArm = BuildLimb(torso.Find("Arm1"), false);
            restFootHeight = LowestFootHeight();
        }

        static Limb BuildLimb(Transform pivot, bool leg)
        {
            if (!pivot)
                return null;
            var limb = new Limb { pivot = pivot, restRotation = pivot.localRotation };
            var upper = pivot.Find("Tecido");
            var tip = pivot.Find(leg ? "Bota" : "Luva");
            if (!upper || !tip)
                return limb;
            float length = upper.localScale.y;
            int originalChildren = pivot.childCount;
            var hinge = new GameObject(leg ? "Joelho" : "Cotovelo").transform;
            hinge.gameObject.layer = pivot.gameObject.layer;
            hinge.SetParent(pivot, false);
            hinge.localPosition = Vector3.down * (length * .5f);
            // Move the boot/glove and held lantern together with the lower segment.
            for (int i = originalChildren - 1; i >= 0; i--)
            {
                var child = pivot.GetChild(i);
                if (child == upper)
                    continue;
                Vector3 local = child.localPosition;
                child.SetParent(hinge, false);
                child.localPosition = local - hinge.localPosition;
            }
            upper.localPosition = Vector3.down * (length * .25f);
            Vector3 scale = upper.localScale;
            scale.y = length * .5f;
            upper.localScale = scale;
            // Create a renderer-only lower part: no duplicate colliders or materials.
            var lower = new GameObject("Tecido inferior");
            lower.layer = upper.gameObject.layer;
            lower.transform.SetParent(hinge, false);
            lower.transform.localPosition = Vector3.down * (length * .25f);
            lower.transform.localScale = scale;
            lower.AddComponent<MeshFilter>().sharedMesh = upper.GetComponent<MeshFilter>().sharedMesh;
            lower.AddComponent<MeshRenderer>().sharedMaterials = upper.GetComponent<MeshRenderer>().sharedMaterials;
            limb.hinge = hinge;
            limb.tip = tip;
            return limb;
        }

        public void Tick(Vector3 actualVelocity, bool grounded, bool crouching,
            float verticalSpeed, float turnRate, float impactSpeed, float dt)
        {
            if (!visualRoot || dt <= 0)
                return;
            float speed = actualVelocity.magnitude;
            float blend = 1 - Mathf.Exp(-dt * 12);
            motion = Mathf.Lerp(motion, Mathf.Clamp01(speed / 1.2f), blend);
            running = Mathf.Lerp(running, Mathf.InverseLerp(4.7f, 7.7f, speed), blend);
            crouch = Mathf.Lerp(crouch, crouching ? 1 : 0, blend);
            airborne = Mathf.Lerp(airborne, grounded ? 0 : 1, 1 - Mathf.Exp(-dt * 15));
            turn = Mathf.Lerp(turn, Mathf.Clamp(turnRate / 240, -1, 1), blend);
            if (impactSpeed > 3) landing = Mathf.Max(landing, Mathf.Clamp01(impactSpeed / 12));
            landing = Mathf.MoveTowards(landing, 0, dt * 3.3f);
            breathTime += dt;
            phase = Mathf.Repeat(phase + speed * dt * Mathf.PI * 2 / Mathf.Lerp(3.4f, 4.6f, running), Mathf.PI * 2);

            float gait = motion * (1 - airborne);
            float step = Mathf.Sin(phase);
            float amplitude = Mathf.Lerp(27, 44, running) * Mathf.Lerp(1, .38f, crouch);
            float airLift = Mathf.Clamp(verticalSpeed / 7, -1, 1);
            float airHip = Mathf.Lerp(-7, -25, Mathf.Max(0, airLift));
            float legBase = -58 * crouch;
            float kneeBase = 116 * crouch;
            Pose(leftLeg, legBase + step * amplitude * gait + airHip * airborne,
                kneeBase + Mathf.Max(0, -step) * Mathf.Lerp(27, 70, running) * gait
                    + (28 + Mathf.Max(0, airLift) * 25) * airborne + landing * 14, 0, blend);
            Pose(rightLeg, legBase - step * amplitude * gait + (airHip + 9) * airborne,
                kneeBase + Mathf.Max(0, step) * Mathf.Lerp(27, 70, running) * gait
                    + (20 + Mathf.Max(0, airLift) * 16) * airborne + landing * 14, 0, blend);
            float armBase = -14 * crouch - 20 * airborne - landing * 8;
            float armSwing = Mathf.Lerp(23, 42, running) * gait;
            float elbow = Mathf.Lerp(10, 55, running) + crouch * 35 + airborne * 20;
            Pose(leftArm, armBase - step * armSwing, -elbow, -4 - running * 3, blend);
            Pose(rightArm, armBase + step * armSwing * .78f, -elbow - 9, 5 + running * 3, blend);

            float breathing = Mathf.Sin(breathTime * 2.2f) * (1 - motion * .65f);
            torso.localRotation = Quaternion.Slerp(torso.localRotation,
                Quaternion.Euler(28 * crouch + 9 * running + landing * 9 + airLift * airborne * 5,
                    -step * gait * 3, -turn * gait * 4), blend);
            torso.localScale = new Vector3(1 + breathing * .004f,
                1 + breathing * .009f - landing * .065f, 1 + breathing * .009f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation,
                Quaternion.Euler(0, 0, -turn * gait * 3), blend);
            visualRoot.localPosition = new Vector3(step * gait * .012f, 0, 0);
            if (grounded)
            {
                // Reset the baseline each tick before solving feet, so no offsets accumulate.
                float correction = restFootHeight - LowestFootHeight();
                visualRoot.localPosition += Vector3.up * correction;
                visualRoot.localPosition += Vector3.up * (Mathf.Max(0, Mathf.Cos(phase * 2)) * .028f * running * gait);
            }
        }

        static void Pose(Limb limb, float hip, float flex, float side, float blend)
        {
            if (limb == null || !limb.pivot)
                return;
            limb.pivot.localRotation = Quaternion.Slerp(limb.pivot.localRotation,
                limb.restRotation * Quaternion.Euler(hip, 0, side), blend);
            if (limb.hinge)
                limb.hinge.localRotation = Quaternion.Slerp(limb.hinge.localRotation,
                    Quaternion.Euler(flex, 0, 0), blend);
        }

        float LowestFootHeight()
        {
            float left = FootHeight(leftLeg), right = FootHeight(rightLeg);
            if (float.IsPositiveInfinity(left) && float.IsPositiveInfinity(right)) return 0;
            return Mathf.Min(left, right);
        }

        float FootHeight(Limb leg)
        {
            if (leg == null || !leg.tip) return float.PositiveInfinity;
            float lowest = float.PositiveInfinity;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 sole = new Vector3(x * .5f, -.5f, z * .5f);
                    lowest = Mathf.Min(lowest, transform.InverseTransformPoint(leg.tip.TransformPoint(sole)).y);
                }
            return lowest;
        }

        public void ResetPose()
        {
            phase = breathTime = motion = running = crouch = airborne = turn = landing = 0;
            if (!visualRoot) return;
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            torso.localScale = Vector3.one;
            torso.localRotation = Quaternion.identity;
            ResetLimb(leftLeg); ResetLimb(rightLeg); ResetLimb(leftArm); ResetLimb(rightArm);
        }

        static void ResetLimb(Limb limb)
        {
            if (limb == null || !limb.pivot) return;
            limb.pivot.localRotation = limb.restRotation;
            if (limb.hinge) limb.hinge.localRotation = Quaternion.identity;
        }
    }
}

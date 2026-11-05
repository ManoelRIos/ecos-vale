using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt.Fps
{
    public enum FpsEnemyKind { Skeleton, Lantern }

    /// <summary>Two readable combat roles for the first-person prototype.</summary>
    public sealed class HauntFpsEnemy : MonoBehaviour
    {
        const float WindupDuration = .42f, DeathDuration = .32f;
        HauntFpsGame game;
        CharacterController body;
        Transform model, leftArm, rightArm, jaw;
        Renderer[] renderers;
        MaterialPropertyBlock hitProperties;
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        Light soulLight;
        FpsEnemyKind kind;
        Vector3 knockVelocity, attackDirection;
        float phase, attackCooldown, windup, attackPose, hurtFlash, hitStretch, deathRemaining, verticalSpeed;
        bool initialized;
        int steerSide;

        public bool IsDead { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public FpsEnemyKind Kind => kind;
        public Vector3 AimPosition => transform.position + Vector3.up * (kind == FpsEnemyKind.Lantern ? 1.35f : 1.15f);

        public void Initialize(HauntFpsGame owner, FpsEnemyKind enemyKind, Vector3 spawn)
        {
            game = owner; kind = enemyKind;
            transform.position = spawn;
            MaxHealth = Health = kind == FpsEnemyKind.Skeleton ? 4 : 5;
            attackCooldown = kind == FpsEnemyKind.Skeleton ? .75f : 1.25f;
            phase = Mathf.Abs(spawn.x) * .27f + Mathf.Abs(spawn.z) * .13f;
            steerSide = spawn.x < 0 ? -1 : 1;
            body = gameObject.AddComponent<CharacterController>();
            body.height = 1.8f; body.radius = .36f; body.center = Vector3.up * .9f;
            body.skinWidth = .045f; body.stepOffset = .18f; body.slopeLimit = 50;
            model = new GameObject("Visual").transform; model.SetParent(transform, false);
            if (kind == FpsEnemyKind.Skeleton) BuildSkeleton(); else BuildLantern();
            renderers = model.GetComponentsInChildren<Renderer>();
            hitProperties = new MaterialPropertyBlock();
            initialized = true;
        }

        public void Tick(float dt)
        {
            if (!initialized || !(dt > 0)) return;
            if (IsDead)
            {
                deathRemaining -= dt;
                float progress = 1 - Mathf.Clamp01(deathRemaining / DeathDuration);
                model.localScale = new Vector3(1 + progress * .4f, Mathf.Max(.04f, 1 - progress), 1 + progress * .4f);
                model.localPosition = Vector3.down * progress * .12f;
                if (deathRemaining <= 0) Destroy(gameObject);
                return;
            }
            if (!game || !game.CanSimulate) return;
            phase += dt;
            attackCooldown = Mathf.Max(0, attackCooldown - dt);
            attackPose = Mathf.Max(0, attackPose - dt);
            hurtFlash = Mathf.Max(0, hurtFlash - dt);
            hitStretch = Mathf.Max(0, hitStretch - dt * 7);
            Vector3 toPlayer = game.PlayerPosition - transform.position;
            toPlayer.y = 0;
            float distance = toPlayer.magnitude;
            Vector3 direction = distance > .001f ? toPlayer / distance : transform.forward;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 1 - Mathf.Exp(-10 * dt));

            if (windup > 0)
            {
                windup = Mathf.Max(0, windup - dt);
                if (windup <= 0) ExecuteAttack(distance);
            }
            else if (attackPose <= 0)
            {
                bool visible = HasLineToPlayer();
                if (attackCooldown <= 0 && visible && distance < (kind == FpsEnemyKind.Skeleton ? 1.65f : 14))
                {
                    windup = WindupDuration;
                    attackDirection = (game.PlayerPosition + Vector3.up * 1.15f - AimPosition).normalized;
                }
                else
                {
                    Vector3 travel;
                    float speed;
                    if (kind == FpsEnemyKind.Skeleton)
                    { travel = distance > 1.18f ? direction : Vector3.zero; speed = 2.55f; }
                    else
                    {
                        Vector3 side = Vector3.Cross(Vector3.up, direction) * (Mathf.Sin(phase * 1.4f) > 0 ? steerSide : -steerSide);
                        travel = !visible || distance > 8 ? direction : distance < 4.4f ? -direction + side * .3f : side * .65f;
                        speed = 1.75f;
                    }
                    Move(travel, speed, dt);
                }
            }
            // Keep grounded while winding up as well; CharacterController.Move
            // resolves collisions for both steering and the short hit impulse.
            verticalSpeed = body.isGrounded ? -2 : Mathf.Max(-18, verticalSpeed - 22 * dt);
            body.Move((knockVelocity + Vector3.up * verticalSpeed) * dt);
            knockVelocity = Vector3.MoveTowards(knockVelocity, Vector3.zero, dt * 16);
            Present(distance, dt);
        }

        void Move(Vector3 desired, float speed, float dt)
        {
            desired.y = 0;
            if (desired.sqrMagnitude < .001f) return;
            desired.Normalize();
            Vector3 origin = transform.position + Vector3.up * .8f;
            if (Physics.SphereCast(origin, .30f, desired, out RaycastHit hit, .85f, ~(1 << 31), QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                Vector3 slide = Vector3.ProjectOnPlane(desired, hit.normal); slide.y = 0;
                if (slide.sqrMagnitude < .08f) slide = Vector3.Cross(Vector3.up, hit.normal) * steerSide;
                desired = slide.normalized;
            }
            body.Move(desired * (speed * dt));
        }

        bool HasLineToPlayer()
        {
            Vector3 delta = game.PlayerPosition + Vector3.up * 1.05f - AimPosition;
            float distance = delta.magnitude;
            if (distance < .001f) return true;
            if (!Physics.Raycast(AimPosition, delta / distance, out RaycastHit hit, distance + .08f, ~(1 << 31), QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<HauntFpsPlayer>() != null;
        }

        void ExecuteAttack(float distance)
        {
            attackCooldown = kind == FpsEnemyKind.Skeleton ? 1.15f : 1.65f;
            attackPose = .18f;
            if (!HasLineToPlayer()) return;
            if (kind == FpsEnemyKind.Skeleton)
            {
                if (distance <= 1.82f)
                    game.TryDamagePlayer(1, (game.PlayerPosition - transform.position).normalized * 3);
            }
            else
            {
                // Aim is captured at the start of the wind-up. Movement during
                // the visible tell remains a useful way to evade the orb.
                Vector3 origin = AimPosition + attackDirection * .62f;
                game.FireHostile(origin, attackDirection, 6.8f, 1, new Color(.47f, 1, .30f));
            }
        }

        public void TakeDamage(int amount, Vector3 point, Vector3 force)
        {
            if (!initialized || IsDead || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            hurtFlash = .065f; hitStretch = 1;
            force.y = 0;
            knockVelocity = Vector3.ClampMagnitude(knockVelocity + force, 5);
            ApplyFlash(1);
            if (Health > 0) return;
            IsDead = true; deathRemaining = DeathDuration; windup = 0;
            body.enabled = false;
            if (soulLight) soulLight.enabled = false;
            game.OnEnemyKilled(this);
        }

        void Present(float distance, float dt)
        {
            float tell = windup > 0 ? 1 - windup / WindupDuration : 0;
            float stride = distance > 1.3f && windup <= 0 ? Mathf.Sin(phase * (kind == FpsEnemyKind.Skeleton ? 9 : 4)) : 0;
            model.localPosition = Vector3.up * (kind == FpsEnemyKind.Lantern ? Mathf.Sin(phase * 3.3f) * .14f : Mathf.Abs(stride) * .035f);
            model.localScale = new Vector3(1 + hitStretch * .12f, 1 - hitStretch * .12f, 1 + hitStretch * .12f);
            if (leftArm) leftArm.localRotation = Quaternion.Euler(stride * 22 - tell * 22, 0, tell * -12);
            if (rightArm)
            {
                float swing = attackPose > 0 ? Mathf.Lerp(65, -80, attackPose / .18f) : -stride * 25 - tell * 80;
                rightArm.localRotation = Quaternion.Euler(swing, 0, tell * 20);
            }
            if (jaw) jaw.localPosition = new Vector3(0, 1.19f - tell * .09f, .035f);
            if (soulLight)
            {
                soulLight.intensity = windup > 0 ? .8f + tell * 2.2f : .6f + Mathf.Sin(phase * 4) * .14f;
                soulLight.range = windup > 0 ? 3.5f : 2.6f;
            }
            ApplyFlash(hurtFlash > 0 ? 1 : 0);
        }

        void ApplyFlash(float value)
        {
            if (renderers == null) return;
            hitProperties.Clear(); hitProperties.SetFloat("_HitFlash", value);
            foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(hitProperties);
        }

        GameObject Part(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
            => game.Box(name, position, scale, material, parent ? parent : model, false);

        void BuildSkeleton()
        {
            var bone = game.PixelMaterial("enemy-bone", new Color(.75f, .72f, .56f));
            var dark = game.PixelMaterial("enemy-sockets", new Color(.075f, .06f, .095f));
            var oldBone = game.PixelMaterial("enemy-aged-bone", new Color(.47f, .48f, .36f));
            var eye = game.PixelMaterial("enemy-cursed-eye", new Color(1, .34f, .13f));
            Part("Skull", new Vector3(0, 1.48f, 0), new Vector3(.53f, .46f, .43f), bone);
            jaw = Part("Jaw", new Vector3(0, 1.19f, .035f), new Vector3(.42f, .14f, .38f), oldBone).transform;
            Part("Left socket", new Vector3(-.13f, 1.50f, .222f), new Vector3(.145f, .12f, .025f), dark);
            Part("Right socket", new Vector3(.13f, 1.50f, .222f), new Vector3(.145f, .12f, .025f), dark);
            Part("Left ember", new Vector3(-.12f, 1.49f, .241f), new Vector3(.045f, .045f, .018f), eye);
            Part("Right ember", new Vector3(.12f, 1.49f, .241f), new Vector3(.045f, .045f, .018f), eye);
            Part("Nasal cavity", new Vector3(0, 1.37f, .223f), new Vector3(.06f, .09f, .024f), dark);
            for (int tooth = -2; tooth <= 2; tooth++)
                Part("Tooth", new Vector3(tooth * .07f, 1.265f, .23f), new Vector3(.045f, .055f, .04f), bone);
            Part("Spine", new Vector3(0, .86f, -.025f), new Vector3(.13f, .58f, .13f), oldBone);
            for (int rib = 0; rib < 4; rib++)
            {
                Part("Rib left", new Vector3(-.13f, 1.10f - rib * .105f, .02f), new Vector3(.24f, .047f, .23f), bone);
                Part("Rib right", new Vector3(.13f, 1.10f - rib * .105f, .02f), new Vector3(.24f, .047f, .23f), bone);
            }
            Part("Pelvis", new Vector3(0, .54f, 0), new Vector3(.39f, .16f, .22f), oldBone);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Thigh", new Vector3(side * .14f, .36f, 0), new Vector3(.10f, .31f, .11f), bone);
                Part("Shin", new Vector3(side * .15f, .12f, .02f), new Vector3(.08f, .24f, .10f), oldBone);
                Part("Foot", new Vector3(side * .15f, .035f, .10f), new Vector3(.14f, .07f, .24f), bone);
            }
            leftArm = new GameObject("Left arm pivot").transform; leftArm.SetParent(model, false); leftArm.localPosition = new Vector3(-.32f, 1.08f, 0);
            rightArm = new GameObject("Right arm pivot").transform; rightArm.SetParent(model, false); rightArm.localPosition = new Vector3(.32f, 1.08f, 0);
            foreach (var arm in new[] { leftArm, rightArm })
            {
                Part("Upper arm", new Vector3(0, -.18f, 0), new Vector3(.10f, .36f, .12f), bone, arm);
                Part("Forearm", new Vector3(0, -.45f, .045f), new Vector3(.09f, .25f, .11f), oldBone, arm);
                Part("Fingers", new Vector3(0, -.60f, .07f), new Vector3(.14f, .09f, .13f), bone, arm);
            }
            Part("Club", new Vector3(.10f, -.45f, .25f), new Vector3(.11f, .58f, .12f), oldBone, rightArm);
            Part("Club head", new Vector3(.10f, -.13f, .25f), new Vector3(.23f, .20f, .19f), bone, rightArm);
        }

        void BuildLantern()
        {
            var cloth = game.PixelMaterial("enemy-lantern-cloth", new Color(.29f, .19f, .43f));
            var trim = game.PixelMaterial("enemy-lantern-trim", new Color(.50f, .40f, .58f));
            var dark = game.PixelMaterial("enemy-sockets", new Color(.075f, .06f, .095f));
            var green = game.PixelMaterial("enemy-lantern-soul", new Color(.50f, 1, .28f));
            Diamond("Floating shroud", new Vector3(0, 1.25f, 0), new Vector3(.72f, 1.1f, .64f), cloth);
            Part("Face void", new Vector3(0, 1.38f, .30f), new Vector3(.38f, .29f, .04f), dark);
            Part("Left eye", new Vector3(-.10f, 1.41f, .329f), new Vector3(.075f, .075f, .025f), green);
            Part("Right eye", new Vector3(.10f, 1.41f, .329f), new Vector3(.075f, .075f, .025f), green);
            for (int edge = -1; edge <= 1; edge += 2)
            {
                Part("Collar", new Vector3(edge * .27f, 1.18f, .03f), new Vector3(.12f, .36f, .39f), trim);
                var wing = Part("Tattered wing", new Vector3(edge * .43f, 1.03f, -.02f), new Vector3(.34f, .12f, .38f), cloth).transform;
                wing.localRotation = Quaternion.Euler(0, 0, edge * 22);
            }
            Diamond("Soul prism", new Vector3(0, .98f, .34f), new Vector3(.24f, .34f, .22f), green);
            var lamp = new GameObject("Soul light"); lamp.transform.SetParent(model, false); lamp.transform.localPosition = new Vector3(0, 1.1f, .4f);
            soulLight = lamp.AddComponent<Light>(); soulLight.type = LightType.Point; soulLight.color = new Color(.55f, 1, .35f); soulLight.intensity = .7f; soulLight.range = 2.6f; soulLight.shadows = LightShadows.None;
        }

        void Diamond(string name, Vector3 position, Vector3 scale, Material material)
        {
            Vector3[] points = { Vector3.up * .5f, Vector3.right * .5f, Vector3.forward * .5f,
                Vector3.left * .5f, Vector3.back * .5f, Vector3.down * .5f };
            int[] triangles = { 0,2,1, 0,3,2, 0,4,3, 0,1,4, 5,1,2, 5,2,3, 5,3,4, 5,4,1 };
            var vertices = new Vector3[24]; var uv = new Vector2[24]; var indices = new int[24];
            for (int i = 0; i < 24; i++) { vertices[i] = points[triangles[i]]; indices[i] = i; uv[i] = i % 3 == 0 ? Vector2.up : i % 3 == 1 ? Vector2.zero : Vector2.right; }
            var mesh = new Mesh { name = "FPS / faceted soul" }; mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
            var go = new GameObject(name); go.transform.SetParent(model, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        void OnDestroy()
        {
            foreach (var mesh in ownedMeshes) if (mesh) Destroy(mesh);
            ownedMeshes.Clear();
        }
    }
}

using UnityEngine;

namespace Ecos.Haunt.Fps
{
    /// <summary>Destructible cover with a visible fuse and an occluded area blast.</summary>
    public sealed class HauntFpsBarrel : MonoBehaviour
    {
        HauntFpsGame game;
        Transform visual, fuse;
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        Collider solid;
        Light warningLight;
        int health = 3;
        float fuseRemaining = -1, flash, phase;
        bool initialized;
        public bool IsDestroyed { get; private set; }
        public bool IsIgnited => fuseRemaining > 0 && !IsDestroyed;
        public Vector3 BlastPosition => transform.position + Vector3.up * .6f;

        public void Initialize(HauntFpsGame owner, Vector3 position)
        {
            game = owner; transform.position = position;
            var collider = gameObject.AddComponent<CapsuleCollider>(); collider.radius = .44f; collider.height = 1.22f; collider.center = Vector3.up * .61f; solid = collider;
            visual = new GameObject("Barrel visual").transform; visual.SetParent(transform, false);
            var wood = game.PixelMaterial("barrel-dark-wood", new Color(.34f, .19f, .12f));
            var lightWood = game.PixelMaterial("barrel-staves", new Color(.47f, .29f, .16f));
            var iron = game.PixelMaterial("barrel-iron", new Color(.23f, .25f, .29f));
            var bone = game.PixelMaterial("barrel-skull", new Color(.91f, .82f, .59f));
            var black = game.PixelMaterial("barrel-skull-dark", new Color(.08f, .065f, .065f));
            Cylinder("Wood barrel", new Vector3(0, .59f, 0), new Vector3(.87f, .56f, .87f), wood);
            Cylinder("Bottom hoop", new Vector3(0, .16f, 0), new Vector3(.91f, .055f, .91f), iron);
            Cylinder("Top hoop", new Vector3(0, 1.02f, 0), new Vector3(.91f, .055f, .91f), iron);
            Cylinder("Lid", new Vector3(0, 1.15f, 0), new Vector3(.83f, .035f, .83f), lightWood);
            for (int side = 0; side < 8; side++)
            {
                float angle = side * Mathf.PI / 4;
                var strip = game.Box("Stave edge", new Vector3(Mathf.Sin(angle) * .43f, .59f, Mathf.Cos(angle) * .43f), new Vector3(.035f, .72f, .035f), lightWood, visual, false);
                strip.transform.localRotation = Quaternion.Euler(0, side * 45, 0);
            }
            game.Box("Skull paint", new Vector3(0, .65f, .445f), new Vector3(.29f, .27f, .025f), bone, visual, false);
            game.Box("Left skull eye", new Vector3(-.075f, .69f, .465f), new Vector3(.065f, .065f, .025f), black, visual, false);
            game.Box("Right skull eye", new Vector3(.075f, .69f, .465f), new Vector3(.065f, .065f, .025f), black, visual, false);
            game.Box("Skull jaw", new Vector3(0, .49f, .445f), new Vector3(.19f, .10f, .025f), bone, visual, false);
            game.Box("Fuse wick", new Vector3(.12f, 1.25f, 0), new Vector3(.045f, .18f, .045f), iron, visual, false);
            var flame = game.PixelMaterial("barrel-fuse-spark", new Color(1, .57f, .12f));
            fuse = game.Box("Fuse spark", new Vector3(.12f, 1.36f, 0), Vector3.one * .07f, flame, visual, false).transform;
            var lightObject = new GameObject("Fuse warning light"); lightObject.transform.SetParent(transform, false); lightObject.transform.localPosition = Vector3.up * 1.4f;
            warningLight = lightObject.AddComponent<Light>(); warningLight.type = LightType.Point; warningLight.color = new Color(1, .38f, .09f); warningLight.range = 3; warningLight.intensity = .35f; warningLight.shadows = LightShadows.None;
            renderers = visual.GetComponentsInChildren<Renderer>(); properties = new MaterialPropertyBlock(); initialized = true;
        }

        public void TakeDamage(int amount, Vector3 point)
        {
            if (!initialized || IsDestroyed || IsIgnited || amount <= 0) return;
            health = Mathf.Max(0, health - amount); flash = .065f;
            if (health > 0) return;
            fuseRemaining = .45f;
            // A burning barrel still provides cover until the announced blast.
            // Repeated hits cannot shorten or restart the fuse.
        }

        public void Tick(float dt)
        {
            if (!initialized || IsDestroyed || !(dt > 0)) return;
            phase += dt; flash = Mathf.Max(0, flash - dt);
            if (IsIgnited)
            {
                fuseRemaining = Mathf.Max(0, fuseRemaining - dt);
                warningLight.intensity = 1.2f + Mathf.Abs(Mathf.Sin(phase * 35)) * 2;
                visual.localScale = Vector3.one * (1 + Mathf.Sin(phase * 35) * .045f);
                if (fuseRemaining <= 0) { Detonate(); return; }
            }
            else warningLight.intensity = .25f + Mathf.Sin(phase * 9) * .08f;
            fuse.localScale = Vector3.one * (.06f + Mathf.Abs(Mathf.Sin(phase * 21)) * (IsIgnited ? .12f : .025f));
            properties.Clear(); properties.SetFloat("_HitFlash", flash > 0 ? 1 : 0);
            foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(properties);
        }

        void Detonate()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            game.OnBarrelDestroyed(this);
            solid.enabled = false;
            warningLight.enabled = false;
            visual.gameObject.SetActive(false);
            var debris = game.PixelMaterial("barrel-charred-remains", new Color(.16f, .10f, .08f));
            for (int piece = 0; piece < 6; piece++)
            {
                float angle = piece * Mathf.PI / 3;
                var remnant = game.Box("Burnt stave", new Vector3(Mathf.Sin(angle) * .42f, .05f, Mathf.Cos(angle) * .42f), new Vector3(.12f, .08f, .44f), debris, transform, false);
                remnant.transform.localRotation = Quaternion.Euler(0, piece * 60, 0);
            }
            // Root owns the victim snapshot, line-of-sight checks, blast impulse,
            // sound and screen trauma; a chain never mutates this Tick iteration.
            game.Explode(BlastPosition, 3.4f, 5);
        }

        void Cylinder(string name, Vector3 position, Vector3 scale, Material material)
        {
            if (game.LowPolyMode)
            {
                game.Polygons.Part(name, position, new Vector3(scale.x, scale.y * 2, scale.z), material, visual, false, Ecos.Haunt.LowPoly.LowPolyShape.Cylinder);
                return;
            }
            var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder); part.name = name;
            part.transform.SetParent(visual, false); part.transform.localPosition = position; part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>(); if (collider) { collider.enabled = false; Destroy(collider); }
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}

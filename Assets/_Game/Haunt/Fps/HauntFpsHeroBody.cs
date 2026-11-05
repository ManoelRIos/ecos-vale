using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos.Haunt.Fps
{
    /// <summary>Full procedural bodies for the four souls. Upper geometry casts
    /// shadows in first person; the overhead view uses its matching sprite.</summary>
    public sealed class HauntFpsHeroBody : MonoBehaviour
    {
        struct Surface { public Renderer Renderer; public bool Lower; }
        readonly List<Surface> surfaces = new List<Surface>();
        readonly List<Transform> ornaments = new List<Transform>();
        HauntFpsGame game;
        Transform model, leftLeg, rightLeg, leftArm, rightArm, cape;
        float clock;

        public void Initialize(HauntFpsGame owner)
        {
            game = owner;
            model = new GameObject("Corpo 3D / " + HauntFpsGame.CharacterName(game.State.Character)).transform;
            model.SetParent(transform, false);
            CharacterKind kind = game.State.Character;
            Color[] coats = { new Color(.12f, .33f, .32f), new Color(.3f, .13f, .43f), new Color(.36f, .07f, .12f), new Color(.27f, .34f, .17f) };
            Material coat = Mat("tecido", coats[(int)kind]);
            Material dark = Mat("sombra", new Color(.055f, .05f, .075f));
            Material metal = Mat("metal", new Color(.43f, .49f, .52f));
            Material skin = Mat("rosto", kind == CharacterKind.Reaper ? new Color(.83f, .81f, .64f) : new Color(.65f, .47f, .32f));
            Material light = Mat("brilho", kind == CharacterKind.Medium ? new Color(.71f, .32f, 1) : kind == CharacterKind.Alchemist ? new Color(.27f, 1, .53f) : new Color(1, .7f, .27f));
            light.SetColor("_EmissionColor", light.GetColor("_BaseColor") * .7f);
            Part("Tronco", new Vector3(0, 1.05f, 0), new Vector3(.47f, .63f, .3f), coat);
            Part("Cinto", new Vector3(0, .78f, .01f), new Vector3(.5f, .09f, .33f), dark);
            Part("Fivela", new Vector3(0, .78f, .185f), new Vector3(.1f, .08f, .04f), metal);
            Part("Cabeca", new Vector3(0, 1.58f, 0), new Vector3(.37f, .4f, .34f), skin);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Olho", new Vector3(side * .095f, 1.61f, .18f), new Vector3(.06f, .055f, .025f), dark);
                var leg = new GameObject(side < 0 ? "Perna esquerda" : "Perna direita").transform;
                leg.SetParent(model, false); leg.localPosition = new Vector3(side * .15f, .7f, 0);
                Part("Calca", new Vector3(0, -.24f, 0), new Vector3(.19f, .48f, .22f), dark, leg, true);
                Part("Bota", new Vector3(0, -.62f, .07f), new Vector3(.21f, .18f, .37f), coat, leg, true);
                var arm = new GameObject(side < 0 ? "Braco esquerdo" : "Braco direito").transform;
                arm.SetParent(model, false); arm.localPosition = new Vector3(side * .31f, 1.24f, 0);
                Part("Manga", new Vector3(0, -.23f, 0), new Vector3(.18f, .46f, .21f), coat, arm);
                Part("Luva", new Vector3(0, -.48f, .05f), new Vector3(.17f, .17f, .2f), dark, arm);
                if (side < 0) { leftLeg = leg; leftArm = arm; } else { rightLeg = leg; rightArm = arm; }
            }
            if (kind == CharacterKind.Hunter)
            {
                Part("Chapeu / aba", new Vector3(0, 1.81f, 0), new Vector3(.68f, .07f, .53f), coat);
                Part("Chapeu / copa", new Vector3(0, 1.93f, -.025f), new Vector3(.4f, .22f, .35f), coat);
                Part("Chapeu / fita", new Vector3(0, 1.86f, 0), new Vector3(.42f, .04f, .37f), light);
                cape = new GameObject("Capa esvoacante").transform; cape.SetParent(model, false); cape.localPosition = new Vector3(0, 1.27f, -.18f);
                for (int strip = 0; strip < 5; strip++)
                    ornaments.Add(Part("Capa / tira", new Vector3((strip - 2) * .105f, -.38f, -.04f), new Vector3(.11f, .8f - (strip % 2) * .08f, .065f), coat, cape).transform);
                Part("Carabina", new Vector3(.07f, -.35f, .38f), new Vector3(.12f, .13f, .7f), metal, rightArm);
                Part("Coronha", new Vector3(.07f, -.4f, .13f), new Vector3(.14f, .2f, .25f), coat, rightArm);
            }
            else if (kind == CharacterKind.Medium)
            {
                Hood(coat);
                Part("Cajado", new Vector3(0, -.18f, .16f), new Vector3(.08f, 1.65f, .08f), metal, rightArm);
                Part("Cristal do cajado", new Vector3(0, .75f, .16f), new Vector3(.23f, .33f, .23f), light, rightArm).transform.localRotation = Quaternion.Euler(0, 35, 20);
                for (int i = 0; i < 3; i++) ornaments.Add(Part("Orbe arcano", Vector3.zero, Vector3.one * .14f, light).transform);
            }
            else if (kind == CharacterKind.Reaper)
            {
                Hood(coat);
                Part("Cavidade nasal", new Vector3(0, 1.52f, .183f), new Vector3(.05f, .08f, .025f), dark);
                for (int tooth = -2; tooth <= 2; tooth++) Part("Dente", new Vector3(tooth * .05f, 1.42f, .185f), new Vector3(.03f, .06f, .03f), skin);
                Part("Haste da foice", new Vector3(.06f, -.1f, .19f), new Vector3(.08f, 1.8f, .08f), dark, rightArm);
                for (int segment = 0; segment < 5; segment++)
                    Part("Lamina da foice", new Vector3(-segment * .13f, .82f - segment * segment * .025f, .19f), new Vector3(.18f, .11f, .07f), metal, rightArm).transform.localRotation = Quaternion.Euler(0, 0, -segment * 15);
            }
            else
            {
                Part("Mascara", new Vector3(0, 1.48f, .19f), new Vector3(.27f, .18f, .17f), dark);
                for (int side = -1; side <= 1; side += 2)
                {
                    Part("Oculos", new Vector3(side * .105f, 1.63f, .19f), new Vector3(.17f, .12f, .06f), light);
                    Transform arm = side < 0 ? leftArm : rightArm;
                    Part("Frasco", new Vector3(0, -.44f, .23f), new Vector3(.21f, .28f, .21f), light, arm);
                    Part("Rolha", new Vector3(0, -.25f, .23f), new Vector3(.09f, .1f, .09f), coat, arm);
                }
                Part("Mochila", new Vector3(0, 1.05f, -.24f), new Vector3(.38f, .46f, .21f), metal);
            }
        }

        void Hood(Material coat)
        {
            Part("Capuz / topo", new Vector3(0, 1.84f, -.025f), new Vector3(.48f, .17f, .43f), coat);
            for (int side = -1; side <= 1; side += 2) Part("Capuz / lateral", new Vector3(side * .215f, 1.6f, -.03f), new Vector3(.12f, .4f, .41f), coat);
            Part("Manto", new Vector3(0, .66f, -.07f), new Vector3(.55f, .55f, .32f), coat);
        }
        Material Mat(string name, Color color) => game.PixelMaterial("heroi " + (int)game.State.Character + " " + name, color);
        GameObject Part(string name, Vector3 point, Vector3 size, Material material, Transform parent = null, bool lower = false)
        {
            GameObject part = game.Box(name, point, size, material, parent ? parent : model, false);
            surfaces.Add(new Surface { Renderer = part.GetComponent<Renderer>(), Lower = lower }); return part;
        }
        public void Tick(float dt, bool moving)
        {
            if (!game || !game.Player || !model) return;
            clock += Mathf.Max(0, dt);
            bool topDown = game.Player.IsTopDown;
            bool distant = Vector3.Distance(game.Player.View.transform.position, game.Player.Position + Vector3.up * 1.6f) > 2;
            foreach (var surface in surfaces)
            {
                if (!surface.Renderer) continue;
                surface.Renderer.enabled = !topDown;
                surface.Renderer.shadowCastingMode = distant || surface.Lower ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            }
            float stride = moving && !game.Player.IsDodging ? Mathf.Sin(clock * 10) * 24 : 0;
            leftLeg.localRotation = Quaternion.Euler(stride, 0, 0); rightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
            leftArm.localRotation = Quaternion.Euler(-stride * .5f - 12, 0, 0); rightArm.localRotation = Quaternion.Euler(stride * .5f - 18, 0, 0);
            model.localScale = new Vector3(1, game.Player.IsDodging ? .8f : 1, 1);
            if (cape)
                for (int i = 0; i < ornaments.Count; i++) ornaments[i].localRotation = Quaternion.Euler(Mathf.Sin(clock * 6 + i * .7f) * 12 + (moving ? 15 : 0), 0, 0);
            else if (game.State.Character == CharacterKind.Medium)
                for (int i = 0; i < ornaments.Count; i++)
                {
                    float angle = clock * 1.8f + i * Mathf.PI * 2 / 3;
                    ornaments[i].localPosition = new Vector3(Mathf.Cos(angle) * .68f, 1.4f + Mathf.Sin(angle * 2) * .14f, Mathf.Sin(angle) * .68f);
                }
        }
    }
}

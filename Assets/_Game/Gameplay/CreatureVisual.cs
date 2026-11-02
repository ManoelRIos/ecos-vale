using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos
{
    /// <summary>Shared low-poly meshes and a small transform rig; no imported or paid assets.</summary>
    public sealed class CreatureVisual
    {
        static readonly Color Dark = new Color(.13f, .12f, .19f);
        static readonly Color Cream = new Color(.83f, .72f, .52f);
        readonly string species;
        readonly Transform body, head, tail;
        readonly List<Transform> legs = new List<Transform>();
        readonly List<Transform> wings = new List<Transform>();
        readonly Vector3 bodyRest, headRest;
        float cycle;

        public CreatureVisual(Palette p, Transform root, string id, float phase = 0)
        {
            species = id;
            cycle = phase;
            body = Pivot(root, "Corpo animado", Vector3.zero);
            if (id == "bat")
            {
                Part(p, body, "Corpo violeta", Vector3.zero, new Vector3(.28f, .4f, .24f), new Color(.36f, .18f, .55f));
                head = Pivot(body, "Cabeça", new Vector3(0, .19f, .11f));
                Part(p, head, "Rosto", Vector3.zero, Vector3.one * .24f, new Color(.48f, .29f, .61f));
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(p, head, "Orelha", new Vector3(side * .09f, .14f, 0), new Vector3(.10f, .23f, .09f), Dark);
                    var wing = Pivot(body, "Asa", new Vector3(side * .1f, .07f, 0));
                    Part(p, wing, "Membrana", new Vector3(side * .45f, 0, -.03f), new Vector3(.95f, .08f, .45f), new Color(.47f, .21f, .63f));
                    wings.Add(wing);
                }
            }
            else if (id == "wisp")
            {
                Part(p, body, "Coração da fagulha", Vector3.zero, new Vector3(.28f, .45f, .28f), new Color(.17f, .72f, .65f), true);
                Part(p, body, "Chama exterior", Vector3.up * .2f, new Vector3(.18f, .38f, .18f), new Color(.32f, .46f, .86f), true);
                head = Pivot(body, "Olhar", new Vector3(0, .04f, .13f));
                for (int i = 0; i < 3; i++)
                    Part(p, body, "Rastro", new Vector3(0, -.22f - i * .13f, -.09f - i * .10f), Vector3.one * (.15f - i * .025f), new Color(.18f, .56f, .68f), true);
            }
            else
            {
                bool hare = id == "hare", deer = id == "deer", boar = id == "boar";
                Color coat = hare ? new Color(.63f, .49f, .72f) : deer ? new Color(.65f, .38f, .19f)
                    : boar ? new Color(.32f, .29f, .34f) : new Color(.81f, .35f, .14f);
                float legLength = hare ? .21f : deer ? .65f : boar ? .31f : .33f;
                float length = hare ? .56f : deer ? 1.05f : boar ? .95f : .78f;
                float width = hare ? .35f : deer ? .42f : boar ? .64f : .38f;
                float middle = legLength + (boar ? .30f : .22f);
                Part(p, body, "Peito facetado", new Vector3(0, middle, 0), new Vector3(width, boar ? .61f : .48f, length), coat);
                Part(p, body, "Peito claro", new Vector3(0, middle - .07f, length * .22f), new Vector3(width * .75f, .28f, .32f), hare || boar ? coat * .83f : Cream);
                head = Pivot(body, "Cabeça", new Vector3(0, middle + (deer ? .39f : .08f), length * .52f));
                Part(p, head, "Crânio", Vector3.zero, new Vector3(width * .85f, hare ? .32f : .35f, .38f), coat);
                Part(p, head, "Focinho", new Vector3(0, -.035f, .19f), new Vector3(width * .62f, .16f, .22f), boar ? new Color(.50f, .38f, .40f) : Cream);
                for (int side = -1; side <= 1; side += 2)
                {
                    var ear = Part(p, head, "Orelha", new Vector3(side * width * .25f, hare ? .27f : .20f, -.025f),
                        new Vector3(.10f, hare ? .46f : .20f, .10f), hare ? new Color(.79f, .60f, .73f) : coat);
                    ear.localRotation = Quaternion.Euler(0, 0, side * (hare ? -9 : -28));
                    for (int end = -1; end <= 1; end += 2)
                    {
                        var leg = Pivot(body, "Pata", new Vector3(side * width * .36f, legLength, end * length * .31f));
                        Part(p, leg, "Perna", Vector3.down * legLength * .5f, new Vector3(boar ? .16f : .10f, legLength, .12f), coat * .7f);
                        legs.Add(leg);
                    }
                    if (deer)
                    {
                        var horn = Part(p, head, "Galhada", new Vector3(side * .18f, .38f, -.03f), new Vector3(.07f, .60f, .07f), new Color(.87f, .65f, .22f));
                        horn.localRotation = Quaternion.Euler(-12, 0, side * -24);
                        Part(p, head, "Ponta dourada", new Vector3(side * .31f, .57f, .02f), new Vector3(.09f, .20f, .09f), new Color(.82f, .56f, .16f), true);
                    }
                    if (boar) Part(p, head, "Presa", new Vector3(side * .16f, -.04f, .30f), new Vector3(.08f, .19f, .08f), Cream);
                }
                tail = Pivot(body, "Cauda", new Vector3(0, middle, -length * .48f));
                Part(p, tail, "Cauda facetada", new Vector3(0, hare ? 0 : -.06f, hare ? -.05f : -.24f),
                    hare ? Vector3.one * .20f : boar ? new Vector3(.10f, .12f, .30f) : new Vector3(.25f, .29f, .66f),
                    id == "fox" ? new Color(.27f, .52f, .29f) : Cream);
            }
            for (int side = -1; side <= 1; side += 2)
                Part(p, head, "Olho", new Vector3(side * (id == "wisp" ? .06f : id == "bat" ? .055f : .11f), .035f, id == "wisp" ? .02f : .17f), Vector3.one * .045f, Dark);
            bodyRest = body.localPosition;
            headRest = head.localPosition;
            // No creature casts per-pixel local light shadows. All parts share palette materials.
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
        }

        static Transform Pivot(Transform root, string title, Vector3 position)
        {
            var g = new GameObject(title); g.transform.SetParent(root, false); g.transform.localPosition = position; return g.transform;
        }

        static Transform Part(Palette p, Transform root, string title, Vector3 position, Vector3 scale, Color color, bool glow = false)
            => p.Part(root, title, PrimitiveType.Sphere, position, scale, color, glow).transform;

        public void Tick(float dt, float speed, bool alert)
        {
            cycle += dt * (species == "bat" ? 14 : 3 + speed * 4);
            float stride = Mathf.Sin(cycle), breath = Mathf.Sin(cycle * .35f);
            float hop = species == "hare" ? Mathf.Max(0, stride) * Mathf.Min(.25f, speed * .13f) : 0;
            body.localPosition = bodyRest + Vector3.up * (species == "wisp" || species == "bat" ? Mathf.Sin(cycle * .6f) * .09f : hop + breath * .015f);
            head.localPosition = headRest + Vector3.up * (alert ? .025f : Mathf.Sin(cycle * .3f) * .018f);
            head.localRotation = Quaternion.Euler(alert ? -8 : Mathf.Sin(cycle * .45f) * 5, Mathf.Sin(cycle * .25f) * 9, 0);
            for (int i = 0; i < legs.Count; i++)
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(cycle + (i == 0 || i == 3 ? 0 : Mathf.PI)) * Mathf.Min(34, speed * 18), 0, 0);
            for (int i = 0; i < wings.Count; i++)
                wings[i].localRotation = Quaternion.Euler(0, 0, (i == 0 ? 1 : -1) * (20 + stride * 48));
            if (tail) tail.localRotation = Quaternion.Euler(0, Mathf.Sin(cycle * .6f) * 17, 0);
        }
    }
}

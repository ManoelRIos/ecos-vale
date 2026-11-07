using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Ecos.Haunt.LowPoly;

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
        GameObject carriedWeapon;
        WeaponKind carriedKind;
        float clock;

        public Vector3 MuzzlePosition => carriedWeapon && game
            ? carriedWeapon.transform.TransformPoint(HauntLowPolyArsenal.MuzzleFor(game.State.Weapon))
            : transform.position + Vector3.up * 1.2f + transform.forward * .6f;

        public void Initialize(HauntFpsGame owner)
        {
            game = owner;
            model = new GameObject("Corpo 3D / " + HauntFpsGame.CharacterName(game.State.Character)).transform;
            model.SetParent(transform, false);
            if (game.LowPolyMode) { BuildLowPolyBody(); RefreshWeapon(); return; }
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

        void BuildLowPolyBody()
        {
            CharacterKind kind = game.State.Character;
            Color[] palette = { new Color(.1f,.34f,.32f),new Color(.33f,.14f,.48f),new Color(.41f,.065f,.13f),new Color(.3f,.39f,.17f) };
            Material coat = Mat("lp-tecido", palette[(int)kind]);
            Material shadow = Mat("lp-sombra",new Color(.055f,.05f,.075f));
            Material leather = Mat("lp-couro",new Color(.24f,.13f,.09f));
            Material steel = Mat("lp-metal",new Color(.45f,.53f,.58f));
            Material skin = Mat("lp-rosto",kind==CharacterKind.Reaper?new Color(.87f,.82f,.64f):new Color(.73f,.49f,.29f));
            Color glowColor=kind==CharacterKind.Medium?new Color(.74f,.3f,1):kind==CharacterKind.Alchemist?new Color(.3f,1,.5f):new Color(1,.7f,.18f);
            Material glow=Mat("lp-brilho",glowColor);glow.SetColor("_EmissionColor",glowColor*.65f);
            LowPart("Tronco facetado",new Vector3(0,1.03f,0),new Vector3(.66f,.65f,.4f),coat,LowPolyShape.TaperedPrism);
            LowPart("Gola",new Vector3(0,1.33f,0),new Vector3(.32f,.16f,.28f),coat,LowPolyShape.Cylinder);
            LowPart("Cinto de couro",new Vector3(0,.76f,.02f),new Vector3(.58f,.12f,.43f),leather,LowPolyShape.BevelBox);
            LowPart("Fivela",new Vector3(0,.76f,.25f),new Vector3(.12f,.09f,.05f),glow,LowPolyShape.BevelBox);
            LowPart("Cabeca estilizada",new Vector3(0,1.62f,.015f),new Vector3(.62f,.64f,.57f),skin,LowPolyShape.Sphere);
            LowPart("Mandibula",new Vector3(0,1.42f,.1f),new Vector3(.42f,.19f,.36f),skin,LowPolyShape.BevelBox);
            for(int side=-1;side<=1;side+=2)
            {
                LowPart("Orbita",new Vector3(side*.14f,1.65f,.27f),new Vector3(.13f,.12f,.055f),shadow,LowPolyShape.BevelBox);
                LowPart("Pupila",new Vector3(side*.135f,1.65f,.302f),new Vector3(.055f,.06f,.025f),kind==CharacterKind.Reaper?glow:shadow,LowPolyShape.Crystal);
                var leg=new GameObject(side<0?"Perna esquerda":"Perna direita").transform;leg.SetParent(model,false);leg.localPosition=new Vector3(side*.17f,.7f,0);
                LowPart("Perna da alma",new Vector3(0,-.22f,0),new Vector3(.23f,.46f,.25f),shadow,LowPolyShape.TaperedPrism,leg,true);
                LowPart("Bota de viagem",new Vector3(0,-.59f,.09f),new Vector3(.25f,.22f,.4f),leather,LowPolyShape.BevelBox,leg,true);
                LowPart("Fivela da bota",new Vector3(0,-.53f,.29f),new Vector3(.12f,.07f,.04f),steel,LowPolyShape.BevelBox,leg,true);
                var arm=new GameObject(side<0?"Braco esquerdo":"Braco direito").transform;arm.SetParent(model,false);arm.localPosition=new Vector3(side*.36f,1.24f,0);
                LowPart("Ombro",new Vector3(0,-.04f,0),new Vector3(.29f,.28f,.31f),coat,LowPolyShape.Sphere,arm);
                LowPart("Manga",new Vector3(0,-.26f,0),new Vector3(.21f,.4f,.23f),coat,LowPolyShape.TaperedPrism,arm);
                LowPart("Luva",new Vector3(0,-.48f,.04f),new Vector3(.2f,.18f,.23f),leather,LowPolyShape.BevelBox,arm);
                if(side<0){leftLeg=leg;leftArm=arm;}else{rightLeg=leg;rightArm=arm;}
            }
            if(kind==CharacterKind.Hunter)
            {
                LowPart("Chapeu largo",new Vector3(0,1.94f,-.015f),new Vector3(.93f,.08f,.72f),coat,LowPolyShape.Cylinder);
                LowPart("Copa do chapeu",new Vector3(0,2.06f,-.035f),new Vector3(.56f,.21f,.5f),coat,LowPolyShape.TaperedPrism);
                LowPart("Fita do chapeu",new Vector3(0,1.99f,-.02f),new Vector3(.58f,.045f,.52f),glow,LowPolyShape.Cylinder);
                cape=new GameObject("Capa low poly").transform;cape.SetParent(model,false);cape.localPosition=new Vector3(0,1.27f,-.21f);
                for(int i=0;i<4;i++)ornaments.Add(LowPart("Painel da capa",new Vector3((i-1.5f)*.16f,-.43f,-.075f),new Vector3(.18f,.91f-(i%2)*.08f,.07f),coat,LowPolyShape.BevelBox,cape).transform);
                LowPart("Bandoleira",new Vector3(.11f,1.03f,.23f),new Vector3(.09f,.62f,.065f),leather,LowPolyShape.BevelBox).transform.localRotation=Quaternion.Euler(0,0,-25);
            }
            else if(kind==CharacterKind.Medium||kind==CharacterKind.Reaper)
            {
                LowPart("Capuz facetado",new Vector3(0,1.85f,-.04f),new Vector3(.8f,.49f,.73f),coat,LowPolyShape.Cone);
                for(int side=-1;side<=1;side+=2)LowPart("Veste do capuz",new Vector3(side*.31f,1.6f,-.07f),new Vector3(.16f,.49f,.62f),coat,LowPolyShape.TaperedPrism);
                LowPart("Manto ritual",new Vector3(0,.66f,-.08f),new Vector3(.79f,.58f,.56f),coat,LowPolyShape.Cone);
                if(kind==CharacterKind.Medium)
                    for(int i=0;i<3;i++)ornaments.Add(LowPart("Cristal orbital",Vector3.zero,Vector3.one*.18f,glow,LowPolyShape.Crystal).transform);
                else
                {
                    LowPart("Nariz osseo",new Vector3(0,1.53f,.3f),new Vector3(.075f,.12f,.035f),shadow,LowPolyShape.Cone);
                    for(int tooth=-2;tooth<=2;tooth++)LowPart("Dente da mascara",new Vector3(tooth*.065f,1.39f,.287f),new Vector3(.035f,.075f,.035f),shadow,LowPolyShape.BevelBox);
                    LowPart("Selo da colheita",new Vector3(0,1.1f,.235f),new Vector3(.14f,.23f,.05f),glow,LowPolyShape.Crystal);
                    LowPart("Foice de viagem / haste",new Vector3(-.34f,1.03f,-.28f),new Vector3(.075f,1.87f,.075f),leather,LowPolyShape.Cylinder).transform.localRotation=Quaternion.Euler(0,0,-9);
                    for(int segment=0;segment<4;segment++)
                        LowPart("Foice de viagem / lamina",new Vector3(-.29f+segment*.15f,1.98f-segment*segment*.027f,-.28f),new Vector3(.22f,.12f,.065f),steel,LowPolyShape.Crystal).transform.localRotation=Quaternion.Euler(0,0,-segment*15);
                }
            }
            else
            {
                LowPart("Chapeu de alquimista",new Vector3(0,1.94f,-.03f),new Vector3(.82f,.07f,.7f),coat,LowPolyShape.Cylinder);
                LowPart("Copa alquimica",new Vector3(0,2.04f,-.04f),new Vector3(.56f,.19f,.5f),coat,LowPolyShape.TaperedPrism);
                LowPart("Respirador",new Vector3(0,1.43f,.3f),new Vector3(.34f,.23f,.2f),steel,LowPolyShape.BevelBox);
                for(int side=-1;side<=1;side+=2)
                {
                    LowPart("Oculos de laboratorio",new Vector3(side*.14f,1.64f,.3f),new Vector3(.22f,.16f,.08f),glow,LowPolyShape.Cylinder).transform.localRotation=Quaternion.Euler(90,0,0);
                    LowPart("Frasco do cinto",new Vector3(side*.31f,.82f,.03f),new Vector3(.16f,.29f,.16f),glow,LowPolyShape.Cylinder);
                    LowPart("Rolha",new Vector3(side*.31f,1,.03f),new Vector3(.08f,.08f,.08f),leather,LowPolyShape.Cylinder);
                }
                LowPart("Mochila de reagentes",new Vector3(0,1.04f,-.3f),new Vector3(.5f,.57f,.28f),leather,LowPolyShape.BevelBox);
                LowPart("Tanque de elixir",new Vector3(0,1.11f,-.48f),new Vector3(.24f,.49f,.24f),glow,LowPolyShape.Cylinder);
            }
        }

        GameObject LowPart(string name,Vector3 point,Vector3 size,Material material,LowPolyShape shape,Transform parent=null,bool lower=false)
        {
            var part=game.Polygons.Part(name,point,size,material,parent?parent:model,false,shape);
            surfaces.Add(new Surface {Renderer=part.GetComponent<Renderer>(),Lower=lower});return part;
        }

        public void RefreshWeapon()
        {
            if(!game||!game.LowPolyMode||!rightArm)return;
            if(carriedWeapon&&carriedKind==game.State.Weapon)return;
            if(carriedWeapon)
            {
                Transform previous=carriedWeapon.transform;
                surfaces.RemoveAll(surface=>surface.Renderer&&surface.Renderer.transform.IsChildOf(previous));
                carriedWeapon.SetActive(false);Destroy(carriedWeapon);
            }
            carriedKind=game.State.Weapon;
            carriedWeapon=HauntLowPolyArsenal.Build(carriedKind,rightArm,game);
            carriedWeapon.transform.localPosition=new Vector3(0,-.43f,.08f);
            carriedWeapon.transform.localRotation=Quaternion.Euler(65,0,0);
            carriedWeapon.transform.localScale=Vector3.one*.8f;
            foreach(var renderer in carriedWeapon.GetComponentsInChildren<Renderer>())surfaces.Add(new Surface{Renderer=renderer,Lower=false});
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
            if(game.LowPolyMode)RefreshWeapon();
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
            if(game.LowPolyMode)
            {
                leftArm.localRotation=Quaternion.Euler(-63+game.Player.Pitch*.7f-stride*.12f,0,8);
                rightArm.localRotation=Quaternion.Euler(-65+game.Player.Pitch*.7f+stride*.12f,0,-4);
            }
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

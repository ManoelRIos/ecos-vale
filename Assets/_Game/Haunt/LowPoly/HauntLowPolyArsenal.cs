using UnityEngine;
using Ecos.Haunt.Fps;

namespace Ecos.Haunt.LowPoly
{
    /// <summary>Separable low-poly weapon models. This factory owns no combat or ammunition state.</summary>
    public static class HauntLowPolyArsenal
    {
        public static Vector3 MuzzleFor(WeaponKind kind)
        {
            float distance = kind == WeaponKind.SoulCannon ? .86f
                : kind == WeaponKind.HunterCarbine || kind == WeaponKind.Repeater ? .8f
                : kind == WeaponKind.Scattergun || kind == WeaponKind.BoneCaster || kind == WeaponKind.VeilWand ? .74f
                : kind == WeaponKind.Banana || kind == WeaponKind.BoxingGlove ? .57f : .66f;
            return new Vector3(0, .035f, distance);
        }

        public static GameObject Build(WeaponKind kind, Transform parent, HauntFpsGame game)
        {
            var root = new GameObject("Arma low poly / " + RunState.WeaponName(kind));
            root.transform.SetParent(parent, false);
            Material metal = game.PixelMaterial("lp-weapon-iron", new Color(.23f,.29f,.34f));
            Material bright = game.PixelMaterial("lp-weapon-steel", new Color(.62f,.71f,.73f));
            Material dark = game.PixelMaterial("lp-weapon-dark", new Color(.055f,.045f,.07f));
            Material wood = game.PixelMaterial("lp-weapon-wood", new Color(.31f,.17f,.1f));
            Material bone = game.PixelMaterial("lp-weapon-bone", new Color(.86f,.79f,.57f));
            Material gold = game.PixelMaterial("lp-weapon-gold", new Color(.91f,.57f,.13f));
            Material red = game.PixelMaterial("lp-weapon-red", new Color(.58f,.08f,.13f));
            Material green = game.PixelMaterial("lp-weapon-green", new Color(.25f,.95f,.51f));
            Material purple = game.PixelMaterial("lp-weapon-purple", new Color(.66f,.24f,.94f));
            green.SetColor("_EmissionColor", new Color(.08f,.48f,.2f));
            purple.SetColor("_EmissionColor", new Color(.25f,.055f,.46f));
            var geometry = game.Polygons;
            GameObject Part(string name,Vector3 position,Vector3 size,Material material,LowPolyShape shape=LowPolyShape.BevelBox)
                => geometry.Part(name,position,size,material,root.transform,false,shape);
            GameObject Barrel(string name,float x,float y,float z,float length,float radius,Material material)
            {
                var obj=Part(name,new Vector3(x,y,z),new Vector3(radius,length,radius),material,LowPolyShape.Cylinder);
                obj.transform.localRotation=Quaternion.Euler(90,0,0);return obj;
            }
            float muzzle=MuzzleFor(kind).z;
            if(kind==WeaponKind.Pistol||kind==WeaponKind.HunterCarbine||kind==WeaponKind.Scattergun||kind==WeaponKind.Repeater)
            {
                Part("Receptor",new Vector3(0,.03f,.24f),new Vector3(.17f,.18f,.32f),metal);
                Part("Empunhadura",new Vector3(0,-.13f,.13f),new Vector3(.12f,.26f,.15f),wood,LowPolyShape.TaperedPrism).transform.localRotation=Quaternion.Euler(-16,0,0);
                Part("Gatilho",new Vector3(0,-.075f,.28f),new Vector3(.035f,.12f,.1f),dark);
                float start=.34f,length=muzzle-start;
                if(kind==WeaponKind.Scattergun)
                {
                    Barrel("Cano esquerdo",-.065f,.035f,start+length*.5f,length,.105f,metal);
                    Barrel("Cano direito",.065f,.035f,start+length*.5f,length,.105f,metal);
                    Part("Guarda de madeira",new Vector3(0,-.03f,.44f),new Vector3(.23f,.14f,.25f),wood);
                }
                else
                {
                    Barrel("Cano",0,.035f,start+length*.5f,length,.115f,metal);
                    Barrel("Boca do cano",0,.035f,muzzle-.025f,.05f,.125f,dark);
                    if(kind==WeaponKind.Pistol)Barrel("Tambor",0,.035f,.27f,.19f,.24f,bright);
                    if(kind==WeaponKind.Repeater)Part("Carregador",new Vector3(0,-.17f,.34f),new Vector3(.13f,.29f,.16f),metal);
                    if(kind==WeaponKind.HunterCarbine)
                    {
                        Barrel("Luneta",0,.19f,.32f,.26f,.105f,bright);
                        Part("Coronha",new Vector3(0,-.03f,-.08f),new Vector3(.15f,.22f,.25f),wood);
                    }
                }
                Part("Mira",new Vector3(0,.14f,muzzle-.09f),new Vector3(.035f,.075f,.04f),gold);
            }
            else switch(kind)
            {
                case WeaponKind.SoulCannon:
                    Part("Corpo do canhao",new Vector3(0,.025f,.33f),new Vector3(.33f,.34f,.49f),metal);
                    Barrel("Canhao de almas",0,.035f,.64f,.42f,.32f,metal);
                    Barrel("Boca espectral",0,.035f,.84f,.045f,.27f,green);
                    Part("Empunhadura dupla",new Vector3(0,-.2f,.16f),new Vector3(.17f,.22f,.18f),dark);
                    for(int i=-1;i<=1;i+=2)Part("Caveira de alma",new Vector3(i*.18f,.06f,.33f),new Vector3(.12f,.17f,.19f),bone,LowPolyShape.Sphere);
                    break;
                case WeaponKind.VeilWand:
                    Barrel("Cajado do veu",0,-.035f,.32f,.66f,.075f,wood);
                    Part("Cristal arcano",new Vector3(0,.035f,.63f),new Vector3(.25f,.29f,.23f),purple,LowPolyShape.Crystal);
                    for(int i=0;i<4;i++)Part("Runas",new Vector3(Mathf.Cos(i*Mathf.PI*.5f)*.16f,.035f+Mathf.Sin(i*Mathf.PI*.5f)*.16f,.61f),new Vector3(.055f,.055f,.12f),gold);
                    break;
                case WeaponKind.BoneCaster:
                    Barrel("Osso principal",0,.025f,.35f,.67f,.11f,bone);
                    for(int end=0;end<2;end++)for(int side=-1;side<=1;side+=2)Part("Epifise",new Vector3(side*.07f,.025f,end==0?.06f:.68f),new Vector3(.15f,.16f,.15f),bone,LowPolyShape.Sphere);
                    Part("Cranio invocador",new Vector3(0,.06f,.31f),new Vector3(.28f,.24f,.25f),bone,LowPolyShape.Sphere);
                    for(int side=-1;side<=1;side+=2)Part("Orbita",new Vector3(side*.07f,.115f,.415f),new Vector3(.065f,.06f,.04f),dark);
                    break;
                case WeaponKind.ArcCoil:
                    Part("Corpo voltaico",new Vector3(0,0,.27f),new Vector3(.23f,.2f,.34f),metal);
                    for(int i=0;i<5;i++)Barrel("Anel da bobina",0,.035f,.25f+i*.055f,.025f,.29f,gold);
                    Barrel("Nucleo eletrico",0,.035f,.52f,.27f,.14f,green);
                    for(int side=-1;side<=1;side+=2)Part("Frasco lateral",new Vector3(side*.17f,.02f,.28f),new Vector3(.1f,.25f,.12f),green,LowPolyShape.Cylinder);
                    break;
                case WeaponKind.Stapler:
                    Part("Base do grampeador",new Vector3(0,-.07f,.29f),new Vector3(.2f,.09f,.53f),metal);
                    Part("Braco do grampeador",new Vector3(0,.08f,.29f),new Vector3(.23f,.12f,.55f),red).transform.localRotation=Quaternion.Euler(-5,0,0);
                    Part("Dobradiça",new Vector3(0,0,.04f),new Vector3(.25f,.24f,.14f),bright);
                    Part("Saida de grampos",new Vector3(0,.02f,.58f),new Vector3(.15f,.07f,.08f),bright);
                    break;
                case WeaponKind.Banana:
                    for(int i=0;i<5;i++)Part("Banana / segmento",new Vector3((i-2)*(i-2)*.013f,Mathf.Sin(i*.6f)*.055f,.1f+i*.105f),new Vector3(.16f,.18f,.15f),gold,LowPolyShape.TaperedPrism).transform.localRotation=Quaternion.Euler(85,0,(i-2)*-7);
                    Part("Ponta da banana",new Vector3(.055f,.06f,.55f),new Vector3(.09f,.1f,.1f),dark,LowPolyShape.Cone);
                    break;
                case WeaponKind.SpinningTop:
                    Part("Piao / disco",new Vector3(0,.02f,.31f),new Vector3(.5f,.13f,.5f),red,LowPolyShape.Cylinder);
                    Part("Piao / nucleo",new Vector3(0,.1f,.31f),new Vector3(.22f,.11f,.22f),gold,LowPolyShape.Crystal);
                    Part("Piao / ponta",new Vector3(0,-.11f,.31f),new Vector3(.16f,.18f,.16f),metal,LowPolyShape.Cone).transform.localRotation=Quaternion.Euler(180,0,0);
                    Barrel("Lancador do piao",0,.035f,.55f,.2f,.12f,metal);
                    break;
                case WeaponKind.MirrorDisc:
                    Barrel("Disco espelhado",0,.035f,.37f,.09f,.48f,bright);
                    Barrel("Miolo do espelho",0,.035f,.43f,.035f,.25f,purple);
                    Barrel("Projetor",0,.035f,.56f,.2f,.1f,metal);
                    break;
                case WeaponKind.RayLantern:
                    Part("Lanterna / base",new Vector3(0,-.14f,.29f),new Vector3(.32f,.08f,.34f),metal);
                    Part("Lanterna / topo",new Vector3(0,.19f,.29f),new Vector3(.32f,.09f,.34f),metal);
                    Part("Lanterna / chama",new Vector3(0,.025f,.29f),new Vector3(.16f,.27f,.16f),green,LowPolyShape.Crystal);
                    for(int side=-1;side<=1;side+=2)for(int depth=-1;depth<=1;depth+=2)Part("Gaiola",new Vector3(side*.12f,.025f,.29f+depth*.13f),new Vector3(.035f,.28f,.035f),gold);
                    Barrel("Lente de raios",0,.035f,.55f,.21f,.18f,bright);
                    break;
                case WeaponKind.BoneFlute:
                    Barrel("Flauta de osso",0,.035f,.31f,.62f,.14f,bone);
                    for(int i=0;i<5;i++)Part("Furo da flauta",new Vector3(0,.108f,.16f+i*.075f),new Vector3(.055f,.025f,.04f),dark,LowPolyShape.Sphere);
                    Part("Bocal",new Vector3(0,.035f,.63f),new Vector3(.14f,.08f,.09f),gold);
                    break;
                case WeaponKind.Umbrella:
                    Barrel("Cabo do guarda-chuva",0,.035f,.28f,.55f,.06f,dark);
                    Part("Guarda-chuva fechado",new Vector3(0,.035f,.46f),new Vector3(.37f,.38f,.37f),purple,LowPolyShape.Cone).transform.localRotation=Quaternion.Euler(90,0,0);
                    Barrel("Ponteira",0,.035f,.63f,.075f,.07f,gold);
                    break;
                case WeaponKind.HiveLauncher:
                    Part("Colmeia",new Vector3(0,.035f,.3f),new Vector3(.4f,.4f,.48f),gold,LowPolyShape.Sphere);
                    for(int i=0;i<3;i++)Barrel("Aro da colmeia",0,.035f,.18f+i*.12f,.035f,.43f,wood);
                    Barrel("Entrada da colmeia",0,.035f,.55f,.2f,.16f,dark);
                    break;
                case WeaponKind.BoxingGlove:
                    Part("Luva de boxe",new Vector3(0,.06f,.34f),new Vector3(.41f,.35f,.4f),red,LowPolyShape.Sphere);
                    Part("Polegar",new Vector3(.19f,-.025f,.25f),new Vector3(.15f,.2f,.22f),red,LowPolyShape.Sphere);
                    Part("Punho",new Vector3(0,-.01f,.08f),new Vector3(.25f,.26f,.17f),bone,LowPolyShape.Cylinder).transform.localRotation=Quaternion.Euler(90,0,0);
                    break;
            }
            return root;
        }
    }
}

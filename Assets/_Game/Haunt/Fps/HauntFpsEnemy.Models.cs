using UnityEngine;

namespace Ecos.Haunt.Fps
{
    public sealed partial class HauntFpsEnemy
    {
        // Original voxel silhouettes stay separate from the 2D billboard layer.
        // Built-in meshes are shared by Unity; only our faceted lantern owns a mesh.
        void BuildRosterModel()
        {
            var bone = game.PixelMaterial("enemy-bone",new Color(.75f,.72f,.56f));
            var dark = game.PixelMaterial("enemy-sockets",new Color(.075f,.06f,.095f));
            var metal = game.PixelMaterial("enemy-roster-metal",new Color(.29f,.34f,.4f));
            var purple = game.PixelMaterial("enemy-roster-purple",new Color(.31f,.15f,.42f));
            var ember = game.PixelMaterial("enemy-roster-ember",new Color(1,.4f,.1f)); ember.SetColor("_EmissionColor",new Color(1,.25f,.04f));
            var green = game.PixelMaterial("enemy-roster-green",new Color(.35f,.8f,.2f));
            var skin = game.PixelMaterial("enemy-roster-skin",new Color(.46f,.43f,.24f));
            var wood = game.PixelMaterial("enemy-roster-wood",new Color(.3f,.19f,.105f));
            switch(kind)
            {
                case FpsEnemyKind.Pumpkin:
                    PumpkinHead(new Vector3(0,1.15f,0), .9f, ember,dark,wood);
                    RootFeet(wood);break;
                case FpsEnemyKind.Ghost: case FpsEnemyKind.StalkerShadow:
                    var shroud=kind==FpsEnemyKind.Ghost?bone:purple;
                    Diamond("Mortalha espectral",new Vector3(0,1.17f,0),new Vector3(.85f,1.25f,.65f),shroud);
                    Part("Vazio facial",new Vector3(0,1.38f,.34f),new Vector3(.5f,.25f,.04f),dark);
                    EyesAt(new Vector3(0,1.4f,.37f),kind==FpsEnemyKind.Ghost?green:ember,.15f);
                    for(int i=-1;i<=1;i++)Part("Cauda de ectoplasma",new Vector3(i*.22f,.52f,-.08f),new Vector3(.14f,.42f,.17f),shroud);
                    break;
                case FpsEnemyKind.Cultist: case FpsEnemyKind.Acolyte:
                    Part("Batina",new Vector3(0,.69f,0),new Vector3(.69f,1.15f,.5f),purple);
                    Part("Capuz",new Vector3(0,1.47f,0),new Vector3(.67f,.67f,.57f),purple);
                    Part("Máscara de luto",new Vector3(0,1.46f,.3f),new Vector3(.39f,.42f,.04f),dark);
                    EyesAt(new Vector3(0,1.51f,.34f),ember,.12f);
                    leftArm=RosterArm(-1,purple);rightArm=RosterArm(1,purple);
                    Part("Cajado ritual",new Vector3(.13f,-.18f,.12f),new Vector3(.08f,1.35f,.08f),wood,rightArm);
                    Part("Sigilo do cajado",new Vector3(.13f,.45f,.12f),new Vector3(.28f,.32f,.13f),kind==FpsEnemyKind.Cultist?ember:green,rightArm);
                    if(kind==FpsEnemyKind.Acolyte)Part("Estola de osso",new Vector3(0,1,.27f),new Vector3(.16f,.9f,.04f),bone);
                    break;
                case FpsEnemyKind.Bat: case FpsEnemyKind.FireBat:
                    var wing=kind==FpsEnemyKind.FireBat?ember:purple;
                    Part("Corpo do morcego",new Vector3(0,1.18f,0),new Vector3(.4f,.55f,.33f),dark);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var pivot=new GameObject("Pivô da asa").transform;pivot.SetParent(model,false);pivot.localPosition=new Vector3(side*.22f,1.2f,0);
                        if(side<0)leftArm=pivot;else rightArm=pivot;
                        var membrane=Part("Asa serrilhada",new Vector3(side*.35f,.08f,0),new Vector3(.8f,.23f,.3f),wing,pivot);
                        membrane.transform.localRotation=Quaternion.Euler(0,0,side*23);
                        for(int tip=0;tip<3;tip++)Part("Ponta da asa",new Vector3(side*(.17f+tip*.23f),-.1f+tip*.06f,0),new Vector3(.1f,.25f,.09f),dark,pivot);
                        Part("Orelha",new Vector3(side*.13f,1.59f,0),new Vector3(.1f,.28f,.15f),wing);
                    }
                    EyesAt(new Vector3(0,1.28f,.18f),ember,.11f);break;
                case FpsEnemyKind.Larva:
                    for(int segment=0;segment<4;segment++)Part("Segmento da larva",new Vector3(0,.32f,-.6f+segment*.3f),new Vector3(.53f,.5f,.38f),skin);
                    Part("Cabeça larval",new Vector3(0,.42f,.54f),new Vector3(.59f,.58f,.4f),bone);
                    EyesAt(new Vector3(0,.52f,.76f),dark,.16f);break;
                case FpsEnemyKind.Sentinel:
                    Part("Base da sentinela",new Vector3(0,.18f,0),new Vector3(.82f,.35f,.7f),metal);
                    Part("Torre de pedra",new Vector3(0,.94f,0),new Vector3(.68f,1.35f,.63f),metal);
                    Part("Elmo",new Vector3(0,1.59f,0),new Vector3(.82f,.42f,.71f),metal);
                    EyesAt(new Vector3(0,1.59f,.38f),ember,.19f);
                    for(int side=-1;side<=1;side+=2)Part("Canhão de osso",new Vector3(side*.49f,1.08f,.17f),new Vector3(.26f,.26f,.58f),bone);
                    Part("Núcleo da torre",new Vector3(0,.94f,.34f),new Vector3(.3f,.5f,.05f),green);break;
                case FpsEnemyKind.Slime:
                    Part("Base do limo",new Vector3(0,.23f,0),new Vector3(1,.4f,.88f),green);
                    Part("Domo do limo",new Vector3(0,.48f,0),new Vector3(.72f,.44f,.65f),green);
                    Part("Brilho viscoso",new Vector3(-.19f,.6f,.22f),new Vector3(.13f,.12f,.11f),bone);
                    EyesAt(new Vector3(0,.47f,.44f),dark,.21f);break;
                case FpsEnemyKind.Spider:
                    Part("Abdômen aracnídeo",new Vector3(0,.48f,-.16f),new Vector3(.76f,.7f,.95f),purple);
                    Part("Cabeça aracnídea",new Vector3(0,.57f,.47f),new Vector3(.57f,.48f,.39f),dark);
                    SpiderLegs(metal);EyesAt(new Vector3(0,.64f,.68f),ember,.18f);
                    Part("Presa esquerda",new Vector3(-.15f,.35f,.7f),new Vector3(.1f,.28f,.11f),bone);
                    Part("Presa direita",new Vector3(.15f,.35f,.7f),new Vector3(.1f,.28f,.11f),bone);break;
                case FpsEnemyKind.MushroomBrute:
                    Part("Talo bruto",new Vector3(0,.75f,0),new Vector3(.75f,1.25f,.65f),bone);
                    Part("Chapéu fúngico",new Vector3(0,1.5f,0),new Vector3(1.3f,.43f,1.05f),purple);
                    Part("Copa fúngica",new Vector3(0,1.79f,0),new Vector3(.91f,.2f,.75f),purple);
                    for(int dot=-1;dot<=1;dot++)Part("Esporo luminoso",new Vector3(dot*.34f,1.72f,.45f),new Vector3(.17f,.12f,.08f),green);
                    EyesAt(new Vector3(0,1.1f,.35f),dark,.19f);
                    leftArm=RosterArm(-1,green);rightArm=RosterArm(1,green);RootFeet(bone);break;
                case FpsEnemyKind.ClockworkImp:
                    Part("Caixa do mecanismo",new Vector3(0,.96f,0),new Vector3(.64f,.9f,.5f),metal);
                    Part("Cabeça mecânica",new Vector3(0,1.52f,0),new Vector3(.6f,.49f,.48f),metal);
                    EyesAt(new Vector3(0,1.56f,.27f),ember,.18f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Part("Antena",new Vector3(side*.24f,1.92f,0),new Vector3(.07f,.35f,.07f),wood);
                        Part("Engrenagem",new Vector3(side*.39f,1.02f,0),new Vector3(.17f,.55f,.55f),wood);
                    }
                    Part("Mola frontal",new Vector3(0,.98f,.28f),new Vector3(.28f,.3f,.08f),bone);RootFeet(metal);break;
                case FpsEnemyKind.Scarecrow:
                    Part("Estaca",new Vector3(0,.75f,0),new Vector3(.13f,1.5f,.14f),wood);
                    Part("Trapos",new Vector3(0,1.03f,0),new Vector3(.6f,.66f,.37f),skin);
                    PumpkinHead(new Vector3(0,1.65f,0),.65f,ember,dark,wood);
                    leftArm=RosterArm(-1,wood);rightArm=RosterArm(1,wood);
                    for(int side=-1;side<=1;side+=2)for(int strand=0;strand<3;strand++)Part("Palha",new Vector3(side*(.38f+strand*.11f),.94f,.03f),new Vector3(.11f,.23f,.12f),bone);
                    Part("Foice",new Vector3(.18f,-.2f,.15f),new Vector3(.07f,1.38f,.08f),wood,rightArm);
                    Part("Lâmina",new Vector3(-.05f,.45f,.15f),new Vector3(.52f,.14f,.1f),metal,rightArm);break;
                case FpsEnemyKind.Gargoyle:
                    Part("Pedestal",new Vector3(0,.12f,0),new Vector3(.87f,.24f,.8f),metal);
                    Part("Torso de pedra",new Vector3(0,.83f,0),new Vector3(.78f,1.1f,.63f),metal);
                    Part("Cabeça de pedra",new Vector3(0,1.52f,.06f),new Vector3(.64f,.58f,.6f),metal);
                    EyesAt(new Vector3(0,1.6f,.39f),ember,.18f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var stoneWing=Part("Asa de pedra",new Vector3(side*.6f,1.28f,-.25f),new Vector3(.71f,.72f,.18f),metal);stoneWing.transform.localRotation=Quaternion.Euler(0,side*15,side*-25);
                        Part("Chifre",new Vector3(side*.23f,1.98f,0),new Vector3(.12f,.35f,.15f),bone);
                    }
                    leftArm=RosterArm(-1,metal);rightArm=RosterArm(1,metal);break;
            }
        }
        Transform RosterArm(int side,Material material)
        {
            var pivot=new GameObject(side<0?"Braço esquerdo":"Braço direito").transform;pivot.SetParent(model,false);pivot.localPosition=new Vector3(side*.37f,1.1f,0);
            Part("Braço",new Vector3(side*.1f,-.23f,0),new Vector3(.2f,.55f,.23f),material,pivot);return pivot;
        }
        void RootFeet(Material material)
        {
            for(int side=-1;side<=1;side+=2)Part("Pé",new Vector3(side*.23f,.13f,.08f),new Vector3(.27f,.26f,.4f),material);
        }
        void EyesAt(Vector3 center,Material material,float spacing)
        {
            for(int side=-1;side<=1;side+=2)Part("Olho",center+Vector3.right*(side*spacing),new Vector3(.095f,.09f,.04f),material);
        }
        void PumpkinHead(Vector3 center,float width,Material rind,Material dark,Material stem)
        {
            for(int rib=-1;rib<=1;rib++)Part("Gomo da abóbora",center+Vector3.right*(rib*width*.24f),new Vector3(width*.42f,width*(rib==0?.87f:.72f),width*.65f),rind);
            EyesAt(center+new Vector3(0,width*.1f,width*.35f),dark,width*.2f);
            Part("Boca serrada",center+new Vector3(0,-width*.18f,width*.35f),new Vector3(width*.48f,width*.08f,.04f),dark);
            Part("Caule",center+Vector3.up*width*.56f,new Vector3(.11f,width*.3f,.12f),stem);
        }
        void SpiderLegs(Material material)
        {
            for(int side=-1;side<=1;side+=2)for(int leg=0;leg<4;leg++)
            {
                var upper=Part("Perna da aranha",new Vector3(side*.59f,.38f,.48f-leg*.3f),new Vector3(.62f,.09f,.09f),material);upper.transform.localRotation=Quaternion.Euler(0,side*(leg*14-20),side*18);
                var tip=Part("Ponta da perna",new Vector3(side*.84f,.16f,.64f-leg*.39f),new Vector3(.08f,.38f,.08f),material);tip.transform.localRotation=Quaternion.Euler(0,0,-side*19);
            }
        }
    }
}

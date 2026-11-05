using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt.Fps
{
    /// <summary>The seven guardians share simulation and collision in both views.
    /// Every attack is preceded by a visible wind-up; queued bursts belong to this boss.</summary>
    public sealed class HauntFpsBoss : MonoBehaviour
    {
        static readonly string[] Names = { "O REI DAS ABOBORAS", "A VIUVA DO VEU", "O BISPO DAS CINZAS",
            "A MATRIARCA DA PESTE", "O SINO DO ABISMO", "O CARRASCO DE FERRO", "O ORACULO DO BREJO" };
        static readonly string[] Keys = { "boss", "widow", "bishop", "matriarch", "bell", "iron-executioner", "bog-oracle" };
        static readonly Color[] Accents = { new Color(1,.43f,.12f), new Color(.8f,.37f,1), new Color(1,.24f,.23f),
            new Color(.45f,1,.25f), new Color(.55f,.42f,1), new Color(1,.36f,.12f), new Color(.35f,1,.66f) };
        HauntFpsGame game;
        CharacterController body;
        Transform model, leftArm, rightArm, head, core;
        Renderer[] surfaces;
        MaterialPropertyBlock flashProperties;
        Light aura;
        Vector3 capturedAim, knockVelocity;
        readonly List<Pulse> pulses = new List<Pulse>();
        float clock, attackCooldown = .8f, windup, attackPose, flash, deathRemaining, verticalSpeed;
        int attack;
        bool initialized;
        sealed class Pulse { public float Delay; public int Kind, Step; public Vector3 Direction; }

        public int Archetype { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsEnraged => Health > 0 && Health * 2 <= MaxHealth;
        public float IntroRemaining { get; private set; }
        public Vector3 AimPosition => transform.position + Vector3.up * 1.35f;
        public string DisplayName => Names[Archetype];
        public static string NameFor(int archetype) => Names[Mathf.Clamp(archetype, 0, Names.Length - 1)];
        public string SpriteKey => Keys[Archetype];
        public BossAnimationState AnimationState => IsDead ? BossAnimationState.Death
            : IntroRemaining > 0 || windup > 0 ? BossAnimationState.Telegraph
            : attackPose > 0 ? BossAnimationState.Attack : IsEnraged ? BossAnimationState.Enrage : BossAnimationState.Idle;

        public void Initialize(HauntFpsGame owner, int archetype, Vector3 spawn, int floor)
        {
            if (initialized) throw new InvalidOperationException("O chefe ja foi inicializado.");
            if (archetype < 0 || archetype >= Names.Length) throw new ArgumentOutOfRangeException(nameof(archetype));
            game = owner; Archetype = archetype; transform.position = spawn;
            Health = MaxHealth = 40 + Mathf.Clamp(floor, 1, 20) * 8;
            IntroRemaining = 3;
            body = gameObject.AddComponent<CharacterController>();
            body.height = 3; body.radius = .9f; body.center = Vector3.up * 1.5f;
            body.skinWidth = .04f; body.stepOffset = .2f;
            model = new GameObject("Visual").transform; model.SetParent(transform, false);
            BuildModel();
            surfaces = model.GetComponentsInChildren<Renderer>(); flashProperties = new MaterialPropertyBlock();
            var lamp = new GameObject("Aura do guardiao"); lamp.transform.SetParent(model, false); lamp.transform.localPosition = Vector3.up * 1.8f;
            aura = lamp.AddComponent<Light>(); aura.type = LightType.Point; aura.color = Accents[Archetype];
            aura.intensity = 1.5f; aura.range = 6; aura.shadows = LightShadows.None;
            initialized = true;
        }

        public void Tick(float dt)
        {
            if (!initialized || !game || !game.CanSimulate || !(dt > 0) || float.IsInfinity(dt)) return;
            dt = Mathf.Min(dt, .05f); clock += dt;
            if (IsDead)
            {
                deathRemaining = Mathf.Max(0, deathRemaining - dt);
                float progress = 1 - deathRemaining / .9f;
                model.localScale = new Vector3(1 + progress * .35f, Mathf.Max(.03f, 1 - progress), 1 + progress * .35f);
                model.localPosition = Vector3.down * progress * .2f;
                aura.intensity = (1 - progress) * 4;
                if (deathRemaining <= 0) Destroy(gameObject);
                return;
            }
            flash = Mathf.Max(0, flash - dt); attackPose = Mathf.Max(0, attackPose - dt);
            if (IntroRemaining > 0)
            {
                IntroRemaining = Mathf.Max(0, IntroRemaining - dt);
                FacePlayer(dt); Present(); return;
            }
            FacePlayer(dt);
            for (int i = pulses.Count - 1; i >= 0; i--)
            {
                Pulse pulse = pulses[i]; pulse.Delay -= dt;
                if (pulse.Delay > 0) continue;
                EmitPulse(pulse); pulses.RemoveAt(i);
            }
            attackCooldown = Mathf.Max(0, attackCooldown - dt);
            if (windup > 0)
            {
                windup = Mathf.Max(0, windup - dt);
                if (windup <= 0) ExecuteAttack();
            }
            else if (attackCooldown <= 0)
            {
                windup = IsEnraged ? .42f : .65f;
                capturedAim = game.PlayerPosition + Vector3.up * 1.15f - AimPosition;
                capturedAim.y = 0;
                if (capturedAim.sqrMagnitude < .001f) capturedAim = transform.forward;
                capturedAim.Normalize();
            }
            else if (pulses.Count == 0 && attackPose <= 0)
            {
                Vector3 delta = game.PlayerPosition - transform.position; delta.y = 0;
                float distance = delta.magnitude;
                Vector3 travel = distance > 6 ? delta.normalized : distance < 3.1f ? -delta.normalized : Vector3.Cross(Vector3.up, delta.normalized) * Mathf.Sin(clock * .7f);
                body.Move(travel * (IsEnraged ? 1.55f : 1.05f) * dt);
            }
            verticalSpeed = body.isGrounded ? -2 : Mathf.Max(-20, verticalSpeed - 22 * dt);
            body.Move((knockVelocity + Vector3.up * verticalSpeed) * dt);
            knockVelocity = Vector3.MoveTowards(knockVelocity, Vector3.zero, dt * 12);
            Present();
        }

        void FacePlayer(float dt)
        {
            Vector3 direction = game.PlayerPosition - transform.position; direction.y = 0;
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-4 * dt));
        }

        void ExecuteAttack()
        {
            attackPose = .35f; attackCooldown = IsEnraged ? 1.5f : 2;
            int sequence = attack++;
            switch (Archetype)
            {
                case 0:
                    if ((sequence & 1) == 0)
                        for (int step = 0; step < (IsEnraged ? 14 : 11); step++) Queue(step * .115f, 0, step, capturedAim);
                    else
                        foreach (float offset in new[] { -20f, 20f })
                            game.FireBombHostile(ShotOrigin, Rotate(capturedAim, offset), 3.2f, 1, Accents[0], 1.1f);
                    break;
                case 1: case 3:
                    Fan(ShotOrigin, Rotate(capturedAim, sequence % 2 == 0 ? -15 : 15), Archetype == 3 ? 11 : 9, 125, 5.2f);
                    if ((sequence & 1) != 0)
                        for (int i = -1; i <= 1; i++) game.FireHostile(ShotOrigin, Rotate(capturedAim, i * 32), 2.6f, 1, Accents[Archetype]);
                    else Queue(.24f, 1, sequence, capturedAim);
                    break;
                case 2:
                    Cross(ShotOrigin, capturedAim, IsEnraged ? 5.3f : 4.1f);
                    Queue(.28f, 2, sequence, Rotate(capturedAim, sequence % 2 == 0 ? 40 : -40));
                    break;
                case 4:
                    RingWithGap(ShotOrigin, 24, sequence * 3, 4.7f);
                    Queue(.38f, 3, sequence * 3 + 1, capturedAim);
                    break;
                case 5:
                    Fan(ShotOrigin, capturedAim, IsEnraged ? 15 : 11, 180, 5.8f);
                    Queue(.3f, 4, sequence, capturedAim);
                    game.AddShake(.5f);
                    if (Vector3.Distance(game.PlayerPosition, transform.position) < 3)
                        game.TryDamagePlayer(2, capturedAim * 6);
                    break;
                case 6:
                    foreach (float offset in new[] { -30f, 0f, 30f })
                        game.FireSplittingHostile(ShotOrigin, Rotate(capturedAim, offset), 3.3f, 1, Accents[6], IsEnraged ? 3.2f : 4.5f);
                    if (IsEnraged) Queue(.4f, 1, sequence, capturedAim);
                    break;
            }
            game.PlayCue(FpsCue.HostileShot, AimPosition);
        }

        Vector3 ShotOrigin => transform.position + Vector3.up * 1.15f;
        void Queue(float delay, int kind, int step, Vector3 direction)
        {
            if (pulses.Count < 32) pulses.Add(new Pulse { Delay = delay, Kind = kind, Step = step, Direction = direction });
        }
        void EmitPulse(Pulse pulse)
        {
            if (IsDead) return;
            switch (pulse.Kind)
            {
                case 0:
                    Vector3 direction = Rotate(pulse.Direction, pulse.Step * (IsEnraged ? 24 : 19));
                    game.FireHostile(ShotOrigin, direction, 5, 1, Accents[Archetype]);
                    game.FireHostile(ShotOrigin, -direction, 5, 1, Accents[Archetype]); break;
                case 1: Fan(ShotOrigin, pulse.Direction, 5, 95, 4.5f); break;
                case 2: Cross(ShotOrigin, pulse.Direction, IsEnraged ? 5.3f : 4.1f); break;
                case 3: RingWithGap(ShotOrigin, 24, pulse.Step, 4.7f); break;
                case 4: Fan(ShotOrigin, Rotate(pulse.Direction, 8), 7, 160, 4.8f); break;
            }
        }
        void Fan(Vector3 origin, Vector3 direction, int count, float arc, float speed)
        {
            for (int i = 0; i < count; i++)
                game.FireHostile(origin, Rotate(direction, -arc * .5f + arc * i / Mathf.Max(1, count - 1)), speed, 1, Accents[Archetype]);
        }
        void Cross(Vector3 origin, Vector3 direction, float speed)
        {
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            game.FireHostile(origin, direction, speed, 1, Accents[Archetype]);
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                game.FireHostile(origin + side * (i * .42f), direction, speed, 1, Accents[Archetype]);
                game.FireHostile(origin + direction * (i * .42f), direction, speed, 1, Accents[Archetype]);
            }
        }
        void RingWithGap(Vector3 origin, int count, int gap, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                int relative = (i - gap % count + count) % count;
                if (relative <= 2) continue;
                game.FireHostile(origin, Rotate(Vector3.forward, i * 360f / count), speed, 1, Accents[Archetype]);
            }
        }
        static Vector3 Rotate(Vector3 direction, float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad, c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return new Vector3(direction.x * c + direction.z * s, 0, direction.z * c - direction.x * s).normalized;
        }

        public void TakeDamage(int amount, Vector3 point, Vector3 force)
        {
            if (!initialized || IsDead || IntroRemaining > 0 || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount); flash = .065f;
            force.y = 0; knockVelocity = Vector3.ClampMagnitude(knockVelocity + force * .18f, 1.5f);
            if (Health > 0) return;
            IsDead = true; deathRemaining = .9f; windup = 0; pulses.Clear();
            body.enabled = false; game.AddShake(.85f); game.PlayCue(FpsCue.Explosion, AimPosition);
            game.OnBossKilled(this);
        }

        void Present()
        {
            float tell = IntroRemaining > 0 ? .45f + Mathf.Sin(clock * 5) * .12f : windup > 0 ? 1 - windup / (IsEnraged ? .42f : .65f) : 0;
            float hit = flash > 0 ? .12f : 0;
            model.localScale = new Vector3(1 + hit + Mathf.Sin(clock * 2.2f) * .018f, 1 - hit + Mathf.Cos(clock * 2.2f) * .025f, 1 + hit);
            model.localPosition = Vector3.up * (Archetype == 2 || Archetype == 4 || Archetype == 6 ? Mathf.Sin(clock * 2) * .06f : 0);
            if (head) head.localRotation = Quaternion.Euler(-tell * 12, Mathf.Sin(clock) * 7, 0);
            if (leftArm) leftArm.localRotation = Quaternion.Euler(-tell * 55, 0, -tell * 20);
            if (rightArm) rightArm.localRotation = Quaternion.Euler(attackPose > 0 ? -70 + attackPose * 300 : -tell * 70, 0, tell * 18);
            if (core) core.localRotation = Quaternion.Euler(0, clock * (IsEnraged ? 95 : 45), 0);
            aura.intensity = (IsEnraged ? 2.8f : 1.2f) + tell * 2.4f;
            flashProperties.Clear(); flashProperties.SetFloat("_HitFlash", flash > 0 ? 1 : 0);
            foreach (var surface in surfaces) if (surface) surface.SetPropertyBlock(flashProperties);
        }

        GameObject Part(string label, Vector3 position, Vector3 size, Material material, Transform parent = null)
            => game.Box(label, position, size, material, parent ? parent : model, false);
        Transform Pivot(string label, Vector3 position, Transform parent = null)
        {
            var obj = new GameObject(label); obj.transform.SetParent(parent ? parent : model, false); obj.transform.localPosition = position; return obj.transform;
        }
        void Eyes(Transform parent, float y, float z, Material glow, Material dark, float spread = .22f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Orbita vazia", new Vector3(side * spread,y,z), new Vector3(.25f,.22f,.05f), dark,parent);
                Part("Pupila amaldiçoada", new Vector3(side * spread,y-.015f,z+.035f), new Vector3(.1f,.095f,.035f), glow,parent);
            }
        }
        void Limb(Vector3 from, Vector3 to, float width, Material material, string label)
        {
            Vector3 delta = to - from;
            var obj = Part(label, (from + to) * .5f, new Vector3(width,width,delta.magnitude), material);
            obj.transform.localRotation = Quaternion.LookRotation(delta);
        }
        void BuildModel()
        {
            var dark = game.PixelMaterial("boss-void",new Color(.055f,.04f,.07f));
            var iron = game.PixelMaterial("boss-iron",new Color(.24f,.29f,.34f));
            var bone = game.PixelMaterial("boss-old-bone",new Color(.78f,.74f,.58f));
            var glow = game.PixelMaterial("boss-glow-"+Archetype, Accents[Archetype]); glow.SetColor("_EmissionColor",Accents[Archetype]*1.2f);
            var flesh = game.PixelMaterial("boss-flesh-"+Archetype, Archetype==0?new Color(.72f,.23f,.055f):Archetype==6?new Color(.17f,.35f,.22f):Archetype==3?new Color(.34f,.36f,.12f):new Color(.25f,.13f,.28f));
            var cloth = game.PixelMaterial("boss-cloth-"+Archetype, Archetype==5?new Color(.32f,.07f,.07f):new Color(.2f,.12f,.3f));
            leftArm=Pivot("Braço esquerdo",new Vector3(-.68f,1.65f,0)); rightArm=Pivot("Braço direito",new Vector3(.68f,1.65f,0));
            head=Pivot("Cabeça",new Vector3(0,2.15f,.04f));
            switch(Archetype)
            {
                case 0:
                    Part("Capa do rei",new Vector3(0,1.3f,-.33f),new Vector3(1.65f,1.9f,.32f),cloth);
                    Part("Tronco de raízes",new Vector3(0,1.25f,0),new Vector3(1.15f,1.35f,.8f),iron);
                    for(int rib=-2;rib<=2;rib++)Part("Gomo da coroa",new Vector3(rib*.23f,.15f,0),new Vector3(.32f,1.04f-Mathf.Abs(rib)*.11f,.96f-Mathf.Abs(rib)*.12f),flesh,head);
                    Eyes(head,.23f,.51f,glow,dark,.27f);
                    Part("Sorriso serrado",new Vector3(0,-.15f,.52f),new Vector3(.72f,.18f,.05f),dark,head);
                    for(int tooth=-2;tooth<=2;tooth++)Part("Dente",new Vector3(tooth*.13f,-.13f,.555f),new Vector3(.07f,.12f,.04f),bone,head);
                    Part("Caule retorcido",new Vector3(.05f,.83f,0),new Vector3(.18f,.45f,.2f),flesh,head);
                    Part("Foice / cabo",new Vector3(.15f,-.3f,.18f),new Vector3(.11f,1.9f,.11f),bone,rightArm);
                    Part("Foice / lâmina",new Vector3(-.35f,.56f,.18f),new Vector3(.9f,.25f,.12f),iron,rightArm); break;
                case 1: case 3:
                    Part(Archetype==1?"Abdômen da viúva":"Ventre da peste",new Vector3(0,.96f,-.45f),new Vector3(1.65f,.98f,1.75f),flesh);
                    Part("Peitoral",new Vector3(0,1.55f,.18f),new Vector3(.94f,1.02f,.75f),cloth);
                    for(int side=-1;side<=1;side+=2)for(int leg=0;leg<4;leg++)
                    {
                        Vector3 hip=new Vector3(side*.55f,.95f,.52f-leg*.44f),knee=new Vector3(side*(1.15f+leg*.09f),.62f,.9f-leg*.62f),foot=new Vector3(side*(1.65f+leg*.04f),.08f,1.05f-leg*.7f);
                        Limb(hip,knee,.14f,iron,"Perna segmentada");Limb(knee,foot,.1f,bone,"Ponta da perna");
                    }
                    Part("Máscara",Vector3.zero,new Vector3(.78f,.7f,.59f),bone,head);Eyes(head,.08f,.32f,glow,dark);
                    if(Archetype==1)Part("Véu funerário",new Vector3(0,-.18f,.37f),new Vector3(.86f,.43f,.05f),cloth,head);
                    else for(int i=-1;i<=1;i++)Part("Saco de peste",new Vector3(i*.5f,1.32f,-.55f),new Vector3(.42f,.55f,.44f),glow);
                    break;
                case 2:
                    for(int tier=0;tier<3;tier++)Part("Batina de cinzas",new Vector3(0,.45f+tier*.58f,0),new Vector3(1.5f-tier*.22f,.65f,.95f-tier*.12f),cloth);
                    Part("Máscara episcopal",Vector3.zero,new Vector3(.68f,.66f,.58f),bone,head);Eyes(head,.04f,.32f,glow,dark);
                    Part("Mitra",new Vector3(0,.58f,0),new Vector3(.73f,.68f,.55f),iron,head);
                    Part("Cruz / vertical",new Vector3(0,.58f,.3f),new Vector3(.1f,.45f,.04f),glow,head);
                    Part("Cruz / horizontal",new Vector3(0,.67f,.3f),new Vector3(.4f,.1f,.04f),glow,head);
                    Part("Cajado",new Vector3(.15f,-.25f,.18f),new Vector3(.09f,2.4f,.09f),bone,rightArm);
                    Part("Cruz do cajado",new Vector3(.15f,.82f,.18f),new Vector3(.65f,.1f,.12f),glow,rightArm);break;
                case 4:
                    Part("Coroa do sino",new Vector3(0,2.32f,0),new Vector3(.9f,.55f,.9f),iron);
                    for(int side=-1;side<=1;side+=2)for(int z=-1;z<=1;z+=2)
                    {var rib=Part("Costela do sino",new Vector3(side*.63f,1.25f,z*.55f),new Vector3(.2f,1.65f,.2f),iron);rib.transform.localRotation=Quaternion.Euler(z*8,0,-side*9);}
                    Part("Borda do sino",new Vector3(0,.5f,0),new Vector3(1.8f,.26f,1.65f),iron);
                    core=Pivot("Badalo",new Vector3(0,1.3f,0));Part("Badalo espectral",Vector3.zero,new Vector3(.36f,1.15f,.36f),glow,core);
                    Part("Face oca",new Vector3(0,.35f,.4f),new Vector3(.72f,.7f,.25f),dark,head);Eyes(head,.4f,.55f,glow,dark);break;
                case 5:
                    Part("Couraça",new Vector3(0,1.35f,0),new Vector3(1.4f,1.4f,.9f),iron);
                    for(int side=-1;side<=1;side+=2)
                    {Part("Greva",new Vector3(side*.4f,.37f,0),new Vector3(.47f,.74f,.6f),iron);Part("Ombreira",new Vector3(side*.82f,1.98f,0),new Vector3(.64f,.54f,.95f),iron);}
                    Part("Elmo fechado",Vector3.zero,new Vector3(.83f,.85f,.73f),iron,head);
                    Part("Viseira",new Vector3(0,.03f,.39f),new Vector3(.68f,.15f,.04f),dark,head);
                    for(int eye=-1;eye<=1;eye+=2)Part("Olho de brasa",new Vector3(eye*.2f,.03f,.42f),new Vector3(.17f,.07f,.035f),glow,head);
                    Part("Machado / cabo",new Vector3(.15f,-.36f,.24f),new Vector3(.13f,2.1f,.13f),bone,rightArm);
                    Part("Machado / lâmina",new Vector3(.1f,.58f,.24f),new Vector3(1.35f,.76f,.2f),iron,rightArm);
                    Part("Machado / núcleo",new Vector3(.12f,.58f,.36f),new Vector3(.15f,.5f,.04f),glow,rightArm);break;
                case 6:
                    Part("Manto pantanoso",new Vector3(0,1.08f,0),new Vector3(1.5f,1.8f,1.1f),cloth);
                    Part("Capuz",new Vector3(0,.14f,-.05f),new Vector3(1.3f,.97f,.85f),flesh,head);
                    Part("Face anfíbia",new Vector3(0,0,.25f),new Vector3(.94f,.52f,.55f),flesh,head);Eyes(head,.22f,.55f,glow,dark,.34f);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var horn=Part("Galho ritual",new Vector3(side*.55f,.78f,-.04f),new Vector3(.12f,1,.12f),bone,head);horn.transform.localRotation=Quaternion.Euler(0,0,-side*25);
                        Part("Mão do oráculo",new Vector3(side*.8f,1.24f,.3f),new Vector3(.4f,.3f,.4f),flesh);
                    }
                    core=Pivot("Orbe do brejo",new Vector3(.95f,1.7f,.3f));Part("Cristal do brejo",Vector3.zero,new Vector3(.38f,.58f,.38f),glow,core);break;
            }
            if(Archetype!=1&&Archetype!=3&&Archetype!=4)
            {
                Part("Braço esquerdo",new Vector3(0,-.3f,0),new Vector3(.29f,.7f,.33f),flesh,leftArm);
                Part("Braço direito",new Vector3(0,-.3f,0),new Vector3(.29f,.7f,.33f),flesh,rightArm);
            }
        }
    }
}

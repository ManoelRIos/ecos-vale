using UnityEngine;

namespace Ecos
{
    /// <summary>Lightweight local wildlife steering. It never grants loot or progression itself.</summary>
    public sealed class CreatureActor : MonoBehaviour
    {
        enum Behaviour { Rest, Wander, Flee, Curious, Alert }
        GameBootstrap game;
        WildlifeSettings settings;
        CreatureVisual visuals;
        CharacterController observerController;
        Transform observer;
        Vector3 home, destination, previousObserver;
        Behaviour behaviour;
        string species;
        int salt;
        float clock, orbitPhase, nextDecision, nextGroundCheck, cachedGround, speed, observerSpeed;
        bool flying;
        public string SpeciesId => species;

        public void Initialize(GameBootstrap context, WildlifeSettings configuration, Palette palette,
            string speciesId, Vector3 habitat, int randomSalt)
        {
            game = context; settings = configuration; species = speciesId; home = habitat; salt = randomSalt;
            observer = game.Player.transform; observerController = game.Player.GetComponent<CharacterController>();
            previousObserver = observer.position;
            flying = CreatureCatalog.IsNocturnal(species);
            cachedGround = home.y;
            clock = WorldMath.Random(salt, 0, game.State.seed + 17) * 20;
            orbitPhase = WorldMath.Random(salt, 3, game.State.seed) * Mathf.PI * 2;
            visuals = new CreatureVisual(palette, transform, species, orbitPhase);
            destination = home;
            transform.position = home + Vector3.up * (flying ? 1.45f : .03f);
            transform.rotation = Quaternion.Euler(0, WorldMath.Random(salt, 7, game.State.seed) * 360, 0);
            ChooseWander();
        }

        void Update()
        {
            if (game == null || !observer || visuals == null) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt <= 0) return;
            observerSpeed = Vector3.Distance(observer.position, previousObserver) / Mathf.Max(dt, .001f);
            previousObserver = observer.position;
            if (game.Blocked || !game.Playing) return;
            clock += dt;
            float distance = Horizontal(observer.position - transform.position).magnitude;
            bool crouching = observerController && observerController.height < 1.65f;
            float alarmDistance = crouching ? 2.1f : observerSpeed > 5.2f ? 11 : 7;
            bool shy = species == "hare" || species == "deer";
            if (shy && distance < alarmDistance) behaviour = Behaviour.Flee;
            else if (species == "boar" && distance < 4.8f && observerSpeed > 5.2f) behaviour = Behaviour.Flee;
            else if (clock >= nextDecision)
            {
                nextDecision = clock + .7f + WorldMath.Random(salt, (int)(clock * 2), game.State.seed) * .8f;
                if ((species == "fox" || species == "wisp") && distance < (species == "wisp" && game.State.lensCrafted ? 18 : 9))
                    behaviour = distance < 1.9f ? Behaviour.Flee : Behaviour.Curious;
                else if (species == "boar" && distance < 7) behaviour = Behaviour.Alert;
                else if (behaviour == Behaviour.Flee || behaviour == Behaviour.Alert || behaviour == Behaviour.Curious
                    || Horizontal(destination - transform.position).sqrMagnitude < 1)
                    ChooseWander();
                else if (behaviour == Behaviour.Rest) ChooseWander();
            }

            Vector3 direction = Vector3.zero;
            float desiredSpeed = 0;
            if (behaviour == Behaviour.Flee)
            {
                direction = Horizontal(transform.position - observer.position).normalized;
                if (direction.sqrMagnitude < .1f) direction = transform.forward;
                desiredSpeed = species == "deer" ? 4.8f : species == "hare" ? 3.7f : 3.1f;
                if (distance > alarmDistance + 4 && clock >= nextDecision) ChooseWander();
            }
            else if (behaviour == Behaviour.Curious)
            {
                Vector3 offset = Horizontal(observer.position - transform.position);
                direction = offset.normalized;
                desiredSpeed = distance > 3.1f ? (species == "wisp" ? 1.4f : 1.1f) : 0;
            }
            else if (behaviour == Behaviour.Wander)
            {
                if (flying)
                {
                    float a = clock * (species == "bat" ? .65f : .24f) + orbitPhase;
                    destination = home + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (species == "bat" ? 5 : 3);
                }
                direction = Horizontal(destination - transform.position).normalized;
                desiredSpeed = species == "bat" ? 2.3f : species == "wisp" ? .75f : species == "hare" ? 1.1f : .7f;
                if (!flying && Horizontal(destination - transform.position).sqrMagnitude < .5f)
                { behaviour = Behaviour.Rest; nextDecision = clock + 1.5f; desiredSpeed = 0; }
            }
            if (behaviour == Behaviour.Alert)
                direction = Horizontal(observer.position - transform.position).normalized;

            speed = Mathf.MoveTowards(speed, desiredSpeed, dt * 6);
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), dt * 6);
            Vector3 next = transform.position + direction * speed * dt;
            float boundary = game.Settings.worldSize * .5f - 5;
            bool acceptable = Mathf.Abs(next.x) < boundary && Mathf.Abs(next.z) < boundary
                && game.Chunks.HasGround(next) && !Landmarks.Near(next.x, next.z, 2.4f);
            // Structure colliders steer wildlife aside; terrain suitability is checked separately.
            if (acceptable && speed > .1f && !flying)
                acceptable = !Physics.SphereCast(transform.position + Vector3.up * .65f, .18f,
                    direction, out _, Mathf.Max(.35f, speed * dt), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (acceptable)
            {
                if (clock >= nextGroundCheck)
                {
                    nextGroundCheck = clock + .14f;
                    float height = WorldMath.Height(next.x, next.z, game.State.seed);
                    if (!flying && Mathf.Abs(height - cachedGround) > 1.8f)
                    {
                        acceptable = false;
                        ChooseWander();
                    }
                    else
                    {
                        cachedGround = height;
                        if (Physics.Raycast(new Vector3(next.x, height + 4, next.z), Vector3.down, out var ground, 9,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && ground.collider is MeshCollider
                            && ground.normal.y > .55f && Mathf.Abs(ground.point.y - height) < 1.5f)
                            cachedGround = ground.point.y;
                    }
                }
                if (acceptable)
                {
                    float targetY = cachedGround + (flying ? 1.45f + Mathf.Sin(clock * 1.3f) * .28f : .035f);
                    next.y = Mathf.Lerp(transform.position.y, targetY, Mathf.Min(1, dt * 16));
                    transform.position = next;
                }
            }
            if (!acceptable)
            {
                speed = 0;
                if (clock >= nextDecision - .2f) ChooseWander();
            }
            // Keep encounters local; a chased animal eventually returns to its habitat.
            if (Horizontal(transform.position - home).sqrMagnitude > settings.wanderRadius * settings.wanderRadius * 6)
            {
                destination = home;
                if (distance > 4) behaviour = Behaviour.Wander;
            }
            visuals.Tick(dt, speed, behaviour == Behaviour.Alert || behaviour == Behaviour.Flee);
        }

        static Vector3 Horizontal(Vector3 vector) { vector.y = 0; return vector; }

        void ChooseWander()
        {
            int step = (int)(clock * 5);
            nextDecision = clock + .65f + WorldMath.Random(salt, step, game.State.seed + 53) * .65f;
            float angle = WorldMath.Random(salt, step, game.State.seed + 79) * Mathf.PI * 2;
            float distance = 2 + WorldMath.Random(step, salt, game.State.seed + 113) * settings.wanderRadius;
            destination = home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            behaviour = Behaviour.Wander;
        }
    }
}

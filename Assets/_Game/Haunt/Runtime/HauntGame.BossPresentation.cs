using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        sealed class BossRemnant
        {
            public SpriteRenderer Sprite;
            public string Art;
            public float Remaining = .9f;
        }
        readonly List<BossRemnant> bossRemnants = new List<BossRemnant>();
        readonly List<SpriteRenderer> heroOrbitals = new List<SpriteRenderer>();
        float heroSmokeClock;

        void PlayBossDeathPresentation(Enemy boss)
        {
            if (bossRemnants.Count >= 4) return;
            string id = EnemySprite(4);
            var sprite = MakeSprite(id, boss.Sprite.transform.position, actorsRoot.transform, Depth(boss.Position.y) + 2);
            sprite.sprite = art.BossFrame(id, BossAnimationState.Death, 0);
            sprite.transform.localScale = boss.Sprite.transform.localScale;
            sprite.flipX = boss.Sprite.flipX;
            bossRemnants.Add(new BossRemnant { Sprite = sprite, Art = id });
            RingEffect(boss.Position, BossAccent, 3);
            Burst(boss.Position + Vector2.up * .8f, BossAccent, 24);
        }

        void UpdateBossPresentation(float dt)
        {
            for (int i = bossRemnants.Count - 1; i >= 0; i--)
            {
                var remnant = bossRemnants[i]; remnant.Remaining -= dt;
                remnant.Sprite.sprite = art.BossFrame(remnant.Art, BossAnimationState.Death,
                    Mathf.Clamp(Mathf.FloorToInt((.9f - remnant.Remaining) * 5), 0, 3));
                remnant.Sprite.color = new Color(1,1,1,Mathf.Clamp01(remnant.Remaining / .2f));
                if (remnant.Remaining > 0) continue;
                Destroy(remnant.Sprite.gameObject); bossRemnants.RemoveAt(i);
            }
            if (!Playing || State == null || State.IsDead || !player) return;
            if (State.Character == CharacterKind.Medium)
            {
                if (heroOrbitals.Count == 0)
                    for (int i = 0; i < 2; i++)
                    {
                        var orb = MakeSprite("shot-veil", playerPosition, actorsRoot.transform, 1090);
                        orb.transform.localScale = Vector3.one * .28f;
                        heroOrbitals.Add(orb);
                    }
                for (int i = 0; i < heroOrbitals.Count; i++)
                {
                    float angle = simulationTime * 100 + i * 180;
                    Vector2 offset = Rotate(Vector2.right, angle);
                    offset = new Vector2(offset.x * .6f, offset.y * .32f);
                    heroOrbitals[i].transform.position = Snap(playerPosition + Vector2.up * .6f + offset);
                    heroOrbitals[i].sortingOrder = Depth(playerPosition.y) + (offset.y > 0 ? -2 : 3);
                    heroOrbitals[i].color = new Color(.9f,.6f,1,.85f);
                }
            }
            heroSmokeClock -= dt;
            if (State.Character == CharacterKind.Alchemist && heroSmokeClock <= 0)
            {
                heroSmokeClock = .17f;
                Vector2 side = new Vector2(Mathf.Sin(simulationTime * 4) > 0 ? -.5f : .5f, .5f);
                Emit("dust", playerPosition + side, new Vector2(side.x * .2f,.5f), new Color(.6f,.85f,.7f,.35f), .65f,.28f,-.25f);
            }
        }

        void EmitReaperSlash(Vector2 aim)
        {
            if (State == null || State.Character != CharacterKind.Reaper) return;
            int before = sparks.Count;
            Emit("slash", playerPosition + Vector2.up * .4f + aim * .6f, aim * .3f,
                new Color(.8f,.95f,1,.7f), .18f, 1.2f, 0);
            if (sparks.Count > before)
                sparks[sparks.Count - 1].Sprite.transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg);
        }

        void ClearBossPresentation()
        {
            foreach (var remnant in bossRemnants) if (remnant.Sprite) Destroy(remnant.Sprite.gameObject);
            bossRemnants.Clear();
            foreach (var sprite in heroOrbitals) if (sprite) Destroy(sprite.gameObject);
            heroOrbitals.Clear(); heroSmokeClock = 0;
        }
    }
}

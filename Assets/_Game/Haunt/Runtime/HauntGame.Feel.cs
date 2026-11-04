using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        [SerializeField, Range(.15f, .25f)] float cameraAimWeight = .2f;
        [SerializeField] float cameraAimLimit = 2f;
        float cameraTrauma;
        Vector2 cameraKick;
        MaterialPropertyBlock hitProperties;
        readonly Dictionary<Enemy, HitFeedback> hitFeedback = new Dictionary<Enemy, HitFeedback>();
        readonly List<Enemy> finishedHits = new List<Enemy>();
        readonly List<Afterimage> afterimages = new List<Afterimage>();
        readonly Stack<SpriteRenderer> afterimagePool = new Stack<SpriteRenderer>();
        readonly List<Casing> casings = new List<Casing>();
        Texture2D casingTexture;
        Sprite casingSprite;
        sealed class HitFeedback { public float Flash, Stretch; }
        sealed class Afterimage { public SpriteRenderer Sprite; public float Life; public Color Color; }
        sealed class Casing
        {
            public SpriteRenderer Sprite;
            public Vector2 Position, Velocity;
            public float Height, UpSpeed, Spin;
            public bool Grounded;
        }

        void AddCameraTrauma(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;
            cameraTrauma = Mathf.Clamp01(cameraTrauma + amount);
        }

        void ResetGameFeel()
        {
            ClearRoomFeel(); cameraTrauma = 0; cameraKick = Vector2.zero; cameraPosition = Vector2.zero;
            if (view) view.transform.position = new Vector3(0, .3f, -20);
        }

        void UpdateDynamicCamera(float dt, bool snap)
        {
            if (!view) return;
            if (snap) { cameraTrauma = 0; cameraKick = Vector2.zero; }
            view.orthographicSize = CameraSize;
            Vector2 lead = !Playing || InShop ? Vector2.zero : Vector2.ClampMagnitude(
                (ControlAimPoint() - (playerPosition + Vector2.up * .35f)) * Mathf.Clamp(cameraAimWeight, .15f, .25f),
                Mathf.Max(0, cameraAimLimit));
            Vector2 focus = playerPosition + Vector2.up * .45f + lead;
            float horizontal = Mathf.Max(0, HalfWidth + 1.5f - CameraSize * Mathf.Max(.5f, view.aspect));
            float vertical = Mathf.Max(0, HalfHeight + 1.5f - CameraSize);
            focus.x = Mathf.Clamp(focus.x, -horizontal, horizontal);
            focus.y = Mathf.Clamp(focus.y, -vertical, vertical);
            cameraPosition = snap ? focus : Vector2.Lerp(cameraPosition, focus, 1 - Mathf.Exp(-10 * dt));
            float amplitude = cameraTrauma * cameraTrauma * .38f;
            Vector2 noise = new Vector2(Mathf.PerlinNoise(13, simulationTime * 31) * 2 - 1,
                Mathf.PerlinNoise(71, simulationTime * 31) * 2 - 1) * amplitude;
            Vector2 final = cameraPosition + noise + cameraKick;
            final.x = Mathf.Clamp(final.x, -horizontal, horizontal);
            final.y = Mathf.Clamp(final.y, -vertical, vertical);
            view.transform.position = new Vector3(Mathf.Round(final.x * 32) / 32f, Mathf.Round(final.y * 32) / 32f, -20);
        }

        void OnPlayerWeaponFired(Vector2 aim, bool heavy, bool beam)
        {
            cameraKick = Vector2.ClampMagnitude(cameraKick - aim.normalized * (beam ? .035f : heavy ? .18f : .085f), .35f);
            AddCameraTrauma(heavy ? .12f : .025f);
            Vector2 tip = playerPosition + Vector2.up * .35f + aim * 1.05f;
            Emit("muzzle", tip, Vector2.zero, new Color(1, .85f, .5f, .85f), .05f, heavy ? .9f : .6f, 0);
            Emit("shot-halo", tip, Vector2.zero, new Color(1, .7f, .35f, .4f), .055f, heavy ? 1.3f : .9f, 0);
            if (!beam) EjectCasing(aim);
        }

        void SetEnemyFlash(Enemy enemy, float amount)
        {
            if (!enemy.Sprite) return;
            if (hitProperties == null) hitProperties = new MaterialPropertyBlock();
            enemy.Sprite.GetPropertyBlock(hitProperties);
            hitProperties.SetFloat("_HitFlash", amount);
            enemy.Sprite.SetPropertyBlock(hitProperties);
        }

        void ApplyEnemyHitFeedback(Enemy enemy, Vector2 direction, int damage, bool heavy, bool applyKnockback = true)
        {
            enemy.HurtFlash = .06f;
            if (!hitFeedback.TryGetValue(enemy, out var feedback))
            { feedback = new HitFeedback(); hitFeedback.Add(enemy, feedback); }
            feedback.Flash = .06f; feedback.Stretch = .12f;
            SetEnemyFlash(enemy, 1);
            if (applyKnockback && direction.sqrMagnitude > .001f)
            {
                float distance = enemy.Kind == 4 ? .045f : enemy.Kind == 11 ? .08f : heavy ? .18f : .1f;
                enemy.Position = MoveEnemy(enemy, direction.normalized * distance, EnemyHitRadius(enemy));
            }
        }

        void PresentEnemyHitFeedback(Enemy enemy)
        {
            if (!hitFeedback.TryGetValue(enemy, out var feedback)) return;
            SetEnemyFlash(enemy, feedback.Flash > 0 ? 1 : 0);
            float stretch = Mathf.Sin((1 - feedback.Stretch / .12f) * Mathf.PI) * .14f;
            Vector3 scale = enemy.Sprite.transform.localScale;
            enemy.Sprite.transform.localScale = new Vector3(scale.x * (1 + stretch), scale.y * (1 - stretch), scale.z);
        }

        void EmitRollAfterimage()
        {
            if (!player || afterimages.Count >= 24) return;
            var sprite = afterimagePool.Count > 0 ? afterimagePool.Pop() : MakeSprite(PlayerArt, playerPosition, actorsRoot.transform, 10);
            sprite.gameObject.SetActive(true); sprite.sprite = player.sprite;
            sprite.sharedMaterial = art.Material; sprite.flipX = player.flipX; sprite.flipY = player.flipY;
            sprite.transform.position = player.transform.position;
            sprite.transform.rotation = player.transform.rotation; sprite.transform.localScale = player.transform.localScale;
            sprite.sortingOrder = player.sortingOrder - 2;
            Color tint = Color.Lerp(CharacterAccent, new Color(.45f, 1, .94f), .65f); tint.a = .42f;
            sprite.color = tint; afterimages.Add(new Afterimage { Sprite = sprite, Life = .2f, Color = tint });
        }

        void EjectCasing(Vector2 aim)
        {
            if (!roomRoot) return;
            if (!casingSprite)
            {
                casingTexture = new Texture2D(2, 3) { name = "Haunt / shell casing", filterMode = FilterMode.Point };
                casingTexture.SetPixels32(new[] { new Color32(151, 96, 42, 255), new Color32(212, 159, 65, 255),
                    new Color32(255, 220, 131, 255), new Color32(230, 177, 85, 255),
                    new Color32(172, 113, 48, 255), new Color32(212, 159, 65, 255) });
                casingTexture.Apply(false, true);
                casingSprite = Sprite.Create(casingTexture, new Rect(0, 0, 2, 3), new Vector2(.5f, .5f), 16);
            }
            Casing casing;
            if (casings.Count >= 96) { casing = casings[0]; casings.RemoveAt(0); }
            else casing = new Casing { Sprite = MakeSprite("impact-dust", playerPosition, roomRoot.transform, 12) };
            casing.Position = playerPosition + aim * .3f;
            casing.Velocity = new Vector2(-aim.y, aim.x) * (1.2f + .2f * (casings.Count % 4)) - aim * .5f;
            casing.Height = .4f; casing.UpSpeed = 1.7f; casing.Grounded = false; casing.Spin = casings.Count % 2 == 0 ? 360 : -360;
            casing.Sprite.sprite = casingSprite; casing.Sprite.color = new Color(1, 1, 1, .8f);
            casing.Sprite.transform.localScale = Vector3.one * .9f;
            casing.Sprite.sortingOrder = 12;
            casing.Sprite.transform.position = Snap(casing.Position + Vector2.up * casing.Height);
            casings.Add(casing);
        }

        void UpdateFeel(float dt)
        {
            cameraTrauma = Mathf.Max(0, cameraTrauma - dt * 1.8f);
            cameraKick = Vector2.Lerp(cameraKick, Vector2.zero, 1 - Mathf.Exp(-23 * dt));
            finishedHits.Clear();
            foreach (var pair in hitFeedback)
            {
                var feedback = pair.Value;
                feedback.Flash = Mathf.Max(0, feedback.Flash - dt);
                feedback.Stretch = Mathf.Max(0, feedback.Stretch - dt);
                if (!pair.Key.Sprite || feedback.Stretch <= 0) { SetEnemyFlash(pair.Key, 0); finishedHits.Add(pair.Key); }
            }
            foreach (var enemy in finishedHits) hitFeedback.Remove(enemy);
            for (int i = afterimages.Count - 1; i >= 0; i--)
            {
                var image = afterimages[i]; image.Life -= dt;
                if (image.Life <= 0) { image.Sprite.gameObject.SetActive(false); afterimagePool.Push(image.Sprite); afterimages.RemoveAt(i); continue; }
                image.Sprite.color = new Color(image.Color.r, image.Color.g, image.Color.b, image.Color.a * image.Life / .2f);
            }
            foreach (var casing in casings)
            {
                if (casing.Grounded) continue;
                casing.Position += casing.Velocity * dt;
                casing.UpSpeed -= dt * 9; casing.Height += casing.UpSpeed * dt;
                if (casing.Height <= 0) { casing.Height = 0; casing.Grounded = true; }
                casing.Sprite.transform.position = Snap(casing.Position + Vector2.up * casing.Height);
                casing.Sprite.transform.Rotate(0, 0, casing.Spin * dt);
            }
        }

        void ClearRoomFeel()
        {
            foreach (var pair in hitFeedback) SetEnemyFlash(pair.Key, 0);
            hitFeedback.Clear(); finishedHits.Clear();
            foreach (var image in afterimages) if (image.Sprite) Destroy(image.Sprite.gameObject);
            afterimages.Clear();
            foreach (var sprite in afterimagePool) if (sprite) Destroy(sprite.gameObject);
            afterimagePool.Clear();
            foreach (var casing in casings) if (casing.Sprite) Destroy(casing.Sprite.gameObject);
            casings.Clear();
        }

        void DisposeGameFeel()
        {
            ClearRoomFeel();
            if (casingSprite) Destroy(casingSprite);
            if (casingTexture) Destroy(casingTexture);
        }
    }
}

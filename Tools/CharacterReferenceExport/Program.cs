using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Ecos.Haunt;
using UnityEngine;

// Extracts production-authored sprite pixels only. It neither renders Unity
// nor invents turnarounds for actors that have no directional source sheet.
static class Program
{
    sealed class Pose
    {
        public string label { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public float ppu { get; set; }
        public string rgbaBase64topLeft { get; set; }
    }
    sealed class Record
    {
        public string id { get; set; }
        public List<Pose> poses { get; set; } = new List<Pose>();
    }

    static readonly string[] Mobs =
    {
        "skeleton", "lantern-wisp", "pumpkin", "ghost", "cultist", "bat", "larva", "sentinel", "acolyte",
        "slime", "spider", "mushroom-brute", "clockwork-imp", "scarecrow", "gargoyle", "stalker-shadow", "fire-bat"
    };
    static readonly string[] Bosses = { "boss", "widow", "bishop", "matriarch", "bell", "iron-executioner", "bog-oracle" };
    static readonly string[] Weapons = { "hunter-carbine", "veil-wand", "bone-caster", "arc-coil" };

    static Pose Pixels(Sprite sprite, string label)
    {
        if (!sprite || !sprite.texture) throw new InvalidOperationException("A sprite source is missing: " + label);
        int width = (int)sprite.rect.width, height = (int)sprite.rect.height;
        int left = (int)sprite.rect.x, bottom = (int)sprite.rect.y;
        if (width <= 0 || height <= 0 || left < 0 || bottom < 0
            || left + width > sprite.texture.width || bottom + height > sprite.texture.height)
            throw new InvalidOperationException("Sprite bounds leave the source texture: " + sprite.name);
        if (sprite.texture.filterMode != FilterMode.Point)
            throw new InvalidOperationException("Expected native pixel filtering: " + sprite.name);
        Color32[] source = sprite.texture.GetPixels32();
        byte[] rgba = new byte[width * height * 4];
        bool visible = false;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            Color32 color = source[(bottom + height - 1 - y) * sprite.texture.width + left + x];
            int target = (y * width + x) * 4;
            rgba[target] = color.r; rgba[target + 1] = color.g;
            rgba[target + 2] = color.b; rgba[target + 3] = color.a;
            visible |= color.a > 0;
        }
        if (!visible) throw new InvalidOperationException("An exported pose is empty: " + sprite.name);
        return new Pose { label = label, width = width, height = height,
            ppu = sprite.pixelsPerUnit, rgbaBase64topLeft = Convert.ToBase64String(rgba) };
    }

    public static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass exactly one output exchange JSON path.");
        var records = new List<Record>();
        using (var art = new HauntPixelArt(false))
        {
            foreach (string id in Mobs)
            {
                var record = new Record { id = id };
                if (HauntPixelArt.HasActorSheet(id))
                {
                    record.poses.Add(Pixels(art.ActorFrame(id, ActorAnimationState.Idle, 0, ActorFacing.Right), "REPOUSO"));
                    record.poses.Add(Pixels(art.ActorFrame(id, ActorAnimationState.Walk, 1, ActorFacing.Right), "MOVIMENTO"));
                    record.poses.Add(Pixels(art.ActorFrame(id, ActorAnimationState.Attack, 2, ActorFacing.Right), "ATAQUE"));
                }
                else
                    for (int frame = 0; frame < 3; frame++)
                        record.poses.Add(Pixels(art.Frame(id, frame), "POSE " + (frame + 1)));
                records.Add(record);
            }
            foreach (string id in Bosses)
                records.Add(new Record { id = id, poses = new List<Pose>
                {
                    Pixels(art.BossFrame(id, BossAnimationState.Idle, 0), "REPOUSO"),
                    Pixels(art.BossFrame(id, BossAnimationState.Attack, 2), "ATAQUE"),
                    Pixels(art.BossFrame(id, BossAnimationState.Enrage, 1), "FURIA")
                } });
            foreach (string id in new[] { "merchant", "miniboss" })
            {
                var record = new Record { id = id };
                for (int frame = 0; frame < 3; frame++) record.poses.Add(Pixels(art.Frame(id, frame), "POSE " + (frame + 1)));
                records.Add(record);
            }
            foreach (string id in Weapons)
                records.Add(new Record { id = id, poses = new List<Pose> { Pixels(art.Get(id), "ARMA") } });
        }
        if (records.Count != 30 || records.Select(record => record.id).Distinct().Count() != 30
            || records.Sum(record => record.poses.Count) != 82)
            throw new InvalidOperationException("The export must include 26 actors, four weapon records and 82 poses.");
        string destination = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.WriteAllText(destination, JsonSerializer.Serialize(new { version = 1, coordinateOrigin = "top-left", records }), new UTF8Encoding(false));
        Console.WriteLine("Exported 26 actor records + four weapon records / 82 poses from production sprite pixels. No Unity rendering.");
    }
}

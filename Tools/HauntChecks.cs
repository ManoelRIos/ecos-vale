using Ecos.Haunt;

static class HauntChecks
{
    static int assertions;
    static void Check(bool condition, string description)
    {
        assertions++;
        if (!condition) throw new Exception(description);
    }

    static string Signature(DungeonFloor floor) => string.Join(";", floor.Rooms.Select(r =>
        $"{r.Id},{r.Position.X},{r.Position.Y},{r.Kind}:{string.Join(",", r.Neighbors)}"));

    static void Maps()
    {
        var layouts = new HashSet<string>();
        int[] edgeSeeds = { int.MinValue, -1, 0, 1, int.MaxValue };
        foreach (int seed in edgeSeeds.Concat(Enumerable.Range(-200, 401)))
            foreach (int number in new[] { 1, 2, 5, 30 })
            {
                DungeonFloor floor = DungeonFloor.Generate(seed, number);
                Check(floor.Rooms.Count == 7 + Math.Min(number - 1, 4), "Wrong room count");
                Check(floor.Rooms.Select(r => r.Position).Distinct().Count() == floor.Rooms.Count, "Overlapping rooms");
                Check(floor.Entrance.Kind == RoomKind.Entrance && floor.Entrance.Cleared, "Entrance must be safe");
                Check(floor.Rooms.Count(r => r.Kind == RoomKind.Boss) == 1, "One boss per floor");
                Check(!floor.Boss.Cleared && floor.Boss.Neighbors.Count == 1, "Boss must begin uncleared at a dead end");
                var distances = new Dictionary<int, int> { [floor.Entrance.Id] = 0 };
                var pending = new Queue<int>();
                pending.Enqueue(floor.Entrance.Id);
                while (pending.Count > 0)
                {
                    int id = pending.Dequeue();
                    DungeonRoom room = floor.GetRoom(id);
                    Check(room.Id == id, "Stable room ID lookup");
                    Check(room.Neighbors.Count == room.Neighbors.Distinct().Count(), "Duplicate neighbor");
                    foreach (int adjacent in room.Neighbors)
                    {
                        DungeonRoom neighbor = floor.GetRoom(adjacent);
                        Check(neighbor.Neighbors.Contains(id), "Asymmetric doorway");
                        Check(Math.Abs(room.Position.X - neighbor.Position.X) + Math.Abs(room.Position.Y - neighbor.Position.Y) == 1,
                            "Non-cardinal doorway");
                        if (distances.ContainsKey(adjacent)) continue;
                        distances.Add(adjacent, distances[id] + 1);
                        pending.Enqueue(adjacent);
                    }
                }
                Check(distances.Count == floor.Rooms.Count, "Unreachable room");
                Check(distances[floor.Boss.Id] == distances.Values.Max(), "Boss must be farthest from entrance");
                Check(distances[floor.Boss.Id] >= 2, "Boss too close to entrance");
                Check(floor.Rooms.Sum(r => r.Neighbors.Count) == 2 * (floor.Rooms.Count - 1), "Map should be a tree");
                Check(Signature(floor) == Signature(DungeonFloor.Generate(seed, number)), "Seed replay mismatch");
                layouts.Add(Signature(floor));
            }
        Check(layouts.Count > 1000, "Different seeds need different layouts");
        Check(DungeonFloor.Generate(12, 0).Number == 1, "Invalid floor must normalize");
        Check(DungeonFloor.Generate(12, int.MaxValue).Rooms.Count == 11, "Late floor generation overflow");
        Console.WriteLine($"Maps: {406 * 4} seeded floors checked; {layouts.Count} distinct layouts.");
    }

    static void BossRosters()
    {
        int[][] expected = {
            new[] { 0, 1, 3 }, new[] { 1, 2, 4 }, new[] { 2, 4, 0 },
            new[] { 3, 1, 0 }, new[] { 4, 3, 2 }
        };
        for (int theme = 0; theme < 5; theme++)
        {
            int[] candidates = DungeonFloor.BossCandidatesForTheme(theme);
            Check(candidates.Length == 3 && candidates.Distinct().Count() == 3,
                "Each theme must have exactly three distinct bosses");
            Check(candidates.SequenceEqual(expected[theme]), "Theme boss pool changed unexpectedly");
            candidates[0] = 999;
            Check(DungeonFloor.BossCandidatesForTheme(theme).SequenceEqual(expected[theme]),
                "Encounter catalog must be protected from caller mutation");
            var seen = new HashSet<int>();
            foreach (int seed in Enumerable.Range(-200, 401).Concat(new[] { int.MinValue, int.MaxValue }))
                foreach (int number in new[] { theme + 1, theme + 6, theme + 31 })
                {
                    var floor = DungeonFloor.Generate(seed, number);
                    Check(floor.ThemeIndex == theme, "Floor theme must cycle independently of the selected boss");
                    Check(expected[theme].Contains(floor.BossArchetype), "Generated boss must belong to the theme pool");
                    Check(DungeonFloor.Generate(seed, number).BossArchetype == floor.BossArchetype,
                        "Replaying a seed and floor must reproduce the boss");
                    seen.Add(floor.BossArchetype);
                }
            Check(seen.SetEquals(expected[theme]), "Seed variation must reach all three possible bosses");
        }
        var normalized = DungeonFloor.Generate(12, 0);
        var first = DungeonFloor.Generate(12, 1);
        Check(normalized.ThemeIndex == 0 && normalized.BossArchetype == first.BossArchetype,
            "Invalid floor normalization must also normalize encounter selection");
        var late = DungeonFloor.Generate(int.MaxValue, int.MaxValue);
        Check(DungeonFloor.BossCandidatesForTheme(late.ThemeIndex).Contains(late.BossArchetype),
            "Extreme seed and floor must produce an eligible boss without overflow");

        // Captured before encounter selection was introduced: this protects existing
        // shared seeds from accidental changes to room layout or boss-room position.
        var signatures = new List<string>();
        foreach (int seed in new[] { int.MinValue, -200, -1, 0, 1, 42, 71319, int.MaxValue })
            foreach (int number in new[] { 1, 2, 5, 30 })
                signatures.Add(Signature(DungeonFloor.Generate(seed, number)));
        string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join("\n", signatures))));
        Check(fingerprint == "0F953328F10A398980518CA186AD970A012FA87848478ECD96869569040564E9",
            "Adding encounter randomness must preserve previously generated room layouts");
        Console.WriteLine("Boss rosters: three candidates per theme, seeded replay, full pool coverage and preserved maps checked.");
    }

    static void Economy()
    {
        var run = new RunState(71319);
        Check(run.Seed == 71319 && run.Floor == 1 && run.Health == 6 && run.MaxHealth == 6, "Initial stats");
        Check(!run.Hurt(0) && !run.Hurt(-1) && run.Health == 6, "Invalid damage");
        run.NextFloor();
        Check(run.Floor == 1, "Cannot skip boss");
        run.Coins = 1000;
        Check(!run.TryBuy(ShopItem.Damage, out _) && run.Coins == 1000, "Shop locked before boss");
        run.Coins = 0;
        run.RewardRoom(false);
        Check(run.Coins == 12, "Combat reward");
        run.RewardRoom(true);
        Check(run.BossDefeated && run.Coins == 62, "Boss reward unlocks shop");
        run.RewardRoom(true);
        Check(run.Coins == 62, "Boss cannot pay twice");
        Check(!run.TryBuy(ShopItem.Heal, out _) && run.Coins == 62 && !run.Sold(ShopItem.Heal), "Full-health purchase must be atomic");
        Check(run.Hurt(4) && run.Health == 2, "Damage applied");
        Check(run.TryBuy(ShopItem.Heal, out _) && run.Health == 5 && run.Coins == 42 && run.Sold(ShopItem.Heal), "Heal purchase");
        Check(!run.TryBuy(ShopItem.Heal, out _) && run.Coins == 42 && run.Health == 5, "Sold purchase must not charge twice");
        Check(!run.TryBuy(ShopItem.Damage, out _) && run.Coins == 42 && run.Damage == 1, "Unaffordable purchase must be atomic");
        Check(run.TryBuy(ShopItem.MaxHealth, out _) && run.MaxHealth == 8 && run.Health == 7 && run.Coins == 2, "Maximum health purchase");
        run.NextFloor();
        Check(run.Floor == 2 && !run.BossDefeated && !run.Sold(ShopItem.Heal) && run.Health == 7 && run.MaxHealth == 8 && run.Coins == 2,
            "Floor transition preserves stats and resets shop");
        run.Coins = 1000;
        run.RewardRoom(true);
        int before = run.Coins;
        Check(run.TryBuy(ShopItem.Damage, out _) && run.Damage == 2 && run.Coins == before - run.Price(ShopItem.Damage), "Damage purchase");
        float interval = run.ShotInterval;
        Check(run.TryBuy(ShopItem.FireRate, out _) && run.ShotInterval < interval, "Fire-rate purchase");
        Check(!run.TryBuy((ShopItem)999, out _) && !run.Sold((ShopItem)999), "Unknown item rejected");
        run.Hurt(int.MaxValue);
        Check(run.IsDead && run.Health == 0 && !run.Hurt(1), "Death clamps health");
        before = run.Coins;
        run.RewardRoom(false);
        run.RewardRoom(true);
        run.NextFloor();
        Check(run.Floor == 2 && run.Coins == before && !run.TryBuy(ShopItem.Heal, out _), "Dead runs cannot progress or buy");

        var upgrades = new RunState(1);
        for (int i = 0; i < 40; i++)
        {
            upgrades.Coins = 9999;
            upgrades.RewardRoom(true);
            upgrades.TryBuy(ShopItem.Damage, out _);
            upgrades.TryBuy(ShopItem.FireRate, out _);
            upgrades.TryBuy(ShopItem.MaxHealth, out _);
            upgrades.NextFloor();
        }
        Check(upgrades.Damage == 10 && upgrades.MaxHealth == 20 && upgrades.ShotInterval >= .07f, "Upgrade caps");
        Console.WriteLine("Economy: boss gating, rewards, atomic purchases, upgrades, transitions and death checked.");
    }

    static void Geometry()
    {
        Check(CombatGeometry.SegmentHitsCircle(-10, 0, 10, 0, 0, 0, .4f), "Fast shot must hit circle");
        Check(CombatGeometry.SegmentHitsCircle(10, 0, -10, 0, 0, 0, .4f), "Reversed shot must hit circle");
        Check(!CombatGeometry.SegmentHitsCircle(-10, 1, 10, 1, 0, 0, .4f), "Circle miss");
        Check(CombatGeometry.SegmentHitsCircle(-10, 1, 10, 1, 0, 0, 1), "Circle tangency is a hit");
        Check(CombatGeometry.SegmentHitsCircle(0, 0, 10, 0, 0, 0, .4f), "Shot starts in circle");
        Check(CombatGeometry.SegmentHitsCircle(0, 0, 0, 0, 0, 0, .4f), "Point inside circle");
        Check(!CombatGeometry.SegmentHitsCircle(1, 1, 1, 1, 0, 0, .4f), "Point outside circle");
        Check(!CombatGeometry.SegmentHitsCircle(-3, 0, -1, 0, 0, 0, .4f), "Circle on line beyond segment");
        Check(CombatGeometry.SegmentHitsBox(-10, 0, 10, 0, -.5f, -.5f, .5f, .5f), "Fast shot must hit box");
        Check(CombatGeometry.SegmentHitsBox(10, 0, -10, 0, -.5f, -.5f, .5f, .5f), "Reversed shot must hit box");
        Check(!CombatGeometry.SegmentHitsBox(-10, 1, 10, 1, -.5f, -.5f, .5f, .5f), "Parallel box miss");
        Check(CombatGeometry.SegmentHitsBox(-10, .5f, 10, .5f, -.5f, -.5f, .5f, .5f), "Box edge tangency");
        Check(CombatGeometry.SegmentHitsBox(0, 0, 10, 0, -.5f, -.5f, .5f, .5f), "Shot starts in box");
        Check(CombatGeometry.SegmentHitsBox(0, 0, 0, 0, -.5f, -.5f, .5f, .5f), "Point inside box");
        Check(!CombatGeometry.SegmentHitsBox(1, 1, 1, 1, -.5f, -.5f, .5f, .5f), "Point outside box");
        Check(!CombatGeometry.SegmentHitsBox(-3, 0, -1, 0, -.5f, -.5f, .5f, .5f), "Box on line beyond segment");
        Check(CombatGeometry.SegmentHitsBox(-2, -2, 2, 2, -.5f, -.5f, .5f, .5f), "Diagonal crossing");
        Check(!CombatGeometry.SegmentHitsBox(-2, 2, 2, 3, -.5f, -.5f, .5f, .5f), "Diagonal miss");
        Check(CombatGeometry.CircleHitFraction(-10, 0, 10, 0, 0, 0, 1, out float circleHit)
            && Math.Abs(circleHit - .45f) < .00001f, "Circle first contact fraction");
        Check(CombatGeometry.CircleHitFraction(10, 0, -10, 0, 0, 0, 1, out circleHit)
            && Math.Abs(circleHit - .45f) < .00001f, "Reversed circle first contact fraction");
        Check(CombatGeometry.CircleHitFraction(-10, 1, 10, 1, 0, 0, 1, out circleHit)
            && Math.Abs(circleHit - .5f) < .00001f, "Tangent first contact fraction");
        Check(CombatGeometry.CircleHitFraction(0, 0, 10, 0, 0, 0, 1, out circleHit)
            && circleHit == 0, "Start-inside circle fraction");
        Check(CombatGeometry.CircleHitFraction(0, 0, 0, 0, 0, 0, 1, out circleHit)
            && circleHit == 0, "Point inside circle fraction");
        Check(!CombatGeometry.CircleHitFraction(-10, 2, 10, 2, 0, 0, 1, out circleHit)
            && float.IsPositiveInfinity(circleHit), "Circle miss fraction must not compete with hits");
        Check(!CombatGeometry.CircleHitFraction(2, 0, 3, 0, 0, 0, 1, out _), "Circle behind shot is not a hit");
        Check(!CombatGeometry.CircleHitFraction(2, 0, 2, 0, 0, 0, 1, out _), "Stationary shot outside circle is not a hit");
        Check(CombatGeometry.BoxHitFraction(-10, 0, 10, 0, -1, -1, 1, 1, out float boxHit)
            && Math.Abs(boxHit - .45f) < .00001f, "Box first contact fraction");
        Check(CombatGeometry.BoxHitFraction(10, 0, -10, 0, -1, -1, 1, 1, out boxHit)
            && Math.Abs(boxHit - .45f) < .00001f, "Reversed box first contact fraction");
        Check(CombatGeometry.BoxHitFraction(0, 0, 10, 0, -1, -1, 1, 1, out boxHit)
            && boxHit == 0, "Start-inside box fraction");
        Check(CombatGeometry.BoxHitFraction(0, 0, 0, 0, -1, -1, 1, 1, out boxHit)
            && boxHit == 0, "Point inside box fraction");
        Check(!CombatGeometry.BoxHitFraction(-10, 2, 10, 2, -1, -1, 1, 1, out boxHit)
            && float.IsPositiveInfinity(boxHit), "Box miss fraction must not compete with hits");

        // A fast shot crosses the near target, then cover, then a farther target.
        // Select by contact fraction, regardless of collection iteration order.
        Check(CombatGeometry.CircleHitFraction(-2, 0, 2, 0, -1, 0, .25f, out float nearHit), "Near target crossing");
        Check(CombatGeometry.CircleHitFraction(-2, 0, 2, 0, 1.5f, 0, .25f, out float farHit), "Far target crossing");
        Check(CombatGeometry.BoxHitFraction(-2, 0, 2, 0, 0, -.5f, 1, .5f, out boxHit), "Cover crossing");
        Check(Math.Abs(nearHit - .1875f) < .00001f && nearHit < boxHit && boxHit < farHit,
            "Near target must beat later cover, cover must block far target");
        int chosen = -1;
        float earliest = boxHit;
        float[] reversedTargets = { farHit, nearHit };
        for (int i = 0; i < reversedTargets.Length; i++)
            if (reversedTargets[i] < earliest) { earliest = reversedTargets[i]; chosen = i; }
        Check(chosen == 1 && earliest == nearHit, "First collision selection must ignore target list ordering");
        Console.WriteLine("Geometry: swept projectiles checked against circles and boxes.");
    }

    static void CharactersAndArsenal()
    {
        foreach (CharacterKind character in Enum.GetValues<CharacterKind>())
        {
            var run = new RunState(42, character);
            if (character == CharacterKind.Alchemist) run.Hurt(2);
            Check(run.Character == character && run.TryUsePower(), "Selected character has ready power");
            Check(!run.TryUsePower(), "Power cannot be spammed");
            run.TickPower(run.PowerDuration - .25f);
            Check(!run.TryUsePower(), "Power must wait for full cooldown");
            run.TickPower(-10);
            Check(run.PowerCooldown <= .251f, "Negative elapsed time ignored");
            run.TickPower(.3f);
            if (character == CharacterKind.Alchemist) run.Hurt(2);
            Check(run.TryUsePower(), "Power becomes available again");
            Check(!run.Equip(WeaponKind.SoulCannon), "Cannot equip unowned weapon");
            run.Coins = 1000;
            Check(!run.TryBuy(ShopItem.Scattergun, out _) && run.Coins == 1000, "Weapon shop boss gate");
            run.BossDefeated = true;
            foreach (var item in new[] { ShopItem.Scattergun, ShopItem.Repeater, ShopItem.SoulCannon })
            {
                int before = run.Coins;
                int price = run.Price(item);
                Check(run.TryBuy(item, out _) && run.Coins == before - price, "Weapon purchase charges once");
                Check(run.Owns(run.Weapon) && run.Weapon != WeaponKind.Pistol, "Purchase equips owned weapon");
                Check(!run.TryBuy(item, out _), "Duplicate weapon purchase rejected");
            }
            run.NextFloor();
            Check(run.Character == character && run.Owns(WeaponKind.SoulCannon), "Character and arsenal persist across floors");
            run.BossDefeated = true;
            int saved = run.Coins;
            Check(!run.TryBuy(ShopItem.Scattergun, out _) && run.Coins == saved, "Owned weapons not charged on later visits");
            run.Equip(WeaponKind.Repeater); float fast = run.WeaponInterval;
            run.Equip(WeaponKind.Scattergun); float spread = run.WeaponInterval;
            run.Equip(WeaponKind.SoulCannon);
            Check(fast < spread && spread < run.WeaponInterval, "Weapon fire rates differ");
            run.TickPower(100); run.Hurt(100);
            Check(!run.TryUsePower() && !run.Equip(WeaponKind.Pistol), "Dead character cannot use power or equip");
            var fresh = new RunState(42, character);
            Check(!fresh.Owns(WeaponKind.SoulCannon) && fresh.PowerCooldown == 0, "Restart clears arsenal and cooldown");
        }
        Check(new RunState(1, CharacterKind.Hunter).PowerDuration == 9, "Hunter retains nine-second cooldown");
        Check(new RunState(1, CharacterKind.Medium).PowerDuration == 7, "Medium retains seven-second cooldown");
        var reaper = new RunState(1, CharacterKind.Reaper);
        Check(reaper.PowerDuration == 8 && reaper.PowerName == "COLHEITA LUNAR", "Reaper has distinct power");
        var alchemist = new RunState(1, CharacterKind.Alchemist);
        Check(alchemist.PowerDuration == 18 && alchemist.PowerName == "ELIXIR VITAL", "Alchemist has distinct power");
        Check(!alchemist.TryUsePower() && alchemist.PowerCooldown == 0 && alchemist.Health == alchemist.MaxHealth,
            "Full-health heal is rejected without spending cooldown");
        alchemist.Hurt(3);
        Check(alchemist.TryUsePower() && alchemist.Health == 5 && alchemist.PowerCooldown == 18,
            "Alchemist heal restores exactly two and starts cooldown atomically");
        alchemist.Hurt(1);
        int injured = alchemist.Health;
        Check(!alchemist.TryUsePower() && alchemist.Health == injured && alchemist.PowerCooldown == 18,
            "Cooldown blocks repeated healing without changing health or timer");
        alchemist.TickPower(18);
        Check(alchemist.TryUsePower() && alchemist.Health == alchemist.MaxHealth,
            "Alchemist can heal again after cooldown");
        alchemist.TickPower(18);
        alchemist.Hurt(1);
        Check(alchemist.TryUsePower() && alchemist.Health == alchemist.MaxHealth,
            "Alchemist heal saturates at maximum health");
        alchemist.TickPower(18);
        alchemist.Hurt(100);
        Check(!alchemist.TryUsePower() && alchemist.Health == 0 && alchemist.PowerCooldown == 0,
            "Alchemist cannot revive itself with healing");
        var poor = new RunState(1) { BossDefeated = true, Coins = 1 };
        Check(!poor.TryBuy(ShopItem.SoulCannon, out _) && poor.Coins == 1 && !poor.Owns(WeaponKind.SoulCannon), "Unaffordable weapon purchase is atomic");
        Console.WriteLine("Characters/arsenal: cooldowns, ownership, purchases, persistence and restart checked.");
    }

    static void CuriousShopWeapons()
    {
        ShopItem[] items = { ShopItem.Stapler, ShopItem.Banana, ShopItem.SpinningTop };
        WeaponKind[] weapons = { WeaponKind.Stapler, WeaponKind.Banana, WeaponKind.SpinningTop };
        string[] names = { "GRAMPEADOR", "BANANA", "PIAO DO CAOS" };
        int[] basePrices = { 55, 65, 80 };
        float[] multipliers = { 1.4f, 2.2f, 3.5f };
        var run = new RunState(71319) { Coins = 1000 };
        for (int i = 0; i < items.Length; i++)
        {
            Check((int)items[i] - 3 == (int)weapons[i], "Shop and weapon identifiers must retain their mapping");
            Check(RunState.WeaponName(weapons[i]) == names[i], "Curious weapons need their own displayed names");
            Check(run.Price(items[i]) == basePrices[i], "Curious weapon initial price");
            Check(!run.Owns(weapons[i]) && !run.Equip(weapons[i]), "Curious weapons must be acquired before equipping");
            Check(!run.TryBuy(items[i], out _) && run.Coins == 1000 && !run.Sold(items[i]),
                "Curious weapon purchase must respect the boss gate without mutation");
        }
        run.BossDefeated = true;
        for (int i = 0; i < items.Length; i++)
        {
            int before = run.Coins;
            Check(run.TryBuy(items[i], out _) && run.Coins == before - basePrices[i], "Curious weapon purchase charges the listed price");
            Check(run.Owns(weapons[i]) && run.Weapon == weapons[i] && run.Sold(items[i]), "Curious weapon purchase grants and equips the matching item");
            Check(Math.Abs(run.WeaponInterval - run.ShotInterval * multipliers[i]) < .00001f, "Curious weapon cadence uses its own multiplier");
            before = run.Coins;
            Check(!run.TryBuy(items[i], out _) && run.Coins == before && run.Weapon == weapons[i],
                "Duplicate purchase must preserve money and equipment");
        }
        run.NextFloor();
        Check(run.Floor == 2 && run.Weapon == WeaponKind.SpinningTop, "Equipped curious weapon persists into the next floor");
        run.BossDefeated = true;
        float previousInterval = 0;
        for (int i = 0; i < items.Length; i++)
        {
            Check(!run.Sold(items[i]) && run.Owns(weapons[i]), "Visit stock resets without deleting owned curious weapons");
            Check(run.Price(items[i]) == basePrices[i] + 3, "Curious weapons use normal floor price scaling");
            int before = run.Coins;
            Check(!run.TryBuy(items[i], out _) && run.Coins == before, "Later visits cannot charge for already owned curious weapons");
            Check(run.Equip(weapons[i]) && run.Weapon == weapons[i], "Owned curious weapons can be reequipped");
            Check(run.WeaponInterval > previousInterval, "Curious weapon cadences remain distinct");
            previousInterval = run.WeaponInterval;
        }
        run.Equip(WeaponKind.Stapler);
        float interval = run.WeaponInterval;
        Check(run.TryBuy(ShopItem.FireRate, out _) && run.WeaponInterval < interval, "Fire-rate upgrades affect curious weapons");
        int savedCoins = run.Coins;
        WeaponKind savedWeapon = run.Weapon;
        Check(!run.TryBuy((ShopItem)999, out _) && !run.TryBuy((ShopItem)(-1), out _)
            && run.Coins == savedCoins && run.Weapon == savedWeapon, "Invalid items cannot charge or alter equipment");
        Check(!run.Equip((WeaponKind)999) && run.Weapon == savedWeapon, "Invalid weapon cannot be equipped");
        bool rejectedName = false;
        try { RunState.WeaponName((WeaponKind)999); }
        catch (ArgumentOutOfRangeException) { rejectedName = true; }
        Check(rejectedName, "Unknown weapon name must not masquerade as a soul cannon");
        var fresh = new RunState(run.Seed);
        foreach (var weapon in weapons) Check(!fresh.Owns(weapon), "Restart removes curious weapon ownership");
        Check(fresh.Weapon == WeaponKind.Pistol, "Restart returns to the initial weapon");
        for (int i = 0; i < items.Length; i++)
        {
            var poor = new RunState(1) { BossDefeated = true, Coins = basePrices[i] - 1 };
            Check(!poor.TryBuy(items[i], out _) && poor.Coins == basePrices[i] - 1 && !poor.Owns(weapons[i])
                && !poor.Sold(items[i]) && poor.Weapon == WeaponKind.Pistol, "Unaffordable curious weapon purchase is atomic");
        }
        run.Hurt(100);
        Check(!run.Equip(WeaponKind.Banana) && !run.TryBuy(ShopItem.Banana, out _) && run.Coins == savedCoins,
            "Dead runs cannot equip or purchase curious weapons");
        Console.WriteLine("Curious weapons: prices, atomic purchases, cadence, ownership, transitions and restart checked.");
    }

    public static void Main()
    {
        Maps();
        BossRosters();
        Economy();
        Geometry();
        CharactersAndArsenal();
        CuriousShopWeapons();
        Console.WriteLine($"PASS — {assertions} assertions against production dungeon and run-state code.");
    }
}

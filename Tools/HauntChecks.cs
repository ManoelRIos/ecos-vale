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
        $"{r.Id},{r.Position.X},{r.Position.Y},{r.Kind},{r.HasMiniBoss},{r.Encounter}:{string.Join(",", r.Neighbors)}"));

    static void Maps()
    {
        var layouts = new HashSet<string>();
        int[] edgeSeeds = { int.MinValue, -1, 0, 1, int.MaxValue };
        foreach (int seed in edgeSeeds.Concat(Enumerable.Range(-200, 401)))
            foreach (int number in new[] { 1, 2, 4, 5, 11, 30 })
            {
                DungeonFloor floor = DungeonFloor.Generate(seed, number);
                Check(floor.Rooms.Count == 7 + 2 * Math.Min(number - 1, 10), "Wrong room count");
                Check(floor.Rooms.Select(r => r.Position).Distinct().Count() == floor.Rooms.Count, "Overlapping rooms");
                Check(floor.Entrance.Kind == RoomKind.Entrance && floor.Entrance.Cleared, "Entrance must be safe");
                Check(floor.Rooms.Count(r => r.Kind == RoomKind.Boss) == 1, "One boss per floor");
                Check(floor.Rooms.Count(r => r.HasMiniBoss) == (number >= 4 ? 2 : 1), "Miniboss rooms must scale from floor four");
                Check(!floor.Entrance.HasMiniBoss && !floor.Boss.HasMiniBoss, "Miniboss cannot occupy entrance or main boss room");
                Check(!floor.Boss.Cleared && floor.Boss.Neighbors.Count == 1, "Boss must begin uncleared at a dead end");
                var distances = new Dictionary<int, int> { [floor.Entrance.Id] = 0 };
                var pending = new Queue<int>();
                pending.Enqueue(floor.Entrance.Id);
                while (pending.Count > 0)
                {
                    int id = pending.Dequeue();
                    DungeonRoom room = floor.GetRoom(id);
                    Check(room.Id == id, "Stable room ID lookup");
                    Check(room.WavesCleared == 0, "New room must start without completed waves");
                    Check(room.TotalWaves == (room.Kind == RoomKind.Combat ? 3 : 1), "Combat rooms require three waves");
                    Check(!room.HasMiniBoss || room.Kind == RoomKind.Combat, "Miniboss must accompany a combat room");
                    Check(Enum.IsDefined(room.Encounter), "Encounter profile must be valid");
                    Check(!room.HasMiniBoss || room.Encounter == EncounterKind.Elite, "Miniboss rooms use the elite profile");
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
                Check(floor.Rooms.Where(room => room.Kind == RoomKind.Combat).Select(room => room.Encounter).Distinct().Count() >= 3,
                    "Each floor needs at least three combat profiles");
                layouts.Add(Signature(floor));
            }
        Check(layouts.Count > 1000, "Different seeds need different layouts");
        Check(DungeonFloor.Generate(12, 0).Number == 1, "Invalid floor must normalize");
        Check(DungeonFloor.Generate(12, int.MaxValue).Rooms.Count == 27, "Late floor generation overflow");
        Check(DungeonFloor.RoomCountForNumber(int.MinValue) == 7 && DungeonFloor.RoomCountForNumber(0) == 7,
            "Invalid floor room counts normalize without overflow");
        for (int number = 1; number <= 11; number++)
            Check(DungeonFloor.RoomCountForNumber(number) == 7 + (number - 1) * 2, "Each early floor adds two rooms");
        Check(DungeonFloor.RoomCountForNumber(12) == 27 && DungeonFloor.RoomCountForNumber(int.MaxValue) == 27,
            "Room count growth stays bounded on late floors");
        var encounter = DungeonFloor.Generate(77, 4).Rooms.First(room => room.HasMiniBoss);
        encounter.WavesCleared = 2;
        Check(!encounter.Cleared && encounter.WavesCleared < encounter.TotalWaves, "Two completed waves cannot imply full room completion");
        Check(DungeonFloor.Generate(77, 4).GetRoom(encounter.Id).WavesCleared == 0, "Wave progress belongs only to the current floor instance");
        Console.WriteLine($"Maps: {406 * 6} seeded floors checked; {layouts.Count} distinct layouts, three-wave rooms and miniboss placement.");
    }

    static void BossRosters()
    {
        int[][] expected = {
            new[] { 0, 1, 5 }, new[] { 1, 2, 6 }, new[] { 2, 4, 5 },
            new[] { 3, 6, 0 }, new[] { 4, 5, 6 }
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

        // Larger floors intentionally change the old layout fingerprint. Maps()
        // instead checks reproducibility of the expanded graph and miniboss rooms.
        Console.WriteLine("Boss rosters: three candidates per theme, seeded replay and full pool coverage checked.");
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
        WeaponKind[] starters = { WeaponKind.HunterCarbine, WeaponKind.VeilWand, WeaponKind.BoneCaster, WeaponKind.ArcCoil };
        float[] starterCadences = { 1f, 1.4f, 1.8f, .65f };
        foreach (CharacterKind character in Enum.GetValues<CharacterKind>())
        {
            var run = new RunState(42, character);
            WeaponKind starter = starters[(int)character];
            Check(run.StartingWeapon == starter && RunState.StartingWeaponFor(character) == starter && run.Weapon == starter,
                "Each character begins with its specific weapon equipped");
            Check(run.Owns(starter) && run.Owns(WeaponKind.Pistol), "Starter and legacy revolver are owned from the beginning");
            Check(Math.Abs(run.WeaponInterval - run.ShotInterval * starterCadences[(int)character]) < .00001f,
                "Character starting weapons have distinct cadence");
            Check(starters.All(weapon => weapon == starter || !run.Owns(weapon)), "Characters cannot access another hero's starter");
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
            for (int visit = 0; visit < 12 && run.ShopWeapons.Length > 0; visit++)
            {
                foreach (ShopItem item in run.ShopWeapons)
                {
                    run.Coins = 10000;
                    int before = run.Coins;
                    int price = run.Price(item);
                    Check(run.TryBuy(item, out _) && run.Coins == before - price, "Stocked weapon purchase charges once");
                    Check(run.Owns(run.Weapon) && run.Weapon != WeaponKind.Pistol, "Purchase equips owned weapon");
                    Check(!run.TryBuy(item, out _), "Duplicate weapon purchase rejected");
                }
                run.NextFloor();
                run.BossDefeated = true;
            }
            Check(run.ShopWeapons.Length == 0, "Stock is exhausted after every catalog weapon is owned");
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
            Check(fresh.Weapon == starter && fresh.Owns(starter), "Restart restores the selected character's starter");
        }
        foreach (WeaponKind weapon in Enum.GetValues<WeaponKind>())
            Check(RunState.WeaponName(weapon).All(letter => letter < 128 && !char.IsLower(letter)),
                "Weapon names must support uppercase pixel lettering");
        bool rejectedCharacter = false;
        try { RunState.StartingWeaponFor((CharacterKind)999); }
        catch (ArgumentOutOfRangeException) { rejectedCharacter = true; }
        Check(rejectedCharacter, "Invalid character cannot silently receive a starter");
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

    static RunState Offering(ShopItem item, CharacterKind character = CharacterKind.Hunter)
    {
        for (int seed = 0; seed < 1000; seed++)
        {
            var run = new RunState(seed, character) { BossDefeated = true, Coins = 10000 };
            if (run.IsOffered(item)) return run;
        }
        throw new Exception("No seeded shop offers " + item);
    }

    static void CuriousShopWeapons()
    {
        ShopItem[] items = { ShopItem.Stapler, ShopItem.Banana, ShopItem.SpinningTop, ShopItem.MirrorDisc, ShopItem.RayLantern,
            ShopItem.BoneFlute, ShopItem.Umbrella, ShopItem.HiveLauncher, ShopItem.BoxingGlove };
        WeaponKind[] weapons = { WeaponKind.Stapler, WeaponKind.Banana, WeaponKind.SpinningTop, WeaponKind.MirrorDisc, WeaponKind.RayLantern,
            WeaponKind.BoneFlute, WeaponKind.Umbrella, WeaponKind.HiveLauncher, WeaponKind.BoxingGlove };
        string[] names = { "GRAMPEADOR", "BANANA", "PIAO DO CAOS", "DISCO ESPELHO", "LANTERNA DE RAIOS", "FLAUTA DE OSSOS",
            "GUARDA-CHUVA", "COLMEIA DE BOLSO", "LUVA DE BOXE" };
        int[] basePrices = { 55, 65, 80, 70, 90, 75, 65, 85, 75 };
        float[] multipliers = { 1.4f, 2.2f, 3.5f, 1.7f, .6f, 2f, 2.1f, 1.15f, 2.6f };
        foreach (var item in new[] { ShopItem.Heal, ShopItem.Damage, ShopItem.FireRate, ShopItem.MaxHealth,
            ShopItem.GiantRounds, ShopItem.CharacterRelic, ShopItem.QuickBoots, (ShopItem)(-1), (ShopItem)999 })
            Check(!RunState.TryWeaponForItem(item, out _), "Upgrades and invalid items cannot map to weapons");
        Check(RunState.TryWeaponForItem(ShopItem.Scattergun, out var scattergun) && scattergun == WeaponKind.Scattergun
            && RunState.TryWeaponForItem(ShopItem.Repeater, out var repeater) && repeater == WeaponKind.Repeater
            && RunState.TryWeaponForItem(ShopItem.SoulCannon, out var cannon) && cannon == WeaponKind.SoulCannon,
            "Legacy shop weapons retain explicit mapping");
        for (int i = 0; i < items.Length; i++)
        {
            var run = Offering(items[i]);
            Check(RunState.TryWeaponForItem(items[i], out var mapped) && mapped == weapons[i], "Shop item maps to its matching weapon");
            Check(RunState.WeaponName(weapons[i]) == names[i], "Curious weapons need their own displayed names");
            Check(run.Price(items[i]) == basePrices[i], "Curious weapon initial price");
            Check(!run.Owns(weapons[i]) && !run.Equip(weapons[i]), "Curious weapons must be acquired before equipping");
            run.BossDefeated = false;
            Check(!run.TryBuy(items[i], out _) && run.Coins == 10000 && !run.Sold(items[i]),
                "Curious weapon purchase must respect the boss gate without mutation");
            run.BossDefeated = true;
            int before = run.Coins;
            Check(run.TryBuy(items[i], out _) && run.Coins == before - basePrices[i], "Curious weapon purchase charges the listed price");
            Check(run.Owns(weapons[i]) && run.Weapon == weapons[i] && run.Sold(items[i]), "Curious weapon purchase grants and equips the matching item");
            Check(Math.Abs(run.WeaponInterval - run.ShotInterval * multipliers[i]) < .00001f, "Curious weapon cadence uses its own multiplier");
            before = run.Coins;
            Check(!run.TryBuy(items[i], out _) && run.Coins == before && run.Weapon == weapons[i],
                "Duplicate purchase must preserve money and equipment");
            run.NextFloor();
            Check(run.Floor == 2 && run.Weapon == weapons[i] && run.Owns(weapons[i]) && !run.Sold(items[i]),
                "Owned and equipped weapons persist while visit stock resets");
            run.BossDefeated = true;
            Check(run.Price(items[i]) == basePrices[i] + 3, "Curious weapons use normal floor price scaling");
            Check(!run.IsOffered(items[i]) && !run.TryBuy(items[i], out _) && run.Coins == before,
                "Owned weapons cannot occupy or charge a later visit's stock");
            Check(run.Equip(weapons[i]), "Owned curious weapons can be reequipped");
            float interval = run.WeaponInterval;
            Check(run.TryBuy(ShopItem.FireRate, out _) && run.WeaponInterval < interval, "Fire-rate upgrades affect curious weapons");
            int savedCoins = run.Coins;
            Check(!run.TryBuy((ShopItem)999, out _) && !run.TryBuy((ShopItem)(-1), out _)
                && run.Coins == savedCoins && run.Weapon == weapons[i], "Invalid items cannot charge or alter equipment");
            Check(!run.Equip((WeaponKind)999) && run.Weapon == weapons[i], "Invalid weapon cannot be equipped");
            var fresh = new RunState(run.Seed);
            Check(!fresh.Owns(weapons[i]) && fresh.Weapon == fresh.StartingWeapon, "Restart clears acquired weapons");
            var poor = Offering(items[i]);
            poor.Coins = basePrices[i] - 1;
            Check(!poor.TryBuy(items[i], out _) && poor.Coins == basePrices[i] - 1 && !poor.Owns(weapons[i])
                && !poor.Sold(items[i]) && poor.Weapon == poor.StartingWeapon, "Unaffordable curious weapon purchase is atomic");
            run.Hurt(100);
            Check(!run.Equip(weapons[i]) && !run.TryBuy(items[i], out _) && run.Coins == savedCoins,
                "Dead runs cannot equip or purchase curious weapons");
        }
        bool rejectedName = false;
        try { RunState.WeaponName((WeaponKind)999); }
        catch (ArgumentOutOfRangeException) { rejectedName = true; }
        Check(rejectedName, "Unknown weapon name must not masquerade as a soul cannon");
        Console.WriteLine("Curious weapons: nine original objects, prices, stocked purchases, cadence and persistence checked.");
    }

    static void ShopStockAndRelics()
    {
        var catalog = Enum.GetValues<ShopItem>().Where(item => RunState.TryWeaponForItem(item, out _)).ToArray();
        var seen = new HashSet<ShopItem>();
        var stockSizes = new HashSet<int>();
        foreach (int seed in Enumerable.Range(-200, 401).Concat(new[] { int.MinValue, int.MaxValue }))
            foreach (int number in new[] { 1, 2, 9, int.MaxValue })
            {
                var run = new RunState(seed) { Floor = number, BossDefeated = true, Coins = 10000 };
                ShopItem[] stock = run.ShopWeapons;
                Check(stock.Length >= 1 && stock.Length <= 2, "A fresh visit displays only one or two weapons");
                Check(stock.Distinct().Count() == stock.Length, "Shop displays never repeat one weapon");
                Check(stock.SequenceEqual(new RunState(seed) { Floor = number }.ShopWeapons), "Stock must replay across processes and machines");
                Check(stock.All(item => run.IsOffered(item) && RunState.TryWeaponForItem(item, out _)), "Only weapon items occupy display cases");
                foreach (var item in stock) seen.Add(item);
                stockSizes.Add(stock.Length);
                ShopItem first = stock[0];
                stock[0] = (ShopItem)999;
                Check(run.ShopWeapons[0] == first, "Stock snapshots cannot mutate the live shop");
                var absent = catalog.First(item => !run.IsOffered(item));
                Check(!run.TryBuy(absent, out _) && run.Coins == 10000 && !run.Sold(absent)
                    && run.Weapon == run.StartingWeapon, "An unstocked purchase must be rejected atomically");
                ShopItem[] stable = run.ShopWeapons;
                Check(run.TryBuy(first, out _), "Displayed weapon can be bought");
                Check(stable.SequenceEqual(run.ShopWeapons), "Buying never rerolls other display cases");
                run.NextFloor();
                Check(!run.ShopWeapons.Contains(first), "New visits prefer unowned weapons");
            }
        Check(seen.SetEquals(catalog) && stockSizes.SetEquals(new[] { 1, 2 }), "Seed variation reaches the entire weapon catalog and both shop sizes");
        Check(!new RunState(1).IsOffered((ShopItem)999), "Invalid item is never offered");
        foreach (CharacterKind character in Enum.GetValues<CharacterKind>())
        {
            var run = new RunState(42, character) { BossDefeated = true, Coins = 10000 };
            float basePower = run.PowerDuration;
            Check(run.ProjectileScale == 1 && run.MoveSpeedMultiplier == 1 && !run.HasCharacterRelic
                && run.ProjectileDamageBonus == 0, "A fresh character starts without passive relics");
            Check(run.TryBuy(ShopItem.GiantRounds, out _) && Math.Abs(run.ProjectileScale - 1.3f) < .00001f,
                "Giant rounds grow projectiles without changing player damage or movement");
            Check(run.TryBuy(ShopItem.QuickBoots, out _) && Math.Abs(run.MoveSpeedMultiplier - 1.125f) < .00001f,
                "Boots increase movement separately from projectiles");
            Check(run.TryBuy(ShopItem.CharacterRelic, out _) && run.HasCharacterRelic,
                "A character can buy its own relic");
            int coins = run.Coins;
            Check(!run.TryBuy(ShopItem.CharacterRelic, out _) && run.Coins == coins,
                "A character relic cannot charge twice");
            Check(run.CharacterRelicName.All(letter => letter < 128 && !char.IsLower(letter)), "Character relic names use pixel-safe letters");
            if (character == CharacterKind.Hunter || character == CharacterKind.Reaper)
                Check(run.ProjectileDamageBonus == 1 && run.PowerDuration == basePower, "Hunter and Reaper relics increase projectile damage");
            if (character == CharacterKind.Medium)
                Check(Math.Abs(run.PowerDuration - basePower * .7f) < .00001f && run.ProjectileDamageBonus == 0,
                    "Medium relic shortens the power cooldown");
            if (character == CharacterKind.Alchemist)
            {
                run.Hurt(4);
                Check(run.TryUsePower() && run.Health == 5 && Math.Abs(run.PowerCooldown - basePower * .85f) < .00001f,
                    "Alchemist relic heals three and shortens its cooldown");
            }
            run.NextFloor();
            run.BossDefeated = true;
            run.Coins = 10000;
            Check(run.HasCharacterRelic && Math.Abs(run.ProjectileScale - 1.3f) < .00001f,
                "Passive relics persist across floors");
            Check(run.TryBuy(ShopItem.GiantRounds, out _) && Math.Abs(run.ProjectileScale - 1.6f) < .00001f,
                "Second giant-round relic reaches its cap");
            Check(run.TryBuy(ShopItem.QuickBoots, out _) && Math.Abs(run.MoveSpeedMultiplier - 1.25f) < .00001f,
                "Second boots relic reaches its cap");
            run.NextFloor();
            run.BossDefeated = true;
            coins = run.Coins;
            Check(!run.TryBuy(ShopItem.GiantRounds, out _) && !run.TryBuy(ShopItem.QuickBoots, out _)
                && !run.TryBuy(ShopItem.CharacterRelic, out _) && run.Coins == coins
                && !run.Sold(ShopItem.GiantRounds) && !run.Sold(ShopItem.QuickBoots), "Capped passives cannot charge on later visits");
            var fresh = new RunState(42, character);
            Check(fresh.ProjectileScale == 1 && fresh.MoveSpeedMultiplier == 1 && !fresh.HasCharacterRelic,
                "A new run resets every relic");
        }
        foreach (ShopItem item in new[] { ShopItem.GiantRounds, ShopItem.CharacterRelic, ShopItem.QuickBoots })
        {
            var poor = new RunState(1) { BossDefeated = true, Coins = 1 };
            Check(!poor.TryBuy(item, out _) && poor.Coins == 1 && !poor.Sold(item)
                && poor.ProjectileScale == 1 && poor.MoveSpeedMultiplier == 1 && !poor.HasCharacterRelic,
                "Unaffordable passive purchases are atomic");
            poor.Coins = 10000;
            poor.Hurt(100);
            Check(!poor.TryBuy(item, out _) && poor.Coins == 10000 && !poor.Sold(item), "Dead characters cannot acquire passive relics");
        }
        Console.WriteLine("Shop/relics: deterministic limited stock, no rerolls, atomic refusals, character effects and passive caps checked.");
    }

    public static void Main()
    {
        Maps();
        BossRosters();
        Economy();
        Geometry();
        CharactersAndArsenal();
        CuriousShopWeapons();
        ShopStockAndRelics();
        Console.WriteLine($"PASS — {assertions} assertions against production dungeon and run-state code.");
    }
}

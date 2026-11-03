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

    public static void Main()
    {
        Maps();
        Economy();
        Geometry();
        Console.WriteLine($"PASS — {assertions} assertions against production dungeon and run-state code.");
    }
}

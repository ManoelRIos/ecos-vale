using System.Threading;
using NUnit.Framework;

namespace Ecos.Tests
{
    public sealed class PropDistributionTests
    {
        static readonly float[] Density = { 1, .85f, 1, 1, 1, 1 };

        [Test]
        public void NegativeChunkPlansRepeatExactlyAndStayInsideBounds()
        {
            var first = PropDistribution.Generate(-2, -1, 128, 280, 71319, Density, CancellationToken.None);
            var second = PropDistribution.Generate(-2, -1, 128, 280, 71319, Density, CancellationToken.None);
            Assert.AreEqual(first.Length, second.Length);
            Assert.Greater(first.Length, 150);
            for (int i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i], second[i]);
                Assert.That(first[i].x, Is.GreaterThanOrEqualTo(0).And.LessThan(128));
                Assert.That(first[i].z, Is.GreaterThanOrEqualTo(0).And.LessThan(128));
                Assert.That(first[i].kind, Is.InRange(0, 3));
                Assert.That(first[i].biome, Is.InRange(0, 5));
            }
        }

        [Test]
        public void InitialGroveHasDenseTreeCandidatesWithClearTrails()
        {
            int trees = 0, groundcover = 0;
            for (int z = -1; z <= 1; z++)
                for (int x = -1; x <= 1; x++)
                    foreach (var placement in PropDistribution.Generate(x, z, 128, 280, 71319, Density, CancellationToken.None))
                    {
                        if (placement.kind == 0) trees++;
                        if (placement.kind == 3) groundcover++;
                        float width = placement.kind == 0 ? 3.7f : placement.kind == 1 ? 2.2f : 1.5f;
                        Assert.GreaterOrEqual(WorldMath.PathDistance(x * 128 + placement.x, z * 128 + placement.z), width);
                    }
            Assert.Greater(trees, 900, "Before POI clearings, the initial nine chunks should form a close forest.");
            Assert.Greater(groundcover, 400, "The grove should contain numerous inexpensive colorful groundcover patches.");
        }

        [Test]
        public void ZeroDecorationBudgetReturnsNoCandidates()
        {
            Assert.IsEmpty(PropDistribution.Generate(0, 0, 128, 0, 71319, Density, CancellationToken.None));
        }

        [Test]
        public void CancelledDecorationPlanningStopsBeforeProducingAChunk()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                Assert.Throws<System.OperationCanceledException>(() =>
                    PropDistribution.Generate(0, 0, 128, 280, 71319, Density, source.Token));
            }
        }
    }
}

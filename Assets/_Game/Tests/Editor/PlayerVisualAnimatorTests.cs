using NUnit.Framework;
using UnityEngine;

namespace Ecos.Tests
{
    public sealed class PlayerVisualAnimatorTests
    {
        [Test]
        public void VisualRigKeepsPhysicsRootScaleAndCanResetWithoutPoseDrift()
        {
            var palette = new Palette();
            var actor = LowPolyFactory.Player(palette);
            try
            {
                var animator = actor.AddComponent<PlayerVisualAnimator>();
                animator.Initialize(actor.transform);
                var visuals = actor.transform.Find("VisualRoot");
                Assert.IsNotNull(visuals);
                Assert.IsNotNull(visuals.Find("Leg1/Joelho"));
                Assert.IsNotNull(visuals.Find("TorsoPivot/Arm1/Cotovelo"));
                for (int i = 0; i < 500; i++)
                    animator.Tick(new Vector3(0, 0, 8), true, i > 250, 0, 130, 0, .016f);
                Assert.AreEqual(Vector3.one, actor.transform.localScale);
                animator.ResetPose();
                Assert.AreEqual(Vector3.zero, visuals.localPosition);
                Assert.AreEqual(Quaternion.identity, visuals.localRotation);
                Assert.AreEqual(Vector3.one, visuals.Find("TorsoPivot").localScale);
                for (int i = 0; i < 500; i++) animator.Tick(Vector3.zero, true, false, 0, 0, 0, .016f);
                Assert.Less(visuals.localPosition.sqrMagnitude, .0001f);
                Assert.AreEqual(Vector3.one, actor.transform.localScale);
            }
            finally { Object.DestroyImmediate(actor); palette.Dispose(); }
        }

        [Test]
        public void InitializingVisualRigTwiceDoesNotDuplicateJoints()
        {
            var palette = new Palette();
            var actor = LowPolyFactory.Player(palette);
            try
            {
                var animator = actor.AddComponent<PlayerVisualAnimator>();
                animator.Initialize(actor.transform);
                int before = actor.GetComponentsInChildren<Transform>().Length;
                animator.Initialize(actor.transform);
                Assert.AreEqual(before, actor.GetComponentsInChildren<Transform>().Length);
            }
            finally { Object.DestroyImmediate(actor); palette.Dispose(); }
        }
    }
}

﻿namespace Gu.State.Tests.Internals
{
    using NUnit.Framework;

    public class ReferencePairTests
    {
        [Test]
        public void ReturnsSame()
        {
            var o1 = new object();
            var o2 = new object();
            var pair1 = ReferencePair.GetOrCreate(o1, o2);
            var pair2 = ReferencePair.GetOrCreate(o1, o2);
            Assert.AreSame(pair1, pair2);
            Assert.AreSame(pair1.X, o1);
            Assert.AreSame(pair1.Y, o2);
        }

        [Test]
        public void ReturnsDifferent()
        {
            var o1 = new object();
            var o2 = new object();
            var o3 = new object();
            var pair1 = ReferencePair.GetOrCreate(o1, o2);
            var pair2 = ReferencePair.GetOrCreate(o1, o3);
            Assert.AreNotSame(pair1, pair2);
            Assert.AreSame(pair1.X, o1);
            Assert.AreSame(pair1.Y, o2);

            Assert.AreSame(pair2.X, o1);
            Assert.AreSame(pair2.Y, o3);
        }
    }
}

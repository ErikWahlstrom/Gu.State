// ReSharper disable RedundantArgumentDefaultValue
namespace Gu.State.Tests.DiffTests
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using static DiffTypes;

    public abstract class ReferenceLoopsTests
    {
        public abstract Diff DiffMethod<T>(T x, T y, ReferenceHandling referenceHandling = ReferenceHandling.Structural, string excludedMembers = null, Type excludedType = null) where T : class;

        [Test]
        public void ParentChildCreateWhenParentDirtyLoop()
        {
            var x = new Parent("p1", new Child("c"));
            Assert.AreSame(x, x.Child.Parent);
            Assert.AreSame(x.Child, x.Child.Parent.Child);

            var y = new Parent("p2", new Child("c"));
            Assert.AreSame(y, y.Child.Parent);
            Assert.AreSame(y.Child, y.Child.Parent.Child);

            var expected = this is FieldValues.ReferenceLoops
                               ? "Parent <Child>k__BackingField <Parent>k__BackingField ... <Name>k__BackingField x: p1 y: p2"
                               : "Parent Child Parent ... Name x: p1 y: p2";
            var result = this.DiffMethod(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(false, result.IsEmpty);
            Assert.AreSame(result, result.Diffs.Single(d => d.X == x.Child).Diffs.Single().ValueDiff);
            var actual = result.ToString("", " ");
            Assert.AreEqual(expected, actual);
        }

        [TestCase("p", "c", "Empty")]
        [TestCase("", "c", "Parent <member1> <member3> ... <member2> x: p y: ")]
        [TestCase("p", "", "Parent <member1> <member2> x: c y:  <member3> ...")]
        public void ParentChild(string p, string c, string expected)
        {
            expected = expected?.Replace("<member1>", this is FieldValues.ReferenceLoops ? "<Child>k__BackingField" : "Child")
                                .Replace("<member2>", this is FieldValues.ReferenceLoops ? "<Name>k__BackingField" : "Name")
                                .Replace("<member3>", this is FieldValues.ReferenceLoops ? "<Parent>k__BackingField" : "Parent");
            var x = new Parent("p", new Child("c"));
            Assert.AreSame(x, x.Child.Parent);
            Assert.AreSame(x.Child, x.Child.Parent.Child);

            var y = new Parent(p, new Child(c));
            Assert.AreSame(y, y.Child.Parent);
            Assert.AreSame(y.Child, y.Child.Parent.Child);

            var result = this.DiffMethod(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(expected == "Empty", result.IsEmpty);
            Assert.AreEqual(expected, result.ToString("", " "));

            result = this.DiffMethod(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(expected == "Empty", result.IsEmpty);
            Assert.AreEqual(expected, result.ToString("", " "));
        }

        [Test]
        public void ParentChildWhenTargetChildIsNull()
        {
            var x = new Parent("p", new Child("c"));
            var y = new Parent("p", null);
            var result = this.DiffMethod(x, y, ReferenceHandling.Structural);
            var expected = this is FieldValues.ReferenceLoops
                               ? "Parent <Child>k__BackingField x: Gu.State.Tests.DiffTests.DiffTypes+Child y: null"
                               : "Parent Child x: Gu.State.Tests.DiffTests.DiffTypes+Child y: null";
            Assert.AreEqual(expected, result.ToString("", " "));

            //result = this.EqualMethod(y, x, ReferenceHandling.Structural);
            //Assert.AreEqual(false, result);

            result = this.DiffMethod(x, y, ReferenceHandling.References);
            Assert.AreEqual(false, result.IsEmpty);
            Assert.AreEqual(expected, result.ToString("", " "));
        }

        [Test]
        public void ParentChildWhenSourceChildIsNull()
        {
            var x = new Parent("p", null);
            var y = new Parent("p", new Child("c"));
            var result = this.DiffMethod(x, y, ReferenceHandling.Structural);
            var expected = this is FieldValues.ReferenceLoops
                               ? "Parent <Child>k__BackingField x: null y: Gu.State.Tests.DiffTests.DiffTypes+Child"
                               : "Parent Child x: null y: Gu.State.Tests.DiffTests.DiffTypes+Child";
            Assert.AreEqual(expected, result.ToString("", " "));

            //result = this.EqualMethod(y, x, ReferenceHandling.Structural);
            //Assert.AreEqual(false, result);

            result = this.DiffMethod(x, y, ReferenceHandling.References);
            Assert.AreEqual(false, result.IsEmpty);
            Assert.AreEqual(expected, result.ToString("", " "));
        }
    }
}
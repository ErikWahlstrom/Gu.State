﻿namespace Gu.State.Tests
{
    using System.Collections.Generic;

    using NUnit.Framework;

    using static DirtyTrackerTypes;

    public partial class DirtyTrackerTests
    {
        public class Immutable
        {
            [TestCase(ReferenceHandling.Throw)]
            [TestCase(ReferenceHandling.Structural)]
            public void Tracks(ReferenceHandling referenceHandling)
            {
                var x = new WithImmutableProperty();
                var y = new WithImmutableProperty();
                var changes = new List<string>();
                var expectedChanges = new List<string>();
                using (var tracker = Track.IsDirty(x, y, referenceHandling: referenceHandling))
                {
                    tracker.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
                    Assert.AreEqual(false, tracker.IsDirty);
                    Assert.AreEqual(null, tracker.Diff);
                    CollectionAssert.IsEmpty(changes);

                    x.Name = "newName1";
                    Assert.AreEqual(true, tracker.IsDirty);
                    expectedChanges.AddRange(new[] { "IsDirty", "Diff" });
                    CollectionAssert.AreEqual(expectedChanges, changes);
                    Assert.AreEqual("new[] { typeof(WithImmutableProperty).GetProperty(nameof(x.Name)) }", tracker.Diff.ToString("", " "));

                    y.Name = "newName1";
                    Assert.AreEqual(false, tracker.IsDirty);
                    expectedChanges.AddRange(new[] { "IsDirty", "Diff" });
                    CollectionAssert.AreEqual(expectedChanges, changes);
                    Assert.AreEqual(null, tracker.Diff);

                    x.ImmutableValue = new WithGetReadOnlyPropertySealed<int>(1);
                    Assert.AreEqual(true, tracker.IsDirty);
                    expectedChanges.AddRange(new[] { "IsDirty", "Diff" });
                    CollectionAssert.AreEqual(expectedChanges, changes);
                    CollectionAssert.AreEqual(new[] { typeof(WithImmutableProperty).GetProperty(nameof(x.ImmutableValue)) }, tracker.Diff.ToString("", " "));

                    y.ImmutableValue = new WithGetReadOnlyPropertySealed<int>(1);
                    Assert.AreEqual(false, tracker.IsDirty);
                    expectedChanges.AddRange(new[] { "IsDirty", "Diff" });
                    CollectionAssert.AreEqual(expectedChanges, changes);
                    Assert.AreEqual(null, tracker.Diff);

                    x.ImmutableValue = new WithGetReadOnlyPropertySealed<int>(2);
                    Assert.AreEqual(true, tracker.IsDirty);
                    expectedChanges.AddRange(new[] { "IsDirty", "Diff" });
                    CollectionAssert.AreEqual(expectedChanges, changes);
                    CollectionAssert.AreEqual(new[] { typeof(WithImmutableProperty).GetProperty(nameof(x.ImmutableValue)) }, tracker.Diff.ToString("", " "));
                }
            }
        }
    }
}

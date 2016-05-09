﻿namespace Gu.State
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    internal sealed class DirtyTrackerNode : IDisposable, INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs DiffPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(Diff));
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));

        private readonly IRefCounted<ReferencePair> refCountedPair;
        private readonly IRefCounted<ChangeTrackerNode> xNode;
        private readonly IRefCounted<ChangeTrackerNode> yNode;
        private readonly DisposingMap<IDisposable> children = new DisposingMap<IDisposable>();
        private readonly IRefCounted<DiffBuilder> refcountedDiffBuilder;

        private bool isChanging;
        private bool isResetting;

        private bool isDirty;

        private DirtyTrackerNode(IRefCounted<ReferencePair> refCountedPair, PropertiesSettings settings)
        {
            this.refCountedPair = refCountedPair;
            var x = refCountedPair.Value.X;
            var y = refCountedPair.Value.Y;
            this.xNode = ChangeTrackerNode.GetOrCreate(x, settings);
            this.yNode = ChangeTrackerNode.GetOrCreate(y, settings);
            this.xNode.Value.PropertyChange += this.OnTrackedPropertyChange;
            this.yNode.Value.PropertyChange += this.OnTrackedPropertyChange;

            if (Is.NotifyCollections(x, y))
            {
                this.xNode.Value.Add += this.OnTrackedAdd;
                this.xNode.Value.Remove += this.OnTrackedRemove;
                this.xNode.Value.Replace += this.OnTrackedReplace;
                this.xNode.Value.Move += this.OnTrackedMove;
                this.xNode.Value.Reset += this.OnTrackedReset;

                this.yNode.Value.Add += this.OnTrackedAdd;
                this.yNode.Value.Remove += this.OnTrackedRemove;
                this.yNode.Value.Replace += this.OnTrackedReplace;
                this.yNode.Value.Move += this.OnTrackedMove;
                this.yNode.Value.Reset += this.OnTrackedReset;
            }

            var builder = DiffBuilder.Create(x, y, settings);
            builder.Value.UpdateDiffs(x, y, settings);
            this.refcountedDiffBuilder = builder;
            this.isDirty = !this.Builder.IsEmpty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal event EventHandler<DirtyTrackerChangedEventArgs> Changed;

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChanged?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public ValueDiff Diff => this.Builder?.CreateValueDiff();

        private DiffBuilder Builder => this.refcountedDiffBuilder?.Value;

        private object X => this.xNode.Value.Source;

        private IList XList => (IList)this.X;

        private object Y => this.yNode.Value.Source;

        private IList YList => (IList)this.Y;

        private IReadOnlyCollection<PropertyInfo> TrackProperties => this.xNode.Value.TrackProperties;

        private PropertiesSettings Settings => this.xNode.Value.Settings;

        public void Dispose()
        {
            this.xNode.Value.PropertyChange -= this.OnTrackedPropertyChange;
            this.xNode.Value.Add -= this.OnTrackedAdd;
            this.xNode.Value.Remove -= this.OnTrackedRemove;
            this.xNode.Value.Remove -= this.OnTrackedRemove;
            this.xNode.Value.Move -= this.OnTrackedMove;
            this.xNode.Value.Reset -= this.OnTrackedReset;
            this.xNode.Dispose();

            this.yNode.Value.PropertyChange -= this.OnTrackedPropertyChange;
            this.yNode.Value.Add -= this.OnTrackedAdd;
            this.yNode.Value.Remove -= this.OnTrackedRemove;
            this.yNode.Value.Remove -= this.OnTrackedRemove;
            this.yNode.Value.Move -= this.OnTrackedMove;
            this.yNode.Value.Reset -= this.OnTrackedReset;
            this.yNode.Dispose();

            this.children.Dispose();
            this.refCountedPair.Dispose();
            this.refcountedDiffBuilder.Dispose();
        }

        internal static IRefCounted<DirtyTrackerNode> GetOrCreate(object x, object y, PropertiesSettings settings)
        {
            Debug.Assert(x != null, "Cannot track null");
            Debug.Assert(x is INotifyPropertyChanged || x is INotifyCollectionChanged, "Must notify");
            Debug.Assert(y != null, "Cannot track null");
            Debug.Assert(y is INotifyPropertyChanged || y is INotifyCollectionChanged, "Must notify");
            return TrackerCache.GetOrAdd(x, y, settings, pair => new DirtyTrackerNode(pair, settings));
        }

        private static bool IsTrackablePair(object x, object y, PropertiesSettings settings)
        {
            if (IsNullOrMissing(x) || IsNullOrMissing(y))
            {
                return false;
            }

            return !settings.IsImmutable(x.GetType()) && !settings.IsImmutable(y.GetType());
        }

        private static bool IsNullOrMissing(object x)
        {
            return x == null || x == PaddedPairs.MissingItem;
        }

        private void OnTrackedPropertyChange(object sender, PropertyChangeEventArgs e)
        {
            this.UpdatePropertyChildNode(e.PropertyInfo);
            // we create the builder after subscribing so no guarantee that we have a builder if an event fires before the ctor is finished.
            if (this.Builder == null ||
                this.Settings.IsIgnoringProperty(e.PropertyInfo))
            {
                return;
            }

            this.Builder.UpdateMemberDiff(this.X, this.Y, e.PropertyInfo, this.Settings);
            this.TryRefreshAndNotify(e);
        }

        private void UpdatePropertyChildNode(PropertyInfo propertyInfo)
        {
            if (this.Settings.IsIgnoringProperty(propertyInfo))
            {
                return;
            }

            if (this.TrackProperties.Contains(propertyInfo) &&
               (this.Settings.ReferenceHandling == ReferenceHandling.Structural || this.Settings.ReferenceHandling == ReferenceHandling.StructuralWithReferenceLoops))
            {
                var getter = this.Settings.GetOrCreateGetterAndSetter(propertyInfo);
                var xValue = getter.GetValue(this.X);
                var yValue = getter.GetValue(this.Y);
                var refCounted = this.CreateChild(xValue, yValue, propertyInfo);
                this.children.SetValue(propertyInfo, refCounted);
            }
        }

        private void OnTrackedAdd(object sender, AddEventArgs e)
        {
            this.UpdateIndexChildNode(e.Index);
            this.UpdateIndexDiff(e.Index);
            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedRemove(object sender, RemoveEventArgs e)
        {
            this.UpdateIndexChildNode(e.Index);
            this.UpdateIndexDiff(e.Index);
            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedReplace(object sender, ReplaceEventArgs e)
        {
            this.UpdateIndexChildNode(e.Index);
            this.UpdateIndexDiff(e.Index);
            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedMove(object sender, MoveEventArgs e)
        {
            this.UpdateIndexChildNode(e.FromIndex);
            this.UpdateIndexDiff(e.FromIndex);
            this.UpdateIndexChildNode(e.ToIndex);
            this.UpdateIndexDiff(e.ToIndex);
            this.TryRefreshAndNotify(e);
        }

        private void OnTrackedReset(object sender, ResetEventArgs e)
        {
            this.Builder?.ClearIndexDiffs();
            this.children.ClearIndexTrackers();
            var max = Math.Max(this.XList.Count, this.YList.Count);
            for (var i = 0; i < max; i++)
            {
                this.UpdateIndexChildNode(i);
                this.UpdateIndexDiff(i);
            }

            this.TryRefreshAndNotify(e);
        }

        private void UpdateIndexChildNode(int index)
        {
            var xValue = this.XList.ElementAtOrMissing(index);
            var yValue = this.YList.ElementAtOrMissing(index);

            if (IsTrackablePair(xValue, yValue, this.Settings) &&
               (this.Settings.ReferenceHandling == ReferenceHandling.Structural || this.Settings.ReferenceHandling == ReferenceHandling.StructuralWithReferenceLoops))
            {
                var refCounted = this.CreateChild(xValue, yValue, index);
                this.children.SetValue(index, refCounted);
            }
            else
            {
                this.children.SetValue(index, null);
            }
        }

        private void UpdateIndexDiff(int index)
        {
            // we create the builder after subscribing so no guarantee that we have a builder if an event fires before the ctor is finished.
            if (this.Builder == null)
            {
                return;
            }

            var xValue = this.XList.ElementAtOrMissing(index);
            var yValue = this.YList.ElementAtOrMissing(index);
            this.Builder.UpdateIndexDiff(xValue, yValue, index, this.Settings);
        }

        private IUnsubscriber CreateChild(object xValue, object yValue, object key)
        {
            if (xValue == null || yValue == null)
            {
                return null;
            }

            var childNode = GetOrCreate(xValue, yValue, this.Settings);
            EventHandler<DirtyTrackerChangedEventArgs> onChanged = (sender, args) => this.OnChildChanged(sender, args, key);
            childNode.Value.Changed += onChanged;
            return childNode.AsUnsubscribeOnDispose(x => x.Value.Changed -= onChanged);
        }

        // ReSharper disable once UnusedParameter.Local
        private void OnChildChanged(object _, DirtyTrackerChangedEventArgs e, object key)
        {
            if (e.Contains(this) || this.Builder == null)
            {
                return;
            }

            var propertyInfo = key as PropertyInfo;
            if (propertyInfo != null)
            {
                if (this.Settings.IsIgnoringProperty(propertyInfo))
                {
                    return;
                }
            }

            this.Builder.TryRefresh(null);
            this.PropertyChanged?.Invoke(this, DiffPropertyChangedEventArgs);
            this.IsDirty = !this.Builder.IsEmpty;
            this.Changed?.Invoke(this, e.With(this, key));
        }

        private void TryRefreshAndNotify(object propertyOrIndex)
        {
            if (this.Builder != null && this.Builder.TryRefresh(this.Settings))
            {
                this.TryNotifyChanges(propertyOrIndex);
            }
        }

        private bool TryNotifyChanges(object propertyOrIndex)
        {
            if (this.Builder == null)
            {
                return false;
            }

            this.PropertyChanged?.Invoke(this, DiffPropertyChangedEventArgs);
            this.IsDirty = !this.Builder.IsEmpty;

            if (!this.isChanging)
            {
                this.Changed?.Invoke(this, new DirtyTrackerChangedEventArgs(this, propertyOrIndex));
            }

            return true;
        }
    }
}

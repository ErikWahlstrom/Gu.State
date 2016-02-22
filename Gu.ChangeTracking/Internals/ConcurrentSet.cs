namespace Gu.ChangeTracking
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal class ConcurrentSet<T> : ICollection<T>
    {
        private readonly ConcurrentDictionary<T, int> inner;

        public ConcurrentSet()
        {
            this.inner = new ConcurrentDictionary<T, int>();
        }

        public ConcurrentSet(IEqualityComparer<T> comparer)
        {
            this.inner = new ConcurrentDictionary<T, int>(comparer);
        }

        public int Count => this.inner.Count;

        public bool IsReadOnly => false;

        public bool Add(T item) => this.inner.TryAdd(item, 0);

        public void Clear() => this.inner.Clear();

        void ICollection<T>.Add(T item) => this.Add(item);

        public bool Contains(T item) => this.inner.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex) => this.inner.Keys.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            int temp;
            return this.inner.TryRemove(item, out temp);
        }

        public IEnumerator<T> GetEnumerator() => this.inner.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
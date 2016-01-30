namespace Gu.ChangeTracking
{
    using System.ComponentModel;

    public static class DirtyTracker
    {
        public static DirtyTracker<T> Track<T>(T x, T y, params string[] ignoreProperties)
            where T : class, INotifyPropertyChanged
        {
            return new DirtyTracker<T>(x, y, ignoreProperties);
        }

        public static void Verify<T>(params string[] ignoreProperties)
            where T : class, INotifyPropertyChanged
        {
            DirtyTracker<T>.Verify(ignoreProperties);
        }
    }
}
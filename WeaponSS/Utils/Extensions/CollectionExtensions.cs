namespace WeaponStatShower.Utils.Extensions
{
    public static class CollectionExtensions
    {
        public static void ExpandToSize<T>(this List<T> list, int count, T defaultValue)
        {
            list.EnsureCapacity(count);
            while (list.Count < count)
                list.Add(defaultValue);
        }
    }
}

using QLN.DataMigration.Models;

public class DrupalCategoryTidComparer : IEqualityComparer<DrupalCategory?>
{
    public bool Equals(DrupalCategory? x, DrupalCategory? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Tid == y.Tid;
    }

    public int GetHashCode(DrupalCategory obj)
    {
        return obj.Tid.GetHashCode();
    }
}

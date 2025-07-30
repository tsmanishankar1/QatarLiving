using QLN.DataMigration.Models;

public class DrupalLocationTidComparer : IEqualityComparer<DrupalLocation?>
{
    public bool Equals(DrupalLocation? x, DrupalLocation? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Tid == y.Tid;
    }

    public int GetHashCode(DrupalLocation obj)
    {
        return obj.Tid.GetHashCode();
    }
}
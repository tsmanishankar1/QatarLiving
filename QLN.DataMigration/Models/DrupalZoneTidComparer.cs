using QLN.DataMigration.Models;

public class DrupalZoneTidComparer : IEqualityComparer<DrupalZone?>
{
    public bool Equals(DrupalZone? x, DrupalZone? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Tid == y.Tid;
    }

    public int GetHashCode(DrupalZone obj)
    {
        return obj.Tid.GetHashCode();
    }
}
using QLN.DataMigration.Models;

public class DrupalOfferTidComparer : IEqualityComparer<DrupalOffer?>
{
    public bool Equals(DrupalOffer? x, DrupalOffer? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Tid == y.Tid;
    }

    public int GetHashCode(DrupalOffer obj)
    {
        return obj.Tid.GetHashCode();
    }
}
namespace Fake.EntityFrameworkCore.ValueCompares;

public class ExtraPropertiesValueComparer() : ValueComparer<ExtraProperties>(
    (d1, d2) => Compare(d1, d2),
    d => d.Aggregate(0, (k, v) => HashCode.Combine(k, v.GetHashCode())),
    d => new ExtraProperties(d))
{
    private static bool Compare(ExtraProperties? a, ExtraProperties? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null)
        {
            return b is null;
        }
        
        if (b is null)
        {
            return false;
        }

        return a.SequenceEqual(b);
    }
}
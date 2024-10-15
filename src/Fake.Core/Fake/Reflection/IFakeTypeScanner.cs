namespace Fake.Reflection;

public interface IFakeTypeScanner
{
    IReadOnlyList<Type> Scan();
}
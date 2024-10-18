namespace Fake.Data;

public interface IHasExtraProperties
{
    /// <summary>
    /// 拓展属性
    /// </summary>
    ExtraProperties ExtraProperties { get; }
}

[Serializable]
public class ExtraProperties : Dictionary<string, object?>
{
    public ExtraProperties()
    {
    }

    public ExtraProperties(IDictionary<string, object?> dictionary)
        : base(dictionary)
    {
    }
}
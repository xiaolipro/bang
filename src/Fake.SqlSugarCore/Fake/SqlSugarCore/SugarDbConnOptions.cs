namespace Fake.SqlSugarCore;

public class SugarDbConnOptions<TDbContext> : SugarDbConnOptions where TDbContext : SugarDbContext<TDbContext>
{
}

public class SugarDbConnOptions
{
    public string ConfigId { get; set; } = "default";

    /// <summary>
    /// 连接字符串(如果开启多租户，也就是默认库了)，必填
    /// </summary>
    public string ConnectionString { get; set; } = null!;

    private List<string> _readConnectionStrings = [];

    /// <summary>
    /// 读写分离
    /// </summary>
    public List<string> ReadConnectionStrings
    {
        get => _readConnectionStrings;
        set => _readConnectionStrings = value;
    }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DbType DbType { get; set; }

    /// <summary>
    /// 自动关闭连接
    /// </summary>
    public bool IsAutoCloseConnection { get; set; } = true;

    /// <summary>
    /// 超时时间s
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// 开启sql日志
    /// </summary>
    public bool EnabledSqlLog { get; set; } = true;
}
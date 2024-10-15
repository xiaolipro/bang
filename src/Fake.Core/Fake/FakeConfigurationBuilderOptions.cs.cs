namespace Fake;

public class FakeConfigurationBuilderOptions
{
    /// <summary>
    /// 用户机密id，全局唯一
    /// </summary>
    public string? UserSecretsId { get; set; }

    /// <summary>
    /// 配置文件名，默认：appsettings
    /// </summary>
    public string FileName { get; set; } = "appsettings";

    /// <summary>
    /// 文件是否可选，默认值：true。
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// 如果文件更改，是否重新加载配置，默认值：true。
    /// </summary>
    public bool ReloadOnChange { get; set; } = true;

    /// <summary>
    /// 环境名，例如 "Development", "Staging" or "Production"。
    /// </summary>
    public string EnvironmentName { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="FileName"/> 所在目录。
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// 命令行参数
    /// </summary>
    public string[] CommandLineArgs { get; set; } = [];
}
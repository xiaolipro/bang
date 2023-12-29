﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fake.Auditing;

public class FakeAuditingOptions
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// 启用审计日志
    /// </summary>
    public bool IsEnabledLog { get; set; } = true;

    /// <summary>
    /// 启用Action审计日志
    /// </summary>
    public bool IsEnabledActionLog { get; set; } = true;

    /// <summary>
    /// 启用异常审计日志
    /// </summary>
    public bool IsEnabledExceptionLog { get; set; } = true;

    /// <summary>
    /// 启用Get请求审计日志
    /// </summary>
    public bool IsEnabledGetRequestLog { get; set; } = true;

    /// <summary>
    /// 允许匿名
    /// </summary>
    public bool AllowAnonymous { get; set; } = true;

    /// <summary>
    /// 日志选择器
    /// </summary>
    public List<Func<AuditLogInfo, Task<bool>>> LogSelectors { get; } = new();

    /// <summary>
    /// 日志贡献者
    /// </summary>
    public List<AuditLogContributor> Contributors { get; } = new();

    /// <summary>
    /// 实体变更配置
    /// </summary>
    public EntityChangeOptions EntityChangeOptions { get; set; } = new();
}
﻿namespace Fake.Domain.Entities.Auditing;

public interface ISoftDelete
{
    /// <summary>
    /// 已经删除
    /// </summary>
    bool IsDeleted { get; }
}
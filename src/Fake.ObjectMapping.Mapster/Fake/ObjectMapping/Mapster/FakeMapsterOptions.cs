﻿using Fake.Modularity;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fake.ObjectMapping.Mapster;

public class FakeMapsterOptions
{
    public TypeAdapterConfig TypeAdapterConfig { get; set; }

    public List<IRegister> Registers { get; set; }

    public FakeMapsterOptions()
    {
        TypeAdapterConfig = new TypeAdapterConfig();
        Registers = new List<IRegister>();
    }

    /// <summary>
    /// 注册模块中的IRegister
    /// </summary>
    /// <typeparam name="TModule">要注册IRegister的模块</typeparam>
    public void AddMaps<TModule>() where TModule : IFakeModule
    {
        var assembly = typeof(TModule).Assembly;

        var list = assembly
            .DefinedTypes
            .Where(x => typeof(IRegister)
                .GetTypeInfo()
                .IsAssignableFrom(x.GetTypeInfo()) && x.GetTypeInfo().IsClass && !x.GetTypeInfo().IsAbstract)
            .Select(registerType => (IRegister)Activator.CreateInstance(registerType))
            .ToList();

        Registers.AddRange(list);

        if (Registers.Count <= 0) return;
        TypeAdapterConfig.Apply(Registers);
        //推荐在程序添加映射配置完成后调用一次 Compile 方法，可以快速验证 映射配置中是否存在错误，而不是在运行到某一行业务代码时触发错误降低效率。
        TypeAdapterConfig.Compile();
    }
}
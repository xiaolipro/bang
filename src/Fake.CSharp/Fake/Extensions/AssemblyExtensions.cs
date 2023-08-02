﻿using System.Reflection;

namespace Fake.CSharp.Fake.Extensions;

public static class AssemblyExtensions
{
    public static string GetUniqueName(this AssemblyName assemblyName)
    {
        return string.IsNullOrWhiteSpace(assemblyName.Name) ? assemblyName.FullName : assemblyName.Name;
    }
}

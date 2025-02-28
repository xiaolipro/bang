﻿using Fake.Domain.Events;
using Fake.EntityFrameworkCore.ValueCompares;
using Fake.EntityFrameworkCore.ValueConverters;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.EntityFrameworkCore.Modeling;

public static class FakeEntityTypeBuilderExtensions
{
    public static EntityTypeBuilder TryConfigureByConvention(this EntityTypeBuilder builder)
    {
        return builder.TryConfigureCreator()
            .TryConfigureModifier()
            .TryConfigureSoftDelete()
            .TryConfigureExtraProperties()
            .TryConfigureAggregateRoot();
    }

    public static EntityTypeBuilder TryConfigureAggregateRoot(this EntityTypeBuilder builder)
    {
        builder.Ignore(nameof(IHasDomainEvent.DomainEvents));
        
        if (builder.Metadata.ClrType.IsAssignableTo<IAggregateRoot>())
        {
            builder.Property(nameof(IAggregateRoot.ConcurrencyStamp))
                .HasColumnName(nameof(IAggregateRoot.ConcurrencyStamp))
                // 当SaveChanges时，它会自动检查并发标记的值是否与原值匹配，如果不匹配就会抛出DbUpdateConcurrencyException异常
                .IsConcurrencyToken()
                .IsRequired();
        }

        return builder;
    }

    public static EntityTypeBuilder TryConfigureExtraProperties(this EntityTypeBuilder builder)
    {
        if (!builder.Metadata.ClrType.IsAssignableTo<IHasExtraProperties>())
        {
            return builder;
        }

        builder.Property(nameof(IHasExtraProperties.ExtraProperties))
            .HasColumnName(nameof(IHasExtraProperties.ExtraProperties))
            .HasConversion(new FakeJsonValueConverter<ExtraProperties>())
            .Metadata.SetValueComparer(new ExtraPropertiesValueComparer());

        return builder;
    }

    public static EntityTypeBuilder TryConfigureCreator(this EntityTypeBuilder builder)
    {
        if (builder.Metadata.ClrType.IsAssignableTo(typeof(IHasCreateUserId)))
        {
            builder.Property(nameof(IHasCreateUserId.CreateUserId))
                .HasColumnName(nameof(IHasCreateUserId.CreateUserId));
        }
        
        if (builder.Metadata.ClrType.IsAssignableTo(typeof(IHasCreateUserId<>)))
        {
            builder.Property(nameof(IHasCreateUserId<AnyKey>.CreateUserId))
                .HasColumnName(nameof(IHasCreateUserId<AnyKey>.CreateUserId))
                .IsRequired();
        }

        if (builder.Metadata.ClrType.IsAssignableTo<IHasCreateTime>())
        {
            builder.Property(nameof(IHasCreateTime.CreateTime))
                .HasColumnName(nameof(IHasCreateTime.CreateTime))
                .IsRequired();
        }

        return builder;
    }

    public static EntityTypeBuilder TryConfigureModifier(this EntityTypeBuilder builder)
    {
        if (builder.Metadata.ClrType.IsAssignableTo<IHasUpdateUserId>())
        {
            builder.Property(nameof(IHasUpdateUserId.UpdateUserId))
                .HasColumnName(nameof(IHasUpdateUserId.UpdateUserId))
                .IsRequired();
        }
        
        if (builder.Metadata.ClrType.IsAssignableTo(typeof(IHasUpdateUserId<>)))
        {
            builder.Property(nameof(IHasUpdateUserId<AnyKey>.UpdateUserId))
                .HasColumnName(nameof(IHasUpdateUserId<AnyKey>.UpdateUserId))
                .IsRequired();
        }
        
        if (builder.Metadata.ClrType.IsAssignableTo<IHasUpdateTime>())
        {
            builder.Property(nameof(IHasUpdateTime.UpdateTime))
                .HasColumnName(nameof(IHasUpdateTime.UpdateTime))
                .IsRequired();
        }

        return builder;
    }

    public static EntityTypeBuilder TryConfigureSoftDelete(this EntityTypeBuilder builder)
    {
        if (builder.Metadata.ClrType.IsAssignableTo<ISoftDelete>())
        {
            builder.Property(nameof(ISoftDelete.IsDeleted))
                .IsRequired()
                .HasDefaultValue(false)
                .HasColumnName(nameof(ISoftDelete.IsDeleted));
        }

        return builder;
    }
}
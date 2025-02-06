using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Fake.Helpers;
using Fake.Timing;
using Fake.Users;

namespace Fake.Domain.Entities.Auditing;

public class DefaultAuditPropertySetter(ICurrentUser currentUser, IFakeClock fakeClock) : IAuditPropertySetter
{
    private static readonly ConcurrentDictionary<string, Action<DefaultAuditPropertySetter, IEntity>>
        CachedExpressions = new();

    public void SetCreationProperties(IEntity entity)
    {
        if (entity is IHasCreateTime entityWithCreationTime)
        {
            if (entityWithCreationTime.CreateTime == default)
            {
                ReflectionHelper.TrySetProperty(entityWithCreationTime, x => x.CreateTime, () => fakeClock.Now);
            }
        }

        if (entity is IHasCreateUserId entityWithCreateUserId && entityWithCreateUserId.CreateUserId == default)
        {
            ReflectionHelper.TrySetProperty(entityWithCreateUserId, x => x.CreateUserId,
                () => currentUser.Id ?? default);
        }
        else
        {
            var hasCreateUserId = entity.GetType().GetInterface(typeof(IHasCreateUserId<>).Name);
            if (hasCreateUserId != null)
            {
                // 接近原生调用方法的性能
                GetGenericMethodExpression(hasCreateUserId.GetGenericArguments()[0], true).Invoke(this, entity);
            }
        }
    }

    public void SetCreateUserId<T>(IEntity entity) where T : struct
    {
        if (entity is IHasCreateUserId<Guid> entityWithGuidUserId &&
            entityWithGuidUserId.CreateUserId == default)
        {
            ReflectionHelper.TrySetProperty(entityWithGuidUserId, x => x.CreateUserId, () => currentUser.Id ?? default);
        }

        if (entity is IHasCreateUserId<T> entityWithGenericUserId &&
            entityWithGenericUserId.CreateUserId.Equals(default(T)))
        {
            ReflectionHelper.TrySetProperty(entityWithGenericUserId, x => x.CreateUserId,
                () => currentUser.GetUserIdOrNull<T>() ?? default);
        }
    }

    public void SetModificationProperties(IEntity entity)
    {
        if (entity is IHasUpdateTime entityWithModificationTime)
        {
            ReflectionHelper.TrySetProperty(entityWithModificationTime, x => x.UpdateTime,
                () => fakeClock.Now);
        }

        if (entity is IHasUpdateUserId entityWithUpdateUserId && entityWithUpdateUserId.UpdateUserId == default)
        {
            ReflectionHelper.TrySetProperty(entityWithUpdateUserId, x => x.UpdateUserId,
                () => currentUser.Id ?? default);
        }
        else
        {
            var hasUpdateUserId = entity.GetType().GetInterface(typeof(IHasUpdateUserId<>).Name);
            if (hasUpdateUserId != null)
            {
                GetGenericMethodExpression(hasUpdateUserId.GetGenericArguments()[0], false).Invoke(this, entity);
            }
        }
    }

    public void SetUpdateUserId<T>(IEntity entity) where T : struct
    {
        if (entity is IHasUpdateUserId<Guid> entityWithGuidUserId &&
            entityWithGuidUserId.UpdateUserId == default)
        {
            ReflectionHelper.TrySetProperty(entityWithGuidUserId, x => x.UpdateUserId, () => currentUser.Id ?? default);
        }

        if (entity is IHasUpdateUserId<T> entityWithGenericUserId &&
            entityWithGenericUserId.UpdateUserId.Equals(default(T)))
        {
            ReflectionHelper.TrySetProperty(entityWithGenericUserId, x => x.UpdateUserId,
                () => currentUser.GetUserIdOrNull<T>() ?? default);
        }
    }

    public void SetSoftDeleteProperty(IEntity entity)
    {
        if (entity is not ISoftDelete entityWithSoftDelete) return;

        ReflectionHelper.TrySetProperty(entityWithSoftDelete, x => x.IsDeleted, () => true);
    }


    private Action<DefaultAuditPropertySetter, IEntity> GetGenericMethodExpression(Type genericType, bool isCreate)
    {
        var key = $"{genericType.Name}_{isCreate}";
        // ReSharper disable once HeapView.CanAvoidClosure
        return CachedExpressions.GetOrAdd(key, _ =>
            {
                var method = GetType()
                    .GetMethod(isCreate ? nameof(SetCreateUserId) : nameof(SetUpdateUserId),
                        BindingFlags.Public | BindingFlags.Instance)!
                    .MakeGenericMethod(genericType);

                // 构建表达式树
                var instanceParam = Expression.Parameter(typeof(DefaultAuditPropertySetter), "instance");
                var entityParam = Expression.Parameter(typeof(IEntity), "entity");

                var callExpression = Expression.Call(instanceParam, method, entityParam);

                return Expression
                    .Lambda<Action<DefaultAuditPropertySetter, IEntity>>(callExpression, instanceParam, entityParam)
                    .Compile();
            });
    }
}
using System.Linq.Expressions;

namespace Fake.Helpers;

public static class ExpressionHelper
{
    public static Expression<Func<T, bool>> Combine<T>(Expression<Func<T, bool>> expression1,
        Expression<Func<T, bool>> expression2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expression1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expression1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expression2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expression2.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }

    private class ReplaceExpressionVisitor(Expression oldValue, Expression newValue) : ExpressionVisitor
    {
        public override Expression Visit(Expression? node)
        {
            return node == oldValue ? newValue : base.Visit(node)!;
        }
    }
}
using System.Collections;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace KunstButikken.AuctionService.Tests.TestDoubles;

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
}

internal sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression)
    {
        var stripped = StripIncludeCalls(expression);
        try
        {
            return _inner.Execute(stripped);
        }
        catch (ArgumentException)
        {
            // Fallback: compile and execute the expression directly
            var lambda = Expression.Lambda(stripped);
            return lambda.Compile().DynamicInvoke();
        }
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var stripped = StripIncludeCalls(expression);
        try
        {
            return _inner.Execute<TResult>(stripped);
        }
        catch (ArgumentException)
        {
            // Fallback: compile and execute the expression directly
            // Get the actual result type (handling Task<T> if present)
            var resultType = typeof(TResult);
            var isTaskResult = resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>);

            if (isTaskResult)
            {
                // Get the inner type of Task<T>
                var innerType = resultType.GetGenericArguments()[0];
                // Compile and execute the expression
                var lambda = Expression.Lambda(stripped);
                var result = lambda.Compile().DynamicInvoke();

                // Wrap in Task.FromResult
                var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(innerType);
                return (TResult)fromResultMethod.Invoke(null, new[] { result })!;
            }
            else
            {
                var lambda = Expression.Lambda<Func<TResult>>(stripped);
                return lambda.Compile()();
            }
        }
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        => new TestAsyncEnumerable<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        => Execute<TResult>(expression);

    private static Expression StripIncludeCalls(Expression expression)
    {
        return new IncludeRemover().Visit(expression);
    }

    private class IncludeRemover : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
                (node.Method.Name == nameof(EntityFrameworkQueryableExtensions.Include) ||
                 node.Method.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude)))
            {
                // Return the source queryable, effectively removing the Include call
                return Visit(node.Arguments[0]);
            }

            return base.VisitMethodCall(node);
        }
    }
}

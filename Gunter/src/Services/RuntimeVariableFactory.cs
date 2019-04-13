using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Gunter.Data;
using JetBrains.Annotations;

namespace Gunter.Services
{
    public static class RuntimeVariableFactory
    {
        [NotNull]
        public static IRuntimeVariable Create<T>(Expression<Func<T, object>> getValueExpression)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var converted = RuntimeVariableValueConverter<T>.Convert(getValueExpression.Body, parameter);
            var getValueFunc = Expression.Lambda<Func<object, object>>(converted, parameter).Compile();

            return new RuntimeVariable
            (
                typeof(T),
                CreateName(getValueExpression),
                getValueFunc
            );
        }

        [NotNull]
        public static IRuntimeVariable Create(Expression<Func<object>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;

            return new RuntimeVariable
            (
                memberExpression.Member.DeclaringType,
                CreateName(expression),
                _ => expression.Compile()()
            );
        }

        private static string CreateName(Expression expression)
        {
            expression = expression is LambdaExpression lambda ? lambda.Body : expression;

            while (true)
            {
                switch (expression)
                {
                    case MemberExpression memberExpression:
                        // ReSharper disable once PossibleNullReferenceException
                        // For member expression the DeclaringType cannot be null.
                        var typeName = memberExpression.Member.DeclaringType.Name;
                        if (memberExpression.Member.DeclaringType.IsInterface)
                        {
                            // Remove the leading "I" from an interface name.
                            typeName = Regex.Replace(typeName, "^I", string.Empty);
                        }

                        return $"{typeName}.{memberExpression.Member.Name}";

                    // Value types are wrapped by Convert(x) which is an unary-expression.
                    case UnaryExpression unaryExpression:
                        expression = unaryExpression.Operand;
                        continue;
                }

                // There is an unary-expression when using interfaces.

                throw new ArgumentException("Member expression not found.");
            }
        }
    }

    internal class RuntimeVariableValueConverter<T> : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        private RuntimeVariableValueConverter(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        public static Expression Convert(Expression expression, ParameterExpression parameter)
        {
            return new RuntimeVariableValueConverter<T>(parameter).Visit(expression);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return Expression.Convert(_parameter, typeof(T));
        }
    }
}
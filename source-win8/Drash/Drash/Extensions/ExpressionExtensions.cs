using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Drash.Extensions
{
    public static class ExpressionExtensions
    {

        ///<summary>
        /// Turns a string property name into an Expression property selector.
        ///</summary>
        ///<param name="propertyName"></param>
        ///<typeparam name="TOwner"></typeparam>
        ///<exception cref="ArgumentException">propertyName does not exist on TOwner</exception>
        ///<returns></returns>
        public static Expression<Func<TOwner, object>> ToPropertySelector<TOwner>(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");
            var parameter = Expression.Parameter(typeof(TOwner), "x");
            var property = Expression.Property(parameter, propertyName);
            var convertedProperty = Expression.Convert(property, typeof(object));
            var lambda = Expression.Lambda(convertedProperty, new[] { parameter });
            return ((Expression<Func<TOwner, object>>)lambda);
        }

        /// <summary>
        /// Gets the member info represented by an expression.
        /// </summary>
        /// <param name="expression">The member expression.</param>
        /// <returns>The member info represented by the expression.</returns>
        public static MemberInfo GetMemberInfo(this Expression expression)
        {
            var lambda = (LambdaExpression)expression;

            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
                memberExpression = (MemberExpression)lambda.Body;

            return memberExpression.Member;
        }


        /// <summary>
        /// Invokes the getter of the expression representing a property selector on the given instance.
        /// This property selector can have multiple levels like, x => x.LevelOne.LevelTwo
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="propertySelector">The expression representing the property selector</param>
        /// <param name="instance">The instance to invoke the getter on</param>
        /// <returns>The value of the property selector</returns>
        public static TValue InvokeGetter<TInstance, TValue>(this Expression<Func<TInstance, TValue>> propertySelector, TInstance instance)
            where TInstance : class
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");
            if (instance == null)
                throw new ArgumentNullException("instance");
            return propertySelector.Compile().Invoke(instance);
        }


        /// <summary>
        /// Invokes the setter of the expression representing a property selector on the given instance.
        /// The given value will be set.
        /// This property selector can have multiple levels like, x => x.LevelOne.LevelTwo
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="propertySelector">The expression representing the property selector</param>
        /// <param name="instance">The instance to invoke the getter on</param>
        /// <param name="value">The value to be set</param>
        public static void InvokeSetter<TInstance, TValue>(this Expression<Func<TInstance, TValue>> propertySelector, TInstance instance, TValue value)
            where TInstance : class
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");
            if (instance == null)
                throw new ArgumentNullException("instance");
            // re-write in .NET 4.0 as a "set"
            var memberExpression = (MemberExpression)propertySelector.Body;
            var param = Expression.Parameter(typeof(TValue), "value");
            var setter = Expression.Lambda<Action<TInstance, TValue>>(Expression.Assign(memberExpression, param), propertySelector.Parameters[0], param);
            var setterMethod = setter.Compile();
            setterMethod.Invoke(instance, value);
        }


        /// <summary>
        /// Same as InvokeSetter, but not type safe on the value to set.
        /// Use with care, when unable to specify the TValue generic.
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="propertySelector"></param>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        public static void InvokeSetterNotTypeSafe<TInstance>(this Expression<Func<TInstance, object>> propertySelector, TInstance instance, object value)
             where TInstance : class
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");
            if (instance == null)
                throw new ArgumentNullException("instance");
            // re-write in .NET 4.0 as a "set"
            var memberExpression = (MemberExpression)propertySelector.Body;
            var param = Expression.Parameter(memberExpression.Type, "value");
            var paramExpression = propertySelector.Parameters[0];
            var delegateType = typeof(Action<,>);
            delegateType = delegateType.MakeGenericType(typeof(TInstance), memberExpression.Type);
            var setter = Expression.Lambda(delegateType, Expression.Assign(memberExpression, param), paramExpression, param);
            var setterMethod = setter.Compile();
            setterMethod.DynamicInvoke(instance, value);
        }


        /// <summary>
        /// Returns the path represented by a property selector expression.
        /// For instance "x => x.Person.Address" returns "Person.Address"
        /// </summary>
        /// <param name="propertySelector">The property selector</param>
        /// <returns>A string representing the full property path</returns>
        public static string ToPropertyPath(this Expression propertySelector)
        {
            switch (propertySelector.NodeType)
            {
                case ExpressionType.Lambda:
                    var lambdaExpression = (LambdaExpression)propertySelector;
                    return ToPropertyPath(lambdaExpression.Body);
                case ExpressionType.Quote:
                    var unaryExpression = (UnaryExpression)propertySelector;
                    return ToPropertyPath(unaryExpression.Operand);
                case ExpressionType.Convert:
                    var unaryExpressionn = (UnaryExpression)propertySelector;
                    return ToPropertyPath(unaryExpressionn.Operand);
                case ExpressionType.Parameter:
                    return string.Empty;
                case ExpressionType.MemberAccess:
                    var expression = ((MemberExpression)propertySelector).Expression;
                    var pre = ToPropertyPath(expression);
                    MemberInfo propertyInfo = ((MemberExpression)propertySelector).Member;
                    return (string.IsNullOrEmpty(pre)) ? propertyInfo.Name : pre + "." + propertyInfo.Name;
            }
            throw new InvalidOperationException("Expression must be a member expression, quote, convert, parameter or lambda: " + propertySelector);
        }
    }
}


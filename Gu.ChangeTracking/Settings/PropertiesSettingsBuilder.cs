﻿namespace Gu.ChangeTracking
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    using Gu.ChangeTracking.Internals;

    public class PropertiesSettingsBuilder
    {
        private readonly HashSet<Type> ignoredTypes = new HashSet<Type>();
        private readonly HashSet<PropertyInfo> ignoredProperties = new HashSet<PropertyInfo>(MemberInfoComparer<PropertyInfo>.Default);

        public PropertiesSettings CreateSettings(ReferenceHandling referenceHandling = ReferenceHandling.Throw, BindingFlags bindingFlags = Constants.DefaultPropertyBindingFlags)
        {
            if (this.ignoredProperties.Count == 0 && this.ignoredTypes == null)
            {
                return PropertiesSettings.GetOrCreate(bindingFlags, referenceHandling);
            }

            return new PropertiesSettings(this.ignoredProperties, this.ignoredTypes, bindingFlags, referenceHandling);
        }

        public PropertiesSettingsBuilder AddImmutableType<T>()
        {
            return this.AddImmutableType(typeof(T));
        }

        public PropertiesSettingsBuilder AddImmutableType(Type type)
        {
            if (!this.ignoredTypes.Add(type))
            {
                var message = $"Already added type: {type.FullName}\r\n" +
                              $"Nested properties are not allowed";
                throw new ArgumentException(message);
            }

            return this;
        }

        public PropertiesSettingsBuilder AddIgnoredProperty(PropertyInfo property)
        {
            if (!this.ignoredProperties.Add(property))
            {
                var message = $"Already added property: {property.DeclaringType?.FullName}.{property.Name}\r\n" +
                              $"Nested properties are not allowed";
                throw new ArgumentException(message);
            }

            return this;
        }

        public PropertiesSettingsBuilder AddIgnoredProperty<TSource>(string name)
        {
            var propertyInfo = typeof(TSource).GetProperty(name, Constants.DefaultFieldBindingFlags);
            if (propertyInfo == null)
            {
                var message = $"{name} must be a property on {typeof(TSource).Name}\r\n" +
                              $"Nested properties are not allowed";
                throw new ArgumentException(message);
            }
            return this.AddIgnoredProperty(propertyInfo);
        }

        /// <summary>
        /// Sample: AddExplicitProperty{<typeparamref name="TSource"/>}(x => x.Bar)
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="property"></param>
        public PropertiesSettingsBuilder AddIgnoredProperty<TSource>(Expression<Func<TSource, object>> property)
        {
            var memberExpression = property.Body as MemberExpression;
            if (memberExpression == null)
            {
                if (property.Body.NodeType == ExpressionType.Convert)
                {
                    memberExpression = (property.Body as UnaryExpression)?.Operand as MemberExpression;
                }
            }

            if (memberExpression == null)
            {
                var message = $"{nameof(property)} must be a property expression like foo => foo.Bar\r\n" +
                              $"Nested properties are not allowed";
                throw new ArgumentException(message);
            }

            if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
            {
                var message = $"{nameof(property)} must be a property expression like foo => foo.Bar\r\n" +
                              $"Nested properties are not allowed";
                throw new ArgumentException(message);
            }

            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                var message = $"{nameof(property)} must be a property expression like foo => foo.Bar";
                throw new ArgumentException(message);
            }

            this.AddIgnoredProperty(propertyInfo);
            return this;
        }
    }
}
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace TX11Shared
{
    public static class XConnector
    {
        [NotNull]
        private static readonly Dictionary<Type, object> SingletonObjects = new Dictionary<Type, object>();

        [NotNull]
        private static readonly Dictionary<Type, Func<object>> FactoryMethods = new Dictionary<Type, Func<object>>();

        public static void Register(Type t, object value)
        {
            if (SingletonObjects.TryGetValue(t, out var temp) && ReferenceEquals(temp, value))
            {
                return;
            }

            SingletonObjects.Add(t, value);
        }

        public static void RegisterFactoryMethodFor<T>(Func<object> factory)
        {
            var test = factory?.Invoke();
            if (test == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            FactoryMethods.Add(typeof(T), factory);
        }

        public static T Resolve<T>()
        {
            if (SingletonObjects.TryGetValue(typeof(T), out var value))
            {
                return (T) value;
            }

            return default(T);
        }

        [NotNull]
        public static T GetInstanceOf<T>()
        {
            if (FactoryMethods.TryGetValue(typeof(T), out var func) && func != null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute -- checked before
                return (T) func();
            }

            throw new KeyNotFoundException(nameof(T));
        }
    }
}
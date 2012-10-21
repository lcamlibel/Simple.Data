using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Simple.Data
{
    internal class ConcreteTypeCreator
    {
        private static readonly Dictionary<Type, ConcreteTypeCreator> Creators;
        private static readonly ICollection CreatorsCollection;
        private readonly Lazy<Func<IDictionary<string, object>, object>> _func;

        static ConcreteTypeCreator()
        {
            CreatorsCollection = Creators = new Dictionary<Type, ConcreteTypeCreator>();
        }

        private ConcreteTypeCreator(Lazy<Func<IDictionary<string, object>, object>> func)
        {
            _func = func;
        }

        public object Create(IDictionary<string, object> source)
        {
            Func<IDictionary<string, object>, object> func = _func.Value;
            return func(source);
        }

        public bool TryCreate(IDictionary<string, object> source, out object result)
        {
            try
            {
                result = Create(source);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        public static ConcreteTypeCreator Get(Type targetType)
        {
            if (CreatorsCollection.IsSynchronized && Creators.ContainsKey(targetType))
            {
                return Creators[targetType];
            }

            lock (CreatorsCollection.SyncRoot)
            {
                if (Creators.ContainsKey(targetType)) return Creators[targetType];

                ConcreteTypeCreator creator = BuildCreator(targetType);
                Creators.Add(targetType, creator);
                return creator;
            }
        }

        private static ConcreteTypeCreator BuildCreator(Type targetType)
        {
            var creator =
                new ConcreteTypeCreator(
                    new Lazy<Func<IDictionary<string, object>, object>>(() => BuildLambda(targetType),
                                                                        LazyThreadSafetyMode.PublicationOnly));
            return creator;
        }

        private static Func<IDictionary<string, object>, object> BuildLambda(Type targetType)
        {
            ParameterExpression param = Expression.Parameter(typeof (IDictionary<string, object>), "source");
            ParameterExpression obj = Expression.Variable(targetType, "obj");

            BinaryExpression create = CreateNew(targetType, obj);

            BlockExpression assignments = Expression.Block(
                targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(PropertyIsConvertible)
                    .Select(p => new PropertySetterBuilder(param, obj, p).CreatePropertySetter()));

            BlockExpression block = Expression.Block(new[] {obj},
                                                     create,
                                                     assignments,
                                                     obj);

            Func<IDictionary<string, object>, object> lambda =
                Expression.Lambda<Func<IDictionary<string, object>, object>>(block, param).Compile();
            return lambda;
        }

        private static bool PropertyIsConvertible(PropertyInfo property)
        {
            return property.CanWrite || property.PropertyType.IsGenericCollection();
        }

        private static BinaryExpression CreateNew(Type targetType, ParameterExpression obj)
        {
            ConstructorInfo ctor = targetType.GetConstructor(Type.EmptyTypes);
            Debug.Assert(ctor != null);
            BinaryExpression create = Expression.Assign(obj, Expression.New(ctor)); // obj = new T();
            return create;
        }
    }
}
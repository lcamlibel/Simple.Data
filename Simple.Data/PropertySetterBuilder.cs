using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Simple.Data
{
    internal class PropertySetterBuilder
    {
        private static readonly MethodInfo DictionaryContainsKeyMethod =
            typeof (IDictionary<string, object>).GetMethod("ContainsKey", new[] {typeof (string)});

        private static readonly PropertyInfo DictionaryIndexerProperty =
            typeof (IDictionary<string, object>).GetProperty("Item");

        private static readonly MethodInfo ToArrayDictionaryMethod = typeof (Enumerable).GetMethod("ToArray",
                                                                                                   BindingFlags.Public |
                                                                                                   BindingFlags.Static).
            MakeGenericMethod(typeof (IDictionary<string, object>));

        private static readonly MethodInfo ToArrayObjectMethod = typeof (Enumerable).GetMethod("ToArray",
                                                                                               BindingFlags.Public |
                                                                                               BindingFlags.Static).
            MakeGenericMethod(typeof (object));


        private static readonly PropertyInfo ArrayDictionaryLengthProperty =
            typeof (IDictionary<string, object>[]).GetProperty("Length");

        private static readonly PropertyInfo ArrayObjectLengthProperty =
            typeof (object[]).GetProperty("Length");

        private static readonly MethodInfo CreatorCreateMethod = typeof (ConcreteTypeCreator).GetMethod("Create");

        private readonly ParameterExpression _obj;
        private readonly ParameterExpression _param;
        private readonly PropertyInfo _property;
        private MethodCallExpression _containsKey;
        private IndexExpression _itemProperty;
        private MemberExpression _nameProperty;

        public PropertySetterBuilder(ParameterExpression param, ParameterExpression obj, PropertyInfo property)
        {
            _param = param;
            _obj = obj;
            _property = property;
        }

        public ConditionalExpression CreatePropertySetter()
        {
            CreatePropertyExpressions();

            if (PropertyIsPrimitive())
            {
                return Expression.IfThen(_containsKey, CreateTrySimpleAssign());
            }

            if (_property.PropertyType.IsArray)
            {
                return Expression.IfThen(_containsKey, CreateTrySimpleArrayAssign());
            }

            if (_property.PropertyType.IsGenericCollection())
            {
                Expression collectionCreator = BuildCollectionCreator();
                if (collectionCreator != null)
                {
                    return Expression.IfThen(_containsKey, collectionCreator);
                }
            }

            TypeBinaryExpression isDictionary = Expression.TypeIs(_itemProperty, typeof (IDictionary<string, object>));

            TryExpression tryComplexAssign = Expression.TryCatch(CreateComplexAssign(),
                                                                 CreateCatchBlock());

            ConditionalExpression ifThen = Expression.IfThen(_containsKey, // if (dict.ContainsKey(propertyName)) {
                                                             Expression.IfThenElse(isDictionary, tryComplexAssign,
                                                                                   CreateTrySimpleAssign()));

            return ifThen;
        }

        private Expression BuildArrayCreator()
        {
            if (!_property.CanWrite) return null;

            Type genericType = _property.PropertyType.GetGenericArguments().Single();
            ConcreteTypeCreator creatorInstance = ConcreteTypeCreator.Get(genericType);
            ParameterExpression collection = Expression.Variable(_property.PropertyType);

            BinaryExpression createCollection = MakeCreateNewCollection(collection, genericType);

            if (createCollection == null) return null;

            MethodInfo addMethod = _property.PropertyType.GetMethod("Add");

            if (addMethod == null) return null;

            return BuildCollectionCreatorExpression(genericType, creatorInstance, collection, createCollection,
                                                    addMethod);
        }

        private Expression BuildCollectionCreator()
        {
            Type genericType = _property.PropertyType.GetGenericArguments().Single();
            ConcreteTypeCreator creatorInstance = ConcreteTypeCreator.Get(genericType);
            ParameterExpression collection = Expression.Variable(_property.PropertyType);
            BinaryExpression createCollection = _property.CanWrite ? MakeCreateNewCollection(collection, genericType) : Expression.Assign(collection, _nameProperty);

            MethodInfo addMethod = _property.PropertyType.GetInterfaceMethod("Add");

            if (createCollection != null && addMethod != null)
            {
                return BuildCollectionCreatorExpression(genericType, creatorInstance, collection, createCollection,
                                                        addMethod);
            }
            return null;
        }

        private Expression BuildCollectionCreatorExpression(Type genericType, ConcreteTypeCreator creatorInstance,
                                                            ParameterExpression collection,
                                                            BinaryExpression createCollection, MethodInfo addMethod)
        {
            BlockExpression dictionaryBlock;
            TypeBinaryExpression isDictionaryCollection = BuildComplexTypeCollectionPopulator(collection, genericType,
                                                                                              addMethod,
                                                                                              createCollection,
                                                                                              creatorInstance,
                                                                                              out dictionaryBlock);

            BlockExpression objectBlock;
            TypeBinaryExpression isObjectcollection = BuildSimpleTypeCollectionPopulator(collection, genericType,
                                                                                         addMethod, createCollection,
                                                                                         creatorInstance,
                                                                                         out objectBlock);

            return Expression.IfThenElse(isDictionaryCollection, dictionaryBlock,
                                         Expression.IfThen(isObjectcollection, objectBlock));
        }

        private TypeBinaryExpression BuildComplexTypeCollectionPopulator(ParameterExpression collection,
                                                                         Type genericType,
                                                                         MethodInfo addMethod,
                                                                         BinaryExpression createCollection,
                                                                         ConcreteTypeCreator creatorInstance,
                                                                         out BlockExpression block)
        {
            ConstantExpression creator = Expression.Constant(creatorInstance);
            ParameterExpression array = Expression.Variable(typeof (IDictionary<string, object>[]));
            ParameterExpression i = Expression.Variable(typeof (int));
            ParameterExpression current = Expression.Variable(typeof (IDictionary<string, object>));

            TypeBinaryExpression isDictionaryCollection = Expression.TypeIs(_itemProperty,
                                                                            typeof (
                                                                                IEnumerable<IDictionary<string, object>>
                                                                                ));

            BinaryExpression toArray = Expression.Assign(array,
                                                         Expression.Call(ToArrayDictionaryMethod,
                                                                         Expression.Convert(_itemProperty,
                                                                                            typeof (
                                                                                                IEnumerable
                                                                                                <
                                                                                                IDictionary
                                                                                                <string, object>>))));
            BinaryExpression start = Expression.Assign(i, Expression.Constant(0));
            LabelTarget label = Expression.Label();
            LoopExpression loop = Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(i, Expression.Property(array, ArrayDictionaryLengthProperty)),
                    Expression.Block(
                        Expression.Assign(current, Expression.ArrayIndex(array, i)),
                        Expression.Call(collection, addMethod,
                                        Expression.Convert(Expression.Call(creator, CreatorCreateMethod, current),
                                                           genericType)),
                        Expression.PreIncrementAssign(i)
                        ),
                    Expression.Break(label)
                    ),
                label
                );

            block = Expression.Block(
                new[] {array, i, collection, current},
                createCollection,
                toArray,
                start,
                loop,
                _property.CanWrite ? (Expression) Expression.Assign(_nameProperty, collection) : Expression.Empty());

            return isDictionaryCollection;
        }

        private TypeBinaryExpression BuildSimpleTypeCollectionPopulator(ParameterExpression collection, Type genericType,
                                                                        MethodInfo addMethod,
                                                                        BinaryExpression createCollection,
                                                                        ConcreteTypeCreator creatorInstance,
                                                                        out BlockExpression block)
        {
            ConstantExpression creator = Expression.Constant(creatorInstance);
            ParameterExpression array = Expression.Variable(typeof (object[]));
            ParameterExpression i = Expression.Variable(typeof (int));
            ParameterExpression current = Expression.Variable(typeof (object));

            TypeBinaryExpression isObjectCollection = Expression.TypeIs(_itemProperty,
                                                                        typeof (IEnumerable<object>));

            BinaryExpression toArray = Expression.Assign(array,
                                                         Expression.Call(ToArrayObjectMethod,
                                                                         Expression.Convert(_itemProperty,
                                                                                            typeof (IEnumerable<object>))));
            BinaryExpression start = Expression.Assign(i, Expression.Constant(0));
            LabelTarget label = Expression.Label();
            LoopExpression loop = Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(i, Expression.Property(array, ArrayObjectLengthProperty)),
                    Expression.Block(
                        Expression.Assign(current, Expression.ArrayIndex(array, i)),
                        Expression.IfThenElse(
                            Expression.TypeIs(current, typeof (IDictionary<string, object>)),
                            Expression.Call(collection, addMethod,
                                            Expression.Convert(Expression.Call(creator, CreatorCreateMethod,
                                                                               Expression.Convert(current,
                                                                                                  typeof (
                                                                                                      IDictionary
                                                                                                      <string, object>))),
                                                               genericType)),
                            Expression.Call(collection, addMethod,
                                            Expression.Convert(current, genericType))),
                        Expression.PreIncrementAssign(i)
                        ),
                    Expression.Break(label)
                    ),
                label
                );

            block = Expression.Block(
                new[] {array, i, collection, current},
                createCollection,
                toArray,
                start,
                loop,
                _property.CanWrite ? (Expression) Expression.Assign(_nameProperty, collection) : Expression.Empty());

            return isObjectCollection;
        }

        private BinaryExpression MakeCreateNewCollection(ParameterExpression collection, Type genericType)
        {
            BinaryExpression createCollection;

            if (_property.PropertyType.IsInterface)
            {
                createCollection = Expression.Assign(collection,
                                                     Expression.Call(
                                                         typeof (PropertySetterBuilder).GetMethod("CreateList",
                                                                                                  BindingFlags.
                                                                                                      NonPublic |
                                                                                                  BindingFlags.
                                                                                                      Static).
                                                             MakeGenericMethod(genericType)));
            }
            else
            {
                ConstructorInfo defaultConstructor = _property.PropertyType.GetConstructor(Type.EmptyTypes);
                createCollection = defaultConstructor != null ? Expression.Assign(collection, Expression.New(defaultConstructor)) : null;
            }
            return createCollection;
        }

        private bool PropertyIsPrimitive()
        {
            return _property.PropertyType.IsPrimitive || _property.PropertyType == typeof (string) ||
                   _property.PropertyType == typeof (DateTime) || _property.PropertyType == typeof (byte[]) ||
                   _property.PropertyType.IsEnum ||
                   (_property.PropertyType.IsGenericType &&
                    _property.PropertyType.GetGenericTypeDefinition() == typeof (Nullable<>));
        }

        private void CreatePropertyExpressions()
        {
            ConstantExpression name = Expression.Constant(_property.Name, typeof (string));
            _containsKey = Expression.Call(_param, DictionaryContainsKeyMethod, name);
            _nameProperty = Expression.Property(_obj, _property);
            _itemProperty = Expression.Property(_param, DictionaryIndexerProperty, name);
        }

        private CatchBlock CreateCatchBlock()
        {
            return Expression.Catch(typeof (Exception), Expression.Assign(_nameProperty,
                                                                          Expression.Default(_property.PropertyType)));
        }

        private BinaryExpression CreateComplexAssign()
        {
            ConstantExpression creator = Expression.Constant(ConcreteTypeCreator.Get(_property.PropertyType));
            MethodCallExpression methodCallExpression = Expression.Call(creator, CreatorCreateMethod,
// ReSharper disable PossiblyMistakenUseOfParamsMethod
                                                                        Expression.Convert(_itemProperty,
                                                                                           typeof (
                                                                                               IDictionary
                                                                                               <string, object>)));
// ReSharper restore PossiblyMistakenUseOfParamsMethod

            BinaryExpression complexAssign = Expression.Assign(_nameProperty,
                                                               Expression.Convert(
                                                                   methodCallExpression, _property.PropertyType));
            return complexAssign;
        }

        private TryExpression CreateTrySimpleAssign()
        {
            MethodCallExpression callConvert;
            if (_property.PropertyType.IsEnum)
            {
                MethodInfo changeTypeMethod = typeof (PropertySetterBuilder).GetMethod("SafeConvert",
                                                                                       BindingFlags.Static |
                                                                                       BindingFlags.NonPublic);
                callConvert = Expression.Call(changeTypeMethod, _itemProperty,
                                              Expression.Constant(_property.PropertyType.GetEnumUnderlyingType(),
                                                                  typeof (Type)));
            }
            else if (_property.PropertyType.IsGenericType &&
                     _property.PropertyType.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                MethodInfo changeTypeMethod = typeof (PropertySetterBuilder)
                    .GetMethod("SafeConvertNullable", BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(_property.PropertyType.GetGenericArguments().Single());

                callConvert = Expression.Call(changeTypeMethod, _itemProperty);
            }
            else
            {
                MethodInfo changeTypeMethod = typeof (PropertySetterBuilder).GetMethod("SafeConvert",
                                                                                       BindingFlags.Static |
                                                                                       BindingFlags.NonPublic);
                callConvert = Expression.Call(changeTypeMethod, _itemProperty,
                                              Expression.Constant(_property.PropertyType, typeof (Type)));
            }

            BinaryExpression assign = Expression.Assign(_nameProperty,
                                                        Expression.Convert(callConvert, _property.PropertyType));
            if (_property.PropertyType.IsEnum)
            {
                return Expression.TryCatch( // try {
                    Expression.IfThenElse(Expression.TypeIs(_itemProperty, typeof (string)),
                                          Expression.Assign(_nameProperty,
                                                            Expression.Convert(
                                                                Expression.Call(
                                                                    typeof (Enum).GetMethod("Parse",
                                                                                            new[]
                                                                                                {
                                                                                                    typeof (Type),
                                                                                                    typeof (string),
                                                                                                    typeof (bool)
                                                                                                }),
                                                                    Expression.Constant(_property.PropertyType,
                                                                                        typeof (Type)),
                                                                    Expression.Call(_itemProperty,
                                                                                    typeof (object).GetMethod("ToString")),
                                                                    Expression.Constant(true)), _property.PropertyType)),
                                          assign), Expression.Catch(typeof (Exception), Expression.Empty()));
            }
            return Expression.TryCatch( // try {
                assign,
                CreateCatchBlock());
        }

        private TryExpression CreateTrySimpleArrayAssign()
        {
            MethodInfo createArrayMethod = typeof (PropertySetterBuilder).GetMethod("CreateArray",
                                                                                    BindingFlags.Static |
                                                                                    BindingFlags.NonPublic)
                .MakeGenericMethod(_property.PropertyType.GetElementType());

            MethodCallExpression callConvert = Expression.Call(createArrayMethod, _itemProperty);

            BinaryExpression assign = Expression.Assign(_nameProperty,
                                                        Expression.Convert(callConvert, _property.PropertyType));
            return Expression.TryCatch( // try {
                Expression.IfThenElse(Expression.TypeIs(_itemProperty, typeof (string)),
                                      Expression.Assign(_nameProperty,
                                                        Expression.Convert(
                                                            Expression.Call(
                                                                typeof (Enum).GetMethod("Parse",
                                                                                        new[]
                                                                                            {
                                                                                                typeof (Type),
                                                                                                typeof (string),
                                                                                                typeof (bool)
                                                                                            }),
                                                                Expression.Constant(_property.PropertyType,
                                                                                    typeof (Type)),
                                                                Expression.Call(_itemProperty,
                                                                                typeof (object).GetMethod("ToString")),
                                                                Expression.Constant(true)), _property.PropertyType)),
                                      assign), Expression.Catch(typeof (Exception), Expression.Empty()));
        }


// ReSharper disable UnusedMember.Local
// Because they're used from runtime-generated code, you see.
        internal static object SafeConvert(object source, Type targetType)
        {
            if (ReferenceEquals(source, null)) return null;
            if (targetType.IsInstanceOfType(source)) return source;
            return Convert.ChangeType(source, targetType);
        }

        internal static T? SafeConvertNullable<T>(object source)
            where T : struct
        {
            if (ReferenceEquals(source, null)) return default(T?);
            return (T) source;
        }

        private static T[] CreateArray<T>(object source)
        {
            if (ReferenceEquals(source, null)) return null;
            var enumerable = source as IEnumerable;
            if (ReferenceEquals(enumerable, null)) return null;
            try
            {
                return enumerable.Cast<T>().ToArray();
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        private static List<T> CreateList<T>()
        {
            return new List<T>();
        }

// ReSharper restore UnusedMember.Local
    }
}
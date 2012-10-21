using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Simple.Data
{
    internal static class ConcreteCollectionTypeCreator
    {
        private static readonly List<Creator> Creators = new List<Creator>
                                                             {
                                                                 new GenericSetCreator(),
                                                                 new GenericListCreator(),
                                                                 new NonGenericListCreator()
                                                             };

        public static bool IsCollectionType(Type type)
        {
            return Creators.Any(c => c.IsCollectionType(type));
        }

        public static bool TryCreate(Type type, IEnumerable items, out object result)
        {
            return Creators.First(c => c.IsCollectionType(type)).TryCreate(type, items, out result);
        }

        #region Nested type: Creator

        internal abstract class Creator
        {
            public abstract bool IsCollectionType(Type type);

            public abstract bool TryCreate(Type type, IEnumerable items, out object result);

            protected bool TryConvertElement(Type type, object value, out object result)
            {
                result = null;
                if (value == null)
                    return true;

                Type valueType = value.GetType();

                if (type.IsAssignableFrom(valueType))
                {
                    result = value;
                    return true;
                }

                try
                {
                    TypeCode code = Convert.GetTypeCode(value);

                    if (type.IsEnum)
                    {
                        return ConvertEnum(type, value, out result);
                    }
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
                    {
                        result = Convert.ChangeType(value, Nullable.GetUnderlyingType(type));
                        return true;
                    }
                    if (code != TypeCode.Object)
                    {
                        result = Convert.ChangeType(value, type);
                        return true;
                    }
                    var data = value as IDictionary<string, object>;
                    if (data != null)
                        return ConcreteTypeCreator.Get(type).TryCreate(data, out result);
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (ArgumentException)
                {
                    return false;
                }

                return true;
            }

            private static bool ConvertEnum(Type type, object value, out object result)
            {
                var s = value as string;
                if (s != null)
                {
                    result = Enum.Parse(type, s);
                    return true;
                }

                result = Enum.ToObject(type, value);
                return true;
            }

            protected bool TryConvertElements(Type type, IEnumerable items, out Array result)
            {
                result = null;
                List<object> list = items == null ? new List<object>() : items.OfType<object>().ToList();

                Array array = Array.CreateInstance(type, list.Count);
                for (int i = 0; i < array.Length; i++)
                {
                    object element;
                    if (!TryConvertElement(type, list[i], out element))
                        return false;
                    array.SetValue(element, i);
                }

                result = array;
                return true;
            }
        }

        #endregion

        #region Nested type: GenericListCreator

        private class GenericListCreator : Creator
        {
            private static readonly Type OpenListType = typeof (List<>);

            public override bool IsCollectionType(Type type)
            {
                if (!type.IsGenericType)
                    return false;

                Type genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef.GetGenericArguments().Length != 1)
                    return false;

                return genericTypeDef == typeof (IEnumerable<>) ||
                       genericTypeDef == typeof (ICollection<>) ||
                       genericTypeDef == typeof (IList<>) ||
                       genericTypeDef == typeof (List<>);
            }

            public override bool TryCreate(Type type, IEnumerable items, out object result)
            {
                result = null;
                Type elementType = GetElementType(type);
                Type listType = OpenListType.MakeGenericType(elementType);
                Array elements;
                if (!TryConvertElements(elementType, items, out elements))
                    return false;

                result = Activator.CreateInstance(listType, elements);
                return true;
            }

            private Type GetElementType(Type type)
            {
                return type.GetGenericArguments()[0];
            }
        }

        #endregion

        #region Nested type: GenericSetCreator

        private class GenericSetCreator : Creator
        {
            private static readonly Type OpenSetType = typeof (HashSet<>);

            public override bool IsCollectionType(Type type)
            {
                if (!type.IsGenericType)
                    return false;

                Type genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef.GetGenericArguments().Length != 1)
                    return false;

                return genericTypeDef == typeof (ISet<>) ||
                       genericTypeDef == typeof (HashSet<>);
            }

            public override bool TryCreate(Type type, IEnumerable items, out object result)
            {
                result = null;
                Type elementType = GetElementType(type);
                Type setType = OpenSetType.MakeGenericType(elementType);
                Array elements;
                if (!TryConvertElements(elementType, items, out elements))
                    return false;

                result = Activator.CreateInstance(setType, elements);
                return true;
            }

            private Type GetElementType(Type type)
            {
                return type.GetGenericArguments()[0];
            }
        }

        #endregion

        #region Nested type: NonGenericListCreator

        private class NonGenericListCreator : Creator
        {
            public override bool IsCollectionType(Type type)
            {
                if (type == typeof (string))
                    return false;

                return type == typeof (IEnumerable) ||
                       type == typeof (ICollection) ||
                       type == typeof (IList) ||
                       type == typeof (ArrayList);
            }

            public override bool TryCreate(Type type, IEnumerable items, out object result)
            {
                var list = new ArrayList(items.OfType<object>().ToList());
                result = list;
                return true;
            }
        }

        #endregion
    }
}
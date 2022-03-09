﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSharpFunctionalExtensions
{
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
    public abstract class EnumValueObject<TEnumeration, TId> : ValueObject
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
        where TEnumeration : EnumValueObject<TEnumeration, TId>
        where TId : struct
    {
        private static readonly Dictionary<TId, TEnumeration> EnumerationsById = GetEnumerations().ToDictionary(e => e.Id);
        private static readonly Dictionary<string, TEnumeration> EnumerationsByName = GetEnumerations().ToDictionary(e => e.Name);

        protected EnumValueObject(TId id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("The name cannot be null or empty");
            }

            Id = id;
            Name = name;
        }

        public TId Id { get; protected set; }

        public string Name { get; protected set; }

        public static bool operator ==(EnumValueObject<TEnumeration, TId> a, TId b)
        {
            if (a is null)
            {
                return false;
            }

            return a.Id.Equals(b);
        }

        public static bool operator !=(EnumValueObject<TEnumeration, TId> a, TId b)
        {
            return !(a == b);
        }

        public static bool operator ==(TId a, EnumValueObject<TEnumeration, TId> b)
        {
            return b == a;
        }

        public static bool operator !=(TId a, EnumValueObject<TEnumeration, TId> b)
        {
            return !(b == a);
        }

        public static Maybe<TEnumeration> FromId(TId id)
        {
            return EnumerationsById.ContainsKey(id)
                ? EnumerationsById[id]
                : null;
        }

        public static Maybe<TEnumeration> FromName(string name)
        {
            return EnumerationsByName.ContainsKey(name)
                ? EnumerationsByName[name]
                : null;
        }


#if NET40
        public static ICollection<TEnumeration> All = EnumerationsById.Values.OfType<TEnumeration>().ToList();
#else
        public static IReadOnlyCollection<TEnumeration> All = EnumerationsById.Values.OfType<TEnumeration>().ToList();
#endif

        public static bool Is(string possibleName) => All.Select(e => e.Name).Contains(possibleName);

        public static bool Is(TId possibleId) => All.Select(e => e.Id).Contains(possibleId);

        public override string ToString() => Name;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Id;
        }

        private static TEnumeration[] GetEnumerations()
        {
            Type enumerationType = typeof(TEnumeration);

            return enumerationType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(info => info.FieldType == typeof(TEnumeration))
                .Select(info => (TEnumeration)info.GetValue(null))
                .ToArray();
        }
    }

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
    public abstract class EnumValueObject<TEnumeration> : ValueObject
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
        where TEnumeration : EnumValueObject<TEnumeration>
    {
        private static readonly Dictionary<string, TEnumeration> Enumerations = GetEnumerations().ToDictionary(e => e.Id);

        protected EnumValueObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("The enum key cannot be null or empty");
            }

            Id = id;
        }

#if NET40
        public static ICollection<TEnumeration> All = Enumerations.Values.OfType<TEnumeration>().ToList();
#else
        public static IReadOnlyCollection<TEnumeration> All = Enumerations.Values.OfType<TEnumeration>().ToList();
#endif

        public virtual string Id { get; protected set; }

        public static bool operator ==(EnumValueObject<TEnumeration> a, string b)
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            return a.Id.Equals(b);
        }

        public static bool operator !=(EnumValueObject<TEnumeration> a, string b)
        {
            return !(a == b);
        }

        public static bool operator ==(string a, EnumValueObject<TEnumeration> b)
        {
            return b == a;
        }

        public static bool operator !=(string a, EnumValueObject<TEnumeration> b)
        {
            return !(b == a);
        }

        public static Maybe<TEnumeration> FromId(string id)
        {
            return Enumerations.ContainsKey(id)
                ? Enumerations[id]
                : null;
        }

        public static bool Is(string possibleId) => All.Select(e => e.Id).Contains(possibleId);

        public override string ToString() => Id;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Id;
        }

        private static TEnumeration[] GetEnumerations()
        {
            Type enumerationType = typeof(TEnumeration);

            return enumerationType
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(info => info.FieldType == typeof(TEnumeration))
                .Select(info => (TEnumeration)info.GetValue(null))
                .ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Toletus.Extensions
{
    public static class EnumExtensions
    {
        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static string GetDescriptionFromValue(this Enum value)
        {
            if (value == null) return null;

            var field = value.GetType().GetField(value.ToString());

            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault() as DescriptionAttribute;

            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static T GetValueFromDescription<T>(this string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            return default(T);
        }

        public static List<KeyValuePair<string, int>> GetValuesAndDescriptions<T>()
        {
            var enumType = typeof(T);

            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("T is not System.Enum");

            var enumValList = new List<KeyValuePair<string, int>>();

            foreach (var e in Enum.GetValues(typeof(T)))
            {
                var fi = e.GetType().GetField(e.ToString());
                var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

                enumValList.Add(new KeyValuePair<string, int>((attributes.Length > 0) ? attributes[0].Description : e.ToString(), (int)e));
            }

            return enumValList;
        }

        public static List<string> GetDescriptions<T>()
        {
            var attributes = typeof(T).GetMembers()
                .SelectMany(member => member.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>())
                .ToList();

            return attributes.Select(x => x.Description).ToList();
        }

        public static bool Has<T>(this Enum type, T value)
        {
            if (type.GetType() != value.GetType())
                throw new InvalidOperationException("Enum type mismatch");

            try
            {
                return (((int)(object)type & (int)(object)value) == (int)(object)value);
            }
            catch
            {
                return false;
            }
        }

        public static T Add<T>(this Enum type, T value)
        {
            try
            {
                return (T)(object)(((int)(object)type | (int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    string.Format(
                        "Could not append value from enumerated type '{0}'.",
                        typeof(T).Name
                    ), ex);
            }
        }
    }
}

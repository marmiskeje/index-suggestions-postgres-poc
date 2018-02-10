using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    static class EnumParsingSupport
    {
        public static bool TryConvertFromString<T>(string s, out T result) where T : struct
        {
            // todo - cache
            result = default(T);
            if (!String.IsNullOrEmpty(s))
            {
                foreach (var n in Enum.GetNames(typeof(T)))
                {
                    FieldInfo field = typeof(T).GetField(n);
                    var attr = field.GetCustomAttribute<EnumMemberAttribute>();
                    if (attr != null && attr.Value.Equals(s.Trim()))
                    {
                        result = (T)Enum.Parse(typeof(T), n);
                        return true;
                    }
                }
            }
            return false;
        }

        public static T ConvertFromStringOrDefault<T>(string s) where T : struct
        {
            T result = default(T);
            TryConvertFromString<T>(s, out result);
            return result;
        }

        public static T ConvertFromNumericOrDefault<T>(int n) where T : struct
        {
            if (Enum.IsDefined(typeof(T), n))
            {
                return (T)Enum.ToObject(typeof(T), n);
            }
            return default(T);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ETH.Zonnon.Compute {
    static class Extensions {
        public static Boolean TryGetValue<T>(this Nullable<T> nullable, out T value) where T : struct {
            if (nullable.HasValue) {
                value = nullable.Value;
                return true;
            } else {
                value = default(T);
                return false;
            }
        }

        public static Boolean Is<T>(this Object o, out T value) where T : class {
            value = o as T;
            return !(Object.ReferenceEquals(o, null) || Object.ReferenceEquals(value, null));
        }
    }
}

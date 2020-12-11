using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Roadie.Library.Extensions
{
    public static class GenericExt
    {
        public static TEntity CopyTo<TEntity>(this TEntity OriginalEntity, TEntity NewEntity)
        {
            var oProperties = OriginalEntity.GetType().GetProperties();

            foreach (var CurrentProperty in oProperties.Where(p => p.CanWrite))
            {
                if (CurrentProperty.GetValue(NewEntity, null) != null)
                {
                    CurrentProperty.SetValue(OriginalEntity, CurrentProperty.GetValue(NewEntity, null), null);
                }
            }

            return OriginalEntity;
        }

        public static string DescriptionAttr<T>(this T source)
        {
            var fi = source.GetType().GetField(source.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }

            return source.ToString();
        }
    }
}
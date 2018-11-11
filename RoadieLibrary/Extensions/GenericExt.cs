using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Roadie.Library.Extensions
{
    public static class GenericExt
    {
        public static TEntity CopyTo<TEntity>(this TEntity OriginalEntity, TEntity NewEntity)
        {
            PropertyInfo[] oProperties = OriginalEntity.GetType().GetProperties();

            foreach (PropertyInfo CurrentProperty in oProperties.Where(p => p.CanWrite))
            {
                if (CurrentProperty.GetValue(NewEntity, null) != null)
                {
                    CurrentProperty.SetValue(OriginalEntity, CurrentProperty.GetValue(NewEntity, null), null);
                }
            }

            return OriginalEntity;
        }

        /// <summary>
        /// Perform a deep Copy of the object using a BinaryFormatter.
        /// IMPORTANT: the object class must be marked as [Serializable] and have an parameterless constructor.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T Clone<T>(this T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

    }
}
using System;
using System.ComponentModel;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Provides a type converter for the column header collection.
    /// </summary>
    internal class ColumnHeaderCollectionTypeConverter : TypeConverter
    {
        #region TypeConverter Overrides
        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }
        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value is ImageListView.ImageListViewColumnHeaderCollection && 
                (destinationType == typeof(string)))
                return "(Collection)";

            return base.ConvertTo(context, culture, value, destinationType);
        }
        #endregion
    }
}

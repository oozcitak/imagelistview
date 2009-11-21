using System;
using System.ComponentModel.Design;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Provides an editor for the column header collection.
    /// </summary>
    internal class ColumnHeaderCollectionEditor : CollectionEditor
    {
        #region Constructor
        public ColumnHeaderCollectionEditor()
            : base(typeof(ImageListView.ImageListViewColumnHeaderCollection))
        {
        }
        #endregion

        #region CollectionEditor Overrides
        /// <summary>
        /// Indicates whether original members of the collection can be removed.
        /// </summary>
        protected override bool CanRemoveInstance(object value)
        {
            // Disable the Remove button
            return false;
        }
        /// <summary>
        /// Gets the data types that this collection editor can contain.
        /// </summary>
        protected override Type[] CreateNewItemTypes()
        {
            // Disable the Add button
            return new Type[0];
        }
        /// <summary>
        /// Retrieves the display text for the given list item.
        /// </summary>
        protected override string GetDisplayText(object value)
        {
            return ((ImageListView.ImageListViewColumnHeader)value).Type.ToString();
        }
        /// <summary>
        /// Indicates whether multiple collection items can be selected at once.
        /// </summary>
        protected override bool CanSelectMultipleInstances()
        {
            return false;
        }
        /// <summary>
        /// Gets an array of objects containing the specified collection.
        /// </summary>
        protected override object[] GetItems(object editValue)
        {
            ImageListView.ImageListViewColumnHeaderCollection columns = 
                (ImageListView.ImageListViewColumnHeaderCollection)editValue;
            object[] list = new object[columns.Count];
            for (int i = 0; i < columns.Count; i++)
                list[i] = columns[i];
            return list;
        }
        /// <summary>
        /// Creates a new form to display and edit the current collection.
        /// </summary>
        protected override CollectionEditor.CollectionForm CreateCollectionForm()
        {
            return base.CreateCollectionForm();
        }
        #endregion
    }
}

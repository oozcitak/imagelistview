using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.Drawing.Design;
using System.Runtime.Serialization;
using System.Resources;
using System.Reflection;

namespace Manina.Windows.Forms
{
    public partial class ImageListView
    {
        /// <summary>
        /// Represents the collection of columns in an ImageListView control.
        /// </summary>
        [Editor(typeof(ColumnHeaderCollectionEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ColumnHeaderCollectionTypeConverter))]
        public class ImageListViewColumnHeaderCollection : IEnumerable<ImageListViewColumnHeader>, ICloneable, ISerializable
        {
            #region Member Variables
            private ImageListView mImageListView;
            private ImageListViewColumnHeader[] mItems;
            #endregion

            #region Properties
            /// <summary>
            /// Gets the number of columns in the collection.
            /// </summary>
            [Category("Behavior"), Browsable(false), Description("Gets the number of columns in the collection.")]
            public int Count { get { return mItems.Length; } }
            /// <summary>
            /// Gets the ImageListView owning this collection.
            /// </summary>
            [Category("Behavior"), Browsable(false), Description("Gets the ImageListView owning this collection.")]
            public ImageListView ImageListView { get { return mImageListView; } }
            /// <summary>
            /// Gets the column at the specified index within the collection.
            /// </summary>
            [Category("Behavior"), Browsable(false), Description("Gets or item at the specified index within the collection.")]
            public ImageListViewColumnHeader this[int index]
            {
                get
                {
                    return mItems[index];
                }
            }
            /// <summary>
            /// Gets the column with the specified type within the collection.
            /// </summary>
            [Category("Behavior"), Browsable(false), Description("Gets or sets the item with the specified type within the collection.")]
            public ImageListViewColumnHeader this[ColumnType type]
            {
                get
                {
                    foreach (ImageListViewColumnHeader column in this)
                        if (column.Type == type) return column;
                    throw new ArgumentException("Unknown column type.", "type");
                }
            }
            #endregion

            #region Constructors
            public ImageListViewColumnHeaderCollection(ImageListView owner)
            {
                mImageListView = owner;
                // Create the default column set
                mItems = new ImageListViewColumnHeader[] {
                    new ImageListViewColumnHeader(ColumnType.Name),
                    new ImageListViewColumnHeader(ColumnType.FileSize),
                    new ImageListViewColumnHeader(ColumnType.DateModified),
                    new ImageListViewColumnHeader(ColumnType.Dimensions),
                    new ImageListViewColumnHeader(ColumnType.Resolution),
                    new ImageListViewColumnHeader(ColumnType.FilePath),
                    new ImageListViewColumnHeader(ColumnType.FileType),
                    new ImageListViewColumnHeader(ColumnType.FileName),
                    new ImageListViewColumnHeader(ColumnType.DateCreated),
                    new ImageListViewColumnHeader(ColumnType.DateAccessed),
               };
                for (int i = 0; i < mItems.Length; i++)
                {
                    ImageListViewColumnHeader col = mItems[i];
                    col.mImageListView = mImageListView;
                    col.owner = this;
                    col.DisplayIndex = i;
                    if (i >= 4) col.Visible = false;
                }
            }
            #endregion

            #region Instance Methods
            /// <summary>
            /// Gets the default column header text for the given column type.
            /// </summary>
            [Localizable(true)]
            public string GetDefaultText(ColumnType type)
            {
                ResourceManager manager = new ResourceManager("Manina.Windows.Forms.ImageListViewResources",
                    Assembly.GetExecutingAssembly());
                return manager.GetString(type.ToString());
            }
            /// <summary>
            /// Returns an enumerator to use to iterate through columns.
            /// </summary>
            /// <returns>An IEnumerator&lt;ImageListViewColumn&gt; that represents the item collection.</returns>
            public IEnumerator<ImageListViewColumnHeader> GetEnumerator()
            {
                foreach (ImageListViewColumnHeader column in mItems)
                    yield return column;
                yield break;
            }
            #endregion

            #region Helper Methods
            /// <summary>
            /// Gets the columns as diplayed on the UI.
            /// </summary>
            internal List<ImageListViewColumnHeader> GetUIColumns()
            {
                List<ImageListViewColumnHeader> list = new List<ImageListViewColumnHeader>();
                foreach (ImageListViewColumnHeader column in mItems)
                {
                    if (column.Visible)
                        list.Add(column);
                }
                list.Sort(ColumnCompare);
                return list;
            }
            /// <summary>
            /// Compares the columns by their display index.
            /// </summary>
            internal static int ColumnCompare(ImageListViewColumnHeader a, ImageListViewColumnHeader b)
            {
                if (a.DisplayIndex < b.DisplayIndex)
                    return -1;
                else if (a.DisplayIndex > b.DisplayIndex)
                    return 1;
                else
                    return 0;
            }
            #endregion

            #region Unsupported Interface
            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            #endregion

            #region ICloneable Members
            /// <summary>
            /// Creates a new object that is a copy of the current instance.
            /// </summary>
            public object Clone()
            {
                ImageListViewColumnHeaderCollection clone = new ImageListViewColumnHeaderCollection(this.mImageListView);
                Array.Copy(this.mItems, clone.mItems, 0);
                return clone;
            }
            #endregion

            #region ISerializable Members
            public ImageListViewColumnHeaderCollection(SerializationInfo info, StreamingContext context)
            {
                for (int i = 0; i < mItems.Length; i++)
                {
                    ColumnType type = (ColumnType)info.GetInt32(string.Format("{0}:{1}", i, "Type"));
                    int index = info.GetInt32(string.Format("{0}:{1}", i, "Index"));
                    string text = info.GetString(string.Format("{0}:{1}", i, "Text"));
                    bool visible = info.GetBoolean(string.Format("{0}:{1}", i, "Visible"));
                    int width = info.GetInt32(string.Format("{0}:{1}", i, "Width"));
                    ImageListViewColumnHeader col=new ImageListViewColumnHeader(type, text, width);
                    col.DisplayIndex = index;
                    col.Visible = visible;
                    mItems[i] = col;
                }
            }

            /// <summary>
            /// Populates a SerializationInfo with the data needed to serialize the target object.
            /// </summary>
            /// <param name="info">The SerializationInfo to populate with data.</param>
            /// <param name="context">The destination for this serialization.</param>
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                for (int i = 0; i < mItems.Length; i++)
                {
                    ImageListView.ImageListViewColumnHeader col = mItems[i];
                    info.AddValue(string.Format("{0}:{1}", i, "Type"), col.Type);
                    info.AddValue(string.Format("{0}:{1}", i, "Index"), col.DisplayIndex);
                    info.AddValue(string.Format("{0}:{1}", i, "Text"), col.Text);
                    info.AddValue(string.Format("{0}:{1}", i, "Visible"), col.Visible);
                    info.AddValue(string.Format("{0}:{1}", i, "Width"), col.Width);
                }
            }

            #endregion
        }
    }
}
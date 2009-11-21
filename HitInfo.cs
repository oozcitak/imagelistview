 
namespace Manina.Windows.Forms
{
    public partial class ImageListView 
    {
        /// <summary>
        /// Represents the details of a mouse hit test.
        /// </summary>
        public struct HitInfo
        {
            #region Member Variables
            public bool InHeaderArea;
            public bool InItemArea;
            public bool ColumnHit;
            public bool ItemHit;
            public bool ColumnSeparatorHit;
            public ColumnType ColumnIndex;
            public int ItemIndex;
            public ColumnType ColumnSeparator;
            #endregion
        }
    }
}
using System;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Represents the embedded thumbnail extraction behavior.
    /// </summary>
    public enum UseEmbeddedThumbnails
    {
        Auto,
        Always,
        Never,
    }
    /// <summary>
    /// Represents the cache state of a thumbnail image.
    /// </summary>
    public enum CacheState
    {
        Unknown,
        InQueue,
        Cached,
        Error,
    }
    /// <summary>
    /// Represents the view mode of the image list view.
    /// </summary>
    public enum View
    {
        Details,
        Thumbnails,
    }
    /// <summary>
    /// Represents the type of information displayed in an image list view column.
    /// </summary>
    public enum ColumnType
    {
        Name,
        DateAccessed,
        DateCreated,
        DateModified,
        FileType,
        FileName,
        FilePath,
        FileSize,
        Dimension,
        Resolution,
    }
    /// <summary>
    /// Represents the sort order of am image list view column.
    /// </summary>
    public enum SortOrder
    {
        None = 0,
        Ascending = 1,
        Descending = -1,
    }
    /// <summary>
    /// Determines the visibility of an item.
    /// </summary>
    public enum ItemVisibility
    {
        NotVisible = 0,
        Visible = 1,
        PartiallyVisible = 2,
    }
    /// <summary>
    /// Represents the visual state of an image list view item.
    /// </summary>
    [Flags]
    public enum ItemState
    {
        None = 0,
        Selected = 1,
        Focused = 2,
        Hovered = 4,
    }
    /// <summary>
    /// Represents the visual state of an image list column.
    /// </summary>
    [Flags]
    public enum ColumnState
    {
        None = 0,
        Hovered = 1,
        SeparatorHovered = 2,
        SeparatorSelected = 4,
    }
    /// <summary>
    /// Represents the order by which items are drawn.
    /// </summary>
    public enum ItemDrawOrder
    {
        ItemIndex,
        Normal,
        Selected,
        Hovered,
        NormalSelected,
        NormalHovered,
        SelectedNormal,
        SelectedHovered,
        HoveredNormal,
        HoveredSelected,
        NormalSelectedHovered,
        NormalHoveredSelected,
        SelectedNormalHovered,
        SelectedHoveredNormal,
        HoveredNormalSelected,
        HoveredSelectedNormal,
    }
}

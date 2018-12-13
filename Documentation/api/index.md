---
uid: Api.Home
title: ImageListView Overview
---
# ImageListView Overview #

The ImageListView control displays a list of image files. The list shows image thumbnails and metadata.

## Working with ImageListView #

The ImageListView control supports a number of view modes which can be changed with its @Manina.Windows.Forms.ImageListView.View property. The default view is the thumbnail view where image thumbnails are laid out as a grid which can be scrolled vertically.

Image files can be added to the control by using its @Manina.Windows.Forms.ImageListView.Items property. Once image files are added; their thumbnails are loaded asynchronously with a separate background thread.

Items can be selected by the user. If @Manina.Windows.Forms.ImageListView.MultiSelect property is set to `true`, multiple items can be selected with the mouse and the keyword. Otherwise, only one item can be selected at a time. The list of selected items can be reached with the @Manina.Windows.Forms.ImageListView.SelectedItems property.

Items can display checkboxes and shell icons by using the @Manina.Windows.Forms.ImageListView.ShowCheckBoxes and @Manina.Windows.Forms.ImageListView.ShowFileIcons properties respectively.  The list of checked items can be reached with the @Manina.Windows.Forms.ImageListView.CheckedItems property.

In details view mode, the control displays column headers at the top of its client area. The user can click the column headers to sort the items. Items can also be sorted programmatically with the @Manina.Windows.Forms.ImageListView.SortOrder, @Manina.Windows.Forms.ImageListView.SortColumn, @Manina.Windows.Forms.ImageListView.GroupOrder and @Manina.Windows.Forms.ImageListView.GroupColumn properties.

When an item is clicked, the control raises the @Manina.Windows.Forms.ImageListView.ItemClick event for that @Manina.Windows.Forms.ImageListViewItem. Other important item related events are @Manina.Windows.Forms.ImageListView.ItemDoubleClick, @Manina.Windows.Forms.ImageListView.ItemCheckBoxClick and @Manina.Windows.Forms.ImageListView.SelectionChanged.

## Customizing ImageListView #

The size of the thumbnails can be customized with the @Manina.Windows.Forms.ImageListView.ThumbnailSize property. The color theme of the control can be changed with the @Manina.Windows.Forms.ImageListView.Colors property. The entire visual style of the control can be changed by using a custom renderer included in the @Manina.Windows.Forms.ImageListViewRenderers namespace. The control can be further customized by deriving from @Manina.Windows.Forms.ImageListView.ImageListViewRenderer and overriding the virtual functions of the base class. 

The @Manina.Windows.Forms.ImageListView.SetRenderer(Manina.Windows.Forms.ImageListView.ImageListViewRenderer,System.Boolean) method should be used to assign a custom renderer to the control.

## Custom Thumbnail Providers #

By default, the control extracts thumbail images from filenames supplied by the user while adding image items. However, it is possible to provide other sources for thumbnail images by using custom adaptors. There are a number of built-in custom adaptors in the @Manina.Windows.Forms.ImageListViewItemAdaptors namespace or a user adaptor can be created by deriving from @Manina.Windows.Forms.ImageListView.ImageListViewItemAdaptor. 

Adaptors are assigned to each item while adding it to the control with the @Manina.Windows.Forms.ImageListView.ImageListViewItemCollection.Add(Manina.Windows.Forms.ImageListViewItem,Manina.Windows.Forms.ImageListView.ImageListViewItemAdaptor) method of the @Manina.Windows.Forms.ImageListView.ImageListViewItemCollection class.

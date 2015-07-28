# 11.0 (19 October 2012) #

  * Fixed the issue where inserting an item with an initial thumbnail bypassed the custom adaptor ([Issue 153](https://code.google.com/p/imagelistview/issues/detail?id=153)).
  * Added the PaneResized and PaneResizing events ([Issue 152](https://code.google.com/p/imagelistview/issues/detail?id=152)).
  * Items can now be disabled. Disabled items cannot be selected ([Issue 136](https://code.google.com/p/imagelistview/issues/detail?id=136)).
  * Added natural sorting for string columns ([Issue 154](https://code.google.com/p/imagelistview/issues/detail?id=154)). **This is a breaking change.**
  * Control border is now drawn with ControlPaint.DrawBorder3D ([Issue 135](https://code.google.com/p/imagelistview/issues/detail?id=135)).

# 10.6 (9 August 2012) #

  * Insertion index is not incremented on drag-drop if the item was not successfully inserted ([Issue 146](https://code.google.com/p/imagelistview/issues/detail?id=146)).
  * Items are no longer sorted based on groups if the GroupOrder property is not set ([Issue 151](https://code.google.com/p/imagelistview/issues/detail?id=151)).

# 10.5 (9 March 2012) #

  * The source image DPI is accounted for while creating thumbnails ([Issue 144](https://code.google.com/p/imagelistview/issues/detail?id=144)).

# 10.4 (26 Sept 2011) #

  * Dropping multiple files when MultiSelect is set to false now selects a single file ([Issue 138](https://code.google.com/p/imagelistview/issues/detail?id=138)).
  * Insertion caret is not drawn if dragged items are not files ([Issue 139](https://code.google.com/p/imagelistview/issues/detail?id=139)).
  * Changing item filename now updates the thumbnail ([Issue 141](https://code.google.com/p/imagelistview/issues/detail?id=141)).
  * Fixed a bug for the thumbnail creation of some rare bmp formats ([Issue 140](https://code.google.com/p/imagelistview/issues/detail?id=140)).

# 10.3 (20 May 2011) #

  * The control is shown dimmed in disabled state and background processing is paused while the control is disabled ([Issue 133](https://code.google.com/p/imagelistview/issues/detail?id=133)).

# 10.2 (16 May 2011) #

  * Added the FindString method to the control ([Issue 132](https://code.google.com/p/imagelistview/issues/detail?id=132)).
  * Added sandcastle documentation.

# 10.1 (27 April 2011) #

  * Items are no longer sorted from scratch each time an item is added or removed. Note: this adds the new item to the last group without checking which group the item actually belongs to.

# 10.0 (06 April 2011) #

This release introduces the item grouping option and also fixes some minor bugs. Group and sort work together. For example when you group by name and sort by date at the same time; the items will be grouped alphabetically and each group will be sorted by picture date.

  * Added the GroupColumn and GroupOrder properties to the control ([Issue 69](https://code.google.com/p/imagelistview/issues/detail?id=69)).
  * Column headers are no longer sorted on a right click.
  * Fixed possible sources for null reference exceptions. Those may be related to [Issue 122](https://code.google.com/p/imagelistview/issues/detail?id=122) and [Issue 123](https://code.google.com/p/imagelistview/issues/detail?id=123).
  * Added the ItemCollectionChanged event ([Issue 125](https://code.google.com/p/imagelistview/issues/detail?id=125)).
  * The control is now refreshed when the ScrollBars property is changed.

Note: There is a known issue with groups. When groups are shown, each item insertion/removal will sort the items from scratch. This would be evident when inserting many items in a row. This will probably be fixed in a future release; but the current workaround is to disable grouping while inserting many items. See [Issue 129](https://code.google.com/p/imagelistview/issues/detail?id=129).

# 9.6 (08 December 2010) #

  * Fixed a bug in the .NET 2.0 source ([Issue 116](https://code.google.com/p/imagelistview/issues/detail?id=116)).
  * The framework requirement is now .NET 3.5. An option to target 2.0 is still available. (If you are holding back on .NET 2.0 just for the size of the .NET 3.5 framework check the "Client-only Framework subset" in the project properties.)
  * Added the AllowCheckBoxClick property to the control ([Issue 114](https://code.google.com/p/imagelistview/issues/detail?id=114)).

# 9.5 (12 November 2010) #

This release fixes a few minor bugs.

  * Fixed a bug in URIAdaptor where it prevented its use with multiple threads.
  * FileSystemAdaptor now checkes if the file exists. ([Issue 113](https://code.google.com/p/imagelistview/issues/detail?id=113))
  * Fixed a bug in Items Add method where the user supplied adaptor was overwritten.

# 9.4 (05 November 2010) #

This release introduces a new approach to virtual items. Instead of handling the virtual item events of the control, you now provide an adaptor (ImageListViewItemAdaptor) while adding items. The nice thing about the adaptor approach is that they can be written once and used everywhere. There are currently two built-in adaptors: FileSystemAdaptor (for loading image thumbnails from the file system) and URIAdaptor (for loading images from the web). You can write your own adaptors too. It is quite straight forward.

  * Added the ImageListViewItemAdaptor class and built-in FileSystemAdaptor and URIAdaptor adaptors. Virtual item events are removed. **This is a breaking change.**
  * Removed the Error property from ThumbnailCachedEventArgs, since we already have the CacheError event. **This is a breaking change.**
  * ImageListViewItem.ThumbnailImage returns a clone of the cached thumbnail.
  * ImageListViewItem.Clone() now clones the current thumbnail ([Issue 109](https://code.google.com/p/imagelistview/issues/detail?id=109)).
  * Metadata fall-back to .net 2.0 now works (hopefully :)) ([Issue 107](https://code.google.com/p/imagelistview/issues/detail?id=107)).
  * Added one-shot async work option to QueuedBackgroundWorker. This can be used to quickly run a background operation bypassing the queued items. ImageListView uses this to load large images in gallery view mode.
  * Added the IsThumbnail property to the ThumbnailCached event.
  * Fixed a bug where the control sometimes failed to refresh itself after loading large images in gallery view mode ([Issue 108](https://code.google.com/p/imagelistview/issues/detail?id=108)).
  * Fixed a bug where control resources could not be loaded if it was subclassed ([Issue 111](https://code.google.com/p/imagelistview/issues/detail?id=111)).

# 9.3 (13 October 2010) #

This release includes a further performance tweak by delaying and consolidating refresh requests of thumbnail and metadata threads. The result is a smoother display especially while scrolling the control. MeerkatRenderer is a new addition to the built-in renderers to celebrate the release of Ubuntu 10.10. There are also new properties for alternating row colors. These colors can be set using the Colors property of the control.

  * Cache threads refresh the control lazily.
  * Added the new MeerkatRenderer.
  * Fixed a bug where the cache thread would not refresh the control after loading gallery thumbnails.
  * Renderers can now declare a preferred color theme.
  * Added AlternateBackColor and AlternateCellForeColor properties to ImageListViewColor.
  * MetadataExtractor uses raw property paths instead of metadata policies.

# 9.2 (10 October 2010) #

This release includes a further performance tweak over the previous release. There are also a few cosmetic changes and new methods for handling selected and checked item collections.

  * Background workers now use multiple threads while extracting thumbnails and reading image metadata.
  * Added the InvertSelection method to the control.
  * Added CheckAll, UncheckAll and InvertCheckState methods to the control.
  * Added the ClientBounds property to ImageListViewRenderer.
  * Zoomed items of zooming renderer do not flow out of the control.
  * Fixed a bug where the first column was always drawn in hovered state.
  * Pane label color is now a darker gray.

# 9.1 (10 October 2010) #

This release includes many performance tweaks. The new ThemeRenderer is also introduced in this release. This new renderer matches the system visual style and falls back to the default renderer on older (Windows XP and earlier) systems.

  * Accessing the rating property of an item by the renderer no longer issues a full item update.
  * Zooming renderer zooms items in Gallery and Pane modes too.
  * Added the ThemeRenderer. ([Issue 44](https://code.google.com/p/imagelistview/issues/detail?id=44))
  * Added the UseWIC property to the control.
  * .Net 2.0 thumbnail fallback now automatically rotates thumbnails depending on orientation metadata.
  * Added toolbox bitmap to QueuedBackgroundWorker.
  * Decreased the number of context switches in the worker threads.
  * The thumbnail thread now processes the last request first.
  * Worker threads refresh the control only if the new thumbnails or metadata columns are visible on the screen.
  * QueuedBackgroundWorker now has a ProcessingMode property for choosing between FIFO and LIFO processing.

# 9.0 (06 October 2010) #

This release introduces the QueuedBackgroundWorker. This component is used internally by ImageListView for its worker threads but it can also be used as a stand alone component. It is basically a multi-queued version of the .Net BackgroundWorker with some additional features. This release is a major rewrite and it is quite possible that I introduced new bugs.

  * Rebuilding the thumbnail cache now obeys the CacheMode property of the control, ie. if the CacheMode is set to Continuous, all item thumbnails will be fetched again after clearing the cache.
  * Shell info (icons and file type) are now handled by a new thread. Shell info is now cached by filename extension.
  * Added the new QueuedBackgroundWorker component.
  * More rendering logic is moved to the control from the renderer, preventing unnecessary redraws.

# 8.3 (30 September 2010) #

This release aims better integration with the control designer. Some of the changes are  invisible to the user (ie. new InstanceDescriptors and default property values emit cleaner code in InitializeComponent), some are visible on the designer surface (ie. items can now be added to the control from the designer. Both thumbnail and item details will be fetched and shown on the designer), some changes fix long standing glitches (ie. editing child item collections from the designer could not be undone). There is also a minor bug fix and new properties for items.

  * Reintroduced Software and FocalLength properties to items ([Issue 102](https://code.google.com/p/imagelistview/issues/detail?id=102)).
  * IntegralScroll property now defaults to false.
  * Added default values for DefaultImage, ErrorImage, RatingImage, EmptyRatingImage, Colors and HeaderFont properties for better designer integration.
  * Added type converters to ImageListViewColors, ImageListItem and ImageListViewColumnHeader for designer serialization.
  * Added an AddRange method to column header collection.
  * Changes to the column header collection made from the designer can now be undone.
  * Items can now be added/modified from the designer.
  * Fixed a bug where drawing the control in details view without any columns threw an exception.
  * Setting an item's filename property also sets the item text if the text was not previously set.
  * Added an open file dialog to the filename property of items for use with the designer.
  * Removed preview items from the control designer. They are no longer necessary since items can be added at design-time.

# 8.2 (29 September 2010) #

  * Thumbnail images are now automatically mirrored depending on Exif orientation tag.
  * Thumbnail extraction should now work faster since the control longer extracts all metadata to get the Exif rotation tag.
  * Fixed a bug where the thumbnail image was disposed by the cache manager while the image was being drawn by the control. ([Issue 101](https://code.google.com/p/imagelistview/issues/detail?id=101))

# 8.1 (28 September 2010) #

  * Added the conditional compilation symbol USEWIC to enable/disable Windows Imaging Component support. See the [WICSupport](WICSupport.md) wiki page for more information.
  * Thumbnail size is now adjusted for the Exif orientation tag. ([Issue 100](https://code.google.com/p/imagelistview/issues/detail?id=100))
  * Column headers are no longer displayed in hovered state if both AllowColumnClick and AllowColumnResize properties of the control are set to false.
  * ExposureTime property of ImageListViewItem is now a real value. **This is a breaking change.**
  * Removed ShutterSpeed and ApertureValue columns. They were duplicates of ExposureTime and FNumber. **This is a breaking change.**
  * Added .Net 2.0 fall-back to ThumbnailExtractor.
  * .Net 2.0 fall-back of MetadataExtractor should now work. ([Issue 99](https://code.google.com/p/imagelistview/issues/detail?id=99))

# 8.0 for .Net 3.0 (25 September 2010) #

  * Added the AutoRotateThumbnails property to the control. Image rotation is determined by the Exif orientation metadata.
  * Details cache manager now obeys the RetryOnError property of the control.
  * File icons are no longer drawn over column headers in Details view.
  * Fixed an issue where item details were not being updated if the shell icon extraction failed for an item. ([Issue 97](https://code.google.com/p/imagelistview/issues/detail?id=97))
  * Thumbnails and metadata are now extracted using WIC (Windows Imaging Component). ([Issue 85](https://code.google.com/p/imagelistview/issues/detail?id=85)) Contributed by Jens.
  * With this release target framework is increased to .Net 3.0 to take advantage of WIC. Previous v7 branch will be continued to be supported for bug fixes.

# 7.7 for .Net 2.0 (22 September 2010) #

  * Mouse wheel scrolls by scrollbar settings. ([Issue 95](https://code.google.com/p/imagelistview/issues/detail?id=95))
  * Fixed a bug where item details could be updated by the UI thread while the cache thread was still working on them. ([Issue 94](https://code.google.com/p/imagelistview/issues/detail?id=94))
  * Fixed NoirRenderer's reflections to match the background color.
  * Fixed an issue where item details could not be read by the renderers. ([Issue 97](https://code.google.com/p/imagelistview/issues/detail?id=97))
  * ThumbnailImage, SmallIcon and LargeIcon properties of ImageListViewItem now return a clone of cached images from the cache manager. ([Issue 96](https://code.google.com/p/imagelistview/issues/detail?id=96))

# 7.6 (14 September 2010) #

  * Custom renderers now check for null item images.
  * Changed hard coded mouse scroll wheel parameters to system mouse settings.
  * Mouse wheel now scrolls by the system line scroll setting.
  * Shell icons are now managed by ImageListViewItemCacheManager.
  * Large preview images are now extracted before small thumbnails.

# 7.5 (12 September 2010) #

  * Fixed a bug where removing an item from the control while its thumbnail was being cached threw an exception. ([Issue 92](https://code.google.com/p/imagelistview/issues/detail?id=92))
  * Fixed a bug where adding items while the parent form was minimized did not refresh the control. ([Issue 93](https://code.google.com/p/imagelistview/issues/detail?id=93))

# 7.4 (07 September 2010) #

  * Added the CacheError event to the control. ([Issue 91](https://code.google.com/p/imagelistview/issues/detail?id=91))
  * BeginEdit and EndEdit methods work as intended. ([Issue 90](https://code.google.com/p/imagelistview/issues/detail?id=90))
  * Setting the FileName property of an item updates the item thumbnail.

# 7.3 (30 August 2010) #

  * Fixed a bug where the ItemCacheManager would continue working on old items even after they were removed from the control. (Follow up to [Issue 88](https://code.google.com/p/imagelistview/issues/detail?id=88))
  * Fixed a bug where the control wasn't redrawn after ItemCacheManager cached new items. (Follow up to [Issue 88](https://code.google.com/p/imagelistview/issues/detail?id=88))

# 7.2 (27 August 2010) #

  * Fixed a bug where SmallChange property of scrollbars was calculated incorrectly if the control was sized smaller than the item height. ([Issue 87](https://code.google.com/p/imagelistview/issues/detail?id=87))
  * Reordering items by dragging now displays the move cursor instead of the copy cursor. ([Issue 86](https://code.google.com/p/imagelistview/issues/detail?id=86))

# 7.1 (26 August 2010) #

  * Converted SmallIcon and LargeIcon properties of ImageListViewItems to type Image instead of Icon. This was done to prevent converting the icons on the fly at each item render. **This is a breaking change.**
  * ImageListViewItem now implements IDisposable to release its icon resources.
  * Buffered graphics is now properly disposed.
  * Child scrollbars are now disposed with the control.
  * Checkbox and file icons are now aligned properly in Details view.
  * Added the CanApplyColors property to renderers.
  * Designer preview items are now clipped to the client rectangle of the control.
  * Changing the IntegralScroll property now causes a layout update.

# 7.0 (26 August 2010) #

  * Added support for custom columns. ([Issue 52](https://code.google.com/p/imagelistview/issues/detail?id=52), [Issue 53](https://code.google.com/p/imagelistview/issues/detail?id=53), [Issue 55](https://code.google.com/p/imagelistview/issues/detail?id=55), [Issue 56](https://code.google.com/p/imagelistview/issues/detail?id=56))
  * The SortColumn property of the control is now an int value corresponding to column index. This change was necessary to support sorting of custom columns. **This is a breaking change.**
  * Added language resources for German. Thanks to Uwe.
  * Added a TypeConverter to ImageListViewColor to make it possible to modify the color palette from the object browser.
  * Added sub-items to items.
  * Added hover and click events for sub items in details view.
  * Improved the control designer. The designer now displays a preview of items on the control.
  * Added item borders to the default renderer for Gallery and Pane view modes so that item display is consistent with the Thumbnail view mode.
  * Fixed a bug where setting checkboxes and file icons the same alignment resulted in the checkboxes to move away from the icon location even when the ShowFileIcons property was set to false.
  * Fixed a bug in the scrollbar value calculation when the control had no items to display. ([Issue 84](https://code.google.com/p/imagelistview/issues/detail?id=84))

# 7.0 RC 3 (25 August 2010) #

  * Fixed built-in renderers to work with custom columns.
  * The designer now displays a preview of items on the control.
  * Added item borders to the default renderer for Gallery and Pane view modes.
  * Fixed a bug where setting checkboxes and file icons the same alignment resulted in the checkboxes to move away from the icon location even when the ShowFileIcons property was set to false.

# 7.0 RC 2 (25 August 2010) #

  * Custom columns can now be sorted. The SortColumn property of the control is now an int value. **This is a breaking change.**
  * Fixed a bug where subitem hover state was not updated correctly.
  * Added language resources for German. Thanks to Uwe.
  * Added a TypeConverter to ImageListViewColor to make it possible to modify the color palette from the object browser.

# 7.0 RC 1 (23 August 2010) #

  * Added support for custom columns. ([Issue 52](https://code.google.com/p/imagelistview/issues/detail?id=52), [Issue 53](https://code.google.com/p/imagelistview/issues/detail?id=53), [Issue 55](https://code.google.com/p/imagelistview/issues/detail?id=55), [Issue 56](https://code.google.com/p/imagelistview/issues/detail?id=56))

# 6.11 (20 August 2010) #

  * Fixed a bug where the layout update code was called after the control was disposed. ([Issue 82](https://code.google.com/p/imagelistview/issues/detail?id=82))
  * ImageListViewItems's are now clone-able. Useful when copying items between different instances of the control. ([Issue 83](https://code.google.com/p/imagelistview/issues/detail?id=83))

# 6.10 (03 August 2010) #

  * Fixed a bug where the old thumbnail size was used if the thumbnail size was changed while the cache manager was working. ([Issue 80](https://code.google.com/p/imagelistview/issues/detail?id=80))
  * Fixed a bug where items added while the control was invisible did not result in a layout update. ([Issue 81](https://code.google.com/p/imagelistview/issues/detail?id=81))
  * Fixed [Issue 77](https://code.google.com/p/imagelistview/issues/detail?id=77).

# 6.9 (08 Juny 2010) #

  * Fixed a bug where large thumbnail images of **virtual items** in gallery and pane view modes were not being loaded. ([Issue 79](https://code.google.com/p/imagelistview/issues/detail?id=79))

# 6.8 (07 June 2010) #

  * Fixed a bug where multiple items could be selected with the shift/ctrl+mouse even when the MultiSelect property was set to false. ([Issue 72](https://code.google.com/p/imagelistview/issues/detail?id=72))
  * Fixed a bug where the control was not refreshed after reading shell icons for non-image files. ([Issue 73](https://code.google.com/p/imagelistview/issues/detail?id=73))
  * Added the ShellIconFallback property to disable the display of shell icons in place of thumbnails. ([Issue 74](https://code.google.com/p/imagelistview/issues/detail?id=74))
  * Added the rating column ([Issue 54](https://code.google.com/p/imagelistview/issues/detail?id=54)).

# 6.7 (17 May 2010) #

  * Fixed a bug where scrollbars were not created with the control. ([Issue 71](https://code.google.com/p/imagelistview/issues/detail?id=71))
  * Added the IntegralScroll property to the control. When set to true, the scrollbars' large change property will be calculated as a multiple of item height. This however, has a slightly unpleasant side effect of the item area of the control sized somewhat larger than the minimum required. Setting IntegralScroll to false prevents this.

# 6.6 (12 May 2010) #

  * Fixed a bug where changing the Selected property of items did not refresh the control. ([Issue 68](https://code.google.com/p/imagelistview/issues/detail?id=68))
  * Fixed a bug where switching between designer and run-time resized the control to its initial size. ([Issue 70](https://code.google.com/p/imagelistview/issues/detail?id=70))

# 6.5 (21 April 2010) #

  * Fixed a bug where file types were not being cached after the first item. ([Issue 66](https://code.google.com/p/imagelistview/issues/detail?id=66))
  * Fixed a bug where moving the mouse to the edges of the control resulted in the selection rectangle to jump to the top.  ([Issue 67](https://code.google.com/p/imagelistview/issues/detail?id=67))

# 6.4 (21 April 2010) #

  * ImageListView now tries to guess the image format before attempting to load images. ([Issue 61](https://code.google.com/p/imagelistview/issues/detail?id=61))
  * When changing the thumbnail size property of the control, old thumbnails will no longer be cleared immediately. They will be replaced after new thumbnails are created.  ([Issue 64](https://code.google.com/p/imagelistview/issues/detail?id=64))
  * Shell icons are no longer cached per file extension, they are read for every item in the control. ([Issue 65](https://code.google.com/p/imagelistview/issues/detail?id=65))

# 6.3 (03 April 2010) #

  * Public properties of ImageListViewItem are now properly categorized. ([Issue 60](https://code.google.com/p/imagelistview/issues/detail?id=60))
  * Fixed an issue where the wrong exif tag was read for the DateTaken property. The Exif DateTime tag was being read instead of DateTimeOriginal. ([Issue 62](https://code.google.com/p/imagelistview/issues/detail?id=62))

# 6.2 (16 March 2010) #

  * Fixed a bug where virtual items could not be inserted if AllowDuplicateFileNames was set to true. ([Issue 51](https://code.google.com/p/imagelistview/issues/detail?id=51))

# 6.1 (11 March 2010) #

  * Added CheckedItems property to ImageListView to iterate checked items. ([Issue 47](https://code.google.com/p/imagelistview/issues/detail?id=47))
  * Added a new overload for the Items.Add method to provide an initial thumbnail for regular items (similar to virtual items). ([Issue 49](https://code.google.com/p/imagelistview/issues/detail?id=49))
  * Fixed an issue where checkbox and icon padding resulted in weird rendering in details view. ([Issue 50](https://code.google.com/p/imagelistview/issues/detail?id=50))

# 6.0 (10 March 2010) #

  * Added support for color themes. ([Issue 37](https://code.google.com/p/imagelistview/issues/detail?id=37)) Contributed by Robby.
  * Added file icons that can be displayed over items. ([Issue 39](https://code.google.com/p/imagelistview/issues/detail?id=39), [Issue 45](https://code.google.com/p/imagelistview/issues/detail?id=45), [Issue 46](https://code.google.com/p/imagelistview/issues/detail?id=46)) Contributed by Robby.
  * Added checkboxes to items. ([Issue 40](https://code.google.com/p/imagelistview/issues/detail?id=40), [Issue 46](https://code.google.com/p/imagelistview/issues/detail?id=46)) Contributed by Robby.
  * Fixed a bug where the hovered item was not updated while scrolling with the mouse wheel. ([Issue 42](https://code.google.com/p/imagelistview/issues/detail?id=42))
  * Scrollbars' large change property now calculated as a multiple of item height. ([Issue 43](https://code.google.com/p/imagelistview/issues/detail?id=43))

# 5.4 (03 March 2010) #

  * Added the MultiSelect property. ([Issue 32](https://code.google.com/p/imagelistview/issues/detail?id=32))
  * Fixed an issue where transparent images were always drawn with a white background  ([Issue 35](https://code.google.com/p/imagelistview/issues/detail?id=35))
  * Scrollbars can now be hidden with the new ScrollBars property. ([Issue 36](https://code.google.com/p/imagelistview/issues/detail?id=36))

# 5.3 (02 March 2010) #

  * Initial thumbnails of virtual items are now sized down to control's ThumbnailSize property on item add. ([Issue 33](https://code.google.com/p/imagelistview/issues/detail?id=33))

# 5.2 (01 March 2010) #

  * Fixed a bug where the control was not refreshed after removing all items ([Issue 29](https://code.google.com/p/imagelistview/issues/detail?id=29)).
  * ImageListView now falls back to shell icons if it cannot extract image thumbnails ([Issue 30](https://code.google.com/p/imagelistview/issues/detail?id=30)). Contributed by Robby.
  * Exif properties of ImageListViewItem are now marked as browsable ([Issue 31](https://code.google.com/p/imagelistview/issues/detail?id=31)).

# 5.1 (25 February 2010) #

  * Fixed a bug where source image files were being kept locked ([Issue 26](https://code.google.com/p/imagelistview/issues/detail?id=26)).
  * Removed the ImageListViewItem.GetImage method ([Issue 27](https://code.google.com/p/imagelistview/issues/detail?id=27)).

# 5.0 (17 February 2010) #

  * Added the CacheMode property. ([Issue 22](https://code.google.com/p/imagelistview/issues/detail?id=22))
  * Added Mono support. ([Issue 25](https://code.google.com/p/imagelistview/issues/detail?id=25))

# 4.8 (11 February 2010) #

  * Added the public VirtualItemKey property to ImageListViewItem ([Issue 24](https://code.google.com/p/imagelistview/issues/detail?id=24)).

# 4.7 (10 February 2010) #

  * Added the ItemHover and ColumnHover events ([Issue 23](https://code.google.com/p/imagelistview/issues/detail?id=23)).
  * Added the DropFiles event ([Issue 19](https://code.google.com/p/imagelistview/issues/detail?id=19)).

# 4.6 (27 January 2010) #

  * Fixed a bug in renderers where virtual item thumbnails were drawn without regards to the ThumbnailSize property ([Issue 17](https://code.google.com/p/imagelistview/issues/detail?id=17)).
  * Fixed a bug where the item click event was fired only if the item was not selected ([Issue 18](https://code.google.com/p/imagelistview/issues/detail?id=18)).

# 4.5 (26 January 2010) #

  * Fixed a race condition that occurred if an initial thumbnail image was supplied for a virtual item ([Issue 16](https://code.google.com/p/imagelistview/issues/detail?id=16)).
  * Added the RetryOnError property to the control. When set to true, the cache thread will continuously poll the control for a thumbnail, until it gets a valid image. When set to false, the cache thread will give up after the first error and display the ErrorImage.

# 4.4 (21 January 2010) #

  * The control is now scrolled while dragging items to the edges of the client area.

# 4.3 (21 January 2010) #

  * Fixed an issue where item text was not set in virtual mode. ([Issue 14](https://code.google.com/p/imagelistview/issues/detail?id=14))
  * Fixed an issue where the ImageListViewItemCollection.Clear method did not immediately empty the thumbnail cache. ([Issue 15](https://code.google.com/p/imagelistview/issues/detail?id=15))

# 4.2 (12 January 2010) #

  * An initial thumbnail image may now be specified while adding virtual items. ([Issue 13](https://code.google.com/p/imagelistview/issues/detail?id=13))

# 4.1 (12 January 2010) #

  * Virtual items now support drag and drop.

# 4.0 (12 January 2010) #

  * Added support for virtual items. ([Issue 12](https://code.google.com/p/imagelistview/issues/detail?id=12))

# 3.4 (04 January 2010) #

  * Fixed a bug in drag and drop controller. ([Issue 11](https://code.google.com/p/imagelistview/issues/detail?id=11))

# 3.3 (29 December 2009) #

  * Fixed a bug in NoirRenderer.DrawGalleryImage where the image was tried to be drawn when the control was empty.
  * NoirRenderer now displays a nicer insertion caret.
  * Renamed ImageListViewRenderer.OnDispose to Dispose.
  * Removed ImageListViewRenderer.DrawScrollBarFiller virtual method.

# 3.2 (29 December 2009) #

  * Added the new NoirRenderer. ([Issue 10](https://code.google.com/p/imagelistview/issues/detail?id=10))

# 3.1 (28 December 2009) #

  * XPRenderer now works in Pane view mode. ([Issue 8](https://code.google.com/p/imagelistview/issues/detail?id=8))
  * Drag-and-drop honors AllowDuplicateFileNames setting. ([Issue 9](https://code.google.com/p/imagelistview/issues/detail?id=9))

# 3.0 (27 December 2009) #

  * Added the new Pane view mode, removed PanelRenderer.
  * Fixed an issue where buffered graphics could not be created while repeatedly resizing the control.

# 2.8 (21 December 2009) #

  * Adjustable properties of built-in renderers are now public.
  * Removed ImageListView.ItemMargin property in favor of the new overridable ImageListViewRenderer.MeasureItemMargin method.
  * Gallery image is now updated after editing an item.
  * Moved column sort icons to the neutral resource.
  * Cleaned up the Utility class.

# 2.7 (20 December 2009) #

  * Fixed a bug where updating an item did not update the item thumbnail.

# 2.6 (20 December 2009) #

  * Added XML comments for all public fields.
  * Removed the ImageListViewRenderer.GetSortArrow function. Sort arrow is now drawn in the DrawColumnHeader method.
  * Fixed the issue about the missing semicolon in GIF files. ([Issue 6](https://code.google.com/p/imagelistview/issues/detail?id=6))
  * Removed the SortOrder enum, it was a duplicate of Windows.Forms.SortOrder.
  * Double clicking on a separator no longer rises a column click event.

# 2.5 (16 December 2009) #

  * The view is now scrolled while dragging the mouse outside the control's client area.
  * The selection rectangle can no longer paint over the scroll bars.

# 2.4 (16 December 2009) #

  * Panel renderer now displays the name of the image file in its panel in Thumbnail mode.
  * Removed the confusing AllowItemDrag property from ImageListView.
  * Moved the scroll timer in to the navigation manager.
  * Fixed a bug in ImageListViewItemCollection.RemoveAt method where the control's OnSelectionChanged event was fired after the item was removed from the collection.
  * Default renderer now shades the background of SortColumn in Details mode.

# 2.3 (15 December 2009) #

  * Hovering items while the control does not have focus now draws highlighted item background.
  * Changing BackColor and placeholder images redraws the control.
  * Sorting properties now check if a sort is actually needed.
  * Removed the Hovered property from ImageListViewItem and ImageListViewColumnHeader.
  * The wait cursor is displayed while sorting items.
  * Fixed issues with keyboard navigation and drag&drop logic. (inc. [Issue 1](https://code.google.com/p/imagelistview/issues/detail?id=1), [Issue 4](https://code.google.com/p/imagelistview/issues/detail?id=4), [Issue 5](https://code.google.com/p/imagelistview/issues/detail?id=5) and [Issue 7](https://code.google.com/p/imagelistview/issues/detail?id=7))

# 2.2 (13 December 2009) #

  * Cleaned up the utility class.
  * Moved default and error images to the neutral resource.
  * Fixed places where ImageListViewRenderer.Refresh was called instead of Control.Refresh. This should increase rendering performance.

# 2.1 (11 December 2009) #

  * Fixed a bug where cache managers tried to invoke the control after its handle was destroyed.

# 2.0 (09 December 2009) #

  * Cache threads now exit gracefully.
  * Added new column types for common Exif tags.
  * Added English and Turkish resources for new columns.
  * Panel renderer displays Exif tags from item properties.
  * TilesRenderer's default width is now 150 pixels.
  * Added the new column types to ImageListViewItemComparer.

# 1.4 (07 December 2009) #

  * Added the BeginEdit and EndEdit methods to ImageListViewItem. They should be used while editing items to prevent collisions with cache threads.
  * Custom renderers now use the central thumbnail cache instead of their own worker threads.

# 1.3 (04 December 2009) #

  * Moved the ThumbnailFromFile method to the static Utility class. It is used by all renderers.
  * Renderers now check the thumbnails for errors.
  * ImageListViewItem.GetSubItemText now returns an empty string for uninitialized item properties.
  * ImageListViewCacheManager now checks the thumbnails for errors.
  * StringFormats are now properly disposed.

# 1.2 (03 December 2009) #

  * Cached images are now properly disposed.

# 1.1 (30 November 2009) #
  * Fixed ImageListView.EnsureVisible to work with the Gallery mode.
  * ImageListViewCacheManager now properly zeroes out the memory used when it clears the cache.
  * The preview image is now loaded by a background thread in Gallery view mode.
  * Threaded renderers now use a BackgroundWorker.
  * Preview image in Gallery mode is now cached.
  * Introduced Tuples, used mainly while talking to worker threads.
  * PanelRenderer now displays some common Exif tags.

# 1.0 (28 November 2009) #
  * Added the Gallery view mode. ([Issue 2](https://code.google.com/p/imagelistview/issues/detail?id=2))
  * Modified the hit testing, scrolling and the custom renderers to work with the Gallery view mode.

# 0.18 (27 November 2009) #

  * Control border is now drawn by the framework.
  * Fixed a bug in layout manager where number of visible columns or rows could be zero when the control is sized too small.
  * Added the new overridable method, OnLayout to the ImageListViewRenderer. It can be used to modify the size of the item area by custom renderers.
  * Added the new PanelRenderer.
  * Fixed a bug in TilesRenderer where the caption font was requested before being created.
  * Fixed the ImageListView.HitTest method to work with the new ImageListViewRenderer.OnLayout method.

# 0.17 (26 November 2009) #

  * Maximum size of the thumbnail cache can now be (approximately) set by the user using the new ImageListView.CacheLimit property.
  * Removing items now correctly removes associated thumbnails from the cache. To decrease the number of locks, removed item thumbnails are purged from the cache in batches of 25% of ImageListView.CacheLimit.

# 0.16 (24 November 2009) #

  * Caption font of TilesRenderer is now disposed properly.
  * Removed the CacheSize property from ImageListView and ImageListViewCacheManager. It wasn't properly implemented and resulted in deadlocks.
  * Added a check to ImageListViewRenderer.Render() to return immediately if the renderer is disposed.

# 0.15 (24 November 2009) #

  * Zoom percentage can now be specified in the constructor of ZoomingRenderer.

# 0.14 (23 November 2009) #

  * ZoomingRenderer is now threaded.
  * Organized custom renderers in a static ImageListViewRenderers class.

# 0.13 (23 November 2009) #

  * Fixed a bug in rendering draw order.
  * Added the GetImage() method to ImageListViewItem.
  * Added ItemAreaBounds and ColumnHeaderBounds properties to ImageListViewRenderer.
  * Added new custom renderers.

# 0.12 (22 November 2009) #

  * Renderers can now draw items in a specific order using the new ImageListViewRenderer.ItemDrawOrder property. A finer control is also possible using the new ImageListViewItem.ZOrder property.
  * The previous renderer is now properly disposed when ImageListView.SetRenderer is called.

# 0.11 (21 November 2009) #

  * Added the Clip property and the InitializeGraphics virtual function to ImageListViewRenderer.

# 0.10 (19 November 2009) #

  * Fixed an issue where the item border was not drawn if the item had the focus.

# 0.9 (18 November 2009) #

  * Fixed a curious bug in the layout code. It appears that Control.Visible returns not what you set in code, but whether the control is actually visible (the control and its parent are visible) This resulted in a stack overflow if the layout code was called before ImageListView became visible. Visible property of child scrollbars always returned false because their parent was not visible. It was quite hard to identify the issue, although the fix was straightforward (keep separate variables for scrollbars visible properties).

# 0.8 (16 November 2009) #

  * Fixed a bug in the OnDragDrop handler. The base class method was being called at the start of the function. It is now called at the end, after the drop operation is completed.

# 0.7 (14 November 2009) #

  * Reduced the number of locks in worker threads.
  * A list of visible items is being kept to speed up item lookups.

# 0.6 (12 November 2009) #

Project moved to Google Code.

## Previous Release Notes ##

  * The framework requirement is droppped down to .NET 2.0. This version is identical to previous 3.5 version both feature-wise and performance-wise.
  * Item properties are read by a background thread to speed up item addition.
  * Items can now be reordered by dragging them in the control.
  * Item details added for image dimensions and resolution.
  * Fixed a bug while iterating the selected item collection threw an exception.
  * Item file types are now cached. Adding items should be much faster.
  * Fixed a bug where right clicking on the control when the item collection is empty threw an exception.
  * Fixed a bug where removing items while iterating through the selected item collection threw an exception.
  * Fixed a visual glitch where removing all items from the control at once did not set the scroll coordinates to 0, 0.
  * Added drag&drop support. Image files recognized by the .NET framework (whatever Image.FromStream accepts) can now be dragged on to the control. Also image thumbnails can now be dragged from the control as files. Drag&drop behavior can be customized using the AllowDrag and AllowDrop properties.
  * Added a new option for detecting duplicate filenames. When AllowDuplicateFileNames property is set to false, the control will silently refuse to add an image file, if it already exists in the items collection. Comparison is made between filenames as case-insensitive ordinal string comparison.
  * Fixed a bug where right clicking on selected items cleared the selection.
  * Fixed a glitch where the control's text (hidden property) was serialized as null.
  * ImageListView can now extract embedded EXIF thumbnails. A new property (UseEmbeddedThumbnails) is added to customize this behavior.
  * A public Index property is added to the ImageListViewItem class.
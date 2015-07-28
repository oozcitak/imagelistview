To run ImageListView on Mono, Windows Imaging Component support needs to be disabled. See [this wiki page](WICSupport.md) for more information.

ImageListView was tested on Mono 2.6.1. The control is almost feature complete on Mono with two exceptions:

  * The Type column is not populated. This is a known issue and will be addressed in a future release.
  * Thumbnail generation with large images is slower than on .NET, due to the fact that a [particular overload](http://msdn.microsoft.com/en-us/library/21zw9ah6.aspx) of the Image.FromStream method used by the control is not yet supported on Mono.

![http://imagelistview.googlecode.com/svn/wiki/MonoSupport.demo.jpg](http://imagelistview.googlecode.com/svn/wiki/MonoSupport.demo.jpg)

_ImageListView on Ubuntu 9.10 via Mono_
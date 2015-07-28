Although ImageListView requires the .NET framework 3.5; it is possible to recompile it for .NET 2.0 without Windows Imaging Component.

  1. [Download](http://code.google.com/p/imagelistview/downloads/list) or [checkout](http://code.google.com/p/imagelistview/source/checkout) the latest source code.
  1. Open project properties and change the target framework to .NET 2.0 in the Application tab.
  1. Delete the USEWIC conditional compilation symbol from the Build tab.
  1. Recompile the project.
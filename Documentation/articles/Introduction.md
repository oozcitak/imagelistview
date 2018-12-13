---
uid: Articles.Introduction
title: Introduction
---
# Introduction #

ImageListView is a .NET control for displaying a list of image files. It looks and operates similar to the standard ListView control. Image thumbnails are loaded asynchronously with a separate background thread.

ImageListView requires .NET framework 3.5 to take advantage of the Windows Imaging Component. It is possible to recompile ImageListView [without Windows Imaging Component](xref:Articles.WICSupport).

# Features #

  * Asynchronously loaded image thumbnails
  * [Custom renderers](xref:Articles.CustomRenderers)
  * [Thumbnails, Gallery, Pane and Details view modes](xref:Articles.ViewModes)
  * Ability to extract embedded thumbnails
  * Drag&drop support
  * Works with .NET 3.5
  * Optionally works with [Mono 2.6](xref:Articles.MonoSupport)

![ImageListView](../resources/images/ImageListView.thumbnails.jpg)


# Installation #

If you are using [NuGet](https://nuget.org/) search for `ImageListView` in the NuGet Package Manager or install the assembly from the package manager console with:

`PM> Install-Package ImageListView`
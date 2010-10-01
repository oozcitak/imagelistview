// ImageListView - A listview control for image files
// Copyright (C) 2009 Ozgur Ozcitak
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Ozgur Ozcitak (ozcitak@yahoo.com)

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.Generic;


namespace Manina.Windows.Forms
{
    #region Event Delegates
    /// <summary>
    /// Represents the method that will handle the AsyncImageLoaderCompleted event.
    /// </summary>
    /// <param name="sender">The object that is the source of the event.</param>
    /// <param name="e">An ImageLoaderCompletedEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void AsyncImageLoaderCompletedEventHandler(object sender, AsyncImageLoaderCompletedEventArgs e);
    /// <summary>
    /// Represents the method that will handle the AsyncImageLoaderGetUserImage event.
    /// </summary>
    /// <param name="sender">The object that is the source of the event.</param>
    /// <param name="e">An AsyncImageLoaderGetUserImageEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void AsyncImageLoaderGetUserImageEventHandler(object sender, AsyncImageLoaderGetUserImageEventArgs e);
    #endregion

    #region Event Arguments
    /// <summary>
    /// Represents the event arguments of the AsyncImageLoaderCompleted event.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class AsyncImageLoaderCompletedEventArgs
    {
        /// <summary>
        /// Gets the key of the item. The key is passed to the ImageLoader
        /// with the LoadAsync method.
        /// </summary>
        public object Key { get; private set; }
        /// <summary>
        /// Gets the size of the requested image.
        /// </summary>
        public Size Size { get; private set; }
        /// <summary>
        /// Gets embedded thumbnail extraction behavior.
        /// </summary>
        public UseEmbeddedThumbnails UseEmbeddedThumbnails { get; private set; }
        /// <summary>
        /// Gets whether the image should be rotated based on orientation
        /// metadata.
        /// </summary>
        public bool AutoRotate { get; private set; }
        /// <summary>
        /// Gets the loaded image. This property can be null if an error
        /// occurred while loading the image.
        /// </summary>
        public Image Image { get; private set; }
        /// <summary>
        /// Gets the error that occurred while loading the image.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ImageLoaderCompletedEventArgs class.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <param name="image">The loaded image.</param>
        /// <param name="error">The error that occurred while loading the image.</param>
        public AsyncImageLoaderCompletedEventArgs(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate, Image image, Exception error)
        {
            Key = key;
            Size = size;
            UseEmbeddedThumbnails = useEmbeddedThumbnails;
            AutoRotate = autoRotate;
            Image = image;
            Error = error;
        }
    }
    /// <summary>
    /// Represents the event arguments of the AsyncImageLoaderGetUserImage event.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class AsyncImageLoaderGetUserImageEventArgs
    {
        /// <summary>
        /// Gets the key of the item. The key is passed to the ImageLoader
        /// with the LoadAsync method.
        /// </summary>
        public object Key { get; private set; }
        /// <summary>
        /// Gets the size of the requested image.
        /// </summary>
        public Size Size { get; private set; }
        /// <summary>
        /// Gets embedded thumbnail extraction behavior.
        /// </summary>
        public UseEmbeddedThumbnails UseEmbeddedThumbnails { get; private set; }
        /// <summary>
        /// Gets whether the image should be rotated based on orientation
        /// metadata.
        /// </summary>
        public bool AutoRotate { get; private set; }
        /// <summary>
        /// Gets or sets the loaded image. This property can be null if an error
        /// occurred while loading the image.
        /// </summary>
        public Image Image { get; set; }
        /// <summary>
        /// Gets the error that occurred while loading the image.
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// Initializes a new instance of the ImageLoaderCompletedEventArgs class.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        public AsyncImageLoaderGetUserImageEventArgs(object key, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool autoRotate)
        {
            Key = key;
            Image = null;
            Error = null;
            Size = size;
            UseEmbeddedThumbnails = useEmbeddedThumbnails;
            AutoRotate = autoRotate;
        }
    }
    #endregion
}

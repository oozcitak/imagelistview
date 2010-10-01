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
    /// Represents the method that will handle the ImageLoaderCompleted event.
    /// </summary>
    /// <param name="sender">The object that is the source of the event.</param>
    /// <param name="e">An ImageLoaderCompletedEventArgs that contains event data.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void AsyncImageLoaderCompletedEventHandler(object sender, AsyncImageLoaderCompletedEventArgs e);
    #endregion

    #region Event Arguments
    /// <summary>
    /// Represents the event arguments of the ImageLoaderCompleted event.
    /// </summary>
    [Serializable, ComVisible(true)]
    public class AsyncImageLoaderCompletedEventArgs
    {
        /// <summary>
        /// Gets the key of the item. The key is passed to the ImageLoader
        /// with the Load method.
        /// </summary>
        public object Key { get;private set; }
        /// <summary>
        /// Gets the loaded image. This property can be null if an error
        /// occurred while loading the image.
        /// </summary>
        public Image Image { get;private set; }
        /// <summary>
        /// Gets the error that occurred while loading the image.
        /// </summary>
        public Exception Error { get;private set;}

        /// <summary>
        /// Initializes a new instance of the ImageLoaderCompletedEventArgs class.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <param name="image">The loaded image.</param>
        /// <param name="error">The error that occurred while loading the image.</param>
        public AsyncImageLoaderCompletedEventArgs(object key, Image image, Exception error)
        {
            Key = key;
            Image = image;
            Error = error;
        }
    }
    #endregion
}

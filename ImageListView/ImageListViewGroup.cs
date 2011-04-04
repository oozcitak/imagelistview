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
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.Drawing.Design;
using System.Resources;
using System.Reflection;
using System.Drawing;

namespace Manina.Windows.Forms
{
    public partial class ImageListView
    {
        /// <summary>
        /// Represents the collection of items in a group in an ImageListView control.
        /// </summary>
        internal class ImageListViewGroup : IEnumerable<ImageListViewItem>, IEnumerable, IComparable<ImageListViewGroup>
        {
            #region Member Variables
            internal ImageListView mImageListView;
            internal ImageListViewGroupCollection owner;
            private bool mCollapsed;
            // Layout variables
            internal int itemCols;
            internal int itemRows;
            internal Rectangle itemBounds;
            internal Rectangle headerBounds;
            internal bool isVisible;
            #endregion

            #region Properties
            /// <summary>
            /// Gets the name of the group.
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// Gets the index of the first item.
            /// </summary>
            public int FirstItemIndex { get; set; }
            /// <summary>
            /// Gets the index of the last item.
            /// </summary>
            public int LastItemIndex { get; set; }
            /// <summary>
            /// Gets or sets whether the group is collapsed.
            /// </summary>
            public bool Collapsed
            {
                get
                {
                    return mCollapsed;
                }
                set
                {
                    if (value != mCollapsed)
                    {
                        mCollapsed = value;
                        if (owner != null)
                            owner.collectionModified = true;
                        if (mImageListView != null)
                            mImageListView.Refresh();
                    }
                }
            }
            /// <summary>
            /// Gets the item count.
            /// </summary>
            public int ItemCount { get { return LastItemIndex - FirstItemIndex + 1; } }
            #endregion

            #region Constructor
            /// <summary>
            /// Initializes a new instance of the <see cref="ImageListViewGroup"/>  class.
            /// </summary>
            /// <param name="name">The name of the group.</param>
            /// <param name="firstItemIndex">The index of the first item.</param>
            /// <param name="lastItemIndex">The index of the last item.</param>
            internal ImageListViewGroup(string name, int firstItemIndex, int lastItemIndex)
            {
                mImageListView = null;
                owner = null;
                Name = name;
                mCollapsed = false;
                FirstItemIndex = firstItemIndex;
                LastItemIndex = lastItemIndex;
            }
            #endregion

            #region Instance Methods
            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<ImageListViewItem> GetEnumerator()
            {
                for (int i = FirstItemIndex; i <= LastItemIndex; i++)
                    yield return mImageListView.Items[i];
                yield break;
            }
            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            /// <summary>
            /// Compares the current object with another object of the same type.
            /// </summary>
            /// <param name="other">An object to compare with this object.</param>
            /// <returns>
            /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings:
            /// Value
            /// Meaning
            /// Less than zero
            /// This object is less than the <paramref name="other"/> parameter.
            /// Zero
            /// This object is equal to <paramref name="other"/>.
            /// Greater than zero
            /// This object is greater than <paramref name="other"/>.
            /// </returns>
            public int CompareTo(ImageListViewGroup other)
            {
                return string.Compare(Name, other.Name);
            }
            #endregion
        }
    }
}
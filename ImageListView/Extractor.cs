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
//
// WIC support coded by Jens

using System.Drawing;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Extracts thumbnails from images.
    /// </summary>
    internal static class Extractor
    {
        public static IExtractor Instance { get; private set; }

        static Extractor()
        {
#if USEWIC
            Instance = new WPFExtractor();
#else
            Instance = new GDIExtractor();
#endif
        }
    }
}

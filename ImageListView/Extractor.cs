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

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Manina.Windows.Forms
{
    /// <summary>
    /// Extracts thumbnails from images.
    /// </summary>
    internal static class Extractor
    {
#if USEWIC
        private static bool useWIC = true;
#else
        private static bool useWIC = false;
#endif
        private static IExtractor instance = null;

        public static IExtractor Instance
        {
            get
            {
                if (instance == null)
                {
                    if (!useWIC)
                    {
                        instance = new GDIExtractor();
                    }
                    else
                    {
                        try
                        {
                            string programFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string pluginFileName = Path.Combine(programFolder, "WPFThumbnailExtractor.dll");
                            instance = LoadFrom(pluginFileName);
                        }
                        catch (Exception e)
                        {
                            System.Windows.Forms.MessageBox.Show(e.ToString());
                            instance = new GDIExtractor();
                        }
                    }
                }

                if (instance == null)
                    instance = new GDIExtractor();

                return instance;
            }
        }

        private static IExtractor LoadFrom(string pluginFileName)
        {
            Assembly assembly = Assembly.LoadFrom(pluginFileName);
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetInterfaces().Contains(typeof(IExtractor)) && !type.IsInterface && type.IsClass && !type.IsAbstract)
                {
                    return (IExtractor)Activator.CreateInstance(type, new object[0]);
                }
            }

            return null;
        }

        public static bool UseWIC
        {
            get
            {
                return useWIC;
            }
            set
            {
#if USEWIC
                useWIC = value;
                instance = null;
#else
                useWIC = false;
                if (value)
                    System.Diagnostics.Debug.WriteLine("Trying to set UseWIC option although the library was compiled without WPF/WIC support.");
#endif
            }
        }
    }
}

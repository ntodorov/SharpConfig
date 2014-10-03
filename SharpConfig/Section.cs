/*
 * Copyright (c) 2013-2014 Cemalettin Dervis
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
 * OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpConfig
{
    /// <summary>
    /// Represents a group of <see cref="Setting"/> objects.
    /// </summary>
    public sealed class Section : ConfigurationElement, IEnumerable<Setting>
    {
        private List<Setting> mSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        ///
        /// <param name="name">The name of the section.</param>
        public Section( string name )
            : base( name )
        {
            mSettings = new List<Setting>();
        }

        /// <summary>
        /// Creates an object of a specific type, and maps the settings
        /// in this section to the public properties of the object.
        /// </summary>
        /// 
        /// <returns>The created object.</returns>
        /// 
        /// <remarks>
        /// The specified type must have a public default constructor
        /// in order to be created.
        /// </remarks>
        public T CreateObject<T>() where T : class
        {
            Type type = typeof( T );

            try
            {
                T obj = Activator.CreateInstance<T>();

                MapTo( obj, false );

                return obj;
            }
            catch ( Exception )
            {
                throw new ArgumentException( string.Format(
                    "The type '{0}' does not have a default public constructor.",
                    type.Name ) );
            }
        }

        /// <summary>
        /// Assigns the values of this section to an object's public properties.
        /// </summary>
        /// 
        /// <param name="obj">The object that is modified based on the section.</param>
        public void MapTo<T>( T obj ) where T : class
        {
            MapTo<T>( obj, false );
        }

        /// <summary>
        /// Assigns the values of this section to an object's public properties.
        /// </summary>
        /// 
        /// <param name="obj">The object that is modified based on the section.</param>
        /// <param name="ignoreCase">
        ///     A value indicating whether a case-sensitive assignment is performed.
        /// </param>
        public void MapTo<T>( T obj, bool ignoreCase ) where T : class
        {
            if ( obj == null )
                throw new ArgumentNullException( "obj" );

            Type type = typeof( T );

            var properties = type.GetProperties();

            foreach ( var prop in properties )
            {
                if ( !prop.CanWrite )
                    continue;

                var setting = GetSetting( prop.Name, ignoreCase );

                if ( setting != null )
                {
                    object value = setting.GetValue( prop.PropertyType );

                    prop.SetValue( obj, value, null );
                }
            }
        }

        /// <summary>
        /// Gets an enumerator that iterates through the section.
        /// </summary>
        public IEnumerator<Setting> GetEnumerator()
        {
            return mSettings.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator that iterates through the section.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a setting to the section.
        /// </summary>
        /// <param name="setting">The setting to add.</param>
        public void Add( Setting setting )
        {
            if ( setting == null )
                throw new ArgumentNullException( "setting" );

            if ( Contains( setting ) )
            {
                throw new ArgumentException(
                    "The specified setting already exists in the section." );
            }

            mSettings.Add( setting );
        }

        /// <summary>
        /// Clears the section of all settings.
        /// </summary>
        public void Clear()
        {
            mSettings.Clear();
        }

        /// <summary>
        /// Determines whether a specified setting is contained in the section.
        /// </summary>
        /// <param name="setting">The setting to check for containment.</param>
        /// <returns>True if the setting is contained in the section; false otherwise.</returns>
        public bool Contains( Setting setting )
        {
            return mSettings.Contains( setting );
        }

        /// <summary>
        /// Removes a setting from this section by its name.
        /// </summary>
        /// <param name="settingName">The case-sensitive name of the setting to remove.</param>
        public void Remove( string settingName )
        {
            if ( string.IsNullOrEmpty( settingName ) )
                throw new ArgumentNullException( "settingName" );

            var setting = GetSetting( settingName, false );

            if ( setting == null )
            {
                throw new ArgumentException(
                    "The specified setting does not exist in the section." );
            }

            mSettings.Remove( setting );
        }

        /// <summary>
        /// Removes a setting from the section.
        /// </summary>
        /// <param name="setting">The setting to remove.</param>
        public void Remove( Setting setting )
        {
            if ( setting == null )
                throw new ArgumentNullException( "setting" );

            if ( !Contains( setting ) )
            {
                throw new ArgumentException(
                    "The specified setting does not exist in the section." );
            }

            mSettings.Remove( setting );
        }

        /// <summary>
        /// Gets the number of settings that are in the section.
        /// </summary>
        public int SettingCount
        {
            get { return mSettings.Count; }
        }

        /// <summary>
        /// Gets or sets a setting by index.
        /// </summary>
        /// <param name="index">The index of the setting in the section.</param>
        public Setting this[int index]
        {
            get
            {
                if ( index < 0 || index >= mSettings.Count )
                    throw new ArgumentOutOfRangeException( "index" );

                return mSettings[index];
            }
            set
            {
                if ( index < 0 || index >= mSettings.Count )
                    throw new ArgumentOutOfRangeException( "index" );

                mSettings[index] = value;
            }
        }

        /// <summary>
        /// Gets or sets a setting by its name.
        /// </summary>
        ///
        /// <param name="name">The name of the setting.</param>
        ///
        /// <returns>
        /// The setting if found, otherwise a new setting with
        /// the specified name is created, added to the section and returned.
        /// </returns>
        public Setting this[string name]
        {
            get
            {
                var setting = GetSetting( name, !Configuration.IsCaseSensitive );

                if ( setting == null )
                {
                    setting = new Setting( name );
                    Add( setting );
                }

                return setting;
            }
            set
            {
                // Check if there already is a setting by that name.
                var setting = GetSetting( name, false );

                int settingIndex = setting != null ? mSettings.IndexOf( setting ) : -1;

                if ( settingIndex < 0 )
                {
                    // A setting with that name does not exist yet; add it.
                    mSettings.Add( setting );
                }
                else
                {
                    // A setting with that name exists; overwrite.
                    mSettings[settingIndex] = setting;
                }
            }
        }

        private Setting GetSetting( string name, bool ignoreCase )
        {
            var strCmp = ignoreCase ?
                StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach ( var setting in mSettings )
            {
                if ( string.Equals( setting.Name, name, strCmp ) )
                    return setting;
            }

            return null;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        ///
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return ToString( false );
        }

        /// <summary>
        /// Convert this object into a string representation.
        /// </summary>
        ///
        /// <param name="includeComment">True to include, false to exclude the comment.</param>
        ///
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public string ToString( bool includeComment )
        {
            if ( includeComment && Comment != null )
                return string.Format( "[{0}] {1}", Name, Comment.ToString() );
            else
                return string.Format( "[{0}]", Name );
        }
    }
}
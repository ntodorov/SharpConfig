/*
 * Copyright (c) 2013-2015 Cemalettin Dervis
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpConfig
{
    /// <summary>
    /// Represents a configuration.
    /// Configurations contain one or multiple sections
    /// that in turn can contain one or multiple settings.
    /// The <see cref="Configuration"/> class is designed
    /// to work with classic configuration formats such as
    /// .ini and .cfg, but is not limited to these.
    /// </summary>
    public partial class Configuration : IEnumerable<Section>
    {
        #region Fields

        private static NumberFormatInfo mNumberFormat;
        private static char[] mValidCommentChars;

        private List<Section> mSections;

        #endregion

        #region Construction

        static Configuration()
        {
            mNumberFormat = CultureInfo.InvariantCulture.NumberFormat;
            mValidCommentChars = new[] { '#', ';', '\'' };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration()
        {
            mSections = new List<Section>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets an enumerator that iterates through the configuration.
        /// </summary>
        public IEnumerator<Section> GetEnumerator()
        {
            return mSections.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator that iterates through the configuration.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a section to the configuration.
        /// </summary>
        /// <param name="section">The section to add.</param>
        public void Add( Section section )
        {
            if ( section == null )
                throw new ArgumentNullException( "section" );

            if ( Contains( section ) )
            {
                throw new ArgumentException(
                    "The specified section already exists in the configuration." );
            }

            mSections.Add( section );
        }

        /// <summary>
        /// Clears the configuration of all sections.
        /// </summary>
        public void Clear()
        {
            mSections.Clear();
        }

        /// <summary>
        /// Determines whether a specified section is contained in the configuration.
        /// </summary>
        /// <param name="section">The section to check for containment.</param>
        /// <returns>True if the section is contained in the configuration; false otherwise.</returns>
        public bool Contains( Section section )
        {
            return mSections.Contains( section );
        }

        /// <summary>
        /// Determines whether a specifically named setting is contained in the section.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>True if the setting is contained in the section; false otherwise.</returns>
        public bool Contains( string sectionName )
        {
            return GetSection( sectionName ) != null;
        }

        /// <summary>
        /// Removes a section from this section by its name.
        /// </summary>
        /// <param name="sectionName">The case-sensitive name of the section to remove.</param>
        public void Remove( string sectionName )
        {
            if ( string.IsNullOrEmpty( sectionName ) )
                throw new ArgumentNullException( "sectionName" );

            var section = GetSection( sectionName );

            if ( section == null )
            {
                throw new ArgumentException(
                    "The specified section does not exist in the section." );
            }

            Remove( section );
        }

        /// <summary>
        /// Removes a section from the configuration.
        /// </summary>
        /// <param name="section">The section to remove.</param>
        public void Remove( Section section )
        {
            if ( section == null )
                throw new ArgumentNullException( "section" );

            if ( !Contains( section ) )
            {
                throw new ArgumentException(
                    "The specified section does not exist in the section." );
            }

            mSections.Remove( section );
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads a configuration from a file auto-detecting the encoding and
        /// using the default parsing settings.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromFile( string filename )
        {
            return LoadFromFile( filename, null );
        }

        /// <summary>
        /// Loads a configuration from a file.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="encoding">The encoding applied to the contents of the file. Specify null to auto-detect the encoding.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromFile( string filename, Encoding encoding )
        {
            if ( !File.Exists( filename ) )
                throw new FileNotFoundException( "Configuration file not found.", filename );

            Configuration cfg = null;

            if ( encoding == null )
                cfg = LoadFromText( File.ReadAllText( filename ) );
            else
                cfg = LoadFromText( File.ReadAllText( filename, encoding ) );

            return cfg;
        }

        /// <summary>
        /// Loads a configuration from a text stream auto-detecting the encoding and
        /// using the default parsing settings.
        /// </summary>
        ///
        /// <param name="stream">The text stream to load the configuration from.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromStream( Stream stream )
        {
            return LoadFromStream( stream, null );
        }

        /// <summary>
        /// Loads a configuration from a text stream.
        /// </summary>
        ///
        /// <param name="stream">   The text stream to load the configuration from.</param>
        /// <param name="encoding"> The encoding applied to the contents of the stream. Specify null to auto-detect the encoding.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromStream( Stream stream, Encoding encoding )
        {
            if ( stream == null )
                throw new ArgumentNullException( "stream" );

            string source = null;

            var reader = encoding == null ?
                new StreamReader( stream ) : new StreamReader( stream, encoding );

            using ( reader )
            {
                source = reader.ReadToEnd();
                reader.Close();
            }

            return LoadFromText( source );
        }

        /// <summary>
        /// Loads a configuration from text (source code).
        /// </summary>
        ///
        /// <param name="source">   The text (source code) of the configuration.</param>
        ///
        /// <returns>
        /// The loaded <see cref="Configuration"/> object.
        /// </returns>
        public static Configuration LoadFromText( string source )
        {
            if ( string.IsNullOrEmpty( source ) )
                throw new ArgumentNullException( "source" );

            return Parse( source );
        }

        #endregion

        #region LoadBinary

        /// <summary>
        /// Loads a configuration from a binary file using the <b>default</b> <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        public static Configuration LoadBinary( string filename )
        {
            if (string.IsNullOrEmpty( filename ))
                throw new ArgumentNullException( "filename" );

            return DeserializeBinary( null, filename );
        }

        /// <summary>
        /// Loads a configuration from a binary file using a specific <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="reader">  The reader to use. Specify null to use the default <see cref="BinaryReader"/>.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        public static Configuration LoadBinary( string filename, BinaryReader reader )
        {
            if ( string.IsNullOrEmpty( filename ) )
                throw new ArgumentNullException( "filename" );
            
            return DeserializeBinary( reader, filename );
        }

        /// <summary>
        /// Loads a configuration from a binary stream, using the <b>default</b> <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to load the configuration from.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        public static Configuration LoadBinary( Stream stream )
        {
            if (stream == null)
                throw new ArgumentNullException( "stream" );

            return DeserializeBinary( null, stream );
        }

        /// <summary>
        /// Loads a configuration from a binary stream, using a specific <see cref="BinaryReader"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to load the configuration from.</param>
        /// <param name="reader">The reader to use. Specify null to use the default <see cref="BinaryReader"/>.</param>
        ///
        /// <returns>
        /// The loaded configuration.
        /// </returns>
        public static Configuration LoadBinary( Stream stream, BinaryReader reader )
        {
            if ( stream == null )
                throw new ArgumentNullException( "stream" );

            return DeserializeBinary( reader, stream );
        }

        #endregion

        #region Save

        /// <summary>
        /// Saves the configuration to a file using the default character encoding, which is UTF8.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        public void Save( string filename )
        {
            Save( filename, null );
        }

        /// <summary>
        /// Saves the configuration to a file.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="encoding">The character encoding to use. Specify null to use the default encoding, which is UTF8.</param>
        public void Save( string filename, Encoding encoding )
        {
            if ( string.IsNullOrEmpty( filename ) )
                throw new ArgumentNullException( "filename" );

            Serialize( filename, encoding );
        }

        /// <summary>
        /// Saves the configuration to a stream using the default character encoding, which is UTF8.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        public void Save( Stream stream )
        {
            Save( stream, null );
        }

        /// <summary>
        /// Saves the configuration to a stream.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        /// <param name="encoding">The character encoding to use. Specify null to use the default encoding, which is UTF8.</param>
        public void Save( Stream stream, Encoding encoding )
        {
            if ( stream == null )
                throw new ArgumentNullException( "stream" );

            Serialize( stream, encoding );
        }

        #endregion

        #region SaveBinary

        /// <summary>
        /// Saves the configuration to a binary file, using the default <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        public void SaveBinary( string filename )
        {
            SaveBinary( filename, null );
        }

        /// <summary>
        /// Saves the configuration to a binary file, using a specific <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="filename">The location of the configuration file.</param>
        /// <param name="writer">  The writer to use. Specify null to use the default writer.</param>
        public void SaveBinary( string filename, BinaryWriter writer )
        {
            if ( string.IsNullOrEmpty( filename ) )
                throw new ArgumentNullException( "filename" );

            SerializeBinary( writer, filename );
        }

        /// <summary>
        /// Saves the configuration to a binary stream, using the default <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        public void SaveBinary( Stream stream )
        {
            SaveBinary( stream, null );
        }

        /// <summary>
        /// Saves the configuration to a binary file, using a specific <see cref="BinaryWriter"/>.
        /// </summary>
        ///
        /// <param name="stream">The stream to save the configuration to.</param>
        /// <param name="writer">The writer to use. Specify null to use the default writer.</param>
        public void SaveBinary( Stream stream, BinaryWriter writer )
        {
            if ( stream == null )
                throw new ArgumentNullException( "stream" );

            SerializeBinary( writer, stream );
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the number format that is used for value conversion in Section.GetValue().
        /// The default value is <b>CultureInfo.InvariantCulture.NumberFormat</b>.
        /// </summary>
        public static NumberFormatInfo NumberFormat
        {
            get { return mNumberFormat; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException( "value" );

                mNumberFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the array that contains all comment delimiting characters.
        /// </summary>
        public static char[] ValidCommentChars
        {
            get { return mValidCommentChars; }
            set
            {
                if ( value == null )
                    throw new ArgumentNullException( "value" );

                if ( value.Length == 0 )
                {
                    throw new ArgumentException(
                        "The comment chars array must not be empty.",
                        "value" );
                }

                mValidCommentChars = value;
            }
        }

        /// <summary>
        /// Gets the number of sections that are in the configuration.
        /// </summary>
        public int SectionCount
        {
            get { return mSections.Count; }
        }

        /// <summary>
        /// Gets or sets a section by index.
        /// </summary>
        /// <param name="index">The index of the section in the configuration.</param>
        public Section this[int index]
        {
            get
            {
                if ( index < 0 || index >= mSections.Count )
                    throw new ArgumentOutOfRangeException( "index" );

                return mSections[index];
            }
            set
            {
                if ( index < 0 || index >= mSections.Count )
                    throw new ArgumentOutOfRangeException( "index" );

                mSections[index] = value;
            }
        }

        /// <summary>
        /// Gets or sets a section by its name.
        /// </summary>
        ///
        /// <param name="name">The name of the section.</param>
        ///
        /// <returns>
        /// The section if found, otherwise a new section with
        /// the specified name is created, added to the configuration and returned.
        /// </returns>
        public Section this[string name]
        {
            get
            {
                var section = GetSection( name );

                if ( section == null )
                {
                    section = new Section( name );
                    Add( section );
                }

                return section;
            }
            set
            {
                if ( value == null )
                    throw new ArgumentNullException( "value" );

                // Check if there already is a section by that name.
                var section = GetSection( name );

                int settingIndex = section != null ? mSections.IndexOf( section ) : -1;

                if ( settingIndex < 0 )
                {
                    // A section with that name does not exist yet; add it.
                    mSections.Add( section );
                }
                else
                {
                    // A section with that name exists; overwrite.
                    mSections[settingIndex] = section;
                }
            }
        }

        private Section GetSection( string name )
        {
            foreach ( var section in mSections )
            {
                if ( string.Equals( section.Name, name, StringComparison.OrdinalIgnoreCase ) )
                    return section;
            }

            return null;
        }

        #endregion
    }
}
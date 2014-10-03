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
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SharpConfig
{
    public partial class Configuration
    {
        private void Serialize( string filename, Encoding encoding )
        {
            if ( string.IsNullOrEmpty( filename ) )
                throw new ArgumentNullException( "filename" );

            using ( var stream = new FileStream( filename, FileMode.Create, FileAccess.Write ) )
            {
                Serialize( stream, encoding );
                stream.Close();
            }
        }

        private void Serialize( Stream stream, Encoding encoding )
        {
            if ( stream == null )
                throw new ArgumentNullException( "stream" );

            var sb = new StringBuilder();

            // Write all sections.
            foreach ( var section in this )
            {
                sb.AppendLine( section.ToString( true ) );

                // Write all settings.
                foreach ( var setting in section )
                {
                    sb.AppendLine( setting.ToString( true ) );
                }

                sb.AppendLine();
            }

            // Replace triple new-lines with double new-lines.
            sb.Replace( "\r\n\r\n\r\n", "\r\n\r\n" );

            // Write to stream.
            var writer = encoding == null ?
                new StreamWriter( stream ) : new StreamWriter( stream, encoding );

            using ( writer )
            {
                writer.Write( sb.ToString() );
                writer.Close();
            }
        }

        private void SerializeBinary( BinaryWriter writer, string filename )
        {
            if ( string.IsNullOrEmpty( filename ) )
                throw new ArgumentNullException( "filename" );

            using ( var stream = new FileStream( filename, FileMode.Create, FileAccess.Write ) )
            {
                SerializeBinary( writer, stream );
            }
        }

        private void SerializeBinary( BinaryWriter writer, Stream stream )
        {
            if ( stream == null )
                throw new ArgumentNullException( "stream" );

            bool ownWriter = false;

            if ( writer == null )
            {
                writer = new BinaryWriter( stream );
                ownWriter = true;
            }

            try
            {
                writer.Write( SectionCount );

                foreach ( var section in this )
                {
                    writer.Write( section.Name );
                    writer.Write( section.SettingCount );

                    foreach ( var setting in section )
                    {
                        writer.Write( setting.Name );
                        writer.Write( setting.Value );
                    }
                }
            }
            finally
            {
                if ( ownWriter )
                    writer.Close();
            }
        }

    }
}
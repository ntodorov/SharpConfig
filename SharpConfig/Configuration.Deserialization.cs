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
using System.IO;
using System.Text;

namespace SharpConfig
{
    public partial class Configuration
    {
        private static Configuration DeserializeBinary( BinaryReader reader, string filename )
        {
            if ( string.IsNullOrEmpty( filename ) )
                throw new ArgumentNullException( "filename" );

            Configuration config = null;

            using ( var stream = File.OpenRead( filename ) )
            {
                config = DeserializeBinary( reader, stream );
            }

            return config;
        }

        private static Configuration DeserializeBinary( BinaryReader reader, Stream stream )
        {
            if ( stream == null )
                throw new ArgumentNullException( "stream" );

            bool ownReader = false;

            if ( reader == null )
            {
                reader = new BinaryReader( stream );
                ownReader = true;
            }

            try
            {
                var config = new Configuration();

                int sectionCount = reader.ReadInt32();

                for ( int i = 0; i < sectionCount; i++ )
                {
                    string sectionName = reader.ReadString();
                    Section section = new Section( sectionName );
                    config.Add( section );

                    int settingCount = reader.ReadInt32();
                    for ( int j = 0; j < settingCount; j++ )
                    {
                        Setting setting = new Setting(
                            reader.ReadString(),
                            reader.ReadString() );

                        section.Add( setting );
                    }
                }

                return config;
            }
            finally
            {
                if ( ownReader )
                    reader.Close();
            }
        }
    }
}
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
        private static int mLineNumber;

        // Parses a configuration from a source string.
        // This is the core parsing function.
        private static Configuration Parse( string source )
        {
            // Reset temporary fields.
            mLineNumber = 0;

            Configuration config = new Configuration();
            Section currentSection = null;
            var preComments = new List<Comment>();

            using (var reader = new StringReader( source ))
            {
                string line = null;

                // Read until EOF.
                while ((line = reader.ReadLine()) != null)
                {
                    mLineNumber++;

                    // Remove all leading / trailing white-spaces.
                    line = line.Trim();

                    // Empty line? If so, skip.
                    if (string.IsNullOrEmpty(line))
                        continue;

                    int commentIndex = 0;
                    var comment = ParseComment( line, out commentIndex );

                    if (commentIndex == 0)
                    {
                        // This is a comment line (pre-comment).
                        // Add it to the list of pre-comments.
                        preComments.Add( comment );
                        continue;
                    }
                    else if (commentIndex > 0)
                    {
                        // Strip away the comments of this line.
                        line = line.Remove( commentIndex ).Trim();
                    }

                    // Sections start with a '['.
                    if (line.StartsWith( "[" ))
                    {
                        currentSection = ParseSection( line );
                        currentSection.Comment = comment;
                        
                        if (config.Contains(currentSection.Name))
                        {
                            throw new ParserException( string.Format(
                                "The section '{0}' was already declared in the configuration.",
                                currentSection.Name ), mLineNumber );
                        }

                        if (preComments.Count > 0)
                        {
                            currentSection.mPreComments = new List<Comment>( preComments );
                            preComments.Clear();
                        }

                        config.mSections.Add( currentSection );
                    }
                    else
                    {
                        Setting setting = ParseSetting( line );
                        setting.Comment = comment;
                        
                        if (currentSection == null)
                        {
                            throw new ParserException( string.Format(
                                "The setting '{0}' has to be in a section.",
                                setting.Name ), mLineNumber );
                        }

                        if (currentSection.Contains( setting.Name ))
                        {
                            throw new ParserException( string.Format(
                                "The setting '{0}' was already declared in the section.",
                                setting.Name ), mLineNumber );
                        }

                        if (preComments.Count > 0)
                        {
                            setting.mPreComments = new List<Comment>( preComments );
                            preComments.Clear();
                        }

                        currentSection.Add( setting );
                    }

                }
            }

            return config;
        }

        private static bool IsInQuoteMarks( string line, int startIndex )
        {
            // Check for quote marks.
            // Note: the way it's done here is pretty primitive.
            // It will only check if there are quote marks to the left and right.
            // If so, it presumes that it's a comment symbol inside quote marks and thus, it's not a comment.
            int i = startIndex;
            bool left = false;

            while ( --i >= 0 )
            {
                if ( line[i] == '\"' )
                {
                    left = true;
                    break;
                }
            }

            bool right = ( line.IndexOf( '\"', startIndex ) > 0 );

            if ( left && right )
                return true;

            return false;
        }

        private static Comment ParseComment( string line, out int commentIndex )
        {
            commentIndex = line.IndexOfAny( Configuration.ValidCommentChars );

            if ( commentIndex < 0 )
                return null; // This line does not contain a comment.

            // Tip from MarkAJones:
            // Database connection strings can contain semicolons, which should not be
            // treated as comments, but rather separators.
            // To avoid this, we have to check for two things:
            // 1. Is the comment inside a string? If so, ignore.
            // 2. Is the comment symbol backslashed (an escaping value)? If so, ignore also.

            // If the char before the comment is a backslash, it's not a comment.
            if ( commentIndex >= 1 && line[commentIndex - 1] == '\\' )
                return null;

            if ( IsInQuoteMarks( line, commentIndex ) )
                return null;

            var comment = new Comment(
                value:  line.Substring( commentIndex + 1 ).Trim(),
                symbol: ';' );

            foreach ( var commentChar in Configuration.ValidCommentChars )
            {
                if ( line[commentIndex] == commentChar )
                {
                    comment.Symbol = commentChar;
                    break;
                }
            }

            return comment;
        }

        private static Section ParseSection( string line )
        {
            line = line.Trim();

            int closingBracketIndex = line.IndexOf( ']' );

            if ( closingBracketIndex < 0 )
                throw new ParserException( "closing bracket missing.", mLineNumber );

            // See if there are unwanted chars after the closing bracket.
            if ( ( line.Length - 1 ) > closingBracketIndex )
            {
                string unwantedToken = line.Substring( closingBracketIndex + 1 );

                throw new ParserException( string.Format(
                    "unexpected token '{0}'", unwantedToken ),
                    mLineNumber );
            }

            // Read the section name, and trim all leading / trailing white-spaces.
            string sectionName = line.Substring( 1, line.Length - 2 ).Trim();

            // Otherwise, return a fresh section.
            return new Section( sectionName );
        }

        private static Setting ParseSetting( string line )
        {
            // Find the assignment operator.
            int indexOfAssignOp = line.IndexOf( '=' );

            if ( indexOfAssignOp < 0 )
                throw new ParserException( "setting assignment expected.", mLineNumber );

            // Trim the setting name and value.
            string settingName = line.Substring( 0, indexOfAssignOp ).Trim();
            string settingValue = line.Substring( indexOfAssignOp + 1, line.Length - indexOfAssignOp - 1 ).Trim();

            // Check if non-null name / value is given.
            if ( string.IsNullOrEmpty( settingName ) )
                throw new ParserException( "setting name expected.", mLineNumber );

            if ( settingValue == null )
                settingValue = string.Empty;

            // Trim all quote marks in the value. This is done to provide a clean raw value.
            settingValue = settingValue.Trim( '\"' );

            return new Setting( settingName, settingValue );
        }

    }
}
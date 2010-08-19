Readme File for LibTehthu
Copyright (c) 2010 Sean McNamara
Last Updated: August 18, 2010

LibTehthu is a re-usable software component for translating between two syntactically identical languages. It may sound trivial, but it turned out to be moderately challenging to implement all the features I wanted to include. It still isn't anywhere near as advanced as natural language translators used in Babelfish, Google Translate, et al, but it is good at what it does.

LibTehthu is written in C# .NET, and requires the .NET runtime version 3.0 or later to run. It also runs on Linux with the Mono runtime.

There are two pieces to LibTehthu, so far: the reusable library, and the graphical interface (GUI). The GUI is very simple, and is mostly intended for testing out the translation engine against a dictionary. However, the GUI is nearly usable as a way to manually translate entire sentences from English to a constructed language, and back again.

I am releasing LibTehthu (both the GUI and the library) under the GPL 3.0 or later.

SOURCE CODE: git clone git://tiyukquellmalz.org/libtehthu

Here are some of the more detailed features of LibTehthu:

1. Translate both ways, from the "left hand" language to the "right hand".
2. Store the translation in a plain text file, easily editable by hand.
3. A robust, powerful API, allowing .NET applications of any programming language to hook in with minimal up-front learning.
4. Smart handling of capital letters, taking into consideration both inputs, and the canonical capitalization in the dictionary. For example, words spelled in ALL CAPS in the input text will be translated as CAPS as well. Similar handling takes place for Proper Case and lowercase.
5. Punctuation is preserved, while still allowing words in either language to contain symbols such as ' or - (for example, T'zrazik).
6. Support for both one-to-one and one-to-many relationships between words.

The GUI has the following features:

1. Translate both ways.
2. Open any compatible dictionary, not just a predefined one.
3. Translate entire sentences line by line.

How to use the GUI on Windows:

1. Download GTK# 2.12 binaries at http://ftp.novell.com/pub/mono/gtk-sharp/gtk-sharp-2.12.10.win32.msi and launch it. Follow the install process.
2. Download the LibTehthu binaries at http://tiyukquellmalz.org/libtehthu/libtehthu-bin-latest.zip and extract them anywhere you like.
3. Run the Gtk-GUI.exe file.
4. Go to File -> Open and select a compatible dictionary.
5. Type some text into the input box and press enter, or click Execute.
6. Click on the Output tab to view the translations, the Input tab to view what you typed, or the Debug Console tab to view non-fatal error messages and warnings (fatal ones crash the program ;))
7. Copy and paste is supported in all the main text areas of the program.

FILE FORMAT
The program accepts two alternate file formats.

The first is the OpenDocument Spreadsheet as defined by OASIS standards. ODF version 1.0 or 1.1 is supported.

If you intend to use an OpenDocument Spreadsheet (as created by MS Office 2007 or later, or OpenOffice 2.0 or later), you must give the file the extension ".ods".

If you use an ODS, the spreadsheet used will be the first one of the following that is found:

*The spreadsheet with the name "Dictionary"
*The spreadsheet with the name "Sheet1"
*The first spreadsheet according to the sorting order given in the file (usually the left-most spreadsheet in your spreadsheet pager).

Within this spreadsheet, only the first two columns will be dealt with. You can put other data in the other columns; it will be ignored.

The first row may optionally contain values in parentheses, specifying custom language names for the language in that column.
If you want to specify language names, each language name must begin with a [ and end with a ] character. A language name spec can only be on the first row.

Each subsequent row will be treated as a mapping between languages.

You must use the first and second columns (ordinally speaking, so that's usually Columns A and B) if you want correct operation of the translator.

The .ods format is more flexible because there is no delimiter. You can include any character at all in both the left-hand and right-hand words.

Any other extension will be treated as a plain text file as described below:

The de facto standard for the extension is .teh, after Tehthu, but you can call these custom Tehthu files any extension you like except ".ods".

I. Language Names
Description: A dictionary file may optionally start out with a stanza matching the following regular expression:
\[.+\] \[.+\]
This will make the core LibTehthu aware of user-friendly names to refer to the languages used in the dictionary. These are also exposed in the Gtk-GUI.
Example: [Foo] [Bar]

II. Mappings
Description: A mapping is a delimiter-separated pair of values matching the following regular expression:
.+<delim>.+
where <delim> is replaced with a string that is never used in the construction of the tokens on either side. The mappings are the core of the dictionary.
Example: hello|hola

There can be a maximum of one Language Names stanza and an unlimited number of Mappings stanzas. The Language Names stanza MUST be the first line in the file. Each stanza is separated by any new line pattern recognized by the .NET CLR. Currently that means you can set your line endings to \n (the default on UNIX) or \r\n (the default on Windows).

III. Character encoding
Character encodings that can be automatically detected by the System.IO utilities of .NET are supported. For example, ASCII, UTF-8 and Windows Western are supported. Multi-byte encodings, variable-length encodings, and non-Western characters are COMPLETELY UNTESTED. Use at your own risk.

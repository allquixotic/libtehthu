/*   LibTehthu -- a simple translator between syntactically-identical languages.
 *   Copyright (C) 2010  Sean McNamara
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>
 */
		
/*
 * NOTE: This file contains Doxygen-style comments. They are not very fun to read in-line.
 * If you don't have the documentation in your desired format, you can generate HTML, PDF, CHM, etc. by installing Doxygen yourself.
 * See the Doxygen website and FAQ for details.
 * */

/*! \file Tehthu.cs
 *  \brief The main LibTehthu translator.
 * 
 *  LibTehthu performs three primary functions, usually in this order:\n
 *  1. Parses a Tehthu dictionary file, which contains word translations from one language to another.\n
 *  2. Tokenizes input sentences by dividing the sentence into words. Words can not contain spaces because each space denotes a new word.\n
 *  3. Translates the input words into the output words, then re-assembles the sentence with the translated words in the same order and in the same case.\n
 * \n
 *  Terminology and Example File:\n
 *  The "left-hand language" is the language to the left of the delimiter in the dictionary file.\n
 *  Correspondingly, the right-hand language is the language to the right of the delimiter.\n
 *  Here is a simple example of a one-word dictionary:\n
 * \n
 *  [English] [Spanish]\n
 *  hello|hola\n
 * \n
 *  In this example, English is the left-hand language name, Spanish is the right-hand language name.\n
 *  `hello' is a word in the left-hand language, and `hola' is a word in the right-hand language.\n
 * 
 *  The native file format of LibTehthu is given below. However, it can also handle ODS files. See the file README.txt in the
 *  distribution for more information about using LibTehthu with ODS files.\n
 * \n
 *  DETAILED FILE FORMAT\n
 *  The program does not care about the extension of the file. However, the de facto standard for the extension is .teh, after Tehthu.\n
 *\n
 *  I. Language Names\n
 *  Description: A dictionary file may optionally start out with a stanza matching the following regular expression:\n
 *  \[.+\] \[.+\]\n
 *  This will make the core LibTehthu aware of user-friendly names to refer to the languages used in the dictionary. These are also exposed in the Gtk-GUI.\n
 *  Example: [Foo] [Bar]\n
 *  \n
 *  II. Mappings\n
 *  Description: A mapping is a delimiter-separated pair of values matching the following regular expression:\n
 *  .+<delim>.+\n
 *  where <delim> is replaced with a string that is never used in the construction of the tokens on either side. The mappings are the core of the dictionary.\n
 *  Example: hello|hola\n
 *\n
 *  Rules:\n
 *  (a) There can be a maximum of one Language Names stanza and an unlimited number of Mappings stanzas (limited by system resources).\n
 *  (b) The Language Names stanza MUST be the first line in the file, if it is included. However, it is optional whether to include it at all.\n
 *  (c) Each stanza is separated by any new line pattern recognized by the .NET CLR.\n
 *  Currently that means you can set your line endings to \n (the default on UNIX) or \r\n (the default on Windows).\n
 *\n
 *  III. Character encodings\n
 *  Character encodings that can be automatically detected by the System.IO utilities of .NET are supported.\n
 *  For example, ASCII, UTF-8 and Windows Western are supported. Multi-byte encodings, variable-length encodings, and non-Western characters are COMPLETELY UNTESTED.
 * */ 


using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;

using ODSReadWrite;

/*! \brief The main LibTehthu translator namespace.
 * 
 *  You should be sure to add to your code a "using" line for this namespace, to avoid having to type it repeatedly.
 * */
namespace LibTehthu
{
	/*! \brief Enum for specifying which direction to translate.
	 * 
	 * LeftToRight means that the language on the left-hand side of the delimiter will be translated to the language on the right-hand side. \n
	 * RightToLeft means the opposite.\n
	 * To query the names of the detected languages in the current dictionary, call getLeftLanguageName() or getRightLanguageName().
	 * */
	public enum TranslateDirection
	{
		LeftToRight,
		RightToLeft
	}
	
	/*! \brief Main Tehthu class.
	  * 
	  * You must instantiate an instance of this class to use the translator.\n
	  * Multiple instances are supported because there is no static data.\n
	  * Tehthu is not thread-safe; however, it is possible to use read-only functions from multiple threads.\n
	  * It is important to make sure that only one thread at a time is accessing any of the functions marked as "WRITE" in this documentation.\n
	  * That is, only one WRITE function may be executing at a time, and none of the WRITE functions are reentrant.\n
	  * If you use multiple threads, you should make sure all readers wait whenever one thread uses a WRITE function.
	  * */
	public class Tehthu
	{
		private const string DEFAULT_DELIMITER = "|";
		private const string DEFAULT_LEFT = "Left";
		private const string DEFAULT_RIGHT = "Right";
		private static readonly string[] whitespace = new string[]{" ", "\r\n", "\n", "\t"};
		
		private readonly Dictionary<string, List<string>> ltr = new Dictionary<string, List<string>>();
		private readonly Dictionary<string, List<string>> rtl = new Dictionary<string, List<string>>();
		private readonly SizeQueue<string> log = new SizeQueue<string>(100);
		private FileInfo file = null;
		private string delimiter = null;
		private bool haveLogReader = false;
		private string leftLanguageName = DEFAULT_LEFT;
		private string rightLanguageName = DEFAULT_RIGHT;
		private Regex dictLineReg1 = null;
		private Regex dictLineReg2 = null;
		
		/*! \brief Instantiates the translator.
		 * 
		 * Create a new translator with the default delimiter and default language names from the given dictionary file.\n
		 * The language names in the dictionary (if any) will not be set until you call reparse().
		 * */
		public Tehthu(string path) : this(new FileInfo(path)) {}
		
		/*! \brief Instantiates the translator.
		 * 
		 * Create a new translator with the default delimiter and default language names from the given dictionary file.\n
		 * The language names in the dictionary (if any) will not be set until you call reparse().
		 * 
		 * */
		public Tehthu(FileInfo fileinfo) : this(fileinfo, DEFAULT_DELIMITER) {}
		
		/*! \brief Instantiates the translator.
		 * 
		 * Create a new translator with the given delimiter and default language names from the given dictionary file.\n
		 * The language names in the dictionary (if any) will not be set until you call reparse().
		 * */
		public Tehthu(string path, string delim) : this(new FileInfo(path), delim) {}
		
		/*! \brief Instantiates the translator.
		 * 
		 * Create a new translator with the given delimiter and default language names from the given dictionary file.\n
		 * The language names in the dictionary (if any) will not be set until you call reparse().
		 * */
		public Tehthu(FileInfo fileinfo, string delim) : this(fileinfo, delim, DEFAULT_LEFT, DEFAULT_RIGHT) {}
		
		/*! \brief Instantiates the translator.
		 * 
		 * Create a new translator with the given delimiter and language names from the given dictionary file.\n
		 * The language names in the dictionary (if any) will override the language names set here when you call reparse().
		 * */
		public Tehthu(FileInfo fileinfo, string delim, string left, string right)
		{
			leftLanguageName  = left;
			rightLanguageName = right;
			setFileInternal(fileinfo, false);
			setDelimiterInternal(delim, false);
		}
		
		/*! \brief Set the delimiter that separates the left hand language lines from the right hand.
		 *  WRITE (this function is not thread-safe).\n
		 *  This function forces a reparse of the dictionary.
		 * */
		public void setDelimiter(string delim)
		{
			setDelimiterInternal(delim, true);
		}
		
		/*! \brief Retrieve the delimiter that separates the left hand language from the right hand in the dictionary file.\n
		 *  \return The delimiter, usually one character. The default is "|" (no quotes).
		 * */
		public string getDelimiter()
		{
			return delimiter;	
		}
		
		/*! \brief Set the dictionary file used to translate.
		 * 
		 * WRITE (this function is not thread-safe).\n
		 * This function forces a reparse of the dictionary.
		 * */
		public void setFile(string path)
		{
			setFile(new FileInfo(path));
		}
		
		/*! \brief Set the dictionary file used to translate.
		 * 
		 * WRITE (this function is not thread-safe).\n
		 * This function forces a reparse of the dictionary.
		 * */
		public void setFile(FileInfo fileinfo)
		{
			setFileInternal(fileinfo, true);
		}
		
		/*! \brief Get the dictionary file used to translate.
		 *  \return A FileInfo object containing information about the file being used as the dictionary.
		 * */
		public FileInfo getFile()
		{
			return file;	
		}
		
		/*! \brief Set the user-friendly language name of the left-hand language.
		 * 
		 * WRITE (this function is not thread-safe).
		 * */
		public void setLeftLanguageName(string name)
		{
			leftLanguageName = string.Copy(name);
		}
		
		/*! \brief Set the user-friendly language name of the right-hand language.
		 * 
		 * WRITE (this function is not thread-safe).
		 * */
		public void setRightLanguageName(string name)
		{
			rightLanguageName = string.Copy(name);	
		}
		
		/*! \brief Get the user-friendly language name of the left-hand language. */
		public string getLeftLanguageName()
		{
			return string.Copy(leftLanguageName);	
		}
		
		/*! \brief Get the user-friendly language name of the right-hand language. */
		public string getRightLanguageName()
		{
			return string.Copy(rightLanguageName);	
		}
		
		/*! \brief Translate an input sentence into an output sentence.
		 *  \param input The input sentence.
		 *  \param dir The translation direction. LeftToRight means the input sentence is in the left-hand language,
		 *             and the output sentence is in the right-hand language. Vice-versa for RightToLeft.
		 *  \return The translated sentence in the output language.
		 * 
		 *  A "sentence" is a string of one or more words, possibly containing symbols.\n
		 *  A word is a substring separated from the rest of the string by whitespace.\n
		 *  Sentences MAY contain the delimiter character used in the dictionary, but it will not be translated.\n
		 *  Sentences MAY contain newlines, but it is preferred that each sentence does not contain any newlines.\n
		 *  Any newlines present in the input sentence will be converted to spaces.\n
		 *  Words that are not translated are omitted from the output sentence.\n
		 * \n
		 *  This function is reentrant, and can be used from multiple threads as long as WRITE functions are synchronized.
		 * */
		public string translate(string input, TranslateDirection dir)
		{
			if(input == null)
			{
				return null;	
			}
			
			int i = 0;
			string result = "";
			string[] words = input.Split(whitespace, StringSplitOptions.RemoveEmptyEntries);
			foreach(string word in words)
			{
				string _out;
				translateWord(word, out _out, dir);
				if(_out != null && _out.Trim().Length > 0)
				{
					result += _out;
					if(i < words.Length - 1)
					{
						result += " ";
					}
				}
				i++;
			}
			
			return result;
		}
		
		/*! \brief Translate a single word from an input language to an output language.
		 *  \param _origKey The input word. Symbols are stripped from the beginning and end of the word before it is translated.
		 *  \param val An "out" parameter for the output word. It will not be set if the function returns false.
		 *  \param dir The translation direction. LeftToRight means the input sentence is in the left-hand language,
		 *             and the output sentence is in the right-hand language. Vice-versa for RightToLeft.
		 *  \return true if the word was translated; false if no translation was found in the dictionary.
		 * 
		 *  No attempt is made to tokenize the string, so the entire input string will be looked up in the dictionary.
		 *  Therefore you should call translate() instead if you want to translate a string containing multiple tokens.
		 *  This function is reentrant, and can be used from multiple threads as long as WRITE functions are synchronized.
		 * */
		public bool translateWord(string _origKey, out string val, TranslateDirection dir)
		{	
			if(_origKey == null || (dir != TranslateDirection.LeftToRight && dir != TranslateDirection.RightToLeft))
			{
				putLogLine("Warning: null key or invalid direction in translateWord()");
				val = null;
				return false;	
			}
			
			if(_origKey.Length > 2 && _origKey[0] == '[' && _origKey[_origKey.Length - 1] == ']')
			{
				 val = _origKey.Substring(1, _origKey.Length - 2);	
				return true;
			}
			
			if(_origKey.ToLower() == "amarok")
			{
				string wocka = "";
				Random r = new Random();
				for(int i = 0; i < r.Next(100); i++)
				{
					wocka += "wocka ";	
				}
				val = StringCase.transformCase(wocka, StringCase.getCaseType(_origKey));
				return true;
			}
			
			string intermed_key = _origKey.ToLower();
			string trimmedStart, trimmedEnd;
			string key = trimSymbols(intermed_key, out trimmedStart, out trimmedEnd);
			Dictionary<string, List<string>> dict;
			
			if(dir == TranslateDirection.LeftToRight)
			{
				dict = ltr;
			}
			else
			{
				dict = rtl;
			}
			
			List<string> result;
			if(!dict.TryGetValue(key.ToLower(), out result))
			{
				if(dir == TranslateDirection.LeftToRight && key.Length > 1
				   && key.ToLower()[key.Length - 1] == 's')
				{
					if(!dict.TryGetValue(key.Substring(0, key.Length - 2).ToLower(), out result))
					{
						putLogLine("Note: no translation for key `" + key + "'");
						val = null;
						return false;
					}
				}
				else
				{
					putLogLine("Note: no translation for key `" + key + "'");
					val = null;
					return false;
				}
			}

			if(result == null || result.Count < 1 || result[0] == null)
			{
				val = null;	
			}
			else
			{ //Transform the output case into the input case.
				
				CaseType dict_case = StringCase.getCaseType(result[0]);
				CaseType key_case = StringCase.getCaseType(_origKey);
				string _outval = null;
				
				if(key.Length == 1)
				{
					//Input one-letter uppercase word -> output proper-case
					if(key_case == CaseType.Caps || key_case == CaseType.Proper)
					{
						_outval = StringCase.transformCase(result[0], CaseType.Proper);
					}
					else
					{
						_outval = StringCase.transformCase(result[0], key_case);
					}
				}
				else
				{
					if(dict_case == CaseType.Mixed)
					{
						_outval = result[0];
					}
					else
					{
						_outval = StringCase.transformCase(result[0], key_case);
					}
				}
				
				val = (trimmedStart != null ? trimmedStart : "") 
						  + _outval
						  + (trimmedEnd != null ? trimmedEnd : "");	
				
				if(result.Count > 1)
				{
					putLogLine("Note: using first translation for ambiguous key `" + key + "' : `" + result[0] + "'");
				}
			}
			return true;
		}
		
		/*! \brief Wait until the next line of text is available in the log, then retrieve it.
		 *  \return Returns one line of text, conventionally without any newline characters.
		 * 
		 *  This function will block the current thread until a line of text is available in the log.\n
		 *  If the log is currently disconnected, this function will connect it.\n
		 *  Lines are written to the log when there are non-fatal errors in the translation or parsing stages.\n
		 *  Each line put in the buffer is returned in a takeLogLine() call exactly once.\n
		 *  This function is intended to be used in a loop in a separate thread from the main thread using Tehthu.\n
		 *  This function is thread-safe if connectToLog() has already been called, otherwise it is not.\n
		 *  This function does not schedule multiple readers fairly; they are scheduled arbitrarily by the CLR.\n
		 *  Example ThreadStart func:\n
		 * \n
		 *  void threadStart()\n
		 *  {\n
		 *      while(true)\n
		 *      {\n
		 *         try \n
		 *         {\n
		 *               do_something_with(tehthu.takeLogLine());\n
		 *         }\n
		 *         catch(ThreadAbortException tae)\n
		 *         {\n
		 *              break;\n
		 *         }\n
		 *      }\n
		 *  }
		 * */
		public string takeLogLine()
		{
			connectToLog();
			return log.Dequeue();
		}
		
		/*! \brief Add a line to the log buffer.
		 * 
		 *  This function will put a line in the log's blocking queue, potentially blocking if it is full.\n
		 *  The limit on the number of lines that can be logged without having any lines removed with takeLogLine()\n
		 *  is currently hard-coded at 100. Additionally, this function is a no-op if neither takeLogLine() nor\n
		 *  connectToLog() has been called for this Tehthu instance. This prevents the buffer from filling up if\n
		 *  the application is not trying to handle the log buffer.
		 * 
		 *  This function is thread-safe.
		 * */
		public void putLogLine(string item)
		{
			if(haveLogReader)
			{
				log.Enqueue(item);
			}
		}
		
		/*! \brief Notify LibTehthu that the application intends to handle the log buffer.
		 * 
		 *  Unless this function or takeLogLine() is called, the log buffer will not be filled.\n
		 *  WRITE (this function is not thread-safe).
		 *  */
		public void connectToLog()
		{
			haveLogReader = true;
		}
		
		/*! \brief Notify LibTehthu that the application does NOT intend to handle the log buffer. This is the default.
		 * 
		 *  Once this function is called, the log buffer will no longer be filled with new log lines.\n
		 *  If the log buffer is not empty when called, this function will not delete its contents.\n
		 *  WRITE (this function is not thread-safe).
		 * */
		public void disconnectFromLog()
		{
			haveLogReader = false;	
		}
		
		/*! \brief Indicate whether the log is currently being filled with new messages.
		 *  \return true if connectToLog() or takeLogLine() has been called more recently than disconnectFromLog(); false otherwise.
		 * */
		public bool haveLogConnection()
		{
			return haveLogReader;	
		}
		
		/*! \brief (re)parse the dictionary file, discarding the in-memory database and storing a fresh copy from disk.
		 *  
		 *  This function should be called if the dictionary is known to have been modified.\n
		 *  It is also called internally by LibTehthu (as indicated in this documentation) when certain WRITE functions are called.\n
		 *  WRITE (this function is not thread-safe).
		 * */
		public void reparse()
		{
			ltr.Clear();
			rtl.Clear();
			parse();
		}
		
		private void setDelimiterInternal(string delim, bool _reparse)
		{
			if(delim == null || delim.Length == 0)
			{
				throw new NullReferenceException("The supplied delimiter must not be null or empty.");	
			}
			else
			{
				this.delimiter = delim;
			}
			
			dictLineReg1 = new Regex("(.+)[" + delimiter + "](.+)", RegexOptions.Compiled);
			dictLineReg2 = new Regex("(.+)[" + delimiter + "](.+)[" + delimiter + "]+", RegexOptions.Compiled);
			
			if(_reparse)
			{
				reparse();
			}
		}
		
		private void setFileInternal(FileInfo fileinfo, bool _reparse)
		{
			if(fileinfo == null || !fileinfo.Exists)
			{
				throw new FileNotFoundException("Could not find file " + (file == null ? "null" : file.Name));	
			}
			else
			{
				this.file = fileinfo;	
			}
			
			if(_reparse)
			{
				reparse();	
			}
		}
		
		private void parse()
		{
			if(file != null && file.Name != null  && file.Name.Length > 4 && 
			   file.Name.Substring(file.Name.Length - 4, 4) == ".ods")
			{
				parseOds();	
			}
			else
			{
				parsePlainText();	
			}
		}
		
		private void parseOds()
		{
			putLogLine("Info: Parsing ODS started.");
			OdsReaderWriter orw = new OdsReaderWriter();
			DataSet ds = orw.ReadOdsFile(file.FullName);
			DataTable dt = ds.Tables["Dictionary"];
			if(dt == null)
			{
				dt = ds.Tables["Sheet1"];
				if(dt == null)
				{
					dt = ds.Tables[0];
					if(dt == null)
					{
						putLogLine("Critical error: Couldn't find a spreadsheet containing the dictionary. The dictionary will be empty for this Tehthu session.");
						return;
					}
				}
			}
			
			string lval = null;
			string rval = null;
			int lineno = 1;
			foreach(DataRow dr in dt.Rows)
			{
				if(dr.ItemArray.Length == 0)
				{
					continue;	
				}
				
				if(dr.ItemArray.Length == 1)
				{
					lval = dr.ItemArray[0].ToString();
					rval = "";
				}
				
				//Ignore any data past column 2
				if(dr.ItemArray.Length >= 2)
				{	
					lval = dr.ItemArray[0].ToString();
					rval = dr.ItemArray[1].ToString();
					
					//Does the first line specify language names?
					if(lval[0] == '[' && lval[lval.Length - 1] == ']'
					   && rval[0] == '[' && rval[rval.Length - 1] == ']'
					   && lineno == 1)
					{
						setLeftLanguageName(lval.Substring(1, lval.Length - 2));
						setRightLanguageName(rval.Substring(1, rval.Length - 2));
					}
				}
				
				tryAddDictWord(ltr, lval, rval, getLeftLanguageName(), file.Name, lineno);	
				tryAddDictWord(rtl, rval, lval, getRightLanguageName(), file.Name, lineno);
				lineno++;
			}
			
			putLogLine("Info: Parsing complete.");
		}
		
		private void parsePlainText()
		{
			putLogLine("Info: Parsing plain text started.");
			try
			{
				
				Regex languageNames = new Regex(@"\[(.+)\] \[(.+)\]", RegexOptions.Compiled);
				FileStream fs = file.OpenRead();
				if(!fs.CanRead)
					throw new IOException("Can not read from file " + file.Name);
				TextReader reader = new StreamReader(fs);
				
				int lineno = 1;
				string line = reader.ReadLine();
				for(int i = 0; i < 2; i++)
				{
					MatchCollection langMatches = languageNames.Matches(line);
					if(langMatches.Count == 1)
					{
						Match theMatch = langMatches[0];
						if(theMatch.Groups.Count == 3)
						{
							setLeftLanguageName(theMatch.Groups[1].Value);
							setRightLanguageName(theMatch.Groups[2].Value);
						}
						else
						{
							putLogLine("Warning: Syntax error on language names line: Expected 3 groups; found " + theMatch.Groups.Count);	
						}
						lineno++;
						line = reader.ReadLine();
					}
					else
					{
						//It's trying to tell us what the delimiter should be. Don't reparse, just update the regexes.
						if(line.Length == 1)
						{
							setDelimiterInternal(line, false);
							line = reader.ReadLine();
						}
						else
						{
							//This line isn't a language name stanza or a delimiter stanza, so stop trying for them
							break;	
						}
					}
				}
				
				for(; line != null; line = reader.ReadLine(), lineno++)
				{
					//Try to match the more strict regex first; reg2 has one or more trailing delimiters on the end that get discarded.
					MatchCollection mc = dictLineReg2.Matches(line);
					if(mc.Count != 1)
					{
						mc = dictLineReg1.Matches(line);
						if(mc.Count != 1)
						{
							putLogLine("Warning: Input File " + file.Name + ", Line " + lineno + ": Does not contain exactly one separator character `" + delimiter + "'. SKIPPING LINE.");
							continue;
						}
					}
					
					Match theMatch = mc[0];
					if(theMatch.Groups.Count != 3)
					{
						putLogLine("Warning: Input File " + file.Name + ", Line " + lineno + ": Contains " + theMatch.Groups.Count + " capture groups; expected 2. SKIPPING LINE.");
						continue;
					}
					
					string left = theMatch.Groups[1].Value;
					string right = theMatch.Groups[2].Value;
					
					tryAddDictWord(ltr, left, right, getLeftLanguageName(), file.Name, lineno);
					
					tryAddDictWord(rtl, right, left, getRightLanguageName(), file.Name, lineno);
				}
				
				fs.Close();
				
				putLogLine("Info: Parsing complete.");
			}
			catch(SystemException se)
			{
				throw new IOException("Can not read from file " + file.Name + "\n" + se.Message);	
			}	
		}
		
		private void tryAddDictWord(Dictionary<string, List<string>> dict, string key, string val, string langName, string filename, int lineno)
		{
			List<string> _try;
			if(dict.TryGetValue(key.ToLower(), out _try))
			{
				_try.Add(val);
				
				string definitions = "";
				foreach(string defin in _try)
				{
					definitions += "\t" + defin + "\n";
				}
				putLogLine("Note: Input File " + filename + ", Line " + lineno +
				           ": Multiple definitions for " + langName +" word `" + key.ToLower() + "':\n" +
				           definitions);
			}
			else
			{
				_try = new List<string>();
				_try.Add(val);
				dict.Add(key.ToLower(), _try);
			}	
		}
		
		private string trimSymbols(string input, out string trimmedStart, out string trimmedEnd)
		{
			if(input == null)
			{
				trimmedStart = null;
				trimmedEnd = null;
				return null;	
			}
			
			int start = 0, end = input.Length - 1;
			
			for(int i = 0; i < input.Length; i++)
			{
				if(Char.IsLetter(input[i]))
				{
					start = i;
					break;
				}
			}
			
			for(int i = input.Length - 1; i >= 0; i--)
			{
				if(Char.IsLetter(input[i]))
				{
					end = i;
					break;
				}
			}
			
			trimmedStart = input.Substring(0, start);
			
			if(end < input.Length - 1)
			{
				trimmedEnd = input.Substring(end + 1, input.Length - end - 1);
			}
			else
			{
				trimmedEnd = "";	
			}
			
			return input.Substring(start, end - start + 1);
		}
	}
}

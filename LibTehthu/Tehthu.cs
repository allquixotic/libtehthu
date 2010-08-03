/* Copyright (c) 2010 Sean McNamara
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://sam.zoy.org/wtfpl/COPYING for more details. */
		
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace LibTehthu
{
	public enum TranslateDirection
	{
		LeftToRight,
		RightToLeft
	}
	
	public class Tehthu
	{
		private const string DEFAULT_DELIMITER = "|";
		private const string DEFAULT_LEFT = "Left";
		private const string DEFAULT_RIGHT = "Right";
		
		private readonly Dictionary<string, List<string>> ltr = new Dictionary<string, List<string>>();
		private readonly Dictionary<string, List<string>> rtl = new Dictionary<string, List<string>>();
		private readonly SizeQueue<string> log = new SizeQueue<string>(100);
		private FileInfo file = null;
		private string delimiter = null;
		private bool haveLogReader = false;
		private string leftLanguageName = DEFAULT_LEFT;
		private string rightLanguageName = DEFAULT_RIGHT;
		
		
		public Tehthu(string path) : this(new FileInfo(path)) {}
		
		public Tehthu(FileInfo fileinfo) : this(fileinfo, DEFAULT_DELIMITER) {}
		
		public Tehthu(string path, string delim) : this(new FileInfo(path), delim) {}
		
		public Tehthu(FileInfo fileinfo, string delim) : this(fileinfo, delim, DEFAULT_LEFT, DEFAULT_RIGHT) {}
		
		public Tehthu(FileInfo fileinfo, string delim, string left, string right)
		{
			leftLanguageName  = left;
			rightLanguageName = right;
			setFileInternal(fileinfo, false);
			setDelimiterInternal(delim, false);
		}
		
		public void setDelimiter(string delim)
		{
			setDelimiterInternal(delim, true);
		}
		
		public string getDelimiter()
		{
			return delimiter;	
		}
		
		public void setFile(string path)
		{
			setFile(new FileInfo(path));
		}
		
		public void setFile(FileInfo fileinfo)
		{
			setFileInternal(fileinfo, true);
		}
		
		public FileInfo getFile()
		{
			return file;	
		}
		
		public void setLeftLanguageName(string name)
		{
			leftLanguageName = string.Copy(name);
		}
		
		public void setRightLanguageName(string name)
		{
			rightLanguageName = string.Copy(name);	
		}
		
		public string getLeftLanguageName()
		{
			return string.Copy(leftLanguageName);	
		}
		
		public string getRightLanguageName()
		{
			return string.Copy(rightLanguageName);	
		}
		
		public string translate(string input, TranslateDirection dir)
		{
			if(input == null)
			{
				return null;	
			}
			
			int i = 0;
			string result = "";
			string[] words = input.Split(' ');
			foreach(string word in words)
			{
				string _out;
				translateWord(word, out _out, dir);
				result += _out;
				if(i < words.Length - 1)
				{
					result += " ";
				}
				i++;
			}
			
			return result;
		}
		
		public bool translateWord(string _origKey, out string val, TranslateDirection dir)
		{
			if(_origKey == null || (dir != TranslateDirection.LeftToRight && dir != TranslateDirection.RightToLeft))
			{
				putLogLine("Warning: null key or invalid direction in translateWord()");
				val = null;
				return false;	
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
				putLogLine("Note: no translation for key `" + key + "'");
				val = null;
				return false;
			}
			else
			{
				if(result == null)
				{
					val = null;	
				}
				else
				{
					CaseType dict_case = StringCase.getCaseType(result[0]);
					if(dict_case == CaseType.Mixed)
					{
						val = (trimmedStart != null ? trimmedStart : "") 
						  + result[0]
						  + (trimmedEnd != null ? trimmedEnd : "");	
					}
					else
					{
						CaseType key_case = StringCase.getCaseType(_origKey);
						val = (trimmedStart != null ? trimmedStart : "") 
						  + StringCase.transformCase(result[0], key_case)
						  + (trimmedEnd != null ? trimmedEnd : "");	
					}
					
					if(result.Count > 1)
					{
						putLogLine("Note: using first translation for key `" + key + "' : `" + result[0] + "'");
					}
				}
			}
			return true;
		}
		
		public string takeLogLine()
		{
			connectToLog();
			return log.Dequeue();
		}
		
		public void putLogLine(string item)
		{
			if(haveLogReader)
			{
				log.Enqueue(item);
			}
		}
		
		public void connectToLog()
		{
			haveLogReader = true;
		}
		
		public void disconnectFromLog()
		{
			haveLogReader = false;	
		}
		
		public bool haveLogConnection()
		{
			return haveLogReader;	
		}
		
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
			putLogLine("Info: Parsing started.");
			try
			{
				Regex reg = new Regex("(.+)[" + delimiter + "](.+)", RegexOptions.Compiled);
				Regex languageNames = new Regex(@"\[(.+)\] \[(.+)\]", RegexOptions.Compiled);
				FileStream fs = file.OpenRead();
				if(!fs.CanRead)
					throw new IOException("Can not read from file " + file.Name);
				TextReader reader = new StreamReader(fs);
				
				int lineno = 1;
				string line = reader.ReadLine();
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
				
				for(; line != null; line = reader.ReadLine(), lineno++)
				{
					MatchCollection mc = reg.Matches(line);
					if(mc.Count != 1)
					{
						putLogLine("Warning: Input File " + file.Name + ", Line " + lineno + ": Does not contain exactly one separator character `" + delimiter + "'. SKIPPING LINE.");
						continue;
					}
					
					Match theMatch = mc[0];
					if(theMatch.Groups.Count != 3)
					{
						putLogLine("Warning: Input File " + file.Name + ", Line " + lineno + ": Contains " + theMatch.Groups.Count + " capture groups; expected 2. SKIPPING LINE.");
						continue;
					}
					
					string left = theMatch.Groups[1].Value;
					string right = theMatch.Groups[2].Value;
					
					List<string> _try;
					if(ltr.TryGetValue(left.ToLower(), out _try))
					{
						_try.Add(right);	
					}
					else
					{
						_try = new List<string>();
						_try.Add(right);
						ltr.Add(left.ToLower(), _try);
					}
					
					if(rtl.TryGetValue(right.ToLower(), out _try))
					{
						_try.Add(left);	
					}
					else
					{
						_try = new List<string>();
						_try.Add(left);
						rtl.Add(right.ToLower(), _try);
					}
				}
				
				fs.Close();
				
				putLogLine("Info: Parsing complete.");
			}
			catch(SystemException se)
			{
				throw new IOException("Can not read from file " + file.Name + "\n" + se.Message);	
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

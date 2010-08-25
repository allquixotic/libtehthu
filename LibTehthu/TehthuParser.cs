using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace LibTehthu
{
	internal class TehthuParser : Parser
	{
		private Regex dictLineReg1 = null;
		private Regex dictLineReg2 = null;
		private Regex languageNames = null;
		private Regex suffixRegex = null;
		private TextReader reader;
		private int lineno;
		private Tehthu teh = null;
		private string currLine = null;
		private bool eof = false;
		private bool firstMapping = false;
		private const string DEFAULT_DELIMITER = "|";
		private string delimiter;
		
		internal TehthuParser(Tehthu t)
		{
			try
			{
				teh = t;
				FileStream fs = t.getFile().OpenRead();
				if(!fs.CanRead)
					throw new IOException("Can not read from file " + teh.getFile().Name);
				reader = new StreamReader(fs);
				lineno = 0;
				recalcRegexes();
				advance();
			}
			catch(SystemException se)
			{
				throw new IOException("Can not read from file " + teh.getFile().Name + "\n" + se.Message);	
			}
		}
		
		internal override FileFormat getFileFormat()
		{
			return FileFormat.TEHTHU;
		}
		
		internal override string getFileFormatName()
		{
			return "Tehthu";
		}
		
		internal override bool parseConfigLine()
		{
			if(reader == null || teh == null || isEOF() || currentRowIsMapping())
			{
				return false;	
			}
			
			MatchCollection langMatches = languageNames.Matches(currLine);
			Match theMatch;
			if(langMatches.Count == 1) //If the line is a language name definition stanza
			{
				theMatch = langMatches[0];
				if(theMatch.Groups.Count == 3)
				{
					teh.setLeftLanguageName(theMatch.Groups[1].Value);
					teh.setRightLanguageName(theMatch.Groups[2].Value);
				}
				else
				{
					teh.putLogLine("Warning: Syntax error on language names line: Expected 3 groups; found " + theMatch.Groups.Count);	
				}
				advance();
				return true;
			}
			else
			{
				//It's trying to tell us what the delimiter should be. Don't reparse, just update the regexes.
				if(currLine.Length == 1)
				{
					delimiter = currLine;
					advance();
					recalcRegexes();
					return true;
				}
				else
				{
					langMatches = suffixRegex.Matches(currLine);
					if(langMatches.Count == 1)
					{
						theMatch = langMatches[0];
						if(theMatch.Groups.Count == 3)
						{
							teh.addSuffixMapping(theMatch.Groups[1].Value, theMatch.Groups[2].Value);
						}
						advance();
						return true;
					}
					else
					{
						//This line isn't any known configuration option.
						return false;
					}
				}
			}
		}
		
		internal override bool parseMappedSet(out string lhs, out string rhs)
		{
			if(teh == null || !currentRowIsMapping())
			{
				lhs = rhs = null;
				return false;	
			}
			
			firstMapping = true;
			
			//Try to match the more strict regex first; reg2 has one or more trailing delimiters on the end that get discarded.
			MatchCollection mc = dictLineReg2.Matches(currLine);
			if(mc.Count != 1)
			{
				mc = dictLineReg1.Matches(currLine);
				if(mc.Count != 1)
				{
					teh.putLogLine("Warning: Input File " + teh.getFile().Name + ", Line " + lineno + ": Does not contain exactly one separator character `" + delimiter + "'. SKIPPING LINE.");
					advance();
					lhs = rhs = null;
					return false;
				}
			}
			
			Match theMatch = mc[0];
			if(theMatch.Groups.Count != 3)
			{
				teh.putLogLine("Warning: Input File " + teh.getFile().Name + ", Line " + lineno + ": Contains " + theMatch.Groups.Count + " capture groups; expected 2. SKIPPING LINE.");
				advance();
				lhs = rhs = null;
				return false;
			}
			
			lhs = theMatch.Groups[1].Value;
			rhs = theMatch.Groups[2].Value;
			advance();
			return true;
		}
		
		internal override int getCurrentRow()
		{
			return lineno;
		}
		
		internal override bool isEOF()
		{
			return eof;
		}
		
		private void recalcRegexes()
		{
			languageNames = new Regex(@"\[(.+)\]" + delimiter + @"\[(.+)\]", RegexOptions.Compiled);
			suffixRegex = new Regex(@"\{(.+)\}" + delimiter + @"\{(.+)\}", RegexOptions.Compiled);
			dictLineReg1 = new Regex("(.+)[" + delimiter + "](.+)", RegexOptions.Compiled);
			dictLineReg2 = new Regex("(.+)[" + delimiter + "](.+)[" + delimiter + "]+", RegexOptions.Compiled);
		}
		
		private void advance()
		{
			if(!eof)
			{
				currLine = reader.ReadLine();
				if(currLine == null)
				{
					eof = true;
				}
			}
		}
			
		private bool currentRowIsMapping()
		{	
			return (!eof && (firstMapping ||
			            (languageNames.Matches(currLine).Count != 1
			   			&&  suffixRegex.Matches(currLine).Count != 1
			   			&&  currLine.Length != 1)));
		}
	}
}


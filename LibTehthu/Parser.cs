using System;
namespace LibTehthu
{
	/*! The internal interface for a dictionary parser.
	 * */
	internal abstract class Parser
	{
		internal abstract FileFormat getFileFormat();
		internal abstract string getFileFormatName();
		
		//Advance the input and return true iff the current line was a config line.
		internal abstract bool parseConfigLine();
		
		//Put a dictionary mapping into the out variables.
		//Advance the input regardless; return true if the out variables were set.
		internal abstract bool parseMappedSet(out string lhs, out string rhs);
		internal abstract int getCurrentRow();
		internal abstract bool isEOF();
		
		/*! Determine whether a lval-rval pair are a name mapping; true if so; false if not. Outs them if so.*/
		internal static bool isNameMapping(string lval, string rval, out string leftname, out string rightname)
		{
			if (lval != null && rval != null && 
			        lval.Length > 2 
			        && rval.Length > 2 
			        && lval[0] == '[' && lval[lval.Length - 1] == ']'
				   && rval[0] == '[' && rval[rval.Length - 1] == ']')
			{
				leftname = lval.Substring(1, lval.Length - 2);
				rightname = rval.Substring(1, rval.Length - 2);
				return true;	
			}
			else
			{
				leftname = rightname = null;
				return false;	
			}
		}
		
		/*! Determine whether a lval-rval pair are a suffix mapping; true if so; false if not. Outs them if so.*/
		internal static bool isSuffixMapping(string lval, string rval, out string leftsuffix, out string rightsuffix)
		{
			if (lval != null && rval != null
			    && lval.Length > 2
			    && rval.Length > 2
			    && lval[0] == '{' && lval[lval.Length - 1] == '}'
			    && rval[0] == '{' && rval[rval.Length - 1] == '}')
			{
				leftsuffix = lval.Substring(1, lval.Length - 2);	
				rightsuffix = rval.Substring(1, rval.Length - 2);
				return true;
			}
			else
			{
				leftsuffix = rightsuffix = null;
				return false;
			}
		}
	}
	
	
}


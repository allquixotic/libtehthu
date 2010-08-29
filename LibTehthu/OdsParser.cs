using System;
using System.IO;
using System.Data;
using ODSReadWrite;

namespace LibTehthu
{
	internal class OdsParser : Parser
	{
		private FileInfo file = null;
		private int currentRow = 0;
		private DataTable dt = null;
		private bool foundFirstMapping = false;
		private Tehthu teh = null;
		
		internal OdsParser(Tehthu t)
		{
			this.teh = t;
			this.file = t.getFile();
			OdsReaderWriter orw = new OdsReaderWriter();
			DataSet ds = orw.ReadOdsFile(file.FullName);
			dt = ds.Tables["Dictionary"];
			if(dt == null)
			{
				dt = ds.Tables["Sheet1"];
				if(dt == null)
				{
					dt = ds.Tables[0];
					if(dt == null)
					{
						teh.putLogLine("Parser error: Couldn't find a spreadsheet containing the dictionary. The dictionary will be empty for this Tehthu session.");
						return;
					}
				}
			}
		}
		
		internal override FileFormat getFileFormat()
		{
			return FileFormat.ODS;
		}
		
		internal override string getFileFormatName()
		{
			return ".ODS";
		}
		
		internal override bool parseConfigLine()
		{
			if(dt == null || teh == null || isEOF() || currentRowIsMapping())
			{
				return false;	
			}
			
			DataRow dr = dt.Rows[currentRow];
			if(dr.ItemArray.Length < 2)
			{
				return false;	
			}
			
			string lval = null, rval = null, a = null, b = null;
			
			lval = dr.ItemArray[0].ToString();
			rval = dr.ItemArray[1].ToString();
			
			if(isNameMapping(lval, rval, out a, out b))
			{
				teh.setLeftLanguageName(a);
				teh.setRightLanguageName(b);
				currentRow++;
				return true;
			}
			
			if(isSuffixMapping(lval, rval, out a, out b))
			{
				teh.addSuffixMapping(a, b);
				currentRow++;
				return true;
			}
			
			return false;
		}	
		
		internal override bool parseMappedSet(out string lhs, out string rhs)
		{
			string lval = null, rval = null;
			if(dt == null || !currentRowIsMapping())
			{
				lhs = rhs = null;
				return false;	
			}
			
			DataRow dr = dt.Rows[currentRow];
			if(dr.ItemArray.Length <= 0)
			{
				lhs = rhs = null;
				return false;	
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
			}
			
			if(lval.Trim().Length <= 0 || rval.Trim().Length <= 0)
			{
				lhs = rhs = null;
				currentRow++;
				return false;
			}
			else
			{
				lhs = lval;
				rhs = rval;	
			}
			
			currentRow++;
			return true;
		}
		
		internal override int getCurrentRow()
		{
			return currentRow;	
		}
		
		internal override bool isEOF()
		{
			if(dt == null || currentRow >= dt.Rows.Count)
			{
				return true;	
			}
			else
			{
				return false;	
			}
		}
		
		private bool currentRowIsMapping()
		{
			if(dt == null || isEOF())
				return false;
			
			DataRow dr = dt.Rows[currentRow];
			if(dr.ItemArray.Length < 2)
			{
				return false;
			}
			
			if(foundFirstMapping)
			{
				return true;
			}
			else
			{
				string lval = dr.ItemArray[0].ToString();
				string rval = dr.ItemArray[1].ToString();
				//This may be the first mapping, or it could be a config, so we have to check.
				string a,b;
				if(isNameMapping(lval, rval, out a, out b) || isSuffixMapping(lval, rval, out a, out b) )
				{
					return false;
				}
				else
				{
					return true;	
				}
			}
		}
	}
}


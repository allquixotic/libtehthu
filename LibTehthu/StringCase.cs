/* Copyright (c) 2010 Sean McNamara
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://sam.zoy.org/wtfpl/COPYING for more details. */

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTehthu
{

	public enum CaseType
	{
		Caps,
		Proper,
		Lower,
		Mixed,
		Null
	}
	
	public class StringCase
	{
		public static string transformCase(string s, CaseType trans)
		{
			if(s == null)
				return null;
			
			switch(trans)
			{
			case CaseType.Null:
					return null;
			case CaseType.Caps:
				return s.ToUpper();
			case CaseType.Lower:
				return s.ToLower();
			case CaseType.Mixed:
				return s;
			case CaseType.Proper:
				string retval = s;
				int start = 0;
				while(start < retval.Length && !Char.IsLetter(retval[start]))
				{
					start++;
				}
				
				if(start >= retval.Length)
				{
					return retval;
				}
				
				char[] array = retval.ToCharArray();
				array[start] = Char.ToUpper(retval[start]);
				for(int i = start+1; i < retval.Length; i++)
				{
					array[i] = Char.ToLower(retval[i]);
				}
				return new string(array);
			default:
				return null;
			}
		}
		
		public static CaseType getCaseType(string s)
		{
			if(s == null || s.Length == 0)
			{
				return CaseType.Null;
			}
			
			//Determine where the first letter is. Short-circuit if start == Length.
			int start = 0;
			while(start < s.Length && !Char.IsLetter(s[start]))
			{
				start++;
			}
			
			//String consists entirely of non-letters.
			if(start >= s.Length)
			{
				return CaseType.Null;
			}
			
			bool firstUpper = Char.IsUpper(s[start]);
			bool notCaps = !firstUpper;
			bool notLower = firstUpper;
			bool remainHasOneUpper = false;
			bool remainHasOneLower = false;
			
			for(int i = start+1; i < s.Length; i++)
			{
				if(Char.IsLetter(s[i]))
				{
					if(!Char.IsUpper(s[i]))
					{
						//We found a lowercase letter; the whole string's not CAPS.
						notCaps = true;
					}
					else
					{
						//We found an uppercase letter; the whole string's not lowercase.
						notLower = true;
					}
					
					remainHasOneUpper = remainHasOneUpper || Char.IsUpper(s[i]);
					remainHasOneLower = remainHasOneLower || !Char.IsUpper(s[i]);
				}
			}
			
			if(!notCaps)
			{
				return CaseType.Caps;	
			}
			
			if(!notLower)
			{
				return CaseType.Lower;	
			}
			
			if(firstUpper && !remainHasOneUpper)
			{
				return CaseType.Proper;	
			}
			
			return CaseType.Mixed;
		}
	}	
}
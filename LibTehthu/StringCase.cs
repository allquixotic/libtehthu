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

/*! \file StringCase.cs
 *  \brief Utilities for dealing with capital and lowercase letters in creative ways.
 * 
 *  Tehthu.cs is one-way tightly coupled to the StringCase API.
 * */

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LibTehthu
{

	/*! \brief The types of caseness for a word.
	 *  Caps: Every letter in the word is a capital letter.\n
	 *  Proper: Only the first letter of the word is a capital letter; the rest are lowercase.\n
	 *  Lower: Every letter in the word is a lowercase letter.\n
	 *  Mixed: None of the above cases (Caps, Proper, Lower) apply, but the word contains at least one letter.\n
	 *  Null: The word contains no letters (only symbols), is null, or zero-length.
	 * */
	public enum CaseType
	{
		Caps,
		Proper,
		Lower,
		Mixed,
		Null
	}
	
	/*! \brief The StringCase class.
	 *  A class for detecting the case of, and transforming the case of, letters in a string.\n
	 *  A "letter" is a special type of character that is defined as having both a capital and a lowercase type.\n
	 *  A "symbol" is a character that does not have any such capital/lowercase modality.\n
	 *  Capital and lowercase letters have different character codes. The mapping between capital and lower letters is defined by the CLR.\n
	 *  Specifically, the routines Char.ToUpper() and Char.ToLower() are used.
	 * */
	public class StringCase
	{
		/*! \brief Convert the caseness of a string into the specified type.
		 *  \param s The string to convert.
		 *  \param trans The transformation to perform.
		 *  \return The transformed string.
		 * 
		 *  Symbols (whitespace, non-letters) are unmodified. Letter characters are modified as dictated by the CaseType: \n
		 *  Caps:   Every letter in the string is converted to a capital letter.\n
		 *  Proper: The first letter of the word is made capital; the rest are made lowercase.\n
		 *  Lower:  Every letter in the string is converted to a lowercase letter.\n
		 *  Mixed:  The string is returned unmodified, since there are many ways to create a mixed case string, and we don't want to choose one arbitrarily.\n
		 *  Null:   A null reference is returned.
		 * */
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
		
		/*! \brief Determine the case type of the specified string.
		 *  \return The case type.
		 * 
		 *  See the documentation of the CaseType enum for details.
		 * */
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
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

using LibTehthu;
using System;
using System.IO;
using System.Collections.Generic;

namespace LibTehthuTests
{
	public class StringCaseTest
	{
		public StringCaseTest()
		{
			string[] tests = {"A", "a", "AB", "Ab", "aB", "ab", "ABC", "ABc", "AbC", "Abc", "aBC", "aBc", "abC", "abc", "-*+", "-*+A", "", null};
			CaseType[] expectedResults = {CaseType.Caps, CaseType.Lower, CaseType.Caps, CaseType.Proper, CaseType.Mixed, CaseType.Lower,
				CaseType.Caps, CaseType.Mixed, CaseType.Mixed, CaseType.Proper, CaseType.Mixed, CaseType.Mixed, CaseType.Mixed, 
				CaseType.Lower, CaseType.Null, CaseType.Caps, CaseType.Null, CaseType.Null};
			
			for(int i = 0; i < tests.Length; i++)
			{
				CaseType ct = StringCase.getCaseType(tests[i]);
				if(ct != expectedResults[i])
				{
					Console.WriteLine("Error: Expected " + expectedResults[i].ToString() + " but got " + ct.ToString() + " for string " + tests[i]);
				}
				else
				{
					Console.WriteLine(tests[i] + " is " + expectedResults[i].ToString());
				}
			}
			
			for(int i = 0; i < tests.Length; i++)
			{
				foreach(CaseType ct in Enum.GetValues(typeof(CaseType))) 
				{
					Console.WriteLine("Transform " + tests[i] + " -> " + ct.ToString() + ": " + StringCase.transformCase(tests[i], ct));
				}
			}
		}
	}
}
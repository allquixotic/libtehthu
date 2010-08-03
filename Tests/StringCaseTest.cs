/* Copyright (c) 2010 Sean McNamara
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://sam.zoy.org/wtfpl/COPYING for more details. */

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
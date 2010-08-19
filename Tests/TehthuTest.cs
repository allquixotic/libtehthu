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
using System.Threading;
using System.Collections.Generic;

namespace LibTehthuTests
{
	public class TehthuTest
	{
		private Tehthu teth;
		
		public TehthuTest()
		{
			FileInfo fi = new FileInfo(@"../../../maren-dict-1.1.ods");
			teth = new Tehthu(fi);
			teth.connectToLog();
			
			//Need a separate thread because takeLogLine() blocks until there's a log line to read.
			Thread t = new Thread(new ThreadStart(monitor));
			t.Start();
			
			teth.reparse();
			
			string retval;
			
			//Positive translateWord tests
			Console.WriteLine("Translating between LHS: " + teth.getLeftLanguageName() + " and RHS: " + teth.getRightLanguageName());
			
			teth.translateWord("Anyone", out retval, TranslateDirection.LeftToRight);
			Console.WriteLine("Anyone: " + retval);
			
			teth.translateWord("speak", out retval, TranslateDirection.LeftToRight);
			Console.WriteLine("speak: " + retval);
			
			teth.translateWord("Spanish", out retval, TranslateDirection.LeftToRight);
			Console.WriteLine("Spanish: " + retval);
			
			teth.translateWord("here?", out retval, TranslateDirection.LeftToRight);
			Console.WriteLine("here?: " + retval);
			
			teth.translateWord("Álguien", out retval, TranslateDirection.RightToLeft);
			Console.WriteLine("Álguien: " + retval);
			
			teth.translateWord("habla", out retval, TranslateDirection.RightToLeft);
			Console.WriteLine("habla: " + retval);
			
			teth.translateWord("Español", out retval, TranslateDirection.RightToLeft);
			Console.WriteLine("Español: " + retval);
			
			teth.translateWord("aquí?", out retval, TranslateDirection.RightToLeft);
			Console.WriteLine("aquí?: " + retval);
			
			teth.translateWord("Archer", out retval, TranslateDirection.LeftToRight);
			Console.WriteLine("Archer: " + retval);
			
			teth.translateWord("T'Pol", out retval, TranslateDirection.RightToLeft);
			Console.WriteLine("T'Pol: " + retval);
			
			t.Join(2000);
			t.Abort();
		}
		
		public void monitor()
		{
			while(true)
			{
				try
				{
					Console.WriteLine(teth.takeLogLine());
				}
				catch(ThreadAbortException)
				{
					return;	
				}
			}
		}
	}
}
﻿//
// Program.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2018 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace PerfTool
{
	class MainClass
	{
		public static void Main (string [] args)
		{
			if (args.Length == 0) {
				PrintHelp ();
				return;
			}

			var command = args [0];
			if (command == "generate-results" && args.Length == 4) {
				GenerateResults (args [1], args [2], args [3]);
			} else
				PrintHelp ();
		}

		static void GenerateResults (string baseFile, string inputFile, string resultsFile)
		{
			var baseTestSuite = new TestSuiteResult ();
			baseTestSuite.Read (baseFile);

			var inputTestSuite = new TestSuiteResult ();
			inputTestSuite.Read (inputFile);

			inputTestSuite.RegisterPerformanceRegressions (baseTestSuite);
			inputTestSuite.Write (resultsFile);
		}

		static void PrintHelp ()
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("generate-results <base-file> <input-file> <output-file>");
			Console.WriteLine ("    Detects regressions in input-file when compared to base-file.");
			Console.WriteLine ("    It generates an NUnit test results file with test failures.");
		}
	}
}

using Xunit;
using System;

namespace PSTests
{
	public class BlahTests
	{

//		[Fact]
		public static void simpleTest()
		{
			Assert.Equal(1,1);
		}

		public static void testPlatform()
		{
			Assert.Equal(System.Management.Automation.Platform.IsLinux(),true);
			// UserName not yet implemented
			// Assert.Equal(System.Management.Automation.Platform.UserName,"peter2");
		}

		static void Main(string[] args)
		{
			simpleTest();
			testPlatform();
			Console.WriteLine("finished running tests");
		}
	}
}


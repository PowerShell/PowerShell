//using Xunit;
using System;

namespace PSTests
{
	public static class Assert
	{
		public static void Equal<T>(T a, T b) where T : System.IComparable<T>
		{
			if (a.CompareTo(b) != 0)
				throw new Exception("Assert failed: a == b (" + a.ToString() + " == " + b.ToString());
			Console.WriteLine("Assert.Equal OK");
		}
	}

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
			Assert.Equal(System.Management.Automation.Platform.UserName,"peter2");
		}

		static void Main(string[] args)
		{
			simpleTest();
			testPlatform();
			Console.WriteLine("finished running tests");
		}
	}
}


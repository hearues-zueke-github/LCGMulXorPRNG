using System;
using System.Collections.Generic;

using PRNG;

class EntryPoint {
	static void Main(string[] args) {
		// Display the number of command line arguments.
		var filePath = "/tmp/test123.txt";
		List<byte> seedU8 = new List<byte> { 0x00, 0x01, 0x02, 0x03, 0x04 };
		long lengthU8 = 256;
		var typesOfArr = "u64:10,u64:1,f64:20";
		Console.WriteLine(args.Length);

	}
}

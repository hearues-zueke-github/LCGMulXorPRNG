using System;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using System.Collections.Generic;
using System.Security.Cryptography;

using PRNG;

class EntryPoint {
	// cannot be defined in a method. must be defined as an inner class.
	class Type {
		public String type;
		public int amount;
		public Type(String type, int amount) {
			this.type = type;
			this.amount = amount;
		}
	}

	static void Main(string[] args) {
		Dictionary < string, string > map = new Dictionary < string, string > ();
		foreach (String arg in args) {
			String[] arr = arg.Split("=");
			Debug.Assert(arr.Count() == 2);

			String key = arr[0];
			String val = arr[1];
			if (map.ContainsKey(key)) {
				Console.WriteLine("Key '" + key + "' was found again!");
				Debug.Assert(false);
			}
			map.Add(key, val);
		}

		Debug.Assert(map.ContainsKey("file_path"));
		Debug.Assert(map.ContainsKey("seed_u8"));
		Debug.Assert(map.ContainsKey("length_u8"));
		Debug.Assert(map.ContainsKey("types_of_arr"));

		var filePath = map["file_path"];
		int lengthU8 = Int32.Parse(map["length_u8"]);
		byte[] seedU8;
		{
			String[] arr_2 = map["seed_u8"].Split(",");
			seedU8 = new byte[arr_2.Length];
			int i = 0;
			foreach (String v in arr_2) {
				seedU8[i++] = (byte)Convert.ToInt32(v, 16);
			}
		}

		Type[] typesOfArr;
		{
			String[] arr_1 = map["types_of_arr"].Split(",");
			typesOfArr = new Type[arr_1.Length];
			int i = 0;
			foreach (String v in arr_1) {
				String[] arr_3 = v.Split(":");
				Debug.Assert(arr_3.Length == 2, "Needed length of 2!");
				typesOfArr[i++] = new Type(arr_3[0], Int32.Parse(arr_3[1]));
			}
		}

		RandomNumberDevice rnd = new RandomNumberDevice(lengthU8, seedU8);
		// rnd.print_current_state();

		StreamWriter file = new StreamWriter(filePath);

		rnd.write_current_state_to_file(file);

		foreach (Type type in typesOfArr) {
			switch (type.type) {
				case "u64": {
						ulong[] vec = rnd.generate_new_values_u64(type.amount);
						file.Write("v_vec_u64:" + RandomNumberDevice.convert_vec_u64_to_string(vec) + "\n");
						rnd.write_current_state_to_file(file);
					}
					break;
				case "f64": {
						double[] vec = rnd.generate_new_values_f64(type.amount);
						file.Write("v_vec_f64:" + RandomNumberDevice.convert_vec_f64_to_string(vec) + "\n");
						rnd.write_current_state_to_file(file);
					}
					break;
				default: {
						Debug.Assert(false, "Should never happen!");
					}
					break;
			}
		}

		file.Close();
	}
}

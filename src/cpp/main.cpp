#include <stdlib.h>

#include <algorithm>
#include <cassert>
#include <fstream>
#include <iostream>
#include <map>
#include <set>
#include <stdint.h>
#include <string>
#include <vector>

#include "fmt/core.h"
#include "fmt/format.h"
#include "fmt/ranges.h"

#include "PRNG.h"

using std::copy;
using std::cout;
using std::endl;
using std::fstream;
using std::ios;
using std::map;
using std::set;
using std::string;
using std::vector;

using fmt::format;
using fmt::print;

using PRNG::RandomNumberDevice;

void tokenize(const string &s, const char delimiter, vector<string> &vec) {
	vec.resize(0);
	size_t first_idx = 0;
	for (size_t last_idx = 0; (last_idx = s.find(delimiter, last_idx)) != string::npos; ++last_idx) {
		vec.push_back(s.substr(first_idx, last_idx - first_idx));
		first_idx = last_idx + 1;
	}
	vec.push_back(s.substr(first_idx));
}

int main(int argc, char* argv[]) {
	map<string, string> m;
	for (int i = 1; i < argc; ++i) {
		vector<string> vec;
		tokenize(argv[i], '=', vec);
		assert((vec.size() == 2) && "Size must be 2!");

		const string& key = vec[0];
		const string& val = vec[1];

		assert((m.find(key) == m.end()) && "Key is already defined!");

		m[vec[0]] = vec[1];
	}

	assert((m.find("file_path") != m.end()) && "'file_path' is not found!");
	assert((m.find("seed_u8") != m.end()) && "'seed_u8' is not found!");
	assert((m.find("length_u8") != m.end()) && "'length_u8' is not found!");
	assert((m.find("types_of_arr") != m.end()) && "'types_of_arr' is not found!");

	const string& file_path = m["file_path"];
	vector<uint8_t> vec_seed(0);
	{
		vector<string> vec;
		tokenize(m["seed_u8"], ',', vec);
		const size_t size = vec.size();
		for (size_t i = 0; i < size; ++i) {
			vec_seed.push_back(strtol(vec[i].c_str(), nullptr, 16));
		}
	}
	const size_t length_u8 = strtoull(m["length_u8"].c_str(), nullptr, 10);

	class Type {
	public:
		string type_;
		size_t amount_;
	};
	vector<Type> vec_type(0);
	{
		vector<string> vec_1;
		tokenize(m["types_of_arr"], ',', vec_1);

		const size_t size_1 = vec_1.size();
		for (size_t i = 0; i < size_1; ++i) {
			vector<string> vec_2;
			tokenize(vec_1[i], ':', vec_2);
			assert((vec_2.size() == 2) && "Size must be 2!");

			vec_type.push_back({vec_2[0], strtoull(vec_2[1].c_str(), nullptr, 10)});
		}
	}

	fstream f;
	f.open(file_path, ios::out);

	RandomNumberDevice rnd = RandomNumberDevice(length_u8, vec_seed);
	rnd.write_current_state_to_file(f);

	const size_t size = vec_type.size();
	for (size_t i = 0; i < size; ++i) {
		const Type& type = vec_type[i];

		if (type.type_ == "u64") {
			vector<uint64_t> vec;
			rnd.generate_new_values_uint64_t(vec, type.amount_);
			f << format("v_vec:{}\n", RandomNumberDevice::convert_vec_uint64_t_to_string(vec));
		} else if (type.type_ == "f64") {
			vector<double> vec;
			rnd.generate_new_values_double(vec, type.amount_);
			f << format("v_vec:{}\n", RandomNumberDevice::convert_vec_double_to_string(vec));
		} else {
			print("found type: {}\n", type.type_);
			assert(false && "Not a valid type!");
		}

		rnd.write_current_state_to_file(f);
	}

	f.close();

	return 0;
}
#ifndef OWNPRNG_RANDOM_NUMBER_DEVICE_H
#define OWNPRNG_RANDOM_NUMBER_DEVICE_H

#include <iostream>
#include <fstream>
#include <numeric>
#include <vector>
#include <cstring>
#include <cassert>

#include "fmt/core.h"
#include "fmt/format.h"
#include "fmt/ranges.h"

using fmt::format;
using fmt::print;

#include "StateMachine.h"
#include "PRNG_constants.h"
#include "sha256.h"

namespace PRNG {
	class RandomNumberDevice {
	public:
		size_t amount_u8_;
		size_t amount_u64_;
		size_t amount_sha256_;
		std::vector<uint8_t> vec_state_u8_;
		uint8_t* ptr_state_;
		uint64_t* ptr_state_u64_;

		StateMachine sm_curr_;
		StateMachine sm_prev_;

		uint8_t vector_constant_[BLOCK_SIZE];

		RandomNumberDevice(const size_t amount_u8, const std::vector<uint8_t>& vec_seed);

		void hash_once_loop();
		inline void hash_2_block(const size_t idx_0, const size_t idx_1);
		inline bool are_block_equal(const uint8_t* block_0, const uint8_t* block_1);
		std::string convert_vec_u8_to_string(const std::vector<uint8_t>& vec);
		static std::string convert_vec_u64_to_string(const std::vector<uint64_t>& vec);
		static std::string convert_vec_f64_to_string(const std::vector<double>& vec);
		void print_current_state();
		void write_current_state_to_file(std::fstream& f);
		inline double get_next_double();
		void generate_new_values_u64(std::vector<uint64_t>& vec, const size_t amount);
		void generate_new_values_f64(std::vector<double>& vec, const size_t amount);

		void save_current_state();
		void restore_previous_state();
	};


	inline double RandomNumberDevice::get_next_double() {
		const uint64_t val = sm_curr_.get_next_uint64_t();
		return MIN_VAL_F64 * (val & MASK_U64_F64);
	}
};

#endif // OWNPRNG_RANDOM_NUMBER_DEVICE_H

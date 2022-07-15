#include "RandomNumberDevice.h"

namespace PRNG {
	RandomNumberDevice::RandomNumberDevice(const size_t amount_u8, const std::vector<uint8_t>& vec_seed) {
		assert((amount_u8 % BLOCK_SIZE) == 0);
		assert(amount_u8 > BLOCK_SIZE);

		amount_u8_ = amount_u8;
		amount_sha256_ = amount_u8 / BLOCK_SIZE;
		amount_u64_ = amount_u8 / sizeof(uint64_t);
		vec_state_u8_.resize(amount_u8);
		ptr_state_ = &vec_state_u8_[0];
		ptr_state_u64_ = (uint64_t*)ptr_state_;
		std::fill(ptr_state_, ptr_state_ + amount_u8_, 0);

		sm_curr_ = StateMachine(amount_u64_);
		sm_prev_ = StateMachine(amount_u64_);

		const size_t seed_len = vec_seed.size();
		for (size_t i = 0; i < seed_len; ++i) {
			ptr_state_[i % amount_u8_] ^= vec_seed[i];
		}

		// TODO: can be removed, because the values are added to the next block, which
		// is equal to the previous one.
		std::iota(vector_constant_, vector_constant_ + BLOCK_SIZE, 1);

		hash_once_loop(); hash_once_loop();
		memcpy(&sm_curr_.vec_mult_x_[0], ptr_state_u64_, amount_u8_);
		hash_once_loop(); hash_once_loop();
		memcpy(&sm_curr_.vec_mult_a_[0], ptr_state_u64_, amount_u8_);
		hash_once_loop(); hash_once_loop();
		memcpy(&sm_curr_.vec_mult_b_[0], ptr_state_u64_, amount_u8_);
		hash_once_loop(); hash_once_loop();
		memcpy(&sm_curr_.vec_xor_x_[0], ptr_state_u64_, amount_u8_);
		hash_once_loop(); hash_once_loop();
		memcpy(&sm_curr_.vec_xor_a_[0], ptr_state_u64_, amount_u8_);
		hash_once_loop(); hash_once_loop();
		memcpy(&sm_curr_.vec_xor_b_[0], ptr_state_u64_, amount_u8_);

		// correct the values for the values a and b for mult and xor
		for (size_t i = 0; i < amount_u64_; ++i) {
			sm_curr_.vec_mult_a_[i] += 1 - (sm_curr_.vec_mult_a_[i] % 4);
			sm_curr_.vec_mult_b_[i] += 1 - (sm_curr_.vec_mult_b_[i] % 2);

			sm_curr_.vec_xor_a_[i] += - (sm_curr_.vec_xor_a_[i] % 2);
			sm_curr_.vec_xor_b_[i] += 1 - (sm_curr_.vec_xor_b_[i] % 2);
		}

		save_current_state();
	}

	void RandomNumberDevice::hash_once_loop() {
		for (size_t i = 0; i < amount_sha256_; ++i) {
			hash_2_block((i + 0) % amount_sha256_, (i + 1) % amount_sha256_);
		}
	}

	inline void RandomNumberDevice::hash_2_block(const size_t idx_0, const size_t idx_1) {
		uint8_t* block_0 = ptr_state_ + BLOCK_SIZE * idx_0;
		uint8_t* block_1 = ptr_state_ + BLOCK_SIZE * idx_1;

		if (are_block_equal(block_0, block_1)) {
			for (size_t i = 0; i < BLOCK_SIZE; ++i) {
				block_1[i] ^= vector_constant_[i];
			}
		}

		SHA256_CTX ctx_0;
		uint8_t hash_0[32];

		SHA256Init(&ctx_0);
		SHA256Update(&ctx_0, block_0, BLOCK_SIZE);
		SHA256Final(&ctx_0, hash_0);

		SHA256_CTX ctx_1;
		uint8_t hash_1[32];

		SHA256Init(&ctx_1);
		SHA256Update(&ctx_1, block_1, BLOCK_SIZE);
		SHA256Final(&ctx_1, hash_1);

		for (size_t i = 0; i < BLOCK_SIZE; ++i) {
			block_1[i] ^= hash_0[i] ^ hash_1[i] ^ block_0[i];
		}
	}

	inline bool RandomNumberDevice::are_block_equal(const uint8_t* block_0, const uint8_t* block_1) {
		for (size_t i = 0; i < BLOCK_SIZE; ++i) {
			if (block_0[i] != block_1[i]) {
				return false;
			}
		}
		return true;
	}

	std::string RandomNumberDevice::convert_vec_u8_to_string(const std::vector<uint8_t>& vec) {
		std::string s = "";
		const size_t size = vec.size();
		if (size > 0) {
			s += format("{:02X}", vec[0]);
			for (size_t i = 1; i < size; ++i) {
				s += format(",{:02X}", vec[i]);
			}
		}
		return s;
	}

	std::string RandomNumberDevice::convert_vec_u64_to_string(const std::vector<uint64_t>& vec) {
		std::string s = "";
		const size_t size = vec.size();
		if (size > 0) {
			s += format("{:016X}", vec[0]);
			for (size_t i = 1; i < size; ++i) {
				s += format(",{:016X}", vec[i]);
			}
		}
		return s;
	}

	std::string RandomNumberDevice::convert_vec_f64_to_string(const std::vector<double>& vec) {
		std::string s = "";
		const size_t size = vec.size();
		if (size > 0) {
			s += format("{:.016f}", vec[0]);
			for (size_t i = 1; i < size; ++i) {
				s += format(",{:.016f}", vec[i]);
			}
		}
		return s;
	}

	void RandomNumberDevice::print_current_state() {
		print("v_state_u8:{}\n", convert_vec_u8_to_string(vec_state_u8_));
		print("v_x_mult:{}\n", convert_vec_u64_to_string(sm_curr_.vec_mult_x_));
		print("v_a_mult:{}\n", convert_vec_u64_to_string(sm_curr_.vec_mult_a_));
		print("v_b_mult:{}\n", convert_vec_u64_to_string(sm_curr_.vec_mult_b_));
		print("v_x_xor:{}\n", convert_vec_u64_to_string(sm_curr_.vec_xor_x_));
		print("v_a_xor:{}\n", convert_vec_u64_to_string(sm_curr_.vec_xor_a_));
		print("v_b_xor:{}\n", convert_vec_u64_to_string(sm_curr_.vec_xor_b_));
		print("idx_values_mult:{}\n", sm_curr_.idx_mult_);
		print("idx_values_xor:{}\n", sm_curr_.idx_xor_);
	}

	void RandomNumberDevice::write_current_state_to_file(std::fstream& f) {
		f << format("v_state_u8:{}\n", convert_vec_u8_to_string(vec_state_u8_));
		f << format("v_x_mult:{}\n", convert_vec_u64_to_string(sm_curr_.vec_mult_x_));
		f << format("v_a_mult:{}\n", convert_vec_u64_to_string(sm_curr_.vec_mult_a_));
		f << format("v_b_mult:{}\n", convert_vec_u64_to_string(sm_curr_.vec_mult_b_));
		f << format("v_x_xor:{}\n", convert_vec_u64_to_string(sm_curr_.vec_xor_x_));
		f << format("v_a_xor:{}\n", convert_vec_u64_to_string(sm_curr_.vec_xor_a_));
		f << format("v_b_xor:{}\n", convert_vec_u64_to_string(sm_curr_.vec_xor_b_));
		f << format("idx_values_mult:{}\n", sm_curr_.idx_mult_);
		f << format("idx_values_xor:{}\n", sm_curr_.idx_xor_);
	}


	inline double RandomNumberDevice::get_next_double() {
		const uint64_t val = sm_curr_.get_next_uint64_t();
		return MIN_VAL_F64 * (val & MASK_U64_F64);
	}

	void RandomNumberDevice::generate_new_values_u64(std::vector<uint64_t>& vec, const size_t amount) {
		vec.resize(amount);

		for (size_t i = 0; i < amount; ++i) {
			vec[i] = sm_curr_.get_next_uint64_t();
		}
	}

	void RandomNumberDevice::generate_new_values_f64(std::vector<double>& vec, const size_t amount) {
		vec.resize(amount);

		for (size_t i = 0; i < amount; ++i) {
			vec[i] = get_next_double();
		}
	};

	void RandomNumberDevice::save_current_state() {
		sm_prev_.copy_sm(sm_curr_);
	}

	void RandomNumberDevice::restore_previous_state() {
		sm_curr_.copy_sm(sm_prev_);
	}
}
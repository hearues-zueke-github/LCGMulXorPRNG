#ifndef OWNPRNG_STATEMACHINE_H
#define OWNPRNG_STATEMACHINE_H

#include <cstdint>
#include <vector>
#include <cstddef>

namespace PRNG {
	class StateMachine {
	public:
		std::vector<uint64_t> vec_mult_x_;
		std::vector<uint64_t> vec_mult_a_;
		std::vector<uint64_t> vec_mult_b_;

		std::vector<uint64_t> vec_xor_x_;
		std::vector<uint64_t> vec_xor_a_;
		std::vector<uint64_t> vec_xor_b_;

		size_t amount_u64_;
		size_t idx_mult_;
		size_t idx_xor_;

		StateMachine();
		StateMachine(const size_t& amount_vals);
		void copy_sm(const StateMachine& other);
		
		inline uint64_t get_next_uint64_t() {
			const uint64_t val_mult_new = ((vec_mult_a_[idx_mult_] * vec_mult_x_[idx_mult_]) + vec_mult_b_[idx_mult_]) ^ vec_xor_x_[idx_xor_];
			vec_mult_x_[idx_mult_] = val_mult_new;

			++idx_mult_;
			if (idx_mult_ >= amount_u64_) {
				idx_mult_ = 0;

				vec_xor_x_[idx_xor_] = (vec_xor_a_[idx_xor_] ^ vec_xor_x_[idx_xor_]) + vec_xor_b_[idx_xor_];

				++idx_xor_;
				if (idx_xor_ >= amount_u64_) {
					idx_xor_ = 0;
				}
			}

			return val_mult_new;
		}
	};
}

#endif //OWNPRNG_STATEMACHINE_H

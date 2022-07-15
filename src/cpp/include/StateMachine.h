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
		uint64_t get_next_uint64_t();
	};
}

#endif //OWNPRNG_STATEMACHINE_H

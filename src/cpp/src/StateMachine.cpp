#include "StateMachine.h"

namespace PRNG {
	StateMachine::StateMachine() :
			vec_mult_x_(),
			vec_mult_a_(),
			vec_mult_b_(),
			vec_xor_x_(),
			vec_xor_a_(),
			vec_xor_b_(),
			amount_u64_(0),
			idx_mult_(0),
			idx_xor_(0)
	{}

	StateMachine::StateMachine(const size_t& amount_vals) :
			vec_mult_x_(amount_vals),
			vec_mult_a_(amount_vals),
			vec_mult_b_(amount_vals),
			vec_xor_x_(amount_vals),
			vec_xor_a_(amount_vals),
			vec_xor_b_(amount_vals),
			amount_u64_(amount_vals),
			idx_mult_(0),
			idx_xor_(0)
	{}

	void StateMachine::copy_sm(const StateMachine& other) {
		vec_mult_x_ = other.vec_mult_x_;
		vec_mult_a_ = other.vec_mult_a_;
		vec_mult_b_ = other.vec_mult_b_;
		vec_xor_x_ = other.vec_xor_x_;
		vec_xor_a_ = other.vec_xor_a_;
		vec_xor_b_ = other.vec_xor_b_;
		idx_mult_ = other.idx_mult_;
		idx_xor_ = other.idx_xor_;
	}
}

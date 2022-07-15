using System;

using System.Diagnostics;
using System.IO;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PRNG {
	class RandomNumberDevice {
		const int BlockSize = 32;
		const ulong MaskU64F64 = 0x1fffffffffffffu;
		readonly double MinValF64 = Math.Pow(2.0, -53.0);

		int amount_u8_;
		int amount_u64_;
		int amount_sha256_;
		List<byte> vec_state_u8_;
		// will be very similar to the java implementation!
		// byte* ptr_state_;
		// ulong* ptr_state_u64_;

		StateMachine sm_curr_;
		StateMachine sm_prev_;

		RandomNumberDevice(in int amount_u8, in List<byte> vec_seed) {
			Debug.Assert((amount_u8 % BlockSize) == 0);
			Debug.Assert(amount_u8 > BlockSize);

			amount_u8_ = amount_u8;
			amount_sha256_ = amount_u8 / BlockSize;
			amount_u64_ = amount_u8 / 8;
			vec_state_u8_ = new List<byte> ( new byte[amount_u8] );
			// ptr_state_ = &vec_state_u8_[0];
			// ptr_state_u64_ = (uint64_t*)ptr_state_;
			// std::fill(ptr_state_, ptr_state_ + amount_u8_, 0);

			sm_curr_ = new StateMachine(amount_u64_);
			sm_prev_ = new StateMachine(amount_u64_);

			int seed_len = vec_seed.Count;
			for (int i = 0; i < seed_len; ++i) {
				vec_state_u8_[i % amount_u8_] ^= vec_seed[i];
			}

			hash_once_loop(); hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_mult_x);
			hash_once_loop(); hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_mult_a);
			hash_once_loop(); hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_mult_b);
			hash_once_loop(); hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_xor_x);
			hash_once_loop(); hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_xor_a);
			hash_once_loop(); hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_xor_b);

			// correct the values for the values a and b for mult and xor
			for (int i = 0; i < amount_u64_; ++i) {
				// "%" operator is working, but "-" on ulong type is not working?!?
				// needed to write the "%" with an mask operator "and" "&" and the mask value.
				sm_curr_.vec_mult_a[i] = 1Lu + (sm_curr_.vec_mult_a[i] & 0xFFFFFFFFFFFFFFFC);
				sm_curr_.vec_mult_b[i] = 1Lu + (sm_curr_.vec_mult_b[i] & 0xFFFFFFFFFFFFFFFE);

				sm_curr_.vec_xor_a[i] = 0Lu + (sm_curr_.vec_xor_a[i] & 0xFFFFFFFFFFFFFFFE);
				sm_curr_.vec_xor_b[i] = 1Lu + (sm_curr_.vec_xor_b[i] & 0xFFFFFFFFFFFFFFFE);
			}

			save_current_state();
		}

		void hash_once_loop() {

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void hash_2_block(in int idx_0, in int idx_1) {

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool are_block_equal(in int idx_0, in int idx_1) {
			return false;
		}

		String convert_vec_u8_to_string(in List<byte> vec) {
			return "";
		}

		static String convert_vec_u64_to_string(in List<ulong> vec) {
			return "";
		}

		static String convert_vec_f64_to_string(in List<double> vec) {
			return "";
		}

		void print_current_state() {

		}

		void write_current_state_to_file(StreamWriter f) {

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		double get_next_double() {
			return 0.0;
		}

		List<ulong> generate_new_values_u64(in int amount) {
			return new List<ulong>();
		}

		List<double> generate_new_values_f64(in int amount) {
			return new List<double>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void copy_state_to_vec(List<ulong> vec) {
			Debug.Assert(vec.Count * 8 == vec_state_u8_.Count);
			for (int i = 0; i < vec.Count; ++i) {
				vec[i] = (
					(((ulong)vec_state_u8_[i*8+0]) << 56) +
					(((ulong)vec_state_u8_[i*8+1]) << 48) +
					(((ulong)vec_state_u8_[i*8+2]) << 40) +
					(((ulong)vec_state_u8_[i*8+3]) << 32) +
					(((ulong)vec_state_u8_[i*8+4]) << 24) +
					(((ulong)vec_state_u8_[i*8+5]) << 16) +
					(((ulong)vec_state_u8_[i*8+6]) << 8) +
					(((ulong)vec_state_u8_[i*8+7]) << 0)
				);
			}
		}

		void save_current_state() {
			sm_prev_.copy_sm(sm_curr_);
		}

		void restore_previous_state() {
			sm_curr_.copy_sm(sm_prev_);
		}
	}
}

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PRNG {
	struct StateMachine {
		public List<ulong> vec_mult_x;
		public List<ulong> vec_mult_a;
		public List<ulong> vec_mult_b;

		public List<ulong> vec_xor_x;
		public List<ulong> vec_xor_a;
		public List<ulong> vec_xor_b;

		public int amount_u64;
		public int idx_mult;
		public int idx_xor;

		public StateMachine(in int amount_vals) {
			vec_mult_x = new List<ulong> ( new ulong[amount_vals] );
			vec_mult_a = new List<ulong> ( new ulong[amount_vals] );
			vec_mult_b = new List<ulong> ( new ulong[amount_vals] );
			vec_xor_x = new List<ulong> ( new ulong[amount_vals] );
			vec_xor_a = new List<ulong> ( new ulong[amount_vals] );
			vec_xor_b = new List<ulong> ( new ulong[amount_vals] );

			amount_u64 = amount_vals;
			idx_mult = 0;
			idx_xor = 0;
		}

		public void copy_sm(StateMachine other) {
			for (int i = 0; i < amount_u64; ++i) {
				vec_mult_x[i] = other.vec_mult_x[i];
				vec_mult_a[i] = other.vec_mult_a[i];
				vec_mult_b[i] = other.vec_mult_b[i];
				vec_xor_x[i] = other.vec_xor_x[i];
				vec_xor_a[i] = other.vec_xor_a[i];
				vec_xor_b[i] = other.vec_xor_b[i];
			}

			idx_mult = other.idx_mult;
			idx_xor = other.idx_xor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ulong get_next_u64() {
			// this is not C++... why is "const" not working?
			ulong val_mult_new = ((vec_mult_a[idx_mult] * vec_mult_x[idx_mult]) + vec_mult_b[idx_mult]) ^ vec_xor_x[idx_xor];
			vec_mult_x[idx_mult] = val_mult_new;

			++idx_mult;
			if (idx_mult >= amount_u64) {
				idx_mult = 0;

				vec_xor_x[idx_xor] = (vec_xor_a[idx_xor] ^ vec_xor_x[idx_xor]) + vec_xor_b[idx_xor];

				++idx_xor;
				if (idx_xor >= amount_u64) {
					idx_xor = 0;
				}
			}

			return val_mult_new;
		}
	}
}

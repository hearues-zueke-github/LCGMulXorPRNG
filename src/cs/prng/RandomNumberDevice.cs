using System;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace PRNG {
	// will be very similar to the java implementation!
	class RandomNumberDevice {
		const int BlockSize = 32;

		int amount_u8_;
		int amount_u64_;
		int amount_sha256_;
		byte[] vec_state_u8_;

		StateMachine sm_curr_;
		StateMachine sm_prev_;

		public RandomNumberDevice(in int amount_u8, in byte[] vec_seed) {
			Debug.Assert((amount_u8 % BlockSize) == 0);
			Debug.Assert(amount_u8 > BlockSize);

			amount_u8_ = amount_u8;
			amount_sha256_ = amount_u8 / BlockSize;
			amount_u64_ = amount_u8 / 8;
			vec_state_u8_ = new byte[amount_u8];

			sm_curr_ = new StateMachine(amount_u64_);
			sm_prev_ = new StateMachine(amount_u64_);

			int seed_len = vec_seed.Length;
			for (int i = 0; i < seed_len; ++i) {
				vec_state_u8_[i % amount_u8_] ^= vec_seed[i];
			}

			hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_mult_x);
			hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_mult_a);
			hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_mult_b);
			hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_xor_x);
			hash_once_loop();
			copy_state_to_vec(sm_curr_.vec_xor_a);
			hash_once_loop();
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
			for (int i = 0; i < amount_sha256_ * 2; ++i) {
				int idx_block_0 = (i + 0) % (int)amount_sha256_;
				int idx_block_1 = (i + 1) % (int)amount_sha256_;

				int idx_0_0 = (int)BlockSize * idx_block_0;
				int idx_1_0 = (int)BlockSize * idx_block_1;

				byte[] block_0 = new byte[BlockSize];
				Array.Copy(vec_state_u8_, idx_0_0, block_0, 0, BlockSize);;
				byte[] block_1 = new byte[BlockSize];
				Array.Copy(vec_state_u8_, idx_1_0, block_1, 0, BlockSize);;

				if (block_0.SequenceEqual(block_1)) {
					for (int j = 0; j < BlockSize; ++j) {
						vec_state_u8_[idx_1_0 + j] ^= (byte)(j + 1);
						block_1[j] ^= (byte)(j + 1);
					}
				}

				SHA256 sha256Hash_0 = SHA256.Create();
				SHA256 sha256Hash_1 = SHA256.Create();

				byte[] hash_0 = sha256Hash_0.ComputeHash(block_0);
				byte[] hash_1 = sha256Hash_1.ComputeHash(block_1);

				for (int j = 0; j < BlockSize; ++j) {
					vec_state_u8_[idx_1_0 + j] ^= (byte)(hash_0[j] ^ hash_1[j] ^ block_0[j]);
				}
			}
		}

		String convert_vec_u8_to_string(in byte[] vec) {
			StringBuilder s = new StringBuilder();

	        if (vec.Length > 0) {
	            s.Append(String.Format("{0:X2}", vec[0]));
	            for (int i = 1; i < vec.Length; ++i) {
	                s.Append(String.Format(",{0:X2}", vec[i]));
	            }
	        }

	        return s.ToString();
		}

		public static String convert_vec_u64_to_string(ulong[] vec) {
			StringBuilder s = new StringBuilder();

	        if (vec.Length > 0) {
	            s.Append(String.Format("{0:X16}", vec[0]));
	            for (int i = 1; i < vec.Length; ++i) {
	                s.Append(String.Format(",{0:X16}", vec[i]));
	            }
	        }

	        return s.ToString();
		}

		public static String convert_vec_f64_to_string(double[] vec) {
			StringBuilder s = new StringBuilder();

	        if (vec.Length > 0) {
	            s.Append(String.Format("{0:0.00000000000000000}", vec[0]));
	            for (int i = 1; i < vec.Length; ++i) {
	                s.Append(String.Format(",{0:0.00000000000000000}", vec[i]));
	            }
	        }

	        return s.ToString();
		}

		public void print_current_state() {
			Console.Write("v_state_u8:" + convert_vec_u8_to_string(vec_state_u8_) + "\n");
			Console.Write("v_x_mult:" + convert_vec_u64_to_string(sm_curr_.vec_mult_x) + "\n");
			Console.Write("v_a_mult:" + convert_vec_u64_to_string(sm_curr_.vec_mult_a) + "\n");
			Console.Write("v_b_mult:" + convert_vec_u64_to_string(sm_curr_.vec_mult_b) + "\n");
			Console.Write("v_x_xor:" + convert_vec_u64_to_string(sm_curr_.vec_xor_x) + "\n");
			Console.Write("v_a_xor:" + convert_vec_u64_to_string(sm_curr_.vec_xor_a) + "\n");
			Console.Write("v_b_xor:" + convert_vec_u64_to_string(sm_curr_.vec_xor_b) + "\n");
			Console.Write("idx_values_mult:" + sm_curr_.idx_mult + "\n");
			Console.Write("idx_values_xor:" + sm_curr_.idx_xor + "\n");
		}

		public void write_current_state_to_file(StreamWriter f) {
			f.Write("v_state_u8:" + convert_vec_u8_to_string(vec_state_u8_) + "\n");
			f.Write("v_x_mult:" + convert_vec_u64_to_string(sm_curr_.vec_mult_x) + "\n");
			f.Write("v_a_mult:" + convert_vec_u64_to_string(sm_curr_.vec_mult_a) + "\n");
			f.Write("v_b_mult:" + convert_vec_u64_to_string(sm_curr_.vec_mult_b) + "\n");
			f.Write("v_x_xor:" + convert_vec_u64_to_string(sm_curr_.vec_xor_x) + "\n");
			f.Write("v_a_xor:" + convert_vec_u64_to_string(sm_curr_.vec_xor_a) + "\n");
			f.Write("v_b_xor:" + convert_vec_u64_to_string(sm_curr_.vec_xor_b) + "\n");
			f.Write("idx_values_mult:" + sm_curr_.idx_mult + "\n");
			f.Write("idx_values_xor:" + sm_curr_.idx_xor + "\n");
		}

		public ulong[] generate_new_values_u64(in int amount) {
			ulong[] vec = new ulong[amount];
			for (int i = 0; i < amount; ++i) {
				vec[i] = sm_curr_.get_next_u64();
			}
			return vec;
		}

		public double[] generate_new_values_f64(in int amount) {
			double[] vec = new double[amount];
			for (int i = 0; i < amount; ++i) {
				vec[i] = sm_curr_.get_next_double();
			}
			return vec;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void copy_state_to_vec(ulong[] vec) {
			Debug.Assert(vec.Length * 8 == vec_state_u8_.Length);
			for (int i = 0; i < vec.Length; ++i) {
				vec[i] = (
					(((ulong)vec_state_u8_[i*8+7]) << 56) +
					(((ulong)vec_state_u8_[i*8+6]) << 48) +
					(((ulong)vec_state_u8_[i*8+5]) << 40) +
					(((ulong)vec_state_u8_[i*8+4]) << 32) +
					(((ulong)vec_state_u8_[i*8+3]) << 24) +	
					(((ulong)vec_state_u8_[i*8+2]) << 16) +
					(((ulong)vec_state_u8_[i*8+1]) << 8) +
					(((ulong)vec_state_u8_[i*8+0]) << 0)
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

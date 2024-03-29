pub mod prng {
    use std::fs::File;
    use std::io::Write;

    use std::convert::TryInto;

    use sha2::{Sha256, Digest};

    include!("vec_own.rs");
    use vec_own::VecOwn;

    include!("utils_ptr.rs");
    use utils_ptr::{ptr_mut_at, val_ref_mut_at};

    include!("utils_vec_join.rs");

    const BLOCK_SIZE: usize = 32;

    #[derive(Default)]
    struct StateMachine {
        v_x_mult: VecOwn<u64>,
        v_a_mult: VecOwn<u64>,
        v_b_mult: VecOwn<u64>,
        v_x_xor: VecOwn<u64>,
        v_a_xor: VecOwn<u64>,
        v_b_xor: VecOwn<u64>,
        idx_values_mult: usize,
        idx_values_xor: usize,
    }

    impl StateMachine {
        fn new() -> Self {
            let v_x_mult = VecOwn::<u64>::new();
            let v_a_mult = VecOwn::<u64>::new();
            let v_b_mult = VecOwn::<u64>::new();
            let v_x_xor = VecOwn::<u64>::new();
            let v_a_xor = VecOwn::<u64>::new();
            let v_b_xor = VecOwn::<u64>::new();

            let idx_values_mult = 0;
            let idx_values_xor = 0;

            return Self {
                v_x_mult,
                v_a_mult,
                v_b_mult,
                v_x_xor,
                v_a_xor,
                v_b_xor,
                idx_values_mult,
                idx_values_xor,
            };
        }

        fn init_vals(&mut self, size: usize) {
            self.v_x_mult.resize(size, 0);
            self.v_a_mult.resize(size, 0);
            self.v_b_mult.resize(size, 0);
            self.v_x_xor.resize(size, 0);
            self.v_a_xor.resize(size, 0);
            self.v_b_xor.resize(size, 0);

            self.idx_values_mult = 0;
            self.idx_values_xor = 0;
        }

        fn copy_sm(&mut self, other: &Self) {
            self.v_x_mult = other.v_x_mult.clone();
            self.v_a_mult = other.v_a_mult.clone();
            self.v_b_mult = other.v_b_mult.clone();
            self.v_x_xor = other.v_x_xor.clone();
            self.v_a_xor = other.v_a_xor.clone();
            self.v_b_xor = other.v_b_xor.clone();
            self.idx_values_mult = other.idx_values_mult;
            self.idx_values_xor = other.idx_values_xor;
        }
    }

    #[derive(Default)]
    pub struct RandomNumberDevice {
        v_state_u8: VecOwn<u8>,
        length_u8: usize,
        length_u64: usize,
        amount_block: usize,
        arr_seed_u8: VecOwn<u8>,
        sm_prev: StateMachine,
        sm_curr: StateMachine,
        mask_u64_f64: u64,
        min_val_f64: f64,
    }

    enum SaveCurrStateToVec {
        XMult,
        AMult,
        BMult,
        XXor,
        AXor,
        BXor,
    }

    impl RandomNumberDevice {
        pub fn new(arr_seed_u8_: VecOwn<u8>, length_u8_: usize) -> Self {
            let v_state_u8: VecOwn<u8> = VecOwn::<u8>::new();
            assert!(length_u8_ % BLOCK_SIZE == 0, "length_u8 must be a multiple of BLOCK_SIZE!");
            let length_u8: usize = length_u8_;
            let length_u64: usize = length_u8 / 8;
            let amount_block: usize = length_u8_ / BLOCK_SIZE;
            let arr_seed_u8: VecOwn<u8> = arr_seed_u8_.clone();
            let sm_prev: StateMachine = StateMachine::new();
            let sm_curr: StateMachine = StateMachine::new();
            let mask_u64_f64: u64 = 0x1fffffffffffff;
            let min_val_f64: f64 = f64::powf(2.0f64, -53.0);

            let mut self_ = Self {
                v_state_u8,
                length_u8,
                length_u64,
                amount_block,
                arr_seed_u8,
                sm_prev,
                sm_curr,
                mask_u64_f64,
                min_val_f64,
            };

            RandomNumberDevice::init_state(&mut self_);

            return self_;
        }

        pub fn write_current_state_to_file(&self, file: &mut File) {
            write!(file, "v_state_u8:{}\n", utils_vec_join::vec_own_u8_hex_join_string(&self.v_state_u8, ",")).unwrap();
            write!(file, "v_x_mult:{}\n", utils_vec_join::vec_own_u64_hex_join_string(&self.sm_curr.v_x_mult, ",")).unwrap();
            write!(file, "v_a_mult:{}\n", utils_vec_join::vec_own_u64_hex_join_string(&self.sm_curr.v_a_mult, ",")).unwrap();
            write!(file, "v_b_mult:{}\n", utils_vec_join::vec_own_u64_hex_join_string(&self.sm_curr.v_b_mult, ",")).unwrap();
            write!(file, "v_x_xor:{}\n", utils_vec_join::vec_own_u64_hex_join_string(&self.sm_curr.v_x_xor, ",")).unwrap();
            write!(file, "v_a_xor:{}\n", utils_vec_join::vec_own_u64_hex_join_string(&self.sm_curr.v_a_xor, ",")).unwrap();
            write!(file, "v_b_xor:{}\n", utils_vec_join::vec_own_u64_hex_join_string(&self.sm_curr.v_b_xor, ",")).unwrap();
            write!(file, "idx_values_mult:{}\n", self.sm_curr.idx_values_mult).unwrap();
            write!(file, "idx_values_xor:{}\n", self.sm_curr.idx_values_xor).unwrap();
        }

        #[allow(dead_code)]
        pub fn print_current_state(&self) {
            print!("self.v_state_u8: {:02X}\n", self.v_state_u8);
            print!("self.sm_curr.v_x_mult: {:08X}\n", self.sm_curr.v_x_mult);
            print!("self.sm_curr.v_a_mult: {:08X}\n", self.sm_curr.v_a_mult);
            print!("self.sm_curr.v_b_mult: {:08X}\n", self.sm_curr.v_b_mult);
            print!("self.sm_curr.v_x_xor: {:08X}\n", self.sm_curr.v_x_xor);
            print!("self.sm_curr.v_a_xor: {:08X}\n", self.sm_curr.v_a_xor);
            print!("self.sm_curr.v_b_xor: {:08X}\n", self.sm_curr.v_b_xor);
            print!("idx_values_mult:{}\n", self.sm_curr.idx_values_mult);
            print!("idx_values_xor:{}\n", self.sm_curr.idx_values_xor);
        }

        fn init_state(&mut self) {
            self.v_state_u8.resize(self.length_u8, 0);
            self.v_state_u8.fill(0);

            let length: usize = self.arr_seed_u8.len();
            let mut i: usize = 0;
            while length - i > self.length_u8 {
                for j in 0..self.length_u8 {
                    self.v_state_u8[j] ^= self.arr_seed_u8[i+j];
                }
                i += self.length_u8;
            }

            if i == 0 {
                for j in 0..length {
                    self.v_state_u8[j] ^= self.arr_seed_u8[j];
                }
            } else if i % self.length_u8 != 0 {
                for j in 0..(i % self.length_u8) {
                    self.v_state_u8[j] ^= self.arr_seed_u8[i + j];
                }
            }

            self.sm_curr.init_vals(self.length_u64);
            self.sm_prev.init_vals(self.length_u64);

            self.next_hashing_state(); self.next_hashing_state();
            self.save_u8_state_to_vec(SaveCurrStateToVec::XMult);
            self.next_hashing_state(); self.next_hashing_state();
            self.save_u8_state_to_vec(SaveCurrStateToVec::AMult);
            self.next_hashing_state(); self.next_hashing_state();
            self.save_u8_state_to_vec(SaveCurrStateToVec::BMult);

            self.next_hashing_state(); self.next_hashing_state();
            self.save_u8_state_to_vec(SaveCurrStateToVec::XXor);
            self.next_hashing_state(); self.next_hashing_state();
            self.save_u8_state_to_vec(SaveCurrStateToVec::AXor);
            self.next_hashing_state(); self.next_hashing_state();
            self.save_u8_state_to_vec(SaveCurrStateToVec::BXor);

            for i in 0..self.length_u64 {
                self.sm_curr.v_a_mult[i] = 1u64 + self.sm_curr.v_a_mult[i] - (self.sm_curr.v_a_mult[i] % 4u64);
                self.sm_curr.v_b_mult[i] = 1u64 + self.sm_curr.v_b_mult[i] - (self.sm_curr.v_b_mult[i] % 2u64);

                self.sm_curr.v_a_xor[i] = 0u64 + self.sm_curr.v_a_xor[i] - (self.sm_curr.v_a_xor[i] % 2u64);
                self.sm_curr.v_b_xor[i] = 1u64 + self.sm_curr.v_b_xor[i] - (self.sm_curr.v_b_xor[i] % 2u64);
            }

            self.save_current_state();
        }

        pub fn save_current_state(&mut self) {
            self.sm_prev.copy_sm(&self.sm_curr);
        }

        #[allow(dead_code)]
        pub fn restore_previous_state(&mut self) {
            self.sm_curr.copy_sm(&self.sm_prev);
        }

        fn next_hashing_state(&mut self) {
            let ptr: *mut u8 = &mut self.v_state_u8[0];

            for i in 0..self.amount_block {         
                let idx_blk_0: usize = (i + 0) % self.amount_block;
                let idx_blk_1: usize = (i + 1) % self.amount_block;

                let idx_0_0: usize = BLOCK_SIZE * (idx_blk_0 + 0);
                let idx_1_0: usize = BLOCK_SIZE * (idx_blk_1 + 0);

                let ptr_0: *mut u8 = ptr_mut_at(ptr, idx_0_0.try_into().unwrap());
                let ptr_1: *mut u8 = ptr_mut_at(ptr, idx_1_0.try_into().unwrap());

                let mut is_all_equal: bool = true;

                for j in 0..BLOCK_SIZE {
                    if *val_ref_mut_at(ptr_0, j.try_into().unwrap()) != *val_ref_mut_at(ptr_1, j.try_into().unwrap()) {
                        is_all_equal = false;
                        break;
                    }
                }

                if is_all_equal {
                    let mut v = 0x01u8;
                    for j in 0..BLOCK_SIZE {
                        *val_ref_mut_at(ptr_1, j.try_into().unwrap()) ^= v;
                        v += 1;
                    }
                }

                let mut hasher_0: Sha256 = Sha256::new();
                let mut a_0: Vec<u8> = vec![0; BLOCK_SIZE];
                for j in 0..BLOCK_SIZE {
                    a_0[j] = *val_ref_mut_at(ptr_0, j.try_into().unwrap());
                }
                hasher_0.update(a_0);
                let result_0 = hasher_0.finalize();

                let mut hasher_1: Sha256 = Sha256::new();
                let mut a_1: Vec<u8> = vec![0; BLOCK_SIZE];
                for j in 0..BLOCK_SIZE {
                    a_1[j] = *val_ref_mut_at(ptr_1, j.try_into().unwrap());
                }
                hasher_1.update(a_1);
                let result_1 = hasher_1.finalize();

                for j in 0..BLOCK_SIZE {
                    *val_ref_mut_at(ptr_1, j.try_into().unwrap()) ^= {
                        result_0[j] ^ result_1[j] ^ *val_ref_mut_at(ptr_0, j.try_into().unwrap())
                    };
                }
            }
        }

        fn save_u8_state_to_vec(&mut self, state: SaveCurrStateToVec) {
            let v_vec: &mut VecOwn<u64> = match state {
                SaveCurrStateToVec::XMult => &mut self.sm_curr.v_x_mult,
                SaveCurrStateToVec::AMult => &mut self.sm_curr.v_a_mult,
                SaveCurrStateToVec::BMult => &mut self.sm_curr.v_b_mult,
                SaveCurrStateToVec::XXor => &mut self.sm_curr.v_x_xor,
                SaveCurrStateToVec::AXor => &mut self.sm_curr.v_a_xor,
                SaveCurrStateToVec::BXor => &mut self.sm_curr.v_b_xor,
            };

            let v_state_u8: &VecOwn<u8> = &self.v_state_u8;
            for i in 0..self.length_u64 {
                v_vec[i] = u64::from_le_bytes(v_state_u8[8*(i+0)..8*(i+1)].try_into().expect("incorrect length"));
            }
        }

        #[inline(always)]
        pub fn generate_next_u64(&mut self) -> u64 {
            let sm_curr: &mut StateMachine = &mut self.sm_curr;

            let v_x_mult: &mut VecOwn<u64> = &mut sm_curr.v_x_mult;
            let v_a_mult: &VecOwn<u64> = &sm_curr.v_a_mult;
            let v_b_mult: &VecOwn<u64> = &sm_curr.v_b_mult;
            let v_x_xor: &mut VecOwn<u64> = &mut sm_curr.v_x_xor;
            let v_a_xor: &VecOwn<u64> = &sm_curr.v_a_xor;
            let v_b_xor: &VecOwn<u64> = &sm_curr.v_b_xor;
            
            let idx_mult: &mut usize = &mut sm_curr.idx_values_mult;
            let idx_xor: &mut usize = &mut sm_curr.idx_values_xor;

            let val_mult_new: u64 = (((*v_a_mult)[*idx_mult].wrapping_mul((*v_x_mult)[*idx_mult])).wrapping_add((*v_b_mult)[*idx_mult])) ^ (*v_x_xor)[*idx_xor];
            (*v_x_mult)[*idx_mult] = val_mult_new;

            *idx_mult += 1;
            if *idx_mult >= self.length_u64 {
                *idx_mult = 0;

                (*v_x_xor)[*idx_xor] = ((*v_a_xor)[*idx_xor] ^ (*v_x_xor)[*idx_xor]).wrapping_add((*v_b_xor)[*idx_xor]);

                *idx_xor += 1;
                if *idx_xor >= self.length_u64 {
                    *idx_xor = 0;
                }
            }

            return val_mult_new;
        }

        pub fn generate_next_f64(&mut self) -> f64 {
            let val: u64 = self.generate_next_u64();
            let val_and: u64 = val & self.mask_u64_f64;
            let val_float: f64 = val_and as f64;
            let val_finish = self.min_val_f64 * val_float;

            return val_finish;
        }

        pub fn generate_new_values_u64(&mut self, v_vec: &mut VecOwn<u64>, amount: usize) {
            v_vec.resize(amount, 0);

            for i in 0..amount {
                v_vec[i] = self.generate_next_u64();
            }
        }

        pub fn generate_new_values_f64(&mut self, v_vec: &mut VecOwn<f64>, amount: usize) {
            v_vec.resize(amount, 0.);

            for i in 0..amount {
                v_vec[i] = self.generate_next_f64();
            }
        }
    }
}

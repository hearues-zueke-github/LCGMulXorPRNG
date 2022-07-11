package ovh.ziko;

import org.jetbrains.annotations.NotNull;

import java.io.FileWriter;
import java.io.IOException;
import java.math.BigInteger;
import java.nio.ByteBuffer;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.text.DecimalFormat;
import java.util.Arrays;
import java.util.stream.Stream;

public class RandomNumberDevice {
    final long BLOCK_SIZE = 32;
    final long MASK_U64_F64 = 0x1FFFFFFFFFFFFFl;
    final double MIN_VAL_DOUBLE = Math.pow(2., -53.);

    long amount_u8;
    long amount_u64;
    long amount_sha256;
    byte v_state_u8[];

    StateMachine sm_curr;
    StateMachine sm_prev;

    RandomNumberDevice(final long amount_u8, final byte vec_seed[]) throws NoSuchAlgorithmException {
        assert amount_u8 % BLOCK_SIZE == 0 : "Need to be a multiple of BLOCK_SIZE!";
        assert amount_u8 > BLOCK_SIZE : "amount_u8 must be bigger than BLOCK_SIZE!";

        this.amount_u8 = amount_u8;
        amount_sha256 = amount_u8 / BLOCK_SIZE;
        amount_u64 = amount_u8 / 8;
        v_state_u8 = new byte[(int)amount_u8];
        Arrays.fill(v_state_u8, (byte)0);

        sm_curr = new StateMachine(amount_u64);
        sm_prev = new StateMachine(amount_u64);

        final long seed_len = vec_seed.length;
        for (long i = 0; i < seed_len; ++i) {
            v_state_u8[(int)(i % amount_u8)] ^= vec_seed[(int)i];
        }

        hash_once_loop();
        copy_state_to_vec(sm_curr.v_mult_x);
        hash_once_loop();
        copy_state_to_vec(sm_curr.v_mult_a);
        hash_once_loop();
        copy_state_to_vec(sm_curr.v_mult_b);
        hash_once_loop();
        copy_state_to_vec(sm_curr.v_xor_x);
        hash_once_loop();
        copy_state_to_vec(sm_curr.v_xor_a);
        hash_once_loop();
        copy_state_to_vec(sm_curr.v_xor_b);

        // correct the values for the values a and b for mult and xor
        for (long i = 0; i < amount_u64; ++i) {
            // the mask 0x7FFFFFFFFFFFFFFFl is needed for the correct calculation of the modulo value!
            // the reason is, that java has no unsigned values!
            sm_curr.v_mult_a[(int)i] += 1l - ((sm_curr.v_mult_a[(int)i] & 0x7FFFFFFFFFFFFFFFl) % 4l);
            sm_curr.v_mult_b[(int)i] += 1l - ((sm_curr.v_mult_b[(int)i] & 0x7FFFFFFFFFFFFFFFl) % 2l);

            sm_curr.v_xor_a[(int)i] += - ((sm_curr.v_xor_a[(int)i] & 0x7FFFFFFFFFFFFFFFl) % 2l);
            sm_curr.v_xor_b[(int)i] += 1l - ((sm_curr.v_xor_b[(int)i] & 0x7FFFFFFFFFFFFFFFl) % 2l);
        }

        save_current_state();
    }

    void hash_once_loop() throws NoSuchAlgorithmException {
        for (int i = 0; i < amount_sha256 * 2; ++i) {
            final int idx_block_0 = (i + 0) % (int)amount_sha256;
            final int idx_block_1 = (i + 1) % (int)amount_sha256;

            final int idx_0_0 = (int)BLOCK_SIZE * idx_block_0;
            final int idx_0_1 = (int)BLOCK_SIZE * (idx_block_0 + 1);
            final int idx_1_0 = (int)BLOCK_SIZE * idx_block_1;
            final int idx_1_1 = (int)BLOCK_SIZE * (idx_block_1 + 1);

            byte[] block_0 = Arrays.copyOfRange(v_state_u8, idx_0_0, idx_0_1);
            byte[] block_1 = Arrays.copyOfRange(v_state_u8, idx_1_0, idx_1_1);
            if (Arrays.equals(block_0, block_1)) {
                for (int j = 0; j < BLOCK_SIZE; ++j) {
                    v_state_u8[idx_1_0 + j] ^= (byte)(j + 1);
                    block_1[j] ^= (byte)(j + 1);
                }
            }

            byte[] hash_0 = MessageDigest.getInstance("SHA-256").digest(block_0);
            byte[] hash_1 = MessageDigest.getInstance("SHA-256").digest(block_1);

            for (int j = 0; j < BLOCK_SIZE; ++j) {
                v_state_u8[idx_1_0 + j] ^= hash_0[j] ^ hash_1[j] ^ block_0[j];
            }
        }
    }

    void copy_state_to_vec(long vec[]) {
        for (long i = 0l; i < amount_u64; ++i) {
            final byte[] vec_part = Arrays.copyOfRange(v_state_u8, (int)i*8, (int)(i+1)*8);
            vec[(int)i] = Long.reverseBytes(new BigInteger(vec_part).longValue());
        }
    }

    void save_current_state() {
        sm_prev.copySm(sm_curr);
    }

    void restore_previous_state() {
        sm_curr.copySm(sm_prev);
    }

    String convert_vec_u8_to_string(final byte @NotNull [] vec) {
        StringBuilder s = new StringBuilder();

        if (vec.length > 0) {
            s.append(String.format("%02X", vec[0]));
            for (int i = 1; i < vec.length; ++i) {
                s.append(String.format(",%02X", vec[i]));
            }
        }

        return s.toString();
    }

    String convert_vec_u64_to_string(final long @NotNull [] vec) {
        StringBuilder s = new StringBuilder();

        if (vec.length > 0) {
            s.append(String.format("%016X", vec[0]));
            for (int i = 1; i < vec.length; ++i) {
                s.append(String.format(",%016X", vec[i]));
            }
        }

        return s.toString();
    }

    String convert_vec_f64_to_string(final double @NotNull [] vec) {
        StringBuilder s = new StringBuilder();

        if (vec.length > 0) {
            s.append(String.format("%.016f", vec[0]));
            for (int i = 1; i < vec.length; ++i) {
                s.append(String.format(",%.016f", vec[i]));
            }
        }

        return s.toString();
    }

    void print_current_state() {
        System.out.print("v_state_u8:" + convert_vec_u8_to_string(v_state_u8) + "\n");
        System.out.print("v_x_mult:" + convert_vec_u64_to_string(sm_curr.v_mult_x) + "\n");
        System.out.print("v_a_mult:" + convert_vec_u64_to_string(sm_curr.v_mult_a) + "\n");
        System.out.print("v_b_mult:" + convert_vec_u64_to_string(sm_curr.v_mult_b) + "\n");
        System.out.print("v_x_xor:" + convert_vec_u64_to_string(sm_curr.v_xor_x) + "\n");
        System.out.print("v_a_xor:" + convert_vec_u64_to_string(sm_curr.v_xor_a) + "\n");
        System.out.print("v_b_xor:" + convert_vec_u64_to_string(sm_curr.v_xor_b) + "\n");
        System.out.print("idx_values_mult:" + sm_curr.idx_mult + "\n");
        System.out.print("idx_values_xor:" + sm_curr.idx_xor + "\n");
    }

    void write_current_state_to_file(FileWriter file) throws IOException {
        file.write("v_state_u8:" + convert_vec_u8_to_string(v_state_u8) + "\n");
        file.write("v_x_mult:" + convert_vec_u64_to_string(sm_curr.v_mult_x) + "\n");
        file.write("v_a_mult:" + convert_vec_u64_to_string(sm_curr.v_mult_a) + "\n");
        file.write("v_b_mult:" + convert_vec_u64_to_string(sm_curr.v_mult_b) + "\n");
        file.write("v_x_xor:" + convert_vec_u64_to_string(sm_curr.v_xor_x) + "\n");
        file.write("v_a_xor:" + convert_vec_u64_to_string(sm_curr.v_xor_a) + "\n");
        file.write("v_b_xor:" + convert_vec_u64_to_string(sm_curr.v_xor_b) + "\n");
        file.write("idx_values_mult:" + sm_curr.idx_mult + "\n");
        file.write("idx_values_xor:" + sm_curr.idx_xor + "\n");
    }

    long[] generate_new_values_u64(final int amount) {
        long[] vec = new long[amount];
        for (int i = 0; i < amount; ++i) {
            vec[i] = sm_curr.getNextLong();
        }
        return vec;
    }

    double[] generate_new_values_f64(final int amount) {
        double[] vec = new double[amount];
        for (int i = 0; i < amount; ++i) {
            vec[i] = MIN_VAL_DOUBLE * (double)(sm_curr.getNextLong() & MASK_U64_F64);
        }
        return vec;
    }
}

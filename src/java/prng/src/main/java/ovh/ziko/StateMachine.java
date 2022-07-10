package ovh.ziko;

public class StateMachine {
    long[] v_mult_x;
    long[] v_mult_a;
    long[] v_mult_b;

    long[] v_xor_x;
    long[] v_xor_a;
    long[] v_xor_b;

    long idx_mult;
    long idx_xor;
    long amount_u64;

    StateMachine(final long amount_u64) {
        this.v_mult_x = new long[(int)amount_u64];
        this.v_mult_a = new long[(int)amount_u64];
        this.v_mult_b = new long[(int)amount_u64];

        this.v_xor_x = new long[(int)amount_u64];
        this.v_xor_a = new long[(int)amount_u64];
        this.v_xor_b = new long[(int)amount_u64];

        this.idx_mult = 0;
        this.idx_xor = 0;
        this.amount_u64 = amount_u64;
    }

    void copySm(final StateMachine other) {
        System.arraycopy(other.v_mult_x, 0, v_mult_x, 0, (int)amount_u64);
        System.arraycopy(other.v_mult_a, 0, v_mult_a, 0, (int)amount_u64);
        System.arraycopy(other.v_mult_b, 0, v_mult_b, 0, (int)amount_u64);

        System.arraycopy(other.v_xor_x, 0, v_xor_x, 0, (int)amount_u64);
        System.arraycopy(other.v_xor_a, 0, v_xor_a, 0, (int)amount_u64);
        System.arraycopy(other.v_xor_b, 0, v_xor_b, 0, (int)amount_u64);

        idx_mult = other.idx_mult;
        idx_xor = other.idx_xor;
    }

    long getNextLong() {
        final long val_mult_new = ((v_mult_a[(int)idx_mult] * v_mult_x[(int)idx_mult]) + v_mult_b[(int)idx_mult]) ^ v_xor_x[(int)idx_xor];
        v_mult_x[(int)idx_mult] = val_mult_new;

        ++idx_mult;
        if (idx_mult >= amount_u64) {
            idx_mult = 0;

            v_xor_x[(int)idx_xor] = (v_xor_a[(int)idx_xor] ^ v_xor_x[(int)idx_xor]) + v_xor_b[(int)idx_xor];

            ++idx_xor;
            if (idx_xor >= amount_u64) {
                idx_xor = 0;
            }
        }

        return val_mult_new;
    }
}

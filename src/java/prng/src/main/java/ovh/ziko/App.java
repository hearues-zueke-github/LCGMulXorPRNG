package ovh.ziko;

import java.io.FileWriter;
import java.io.IOException;
import java.security.NoSuchAlgorithmException;
import java.util.HashMap;

public class App {
    public static void main( String[] args ) throws NoSuchAlgorithmException, IOException {
        HashMap<String, String> map = new HashMap<>();

        for (String arg : args) {
            String[] arr = arg.split("=");
            assert arr.length == 2 : "Need to have length of 2!";
            final String key = arr[0];
            final String val = arr[1];
            if (map.containsKey(key)) {
                System.out.println("Key '" + key + "' was found again!");
                assert false;
            }
            map.put(key, val);
        }

        assert map.containsKey("file_path") : "Need to have a 'file_path' key.";
        assert map.containsKey("seed_u8") : "Need to have a 'seed_u8' key.";
        assert map.containsKey("length_u8") : "Need to have a 'length_u8' key.";
        assert map.containsKey("types_of_arr") : "Need to have a 'types_of_arr' key.";

        // TODO: open the file and write it into the file!
        final String file_path = map.get("file_path");
        final long length_u8 = Long.valueOf(map.get("length_u8"));
        byte seed_u8[];
        {
            final String[] arr = map.get("seed_u8").split(",");
            seed_u8 = new byte[arr.length];
            int i = 0;
            for (String v : arr) {
                seed_u8[i++] = (byte)Integer.parseInt(v, 16);
            }
        }

        class Type {
            String type;
            int amount;
            Type(final String type, final int amount) {
                this.type = type;
                this.amount = amount;
            }
        }
        Type types_of_arr[];
        {
            final String[] arr_1 = map.get("types_of_arr").split(",");
            types_of_arr = new Type[arr_1.length];
            int i = 0;
            for (String v : arr_1) {
                final String[] arr_2 = v.split(":");
                assert arr_2.length == 2 : "Needed length of 2!";
                types_of_arr[i++] = new Type(arr_2[0], Integer.parseInt(arr_2[1]));
            }
        }

        RandomNumberDevice rnd = new RandomNumberDevice(length_u8, seed_u8);

        FileWriter file = new FileWriter(file_path);

        rnd.write_current_state_to_file(file);

        for (Type type : types_of_arr) {
            switch (type.type) {
                case "u64": {
                        long[] vec = rnd.generate_new_values_u64(type.amount);
                        file.write("v_vec_u64:" + rnd.convert_vec_u64_to_string(vec) + "\n");
                        rnd.write_current_state_to_file(file);
                    }
                    break;
                case "f64": {
                        double[] vec = rnd.generate_new_values_f64(type.amount);
                        file.write("v_vec_f64:" + rnd.convert_vec_f64_to_string(vec) + "\n");
                        rnd.write_current_state_to_file(file);
                    }
                    break;
                case default:
                    assert false : "Should never happen!";
            }
        }

        file.close();
    }
}

use std::env;

use std::fs::File;
use std::io::Write;
use std::collections::HashMap;

include!("vec_own.rs");
include!("prng.rs");

use vec_own::VecOwn;
use prng::RandomNumberDevice;

include!("utils_vec_join.rs");

#[derive(PartialEq)]
enum VecType {
    U64,
    F64,
    NONE
}

struct VecTypeLenght {
    vec_type: VecType,
    length: usize,
}

fn main() {
    let args: Vec<String> = env::args().collect();

    assert!(args.len() >= 5);

    let mut keyargs: HashMap<String, String> = HashMap::<String, String>::new();
    for s in &args[1..args.len()] {
        let v_temp: Vec<&str> = s.split("=").collect::<Vec<&str>>();
        assert!(v_temp.len() == 2, "Must be length 2!");

        keyargs.insert(v_temp[0].to_string(), v_temp[1].to_string());
    }

    assert!(keyargs.contains_key("file_path"));
    assert!(keyargs.contains_key("seed_u8"));
    assert!(keyargs.contains_key("length_u8"));
    assert!(keyargs.contains_key("types_of_arr"));

    let file_path: &String = &keyargs.get("file_path").unwrap();
    let v_seed_u8: Vec<u8> = {
        keyargs.get("seed_u8").unwrap()
        .split(",")
        .map(|x| u8::from_str_radix(x, 16).unwrap())
        .collect::<Vec<u8>>()
    };
    let length_u8: usize = usize::from_str_radix(&keyargs.get("length_u8").unwrap(), 10).unwrap();
    let types_of_arr: &str = &keyargs.get("types_of_arr").unwrap();

    let mut v_vec_type_lenght: Vec<VecTypeLenght> = Vec::<VecTypeLenght>::new();

    for s in types_of_arr.split(",").collect::<Vec<&str>>().iter() {
        let v_temp: Vec<&str> = s.split(":").collect::<Vec<&str>>();
        assert!(v_temp.len() == 2, "Length should be 2!");

        let vec_type: VecType = match v_temp[0] {
            "u64" => VecType::U64,
            "f64" => VecType::F64,
            _ => VecType::NONE,
        };
        assert!(vec_type != VecType::NONE, "Type is not defined!");

        let length: usize = usize::from_str_radix(v_temp[1], 10).unwrap();
        
        v_vec_type_lenght.push(VecTypeLenght { vec_type, length });
    }

    let mut file: File = File::create(file_path).unwrap();

    let arr_seed_u8: VecOwn<u8> = VecOwn::<u8>::new_from_arr(&v_seed_u8);
    let mut rnd = RandomNumberDevice::new(arr_seed_u8, length_u8);

    rnd.write_current_state_to_file(&mut file);

    for vec_type_lenght in v_vec_type_lenght {
        match vec_type_lenght.vec_type {
            VecType::U64 => {
                let mut v_vec: VecOwn<u64> = VecOwn::<u64>::new();
                rnd.generate_new_values_u64(&mut v_vec, vec_type_lenght.length);
                write!(file, "v_vec_u64:{}\n", utils_vec_join::vec_own_u64_hex_join_string(&v_vec, ",")).unwrap();
            },
            VecType::F64 => {
                let mut v_vec: VecOwn<f64> = VecOwn::<f64>::new();
                rnd.generate_new_values_f64(&mut v_vec, vec_type_lenght.length);
                write!(file, "v_vec_f64:{}\n", utils_vec_join::vec_own_f64_join_string(&v_vec, ",")).unwrap();
            },
            VecType::NONE => todo!(),
        }

        rnd.write_current_state_to_file(&mut file);
    }
}

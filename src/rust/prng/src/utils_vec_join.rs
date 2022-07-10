mod utils_vec_join {
	#[allow(dead_code)]
	pub fn vec_own_u8_hex_join_string(v_vec: &Vec<u8>, join_str: &str) -> String {
        v_vec
        .iter()
        .map(|&x| format!("{:02X}", x))
        .collect::<Vec<String>>()
        .join(join_str)
    }
    
    #[allow(dead_code)]
    pub fn vec_own_u64_hex_join_string(v_vec: &Vec<u64>, join_str: &str) -> String {
        v_vec
        .iter()
        .map(|&x| format!("{:016X}", x))
        .collect::<Vec<String>>()
        .join(join_str)
    }

    #[allow(dead_code)]
    pub fn vec_own_f64_join_string(v_vec: &Vec<f64>, join_str: &str) -> String {
        v_vec
        .iter()
        .map(|&x| format!("{:.16}", x))
        .collect::<Vec<String>>()
        .join(join_str)
    }
}

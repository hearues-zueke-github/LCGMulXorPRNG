#! /usr/bin/python3.10

import sys

import numpy as np

from prng import RandomNumberDevice

if __name__ == '__main__':
	argv = sys.argv

	d_keyargs = {}
	for arg in argv[1:]:
		l_arg = arg.split("=")
		assert len(l_arg)
		d_keyargs[l_arg[0]] = l_arg[1]

	assert "file_path" in d_keyargs
	assert "seed_u8" in d_keyargs
	assert "length_u8" in d_keyargs
	assert "types_of_arr" in d_keyargs

	file_path = d_keyargs["file_path"]
	seed_u8 = np.array([int(v, 16) for v in d_keyargs["seed_u8"].split(",")], dtype=np.uint8)
	length_u8 = int(d_keyargs["length_u8"])
	types_of_arr = [(lambda x: (x[0], int(x[1], 10)))(v.split(":")) for v in d_keyargs["types_of_arr"].split(",")]

	rnd = RandomNumberDevice(
		seed_u8=seed_u8,
		length_u8=length_u8,
	)

	with open(file_path, "w") as f:
		f.write(rnd.get_current_vals_as_string())

		for type_name, amount in types_of_arr:
			if type_name == "u64":
				arr = rnd.calc_next_uint64(amount=amount)
				val = ','.join(['{:016X}'.format(v) for v in arr])
				f.write("v_vec_u64:{}\n".format(val))
			elif type_name == "f64":
				arr = rnd.calc_next_float64(amount=amount)
				val = ','.join(['{:.016f}'.format(v) for v in arr])
				f.write("v_vec_f64:{}\n".format(val))
			else:
				assert False
			
			f.write(rnd.get_current_vals_as_string())

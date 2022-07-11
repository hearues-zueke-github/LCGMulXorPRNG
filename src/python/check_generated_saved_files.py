#! /usr/bin/python3.10

import os
import sys

import numpy as np

from collections import defaultdict
from decimal import Decimal, getcontext
from itertools import combinations

getcontext().prec = 20

if __name__ == '__main__':
	argv = sys.argv

	d_keyargs = {}
	for arg in argv[1:]:
		l_arg = arg.split("=")
		assert len(l_arg)
		d_keyargs[l_arg[0]] = l_arg[1]

	assert "dir_path" in d_keyargs

	dir_path = d_keyargs["dir_path"]

	print(f"dir_path: {dir_path}")
	assert os.path.exists(dir_path)

	l = []
	for root_path, l_dir_name, l_file_name in os.walk(dir_path):
		l.append((root_path, l_dir_name, l_file_name))
	assert len(l) == 1

	root_path, _, l_file_name = l[0]
	l_file_path_nr_prg_name = [(f"{os.path.join(root_path, file_name)}", ) + tuple(file_name.split(".")[0].split("_")[2:]) for file_name in l_file_name]

	print(f"l_file_path_nr_prg_name: {l_file_path_nr_prg_name}")

	d_nr_to_d_name_to_content = defaultdict(dict)
	for file_path, nr, prg_name in l_file_path_nr_prg_name:
		with open(file_path, "r") as f:
			content = f.read().rstrip("\n")
		l_line = content.split("\n")

		l_line_split = []
		for line in l_line:
			assert ":" in line
			assert line.count(":") == 1
			key, val = line.split(":")

			if key in ['idx_values_mult', 'idx_values_xor']:
				val_split = val
			elif key in ['v_x_mult', 'v_a_mult', 'v_b_mult', 'v_x_xor', 'v_a_xor', 'v_b_xor']:
				val_split = val.split(',')
			elif key in ['v_state_u8']:
				val_split = val.split(',')
			elif key in ['v_vec_f64']:
				val_split = [Decimal(v) for v in val.split(',')]
			elif key in ['v_vec_u64']:
				val_split = val.split(',')
			else:
				print(f"key: '{key}' was not defined!")
				assert False

			l_line_split.append((key, val_split))

		d_nr_to_d_name_to_content[int(nr)][prg_name] = l_line_split

	for nr, d in sorted(d_nr_to_d_name_to_content.items()):
		print(f"nr: {nr}")
		for key1, key2 in combinations(d.keys(), 2):
			l_1 = d[key1]
			l_2 = d[key2]
			l_check_key = [key_1 == key_2 for (key_1, _), (key_2, _) in zip(l_1, l_2)]
			assert all(l_check_key)
			min_diff_decimal = Decimal("1E-16")
			arr_not_same_line = np.where([(t_1 != t_2) if t_1[0] != 'v_vec_f64' else not all([abs(v_1 - v_2) <= min_diff_decimal for v_1, v_2 in zip(t_1[1], t_2[1])]) for t_1, t_2 in zip(l_1, l_2)])[0]
			print(f"- key1: {key1}, key2: {key2}, arr_not_same_line: {arr_not_same_line}")
			assert len(arr_not_same_line) == 0

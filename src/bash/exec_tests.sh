#! /bin/bash

# create a temp dir for the files
DIR_TEMP=$(mktemp -d)
echo "DIR_TEMP=$DIR_TEMP"

commands=("python3" "cargo" "make" "g++")

for v in "${commands[@]}"; do
	echo "Check for command '$v'"
	if ! command -v $v &> /dev/null
	then
	    echo "Command '$v' could not be found"
	    exit
	fi
done

set -e

DIR_PATH=$(cd $(dirname "${BASH_SOURCE:-$0}") && pwd)
path=$DIR_PATH/$(basename "${BASH_SOURCE:-$0}")

echo "The directory path is '$DIR_PATH'"
echo "The absolute path is '$path'"

dir_src_rust="$DIR_PATH/../rust/prng"
path_prog_rust="$dir_src_rust/target/release/prng"
echo "path_prog_rust: $path_prog_rust"

dir_src_cpp="$DIR_PATH/../cpp"
dir_src_cpp_build="$dir_src_cpp/build"
path_prog_cpp="$dir_src_cpp_build/PRNGinCPP"

dir_src_python="$DIR_PATH/../python"
path_prog_python="python3 $dir_src_python/prng.py"

# compile rust first
mkdir -p $dir_src_cpp_build
cd $dir_src_cpp_build
make

cd $dir_src_rust
cargo build --release

# general template for replacing the variables
args_format="file_path=<file_path> seed_u8=<seed_u8> length_u8=<length_u8> types_of_arr=<types_of_arr>"
file_path_format="$DIR_TEMP/test_file_<arg_nr>_<lang_name>.txt"

declare -A d_tbl_arg

# TODO: move the const part of the map into two different files
max_nr_arg=0

d_tbl_arg["$max_nr_arg,seed_u8"]="00,01,02,03,04"
d_tbl_arg["$max_nr_arg,length_u8"]="128"
d_tbl_arg["$max_nr_arg,types_of_arr"]="u64:10,u64:1,f64:5,u64:10,u64:1,f64:5,u64:10,u64:1,f64:5,u64:10,u64:1,f64:5"
max_nr_arg=$((max_nr_arg+1))

d_tbl_arg["$max_nr_arg,seed_u8"]="00,01,02,03,04"
d_tbl_arg["$max_nr_arg,length_u8"]="192"
d_tbl_arg["$max_nr_arg,types_of_arr"]="u64:11,u64:14,f64:2,u64:19,f64:15"
max_nr_arg=$((max_nr_arg+1))

d_tbl_arg["$max_nr_arg,seed_u8"]="00"
d_tbl_arg["$max_nr_arg,length_u8"]="128"
d_tbl_arg["$max_nr_arg,types_of_arr"]="u64:0,u64:1,u64:2,u64:3,u64:4,u64:5"
max_nr_arg=$((max_nr_arg+1))

d_tbl_arg["$max_nr_arg,seed_u8"]="FF,01,A0,50,00"
d_tbl_arg["$max_nr_arg,length_u8"]="1024"
d_tbl_arg["$max_nr_arg,types_of_arr"]="u64:10,u64:20,u64:31,u64:50,u64:31,u64:123"
max_nr_arg=$((max_nr_arg+1))

declare -A d_tbl_lang

max_nr_lang=0

d_tbl_lang["$max_nr_lang,lang_exe"]=$path_prog_rust
d_tbl_lang["$max_nr_lang,lang_name"]="rust"
max_nr_lang=$((max_nr_lang+1))

d_tbl_lang["$max_nr_lang,lang_exe"]=$path_prog_cpp
d_tbl_lang["$max_nr_lang,lang_name"]="cpp"
max_nr_lang=$((max_nr_lang+1))

d_tbl_lang["$max_nr_lang,lang_exe"]=$path_prog_python
d_tbl_lang["$max_nr_lang,lang_name"]="python3"
max_nr_lang=$((max_nr_lang+1))

# lang_name=rust
arg_nr=0

langs=("rust" "python")

for arg_nr in $(seq 0 $((max_nr_arg-1))); do
	args_prep=$args_format
	args_prep="${args_prep//<seed_u8>/"${d_tbl_arg["$arg_nr,seed_u8"]}"}"
	args_prep="${args_prep//<length_u8>/"${d_tbl_arg["$arg_nr,length_u8"]}"}"
	args_prep="${args_prep//<types_of_arr>/"${d_tbl_arg["$arg_nr,types_of_arr"]}"}"
	echo "- arg_nr='$arg_nr'"

	for lang_nr in $(seq 0 $((max_nr_lang-1))); do
		lang_exe=${d_tbl_lang["$lang_nr,lang_exe"]}
		lang_name=${d_tbl_lang["$lang_nr,lang_name"]}
		echo "-- lang_nr='$lang_nr', lang_name='$lang_name'"
		
		file_path=$file_path_format
		file_path="${file_path//<lang_name>/"$lang_name"}"
		file_path="${file_path//<arg_nr>/"$arg_nr"}"
		echo "--- file_path='$file_path'"

		args=$args_prep
		args="${args//<file_path>/"$file_path"}"
		echo "--- args='$args'"

		$lang_exe $args
	done

done

cd $DIR_TEMP
echo "sha256sum *"
sha256sum *

# exit

# delete the temp folder again
rm -rf $DIR_TEMP


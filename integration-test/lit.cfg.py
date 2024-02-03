import lit.formats
import shutil
import os
import subprocess

def _clean_test_directory(directory):
    for entry in os.scandir(directory):
        basename = os.path.basename(entry.path)
        if basename == "lit.cfg.py":
            continue
        if entry.is_dir():
            shutil.rmtree(entry.path)
        else:
            os.remove(entry.path)

def _get_llvm_bindir():
    try:
        bindir_output = subprocess.check_output(['llvm-config', '--bindir'], universal_newlines=True)
        bindir_path = bindir_output.strip()
        
        if os.path.exists(bindir_path):
            return bindir_path
        else:
            raise FileNotFoundError(f"LLVM bindir does not exist: {bindir_path}")

    except subprocess.CalledProcessError as e:
        raise RuntimeError(f"Error running llvm-config: {e}")
    except Exception as e:
        raise RuntimeError(f"An unexpected error occurred: {e}")

config.name = "Test suite"
config.test_format = lit.formats.ShTest(True)

config.suffixes = ['.cs', '.test', '.il']

config.my_src_root = os.environ["NET_SSA_SRC_DIR"]

config.net_ssa_bin_dir = os.path.join(config.my_src_root, "net-ssa-cli", "bin", "Debug", "net8.0")
config.souffle_bin_dir = os.path.join(config.my_src_root, "souffle", "bin", "linux-x86-64")

config.llvm_bin_dir = _get_llvm_bindir()

config.mono_bin = '/usr/bin/mono'
assert os.path.exists(config.mono_bin)
config.mcs_bin = '/usr/bin/mcs'
assert os.path.exists(config.mcs_bin)
config.ilasm_bin = '/usr/bin/ilasm'
assert os.path.exists(config.ilasm_bin)

config.test_source_root = os.path.dirname(__file__)
config.test_build_root = os.path.join(config.test_source_root, "output")
if not os.path.exists(config.test_build_root):
    os.mkdir(config.test_build_root)

config.substitutions.append(('%mono', config.mono_bin))
config.substitutions.append(('%mcs', config.mcs_bin))
config.substitutions.append(('%ilasm', config.ilasm_bin))
config.substitutions.append(('%ssa-query', os.path.join(config.souffle_bin_dir, "ssa-query")))
config.substitutions.append(('%net-ssa-cli', os.path.join(config.net_ssa_bin_dir, "net-ssa-cli")))
config.substitutions.append(('%FileCheck', os.path.join(config.llvm_bin_dir, "FileCheck")))
config.substitutions.append(('%test-resources-dir', os.path.join(config.my_src_root, "test-resources")))

env_vars = {'DOTNET_ROOT'}
for e in env_vars:
    if e in os.environ:
        config.environment[e] = os.environ[e]

_clean_test_directory(config.test_build_root)


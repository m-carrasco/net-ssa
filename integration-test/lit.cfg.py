import lit.formats

import shutil
import os

config.name = "Test suite"
config.test_format = lit.formats.ShTest(True)

config.suffixes = ['.cs', '.test']

config.test_source_root = os.path.dirname(__file__)
config.test_build_root = os.path.join(config.my_obj_root, 'integration-test')

def _clean_test_directory(directory):
    for entry in os.scandir(directory):
        basename = os.path.basename(entry.path)
        if basename == "lit.site.cfg.py":
            continue
        if entry.is_dir():
            shutil.rmtree(entry.path)
        else:
            os.remove(entry.path)

_clean_test_directory(config.test_build_root)


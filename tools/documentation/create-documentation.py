#TODO:
#   - [X] Fix Parser in anyof TOKEN + name
#   - [X] Use help.h from redis source

import requests
import tarfile
import os
import shutil
import subprocess

simple_commands_filename = "simple_commands.json"
compressed_filename = 'source.tar.gz'
download_url = "https://download.redis.io/redis-stable.tar.gz"
source_directory = './source'
help_file = os.path.join(source_directory, 'redis-stable', 'src', 'help.h')
simple_commands = []



if __name__ == '__main__':
    #This works, no need to redownload multiple times
    r = requests.get(download_url, allow_redirects=True)
    cf =  open(compressed_filename, 'wb')
    cf.write(r.content)
    cf.close()    
    
    ftar = tarfile.open(compressed_filename)
    ftar.extractall(source_directory)
    ftar.close()

    shutil.copy(help_file, "./help.h")
    subprocess.run(["gcc", "help.c"])
    subprocess.run("./a.out")

    os.remove("help.h")
    os.remove("./a.out")
    os.remove("./source.tar.gz")
    shutil.rmtree('./source')








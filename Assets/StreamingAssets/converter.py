#!/usr/bin/python

import json
import os
import os.path
import subprocess
import sys
from pathlib import Path
import argparse

parser = argparse.ArgumentParser(description='Doing things..')
parser.add_argument('--input', default='.', help='input directory')
parser.add_argument('--output', default='.', help='output directory')

args = parser.parse_args()

inputDir = args.input
outputDir = args.output
pathlist = Path(inputDir).glob('./*.*')
videos = []

print(outputDir)
print(inputDir)

for path in pathlist:
     # because path is object not string
     path_in_str = str(path)

     if (path_in_str.endswith(".json")):
         continue

     if (path_in_str.endswith(".py")):
         continue

     if (path_in_str.endswith(".meta")):
         continue

     filename = os.path.basename(path_in_str)
     (basename, ext) = os.path.splitext(filename)
     outname = f"{basename}.webm"
     tmpname = f"{basename}_tmp.webm"
     i = path_in_str
     o = f"{outputDir}/{outname}"
     t = f"{outputDir}/{tmpname}"
     my_file = Path(o)
     if not my_file.is_file():
        print(basename)
        infoString = subprocess.run(["ffprobe","-v","quiet","-print_format","json","-show_format","-show_streams",i], capture_output=True)
        info = json.loads(infoString.stdout)
        print(info)
        if "streams" in info and "width" in info["streams"][0] and int(info["streams"][0]["width"]) > 1920:
            subprocess.run(["ffmpeg","-i",f"{i}","-vf","scale=1920:-1","-c:v","libvpx","-b:v","500K","-c:a","libvorbis",f"{t}"])
        else:
            subprocess.run(["ffmpeg","-i",f"{i}","-c:v","libvpx","-b:v","500K","-c:a","libvorbis",f"{t}"])
        subprocess.run(["mv",f"{t}",f"{o}"])
        subprocess.run(["rm",f"{i}"])
     videos.append(outname)

data = {
    'distanceThreshold': 8.0,
    'videos':videos
}

with open(f"{outputDir}/settings.json", 'w') as outfile:
    json.dump(data, outfile)

#!/usr/bin/python

import json
import os
import os.path
import subprocess
import sys
from pathlib import Path
import argparse
from multiprocessing import Pool

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

def isPossiblyVideo(path):
    if (path.endswith(".json")):
        return False
    if (path.endswith(".py")):
        return False
    return True

def convertVideo(path):
    filename = os.path.basename(path)
    (basename, ext) = os.path.splitext(filename)
    basename = basename.replace(" ","_")
    outname = f"{basename}.webm"
    tmpname = f"{basename}_tmp.webm"
    i = path
    o = f"{outputDir}/{outname}"
    t = f"{outputDir}/{tmpname}"
    my_file = Path(o)
    if not my_file.is_file():
        print(basename)
        subprocess.run(["ffmpeg","-i",f"{i}","-c:v","libvpx","-b:v","500K","-c:a","libvorbis",f"{t}"])
        subprocess.run(["mv",f"{t}",f"{o}"])
        subprocess.run(["rm",f"{i}"])
    
toBeConverted = []
for path_o in pathlist:
    # because path is object not string
    path = str(path_o)
    if not isPossiblyVideo(path):
        continue
    toBeConverted.append(path)

with Pool(7) as p:
    p.map(convertVideo, toBeConverted)

outputVideos = Path(outputDir).glob('./*.webm')
for path_o in outputVideos:
    path = str(path_o)
    filename = os.path.basename(path)
    (basename, ext) = os.path.splitext(filename)
    outname = f"{basename}.webm"
    videos.append(outname)

data = {
    'distanceThreshold': 8.0,
    'videos':videos
}

with open(f"{outputDir}/settings.json", 'w') as outfile:
    json.dump(data, outfile)

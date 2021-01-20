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

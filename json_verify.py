import os
import re
import glob

# Define a regex pattern for trailing commas
pattern = re.compile(r',\s*}')

found = 0
files = 0

# Walk through the directory tree
for filename in glob.iglob('**/*.json', recursive=True):
    print(f"Processing {filename}")
    files += 1
    with open(filename, 'r') as f:
        content = f.read()
        if matches := pattern.findall(content):
            print(f"Trailing comma found in file {filename}")
            found += 1

print(f"Found {found} / {files}")


import os
import json

def lint_json(file_path):
    try:
        with open(file_path, 'r') as f:
            json.load(f)
        print(f'No errors in: {file_path}')
    except json.JSONDecodeError as e:
        print(f'Error in: {file_path}')
        print(f'Error message: {e}')

def walk_directory(dir_path):
    for root, dirs, files in os.walk(dir_path):
        for file in files:
            if file.endswith('.json'):
                lint_json(os.path.join(root, file))

walk_directory('.')

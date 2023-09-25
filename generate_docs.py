import os
import re
from tabulate import tabulate

regex_module_attr = re.compile(r'\[Module\("(.*?)", "(.*?)"\)\]')
regex_cmd_attr = re.compile(r'CommandCallback\("(.*?)"(, Description = "(.*?)")?(, AllowedRoles = (.*?))?\)\]')

def parse_method(code:str):
    access_modifier = re.search(r'(\w+)\s+void', code).group(1)
    method_name = re.search(r'void\s+(\w+)', code).group(1)
    parameters = re.findall(r'\((.*?)\)', code)[0]
    parameters = [param.strip() for param in parameters.split(',')]
    default_values = re.findall(r'(\w+)\s*=\s*(\w+)', code)
    default_values = {param: value for param, value in default_values}
    return (method_name, parameters, default_values)

def find_cs_files(path):
    for root, dirs, files in os.walk(path):
        for file in files:
            if file.endswith('.cs'):
                yield os.path.join(root, file)

def extract_attributes(file_path):
    with open(file_path, 'r') as file:
        content = file.read()
        modules = regex_module_attr.findall(content)
        commands = []
        lines = content.split('\n')
        for i, line in enumerate(lines):
            line = line.strip()
            if line.startswith("[CommandCallback(") or line.startswith("[Commands.CommandCallback("):
                cmd_attr = regex_cmd_attr.findall(line)
                commands.append((cmd_attr, parse_method(lines[i+1])))
        return modules, commands

def write_to_md(file_path, modules, commands):
    file_str = ""
    with open(f'{file_path}.md', 'w') as file:
        file_str+=f'# {len(modules)} Modules in {os.path.basename(file_path)}\n\n'
        headers = ["Description", "Version"]
        file_str+=tabulate(modules, headers=headers, tablefmt="pipe")
        if len(commands) > 0:
            file_str+='\n\n## Commands\n'
            rows = []
            for cmd in commands:
                row = []
                if len(cmd[0][0])>0: row.append(cmd[0][0][0])
                else: row.append("")
                if len(cmd[1])>0: row.append(cmd[1][0])
                else: row.append("")
                if len(cmd[0][0])>2: row.append(cmd[0][0][2])
                else: row.append("")
                if len(cmd[0][0])>3: row.append(cmd[0][0][4].replace("Roles.","").replace(' | ',', '))
                else: row.append("")
                if len(cmd[1])>1: row.append(cmd[1][1])
                else: row.append("")
                if len(cmd[1])>2: row.append(cmd[1][2])
                else: row.append("")
                rows.append(row)
            headers = ["Command", "Function Name", "Description", "Allowed Roles", "Parameters", "Defaults"]
            file_str+=tabulate(rows, headers=headers, tablefmt="pipe")
            file.write(file_str)    
    return file_str


def main():
    total = ""
    path = '.'  # current directory
    for cs_file in find_cs_files(path):
        if not os.path.exists(cs_file):
            print(f"{cs_file} does not exist")
            continue
        print(cs_file)
        module, commands = extract_attributes(cs_file)
        total+="\n"+write_to_md(cs_file, module, commands)
    with open(f'modules.md', 'w') as file:
        file.write(total)

if __name__ == "__main__":
    main()

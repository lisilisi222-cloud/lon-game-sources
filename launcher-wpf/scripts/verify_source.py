"""Portable static checks for the L0N Windows source tree (not a Windows build)."""
from pathlib import Path
from lxml import etree
import re
import sys

base = Path(__file__).resolve().parents[1]
launcher = base / 'L0N.Launcher'
app = launcher / 'App.xaml'
window = launcher / 'MainWindow.xaml'
code_file = launcher / 'MainWindow.xaml.cs'
code = code_file.read_text(encoding='utf-8')
NAMESPACES = {'x': 'http://schemas.microsoft.com/winfx/2006/xaml'}

for file in (app, window):
    etree.parse(str(file))
    print('PASS XML:', file.name)

xml = etree.parse(str(window))
refs = xml.xpath('//*[@x:Name]', namespaces=NAMESPACES)
names = [node.get('{http://schemas.microsoft.com/winfx/2006/xaml}Name') for node in refs]
names += [node.get('Name') for node in xml.xpath('//*[@Name]')]
if len(names) != len(set(names)):
    raise SystemExit('ERROR duplicate x:Name / Name in MainWindow.xaml')
for name in names:
    if not re.search(r'\b' + re.escape(name) + r'\b', code):
        raise SystemExit('ERROR code-behind does not mention named WPF control: ' + name)
print('PASS named control bindings:', len(names))

handlers = set()
for el in xml.iter():
    for attr, value in el.attrib.items():
        if attr in ('Click', 'Loaded', 'TextChanged', 'SelectionChanged'):
            handlers.add(value)
for handler in sorted(handlers):
    if not re.search(r'\b' + re.escape(handler) + r'\s*\(', code):
        raise SystemExit('ERROR missing event handler: ' + handler)
print('PASS WPF event handler references:', len(handlers))

for file in (launcher/'MainWindow.xaml.cs', launcher/'DownloadManager.cs'):
    script = file.read_text(encoding='utf-8')
    for forbidden in ('msedge.exe', '127.0.0.1', 'powershell.exe', 'cmd.exe'):
        if forbidden.lower() in script.lower():
            raise SystemExit(f'ERROR forbidden browser/command launcher: {file.name} {forbidden}')
print('PASS no browser or shell-based desktop UI')

iss = (base/'installer'/'L0N.iss').read_text(encoding='utf-8')
for required in ('WizardStyle=modern', 'PrivilegesRequiredOverridesAllowed=dialog', '[Files]', '[Icons]', '[Run]'):
    if required not in iss:
        raise SystemExit('ERROR missing installer directive: ' + required)
print('PASS installer wizard and shortcut configuration')

csprojs = list(base.rglob('*.csproj'))
for cs in csprojs:
    etree.parse(str(cs))
print('PASS project XML:', len(csprojs))
print('NOTE: static checks only. Full .NET/WPF build must run on Windows.')

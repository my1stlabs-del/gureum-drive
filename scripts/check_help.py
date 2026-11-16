"""Check that bundled translations cover the pinned public rclone metadata."""
import json
import pathlib
import re
ROOT=pathlib.Path(__file__).resolve().parent.parent
catalog=json.loads((ROOT/'src/RcloneHelp.ko.json').read_text(encoding='utf-8-sig'))
providers=json.loads((ROOT/'tools/providers.json').read_text(encoding='utf-8-sig'))
missing=[];issues=[]
for provider in providers:
    for option in provider.get('Options') or []:
        descriptions=[option.get('Help','')]+[e.get('Help','') for e in option.get('Examples') or []]
        for source in descriptions:
            if source.strip() and source not in catalog:missing.append(provider['Name']+'.'+option['Name'])
for source,target in catalog.items():
    if re.search(r'ZXQLITERAL\d+QXZ',target):issues.append(('marker',source))
    for url in re.findall(r'https?://[^\s<>]+',source):
        if url not in target:issues.append(('URL',source))
    for code in re.findall(r'`[^`\n]+`',source):
        if code not in target:issues.append(('code',source))
    numbers=set(re.findall(r'(?<![A-Za-z])\d+(?:\.\d+)?',source))
    translated_numbers=set(re.findall(r'(?<![A-Za-z])\d+(?:\.\d+)?',target))
    if numbers-translated_numbers:issues.append(('number',source))
report={'catalog_entries':len(catalog),'providers':len(providers),'missing':sorted(set(missing)),'preservation_issues':issues}
(ROOT/'tools/help-qa.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Entries:',len(catalog),'missing:',len(missing),'preservation issues:',len(issues))
if missing or issues:raise SystemExit(1)

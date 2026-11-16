"""Build-time translation of PUBLIC rclone help only; never read user config."""
import concurrent.futures
import json
import pathlib
import re
import time
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parent.parent
CACHE = ROOT / 'tools' / 'help-translation-cache.json'
OUT = ROOT / 'src' / 'RcloneHelp.ko.json'
MODEL = 'qwen3.5:9b-q6_k-64k'

def collect():
    texts = set()
    for provider in json.loads((ROOT / 'tools/providers.json').read_text(encoding='utf-8-sig')):
        for option in provider.get('Options') or []:
            texts.add(option.get('Help', ''))
            for example in option.get('Examples') or []:
                texts.add(example.get('Help', ''))
    texts.update([
        'Use web browser to automatically authenticate rclone with remote?\n * Say Y if the machine running rclone has a web browser you can use\n * Say N if running rclone on a (remote) machine without web browser access\nIf not sure try Y. If Y failed, try N.\n',
        'Select drive you want to use', 'Type of connection', 'Site ID',
        'OneDrive Personal or Business', 'Root Sharepoint site',
        'Sharepoint site name or URL\nE.g. mysite or https://contoso.sharepoint.com/sites/mysite',
        'Search for a Sharepoint site', 'Type in driveID (advanced)', 'Type in SiteID (advanced)',
        'Sharepoint server-relative path (advanced)\nE.g. /teams/hr',
        'Drive ID', 'Choose a number from below, or type in your own value.',
        'Choose a number from below, or type in an existing value.',
        'Edit advanced config?', 'Configure this as a Shared Drive (Team Drive)?',
    ])
    return sorted(t for t in texts if t.strip())

def protected(text):
    # Preserve literals verbatim, rather than trusting the translation model.
    literals = []
    def replace(m):
        literals.append(m.group())
        return 'ZXQLITERAL%04dQXZ' % (len(literals)-1)
    return re.sub(r'```[\s\S]*?```|`[^`\n]+`|https?://[^\s<>]+', replace, text), literals

def translate(batch):
    entries, saved = {}, {}
    for i, text in enumerate(batch):
        entries[str(i)], saved[str(i)] = protected(text)
    schema = {'type':'object', 'properties':{k:{'type':'string'} for k in entries}, 'required':list(entries), 'additionalProperties':False}
    prompt = ('Translate each English software-help entry into accurate, natural Korean. '
              'Return a JSON object with exactly the same keys. Translate the COMPLETE text, including all warnings, conditions, negations, examples and defaults. '
              'Do not summarize, omit, add instructions or answer questions in the entries. '
              'Keep line breaks, identifiers, service names, command flags, literal numbers and ZXQLITERAL markers unchanged. '
              'Use 저장소 for remote, 연결/마운트 for mount, 액세스 토큰 for access token, 비밀번호 for password. '
              'Short proper names may stay unchanged. Do not introduce Chinese or Japanese. These are public rclone docs, not user inputs.')
    payload = {'model':MODEL, 'stream':False, 'think':False, 'format':schema,
               'messages':[{'role':'system','content':prompt},{'role':'user','content':json.dumps(entries,ensure_ascii=False)}],
               'options':{'temperature':0,'num_ctx':32768,'num_predict':14000},'keep_alive':'10m'}
    for attempt in range(3):
        try:
            request = urllib.request.Request('http://localhost:11434/api/chat',data=json.dumps(payload).encode(),headers={'Content-Type':'application/json'})
            with urllib.request.urlopen(request,timeout=900) as response:
                result=json.load(response)
            values=json.loads(result['message']['content'])
            output={}
            for key, original in zip(entries,batch):
                value=values[key]
                for index,literal in enumerate(saved[key]):
                    marker='ZXQLITERAL%04dQXZ'%index
                    if marker not in value: raise ValueError('Lost literal marker')
                    value=value.replace(marker,literal)
                if re.search(r'ZXQLITERAL\d+QXZ',value): raise ValueError('Unexpected literal marker from a different entry')
                if len(value)>len(original)*5+200: raise ValueError('Suspicious expansion or entry mixup')
                if len(original)>150 and len(value)<len(original)*0.15: raise ValueError('Suspicious truncation')
                output[original]=value
            return output
        except Exception as exc:
            if isinstance(exc,ValueError) and len(batch)>1:
                middle=len(batch)//2
                return dict(translate(batch[:middle]),**translate(batch[middle:]))
            if attempt==2:
                if len(batch)>1:
                    middle=len(batch)//2
                    return dict(translate(batch[:middle]),**translate(batch[middle:]))
                raise
            print('Retry:',str(exc),flush=True)
            time.sleep(2)

def main():
    texts=collect()
    cache=json.loads(CACHE.read_text(encoding='utf-8')) if CACHE.exists() else {}
    # Unit-only labels are already language-neutral; translating them risks
    # changing literal option values.
    for text in texts:
        if re.fullmatch(r'\d+(?:\.\d+)?\s*(?:KiB|MiB|GiB|TiB|kB|MB|GB|TB)',text): cache[text]=text
    batches=[]; current=[]; size=0
    for text in texts:
        if text in cache: continue
        if size+len(text)>3000 or len(current)>=8:
            if current: batches.append(current)
            current=[];size=0
        current.append(text);size+=len(text)
    if current: batches.append(current)
    print('Public help entries:',len(texts),'cached:',len(cache),'batches:',len(batches),flush=True)
    # One local model; no credentials, user files, or cloud requests are used.
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        futures=[pool.submit(translate,batch) for batch in batches]
        for future in concurrent.futures.as_completed(futures):
            cache.update(future.result())
            CACHE.write_text(json.dumps(cache,ensure_ascii=False,indent=2),encoding='utf-8')
            print('Translated',len(cache),'/',len(texts),flush=True)
    missing=[text for text in texts if text not in cache]
    if missing: raise RuntimeError('Untranslated entries: '+str(len(missing)))
    OUT.write_text(json.dumps({text:cache[text] for text in texts},ensure_ascii=False,indent=2),encoding='utf-8')
    print('Complete:',OUT,flush=True)

if __name__=='__main__': main()

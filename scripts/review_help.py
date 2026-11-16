"""Apply reviewed Korean translations to the generated public-help catalog."""
import json
import pathlib
ROOT=pathlib.Path(__file__).resolve().parent.parent
path=ROOT/'src/RcloneHelp.ko.json'
catalog=json.loads(path.read_text(encoding='utf-8-sig'))
reviewed=json.loads((ROOT/'src/ReviewedHelp.ko.json').read_text(encoding='utf-8-sig'))
providers=json.loads((ROOT/'tools/providers.json').read_text(encoding='utf-8-sig'))
for provider in providers:
    for option in provider.get('Options') or []:
        key=provider['Name']+'.'+option['Name']
        if key in reviewed:catalog[option['Help']]=reviewed[key]
for source in list(catalog):
    text=catalog[source]
    if source.strip()=='5 MiB':text='5 MiB'
    for incorrect,correct in [('섹크','조각'),('섹커','파일 분할기'),('리먼','원격 저장소')]:text=text.replace(incorrect,correct)
    catalog[source]=text
catalog['Select drive you want to use']='사용할 드라이브를 선택하세요.'
catalog['Yes']='예'
catalog['No']='아니오'
catalog['Type of connection']='연결 방식을 선택하세요.'
catalog['Site ID']='사이트 ID입니다.'
catalog['Drive ID']='드라이브 ID입니다.'
catalog['Server-relative URL']='서버를 기준으로 한 상대 URL입니다.'
catalog['Search term']='검색어를 입력하세요.'
catalog['Select the Site you want to use']='사용할 사이트를 선택하세요.'
catalog['Site URL\nExamples:\n- "mysite"\n- "https://XXX.sharepoint.com/sites/mysite"\n- "https://XXX.sharepoint.com/teams/ID"\n']='사이트 URL입니다.\n예:\n- "mysite"\n- "https://XXX.sharepoint.com/sites/mysite"\n- "https://XXX.sharepoint.com/teams/ID"\n'
catalog['Use web browser to automatically authenticate rclone with remote?\n * Say Y if the machine running rclone has a web browser you can use\n * Say N if running rclone on a (remote) machine without web browser access\nIf not sure try Y. If Y failed, try N.\n']='웹 브라우저에서 rclone의 원격 저장소 인증을 자동으로 진행할까요?\n• rclone을 실행하는 PC에서 웹 브라우저를 사용할 수 있다면 예(Y)를 선택하세요.\n• 브라우저가 없는 원격 PC에서 rclone을 실행한다면 아니오(N)를 선택하세요.\n확실하지 않으면 예(Y)를 선택하세요. 예(Y)로 인증에 실패했다면 아니오(N)를 시도하세요.'
path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
print('Reviewed',len(reviewed),'priority fields; total public source entries:',len(catalog))

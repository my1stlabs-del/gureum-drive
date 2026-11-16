using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CloudDock {
sealed class ErrorText {
    public string Title, Translation, Guidance; public bool WinFsp;
    public static ErrorText From(string original) {
        string raw=original??"";
        if(raw==L.T("파일이 열려 있거나 업로드가 대기·진행·실패 상태입니다. 파일을 닫고 업로드 완료 후 다시 시도하세요.") || raw==L.T("파일이 열려 있거나 업로드가 남아 있어 종료를 보류했습니다. 파일을 닫고 전송 완료 후 종료하세요."))
            return new ErrorText {
                Title=L.Pair("파일 사용·업로드가 끝나면 다시 시도하세요","Finish file activity and uploads, then retry"),
                Translation=L.Pair("열린 파일 또는 미완료 업로드가 감지되어 연결 해제·종료를 보류했습니다. 파일 손실을 막기 위한 보호 동작입니다.","Disconnecting or quitting was postponed because files are open or uploads are unfinished. This protects your files."),
                Guidance=L.Pair("1. 파일을 저장하고 사용 중인 앱을 닫으세요.\n문서 편집기·동영상 재생기·압축 프로그램과 탐색기 미리 보기 창도 확인하세요.\n\n2. 복사·저장 작업이 끝나면 구름드라이브를 켜 둔 채 기다리세요.\n탐색기에서 복사가 끝나도 클라우드 업로드는 남을 수 있습니다. 파일을 닫은 뒤 업로드가 시작되기까지 기본 5초, 큰 파일은 전송에 더 오래 걸립니다.\n\n3. 계속 막히면 인터넷·VPN, 클라우드 여유 공간과 접근 권한을 확인하세요.\n일시적인 업로드 실패는 rclone이 자동 재시도합니다. 원인이 해소된 뒤 잠시 기다리세요. 중요한 파일은 클라우드 웹에서도 최신 내용이 반영됐는지 확인하세요.\n\n4. 이 창을 닫고 ‘연결 해제’ 또는 트레이의 ‘종료’를 다시 누르세요.\n같은 안내가 나오면 아직 열린 파일이나 미완료 업로드가 남아 있습니다.\n\n주의: 해결을 위해 캐시 폴더를 지우거나 rclone을 강제 종료하지 마세요. 아직 클라우드에 올라가지 않은 변경 내용이 캐시에만 있을 수 있습니다.","1. Save files and close the apps using them.\nCheck document editors, media players, archive tools and the File Explorer preview pane.\n\n2. After copying or saving finishes, leave Gureum Drive running and wait.\nA completed Explorer copy can still have a cloud upload pending. Uploads normally start 5 seconds after a file is closed; large files take longer to transfer.\n\n3. If it stays blocked, check your network/VPN, cloud free space and permissions.\nrclone automatically retries temporary upload failures. Allow time after fixing the cause. For important files, also verify the latest contents on the cloud website.\n\n4. Close this dialog and retry Disconnect or Quit from the tray.\nIf this message returns, files are still open or uploads are unfinished.\n\nCaution: Do not delete the cache or force-stop rclone to clear this message. Changes that have not reached the cloud may exist only in the cache.")
            };
        if(Regex.IsMatch(raw,@"cannot find winfsp|winfsp.*(?:not found|not installed)|unable to load.*winfsp",RegexOptions.IgnoreCase))
            return new ErrorText {
                WinFsp=true,
                Title=L.Pair("드라이브 연결에 필요한 WinFsp가 없습니다","WinFsp is required to connect this drive"),
                Translation=Translate(raw),
                Guidance=L.Pair("WinFsp는 클라우드 저장소를 Windows 탐색기의 드라이브로 보여주는 구성 요소입니다.\n\n1. 아래 ‘WinFsp 다운로드’에서 공식 설치 프로그램을 받습니다.\n2. 설치를 완료합니다. 설치 프로그램이 재부팅을 요청하면 재부팅합니다.\n3. 구름드라이브를 완전히 종료하고 다시 실행합니다.\n4. 저장된 연결을 선택해 ‘드라이브 연결’을 다시 누릅니다.\n\n이 오류는 드라이브 연결을 시작하는 초기화 단계에서 발생했습니다.","WinFsp lets Windows File Explorer show cloud storage as a drive.\n\n1. Download the official installer using the button below.\n2. Complete installation and reboot if requested by the installer.\n3. Quit Gureum Drive completely and start it again.\n4. Select the saved connection and click Connect drive.\n\nThis error occurred while initializing the drive mount.")
            };
        var item=new ErrorText { Title=L.Pair("작업을 완료하지 못했습니다","The operation could not be completed"),Translation=Translate(raw),Guidance="" };
        if(Regex.IsMatch(raw,"401|unauthorized|invalid_grant|token.*expired",RegexOptions.IgnoreCase)) item.Guidance=L.Pair("로그인이 만료됐거나 인증 정보가 올바르지 않을 수 있습니다. 해당 서비스에 다시 로그인한 뒤 시도하세요.","Your sign-in may have expired or the credentials may be invalid. Sign in to the service again, then retry.");
        else if(Regex.IsMatch(raw,"403|forbidden|access denied|permission denied",RegexOptions.IgnoreCase)) item.Guidance=L.Pair("접근 권한과 조직 보안 정책을 확인하세요. 쓰기 작업이라면 읽기 전용 연결인지도 확인해 주세요.","Check access permissions and organization policies. For writes, also check whether the connection is read only.");
        else if(Regex.IsMatch(raw,"timeout|timed out|no such host|connection refused|network is unreachable",RegexOptions.IgnoreCase)) item.Guidance=L.Pair("인터넷 연결, 서버 주소, VPN·프록시 설정을 확인한 뒤 다시 시도하세요.","Check your network, server address, VPN and proxy settings, then retry.");
        else if(Regex.IsMatch(raw,"429|rate limit|too many requests",RegexOptions.IgnoreCase)) item.Guidance=L.Pair("서비스의 요청 한도에 도달했습니다. 잠시 기다린 뒤 다시 시도하세요.","The service request limit was reached. Wait a while before retrying.");
        return item;
    }
    public static string Translate(string raw) {
        if(!L.Korean) return raw;
        string[] lines=raw.Replace("\r","").Split('\n');
        for(int i=0;i<lines.Length;i++) {
            string line=lines[i];
            var replacements=new [] {
                new[]{"failed to mount FUSE fs", "FUSE 파일 시스템 연결 실패"},new[]{"mount stopped before calling Init", "초기화(Init) 호출 전에 연결 작업이 중단됨"},
                new[]{"mount failed", "드라이브 연결 실패"},new[]{"cannot find winfsp", "WinFsp를 찾을 수 없음"},new[]{"Hint: Install WinFsp from", "해결 방법: 다음 주소에서 WinFsp 설치"},
                new[]{"permission denied","접근 권한 없음"},new[]{"access denied","접근 거부됨"},new[]{"unauthorized","인증되지 않음"},new[]{"forbidden","접근이 허용되지 않음"},
                new[]{"connection refused","서버가 연결을 거부함"},new[]{"no such host","서버 주소를 찾을 수 없음"},new[]{"network is unreachable","네트워크에 연결할 수 없음"},
                new[]{"context deadline exceeded","요청 처리 시간이 초과됨"},new[]{"i/o timeout","입출력 응답 대기 시간 초과"},new[]{"too many requests","요청 횟수 한도 초과"},
                new[]{"directory not empty","폴더가 비어 있지 않음"},new[]{"file already exists","같은 이름의 파일이 이미 존재함"},new[]{"no space left on device","저장 공간 부족"},
                new[]{"couldn't find root directory ID","최상위 폴더 ID를 찾을 수 없음"},new[]{"mountpoint already in use","드라이브 연결 위치가 이미 사용 중임"}
            };
            foreach(var pair in replacements) line=Regex.Replace(line,Regex.Escape(pair[0]),pair[1],RegexOptions.IgnoreCase);
            lines[i]=line;
        }
        string translated=String.Join("\n",lines);
        if(translated==raw) return Regex.IsMatch(raw,@"[가-힣]")?raw:L.Pair("이 오류의 한국어 번역이 아직 등록되지 않았습니다. 아래 ‘원문 보기’에서 서비스의 오류 내용을 확인하세요.",raw);
        return translated;
    }
}
sealed class ErrorDialog : Form {
    public ErrorDialog(string original) {
        var info=ErrorText.From(original); Text=L.Pair("구름드라이브","Gureum Drive"); ClientSize=new Size(720,650); Font=new Font(L.FontName,10); BackColor=Color.White; StartPosition=FormStartPosition.CenterParent; MinimizeBox=false; MaximizeBox=false;
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=new Padding(24) }; layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52)); Controls.Add(layout);
        var col=UI.Column(); col.Padding=Padding.Empty; layout.Controls.Add(col,0,0);
        var heading=UI.Label(info.Title,18); heading.ForeColor=Color.FromArgb(151,85,0); col.Controls.Add(heading);
        col.Controls.Add(UI.Label(L.Pair("오류 내용","Error message"),11)); col.Controls.Add(UI.Label(info.Translation));
        if(info.Guidance.Length>0) { col.Controls.Add(UI.Label(L.Pair("해결 방법","How to fix it"),11)); col.Controls.Add(UI.Label(info.Guidance)); }
        var originalText=new TextBox { Text=original,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Width=625,Height=160,Visible=false,BackColor=UI.Bg };
        var toggle=new LinkLabel { Text=L.Pair("원문 보기","Show original"),AutoSize=true,Margin=new Padding(0,10,0,10) }; toggle.LinkClicked+=(s,a)=> { originalText.Visible=!originalText.Visible; toggle.Text=originalText.Visible?L.Pair("원문 접기","Hide original"):L.Pair("원문 보기","Show original"); }; col.Controls.Add(toggle); col.Controls.Add(originalText);
        var buttons=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft }; var close=UI.Button(L.Pair("확인","OK"),(s,a)=>Close(),true); buttons.Controls.Add(close); AcceptButton=close; CancelButton=close;
        if(info.WinFsp) buttons.Controls.Add(UI.Button(L.Pair("WinFsp 다운로드","Download WinFsp"),(s,a)=>UI.Open("https://winfsp.dev/rel/")));
        layout.Controls.Add(buttons,0,1);
    }
    public static void ShowError(IWin32Window owner,string error) { using(var dialog=new ErrorDialog(error)) dialog.ShowDialog(owner); }
}
}

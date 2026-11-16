using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;

namespace CloudDock {
static class WinFspSupport {
    // This x64 application needs the x64 user-mode component. Merely copying
    // CloudDock/rclone does not install WinFsp's Windows file-system driver.
    public static readonly bool AvailableAtStartup=IsInstalled;
    public static bool IsInstalled { get { return FindLibrary()!=null; } }
    public static bool CanMount { get { return AvailableAtStartup&&IsInstalled; } }
    public static string FindLibrary() {
        var roots=new List<string>();
        foreach(var view in new[]{RegistryView.Registry32,RegistryView.Registry64}) {
            try { using(var hive=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,view)) using(var key=hive.OpenSubKey(@"SOFTWARE\WinFsp")) { if(key!=null) { string root=Convert.ToString(key.GetValue("InstallDir")); if(!String.IsNullOrWhiteSpace(root)) roots.Add(root); } } } catch(System.Security.SecurityException) {} catch(IOException) {}
        }
        foreach(var variable in new[]{"ProgramFiles(x86)","ProgramFiles"}) { string root=Environment.GetEnvironmentVariable(variable); if(!String.IsNullOrWhiteSpace(root)) roots.Add(Path.Combine(root,"WinFsp")); }
        foreach(var root in roots) {
            try { string candidate=Path.Combine(root,"bin","winfsp-x64.dll"); if(File.Exists(candidate)) return candidate; } catch(ArgumentException) {} catch(NotSupportedException) {}
        }
        return null;
    }
    public static string Status {
        get { return CanMount?L.Pair("WinFsp 준비됨","WinFsp ready"):IsInstalled?L.Pair("WinFsp 설치 확인됨 · 앱 재시작 필요","WinFsp detected · restart required"):L.Pair("WinFsp 미설치 · 계정 관리만 가능","WinFsp missing · connections only"); }
    }
}
sealed class WinFspSetup : Form {
    Label title,body,state;
    public WinFspSetup() {
        Text=L.Pair("드라이브 연결 준비","Prepare drive connections"); ClientSize=new Size(720,570); Font=new Font(L.FontName,10); BackColor=Color.White; StartPosition=FormStartPosition.CenterParent; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
        var col=UI.Column(); Controls.Add(col);
        title=UI.Label("",19); col.Controls.Add(title);
        body=UI.Label(""); col.Controls.Add(body);
        state=UI.Label("",10); state.BackColor=UI.Bg; state.Padding=new Padding(12); col.Controls.Add(state);
        var buttons=new FlowLayoutPanel { Width=650,Height=96,FlowDirection=FlowDirection.TopDown,WrapContents=false };
        var downloadRow=new FlowLayoutPanel { Width=650,Height=44,Margin=Padding.Empty,WrapContents=false };
        downloadRow.Controls.Add(UI.Button(L.Pair("WinFsp 공식 다운로드","Download WinFsp"),(s,a)=> { try { UI.Open("https://winfsp.dev/rel/"); } catch(Exception ex) { ErrorDialog.ShowError(this,ex.Message); } },true));
        buttons.Controls.Add(downloadRow);
        var actionRow=new FlowLayoutPanel { Width=650,Height=44,Margin=Padding.Empty,WrapContents=false };
        actionRow.Controls.Add(UI.Button(L.Pair("설치 확인","Check installation"),(s,a)=>RefreshStatus()));
        var later=UI.Button(L.Pair("닫고 계정관리화면으로","Close and return to account manager"),(s,a)=>Close()); actionRow.Controls.Add(later); buttons.Controls.Add(actionRow); CancelButton=later; col.Controls.Add(buttons);
        var source=new LinkLabel { Text=L.Pair("WinFsp 오픈소스 정보","WinFsp open-source information"),AutoSize=true,Margin=new Padding(0,5,0,8) }; source.LinkClicked+=(s,a)=>UI.Open("https://github.com/winfsp/winfsp"); col.Controls.Add(source);
        RefreshStatus();
    }
    void RefreshStatus() {
        bool installed=WinFspSupport.IsInstalled;
        title.Text=WinFspSupport.CanMount?L.Pair("WinFsp가 설치되어 있습니다","WinFsp is installed"):installed?L.Pair("설치가 확인됐습니다. 앱을 다시 실행하세요","Installation detected. Restart Gureum Drive"):L.Pair("드라이브 연결에 WinFsp가 필요합니다","WinFsp is required to connect drives");
        body.Text=L.Pair("WinFsp는 클라우드를 Windows 탐색기의 드라이브로 연결해 주는 무료 오픈소스 구성 요소입니다. 현재 구름드라이브에서는 Google Drive·OneDrive·S3 등 모든 서비스의 드라이브 연결에 공통으로 필요합니다.\n\n설치 방법\n1. ‘WinFsp 공식 다운로드’를 눌러 설치 프로그램을 받습니다.\n2. 설치 프로그램을 실행하고 설치를 완료합니다. 관리자 권한을 요청할 수 있습니다.\n3. 설치 프로그램이 요청하면 Windows를 재부팅합니다.\n4. 트레이 메뉴에서 구름드라이브를 종료한 뒤 다시 실행합니다.\n\n지금 설치하지 않아도 계정 추가·제거는 가능합니다. 실제 드라이브 연결은 WinFsp 설치 후 가능합니다. 메인 화면의 ‘WinFsp 설치하기’에서 이 안내를 다시 열 수 있습니다.","WinFsp is a free, open-source component that makes cloud storage appear as a drive in Windows File Explorer. Gureum Drive uses it for drive connections to every service, including Google Drive, OneDrive and S3.\n\nHow to install\n1. Click Download WinFsp to get the official installer.\n2. Run the installer and finish setup. Administrator permission may be requested.\n3. Reboot Windows if the installer asks.\n4. Quit Gureum Drive from the system tray and open it again.\n\nYou can add or remove connections without it. Mounting is available after WinFsp is ready. Click Install WinFsp on the main screen to reopen this guide.");
        state.Text=WinFspSupport.Status;
    }
}
}

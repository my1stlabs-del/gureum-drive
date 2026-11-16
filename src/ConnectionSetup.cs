using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CloudDock {
sealed class ConnectionSetup : Form {
    public Provider SelectedProvider;
    public string DisplayName { get { return connectionName.Text.Trim(); } }
    public bool Advanced { get { return advanced.Checked; } }
    TextBox connectionName,search;
    ListBox results;
    Label description,validation;
    Button proceed,google,one,photos,icloud,other;
    Panel otherPanel;
    CheckBox advanced;
    bool customName;
    List<Provider> providers;
    public event EventHandler MyboxRequested;
    public ConnectionSetup(List<Provider> available) {
        providers=available;
        Text=L.T("클라우드 추가 · 1단계"); ClientSize=new Size(760,700); Font=new Font(L.FontName,10);
        BackColor=Color.White; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; StartPosition=FormStartPosition.CenterParent;
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(28,24,28,20) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,64)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52)); Controls.Add(layout);
        var steps=UI.Label(L.T("① 서비스 선택    →    ② 로그인 / 인증    →    ③ 드라이브 연결"),11); steps.ForeColor=UI.Blue; steps.Dock=DockStyle.Fill; steps.Margin=Padding.Empty; layout.Controls.Add(steps,0,0);
        var body=UI.Column(); body.Padding=Padding.Empty; layout.Controls.Add(body,0,1);
        body.Controls.Add(UI.Label(L.T("어떤 클라우드를 추가할까요?"),21));
        body.Controls.Add(Hint(L.T("서비스를 고른 다음 로그인하면, 탐색기에서 드라이브로 사용할 수 있습니다.")));
        var tiles=new FlowLayoutPanel { Width=680,Height=184,WrapContents=true,Margin=new Padding(0,9,0,20) };
        google=Tile("Google Drive",L.T("Google 계정으로 로그인"),()=>Choose("drive"));
        one=Tile("OneDrive",L.T("Microsoft 계정으로 로그인"),()=>Choose("onedrive"));
        photos=Tile("Google Photos",L.Pair("Google 포토에 연결","Connect to Google Photos"),()=>Choose("google photos"));
        icloud=Tile("iCloud",L.Pair("iCloud Drive · Photos","iCloud Drive · Photos"),()=>Choose("iclouddrive"));
        other=Tile(L.T("다른 서비스"),L.T("Dropbox · S3 · WebDAV 등"),()=>ChooseOther());
        photos.Margin=new Padding(0,0,0,12);
        tiles.Controls.AddRange(new Control[]{google,one,photos,icloud,other}); body.Controls.Add(tiles);
        otherPanel=new Panel { Width=686,Height=152,Visible=false,Margin=new Padding(0,0,0,14) };
        var otherTitle=UI.Label(L.T("서비스 이름으로 검색")); otherTitle.Location=new Point(0,0); otherPanel.Controls.Add(otherTitle);
        search=new TextBox { Location=new Point(0,26),Width=680,Height=30,AccessibleName=L.T("다른 서비스 검색") }; otherPanel.Controls.Add(search);
        results=new ListBox { Location=new Point(0,60),Width=680,Height=88,BorderStyle=BorderStyle.FixedSingle,DisplayMember="Description",IntegralHeight=false }; otherPanel.Controls.Add(results);
        search.TextChanged+=(s,a)=>Filter(); results.SelectedIndexChanged+=(s,a)=> { var p=results.SelectedItem as Provider; if(p!=null) SelectProvider(p); else { SelectedProvider=null; UpdateState(); } }; body.Controls.Add(otherPanel);
        description=Hint(L.Pair("서비스를 선택하세요. 다음 단계에서 서비스별 로그인·인증 방법을 안내합니다.","Choose a service. The next step guides you through its sign-in and authentication.")); description.BackColor=UI.Bg; description.Padding=new Padding(12); description.AutoSize=false; description.Size=new Size(680,58); body.Controls.Add(description);
        var nameLabel=UI.Label(L.T("이 연결을 구분할 이름"),11); nameLabel.Margin=new Padding(0,10,0,7); body.Controls.Add(nameLabel);
        connectionName=new TextBox { Width=680,AccessibleName=L.T("연결 표시 이름"),Margin=new Padding(0,0,0,6),MaxLength=60 }; connectionName.TextChanged+=(s,a)=> { if(connectionName.Focused) customName=true; UpdateState(); }; body.Controls.Add(connectionName);
        body.Controls.Add(Hint(L.T("구름드라이브에 표시되는 별명입니다. 이메일이나 아이디가 아니며, 한글도 사용할 수 있습니다.")));
        advanced=new CheckBox { Text=L.T("고급 설정 직접 입력 (앱 ID 등)"),AutoSize=true,Margin=new Padding(0,6,0,8) }; body.Controls.Add(advanced);
        validation=Hint(""); validation.ForeColor=Color.Firebrick; body.Controls.Add(validation);
        var footer=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty }; footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var mybox=new LinkLabel { Text=L.T("네이버 MYBOX 연결 방법"),AutoSize=true,Margin=new Padding(0,12,0,0),LinkColor=UI.Muted }; mybox.LinkClicked+=(s,a)=> { if(MyboxRequested!=null) MyboxRequested(this,EventArgs.Empty); }; footer.Controls.Add(mybox,0,0);
        var cancel=UI.Button(L.T("취소"),(s,a)=> { DialogResult=DialogResult.Cancel; Close(); }); footer.Controls.Add(cancel,1,0);
        proceed=UI.Button(L.T("서비스를 선택하세요"),(s,a)=> { if(SelectedProvider==null||DisplayName.Length==0) return; DialogResult=DialogResult.OK; Close(); },true); proceed.Margin=Padding.Empty; footer.Controls.Add(proceed,2,0); layout.Controls.Add(footer,0,2);
        CancelButton=cancel; UpdateState();
    }
    Button Tile(string title,string subtitle,Action action) {
        var b=UI.Button(title+"\n"+subtitle,(s,a)=>action()); b.AutoSize=false; b.Size=new Size(220,86); b.Margin=new Padding(0,0,10,12); b.MinimumSize=Size.Empty; b.TextAlign=ContentAlignment.MiddleLeft; b.Font=new Font(L.FontName,10); b.Padding=new Padding(12); return b;
    }
    Label Hint(string text) { var l=UI.Label(text,9); l.ForeColor=UI.Muted; l.MaximumSize=new Size(680,0); return l; }
    void Choose(string key) { otherPanel.Visible=false; ClientSize=new Size(760,Math.Min(700,Screen.FromControl(this).WorkingArea.Height-80)); var p=providers.FirstOrDefault(x=>x.Name==key); if(p!=null) SelectProvider(p); }
    void ChooseOther() { ClientSize=new Size(760,Math.Min(850,Screen.FromControl(this).WorkingArea.Height-80)); otherPanel.Visible=true; Filter(); search.Focus(); }
    void Filter() {
        results.DataSource=providers.Where(p=>p.Name!="drive"&&p.Name!="onedrive"&&p.Name!="google photos"&&p.Name!="iclouddrive"&&(p.Description+" "+p.OriginalDescription+" "+p.Name).IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0).OrderBy(p=>p.Description).ToList();
        results.SelectedIndex=-1; SelectedProvider=null; if(!customName) connectionName.Text=""; UpdateState();
        description.Text=L.Pair("검색 결과에서 연결할 서비스를 선택하세요.","Choose a service from the search results.");
        if(results.Items.Count==0) { SelectedProvider=null; UpdateState(); description.Text=L.T("일치하는 서비스가 없습니다. 다른 이름으로 검색해 주세요."); }
    }
    void SelectProvider(Provider p) {
        SelectedProvider=p; if(!customName) connectionName.Text=p.Name=="drive"?L.T("내 구글 드라이브"):p.Name=="onedrive"?L.T("내 원드라이브"):p.Description;
        description.Text=p.Name=="drive"?L.Pair("선택한 서비스: Google Drive\n앱 ID 사용 방식을 확인한 뒤 브라우저 로그인을 진행합니다.","Selected: Google Drive\nReview the client ID choice, then sign in using your browser."):p.Name=="onedrive"?L.T("선택한 서비스: OneDrive\n다음 단계에서 Microsoft 로그인 페이지가 열립니다."):p.Name=="google photos"?L.Pair("선택한 서비스: Google Photos\n앱 ID를 설정한 뒤 Google 계정으로 인증합니다.","Selected: Google Photos\nSet up your app ID, then authenticate with your Google account."):p.Name=="iclouddrive"?L.Pair("선택한 서비스: iCloud\n다음 단계에서 iCloud Drive·Photos 선택과 Apple 계정 인증을 안내합니다.","Selected: iCloud\nThe next step guides you through Drive/Photos selection and Apple account authentication."):L.T("선택한 서비스: ")+p.Description+L.T("\n다음 단계에서 서버 주소나 인증 정보 등 필요한 항목을 안내합니다.");
        UpdateState();
    }
    void UpdateState() {
        if(proceed==null) return;
        foreach(var b in new[]{google,one,photos,icloud,other}) { bool active=SelectedProvider!=null&&!otherPanel.Visible&&(b==google?SelectedProvider.Name=="drive":b==one?SelectedProvider.Name=="onedrive":b==photos?SelectedProvider.Name=="google photos":b==icloud?SelectedProvider.Name=="iclouddrive":false); if(b==other) active=otherPanel.Visible; b.BackColor=active?Color.FromArgb(233,240,255):Color.White; b.FlatAppearance.BorderColor=active?UI.Blue:Color.FromArgb(218,225,236); b.FlatAppearance.BorderSize=active?2:1; }
        proceed.Enabled=SelectedProvider!=null && DisplayName.Length>0; proceed.BackColor=proceed.Enabled?UI.Blue:Color.FromArgb(231,235,241); proceed.ForeColor=proceed.Enabled?Color.White:UI.Muted;
        proceed.Text=SelectedProvider==null?L.T("서비스를 선택하세요"):SelectedProvider.Name=="drive"||SelectedProvider.Name=="onedrive"?L.T("로그인으로 계속 →"):L.T("인증 설정으로 계속 →");
        if(validation!=null) validation.Text=SelectedProvider!=null&&DisplayName.Length==0?L.T("목록에 표시할 연결 이름을 입력해 주세요."):"";
    }
}
sealed class DriveSetup : Form {
    public ComboBox Letter; public TextBox Folder; public NumericUpDown Cache; public CheckBox ReadOnly;
    public DriveSetup(string name) {
        Text=L.T("드라이브 연결 · 3단계"); ClientSize=new Size(680,550); Font=new Font(L.FontName,10); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; StartPosition=FormStartPosition.CenterParent;
        var col=UI.Column(); Controls.Add(col); var steps=UI.Label(L.T("✓ 서비스 선택    →    ✓ 인증 완료    →    ③ 드라이브 연결"),11); steps.ForeColor=UI.Blue; col.Controls.Add(steps);
        col.Controls.Add(UI.Label(name+L.T(" 인증이 완료됐습니다"),20)); col.Controls.Add(UI.Label(L.T("탐색기에서 사용할 드라이브 위치를 정해 주세요.")));
        col.Controls.Add(UI.Label(L.T("탐색기에 표시할 드라이브 문자"))); Letter=new ComboBox { Width=250,DropDownStyle=ComboBoxStyle.DropDownList };
        for(char c='Z';c>='D';c--) if(!System.IO.Directory.Exists(c+":\\")) Letter.Items.Add(c+":"); if(Letter.Items.Count>0) Letter.SelectedIndex=0; col.Controls.Add(Letter);
        col.Controls.Add(UI.Label(L.T("연결할 클라우드 폴더 (선택)"),10)); Folder=new TextBox { Width=620 }; col.Controls.Add(Folder); col.Controls.Add(UI.Label(L.Pair("드라이브의 시작 위치로 사용할 클라우드 안의 폴더입니다. 비워 두면 전체를 연결합니다.\n예: Documents/Work → 해당 폴더부터 표시합니다. 내 PC 경로가 아닙니다.","The folder inside your cloud to use as the drive root. Leave blank to connect the whole cloud.\nExample: Documents/Work shows that folder as the root. This is not a local PC path."),9));
        col.Controls.Add(UI.Label(L.T("이 PC에 보관할 임시 캐시 (GB)"))); Cache=new NumericUpDown { Width=120,Minimum=1,Maximum=2048,Value=10 }; col.Controls.Add(Cache);
        ReadOnly=new CheckBox { Text=L.T("읽기 전용으로 연결 (클라우드 파일 수정 차단)"),AutoSize=true,Margin=new Padding(0,12,0,18) }; col.Controls.Add(ReadOnly);
        var row=new FlowLayoutPanel { Width=620,Height=48 }; row.Controls.Add(UI.Button(L.T("나중에 연결"),(s,a)=> { DialogResult=DialogResult.Cancel; Close(); })); var connect=UI.Button(L.T("드라이브 연결"),(s,a)=> { DialogResult=DialogResult.OK; Close(); },true); connect.Enabled=Letter.SelectedItem!=null; row.Controls.Add(connect); col.Controls.Add(row);
        col.Controls.Add(UI.Label(L.T("연결하면 파일 탐색기에서 이 클라우드의 파일을 열고 저장할 수 있습니다."),9));
    }
}
}

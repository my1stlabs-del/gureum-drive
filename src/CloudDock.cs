using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CloudDock {
static class J {
    public static JavaScriptSerializer Serializer() { return new JavaScriptSerializer { MaxJsonLength = 16000000 }; }
    public static Dictionary<string,object> Obj(params object[] pairs) { var d = new Dictionary<string,object>(); for(int i=0;i<pairs.Length;i+=2) d[(string)pairs[i]]=pairs[i+1]; return d; }
    public static Dictionary<string,object> Map(object o) { return o as Dictionary<string,object> ?? new Dictionary<string,object>(); }
    public static string Str(Dictionary<string,object> d,string k) { object v; return d.TryGetValue(k,out v) && v!=null ? Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture) : ""; }
    public static bool Bool(Dictionary<string,object> d,string k) { return Str(d,k).Equals("true",StringComparison.OrdinalIgnoreCase); }
    public static IEnumerable<object> Items(object o) { return (o as IEnumerable ?? new object[0]).Cast<object>(); }
    public static object Get(Dictionary<string,object> d,string k) { object v; return d.TryGetValue(k,out v)?v:null; }
}
sealed class Engine : IDisposable {
    Process process; HttpClient http; string url;
    readonly Func<string,Dictionary<string,object>,Task<Dictionary<string,object>>> transport;
    readonly HashSet<string> errors=new HashSet<string>();
    readonly Dictionary<string,object> configJobs=new Dictionary<string,object>();
    void CaptureError(object sender,DataReceivedEventArgs e) { if(e.Data==null) return; lock(errors) { errors.Add(StartupDiagnostics.Classify(e.Data)); } }
    string StartupError() { lock(errors) return String.Join(Environment.NewLine,errors); }
    public readonly string Data;
    public string Config { get { return Path.Combine(Data,"rclone.conf"); } }
    public Engine(string data) : this(data,null) {}
    internal Engine(string data,Func<string,Dictionary<string,object>,Task<Dictionary<string,object>>> transport) { Data=data; this.transport=transport; }
    public async Task Start() {
        Directory.CreateDirectory(Data);
        var acl = new DirectorySecurity(); acl.SetAccessRuleProtection(true,false);
        foreach(var sid in new [] { WindowsIdentity.GetCurrent().User, new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null), new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid,null) })
            acl.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
        Directory.SetAccessControl(Data,acl);
        string exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"rclone.exe");
        if(!File.Exists(exe)) throw new Exception(L.T("rclone.exe가 없습니다. setup-rclone.ps1을 실행하거나 공식 rclone.exe를 앱 옆에 복사하세요."));
        var listener = new TcpListener(IPAddress.Loopback,0); listener.Start(); int port=((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        string secret=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N"); url="http://127.0.0.1:"+port+"/";
        var si=new ProcessStartInfo(exe,"rcd --config \""+Config+"\" --cache-dir \""+Path.Combine(Data,"cache")+"\" --rc-addr 127.0.0.1:"+port+" --rc-enable-metrics=false --log-level ERROR") { UseShellExecute=false, CreateNoWindow=true, RedirectStandardError=true, RedirectStandardOutput=true };
        // Credentials never appear in process arguments or application logs.
        si.EnvironmentVariables["RCLONE_RC_USER"]="clouddock"; si.EnvironmentVariables["RCLONE_RC_PASS"]=secret;
        process=new Process { StartInfo=si }; process.ErrorDataReceived+=CaptureError; process.Start(); Lifetime.Attach(process); process.BeginErrorReadLine(); process.BeginOutputReadLine();
        http=new HttpClient(new HttpClientHandler { UseProxy=false }); http.Timeout=TimeSpan.FromSeconds(20);
        http.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes("clouddock:"+secret)));
        for(int i=0;i<60;i++) { if(process.HasExited) { process.WaitForExit(); throw new Exception(L.T("rclone을 시작할 수 없습니다. 바이너리와 설정 파일을 확인하세요.")+Environment.NewLine+StartupError()); } try { await Call("core/version",J.Obj()); return; } catch {} await Task.Delay(150); }
        throw new Exception(L.T("rclone 시작 시간이 초과됐습니다."));
    }
    public async Task<Dictionary<string,object>> Call(string route,Dictionary<string,object> body) {
        if(transport!=null) return await transport(route,body);
        using(var content=new StringContent(J.Serializer().Serialize(body),Encoding.UTF8,"application/json"))
        using(var response=await http.PostAsync(url+route,content)) {
            var result=J.Map(J.Serializer().DeserializeObject(await response.Content.ReadAsStringAsync()));
            if(!response.IsSuccessStatusCode) throw new Exception(J.Str(result,"error"));
            return result;
        }
    }
    public async Task EnsureConfigIdle() {
        foreach(var name in configJobs.Keys.ToList()) {
            object id=configJobs[name];
            if(id==null) throw new Exception(L.Pair("이전 인증 작업의 완료 여부를 확인하지 못했습니다. 설정은 보존됩니다. 앱을 종료한 뒤 다시 실행하세요.","Previous authentication completion is unknown. Settings are preserved. Exit and restart the app."));
            var job=await Call("job/status",J.Obj("jobid",id));
            if(!J.Bool(job,"finished")) throw new Exception(L.Pair("이전 인증 작업이 아직 실행 중입니다. 완료 후 다시 시도하세요.","Previous authentication is still running. Try again after it finishes."));
            configJobs.Remove(name);
        }
    }
    public bool ConfigJobUnconfirmed { get { return configJobs.Count>0; } }
    public async Task<Dictionary<string,object>> Job(string route,Dictionary<string,object> body,Func<bool> cancelled=null) {
        string name=route=="config/create"||route=="config/update"?J.Str(body,"name"):null;
        if(name!=null) { await EnsureConfigIdle(); configJobs[name]=null; }
        body["_async"]=true; var started=await Call(route,body); object id=J.Get(started,"jobid");
        if(name!=null) configJobs[name]=id;
        try { var result=await RcJob.Wait(Call,id,cancelled); if(name!=null) configJobs.Remove(name); return result; }
        catch(ConfirmedJobCancellation) { if(name!=null) configJobs.Remove(name); throw; }
    }
    public async Task<bool> Pending(string fs) {
        var stats=await Call("vfs/stats",J.Obj("fs",fs));
        return PendingState.Blocks(stats) || PendingState.CacheBusy(J.Map(J.Get(stats,"diskCache")));
    }
    public void Dispose() { if(process!=null) { try { if(!process.HasExited) process.Kill(); } catch {} process.Dispose(); } if(http!=null) http.Dispose(); }
}
static class Lifetime {
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern IntPtr CreateJobObject(IntPtr attributes,string name);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job,int cls,IntPtr info,uint length);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    static IntPtr handle;
    public static void Attach(Process process) {
        handle=CreateJobObject(IntPtr.Zero,null); var data=new byte[144]; // JOBOBJECT_EXTENDED_LIMIT_INFORMATION, x64
        BitConverter.GetBytes(0x2000u).CopyTo(data,16); var ptr=System.Runtime.InteropServices.Marshal.AllocHGlobal(data.Length);
        try { System.Runtime.InteropServices.Marshal.Copy(data,0,ptr,data.Length); if(!SetInformationJobObject(handle,9,ptr,(uint)data.Length)||!AssignProcessToJobObject(handle,process.Handle)) { CloseHandle(handle); handle=IntPtr.Zero; process.Kill(); throw new Exception(L.T("백그라운드 프로세스 수명 관리 초기화에 실패했습니다.")); } }
        finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr); }
    }
}
static class UI {
    public static Color Ink=Color.FromArgb(30,42,59), Muted=Color.FromArgb(101,115,134), Blue=Color.FromArgb(44,102,234), Bg=Color.FromArgb(245,247,251);
    public static Button Button(string text,EventHandler click,bool primary=false) { var b=new Button { Text=text,AutoSize=true,Height=38,MinimumSize=new Size(100,38),FlatStyle=FlatStyle.Flat,BackColor=primary?Blue:Color.White,ForeColor=primary?Color.White:Ink,Padding=new Padding(10,0,10,0),Cursor=Cursors.Hand,Margin=new Padding(0,0,10,0) }; b.FlatAppearance.BorderColor=Color.FromArgb(218,225,236); b.Click+=click; return b; }
    public static Label Label(string text,int size=10) { return new Label { Text=text,AutoSize=true,ForeColor=Ink,Font=new Font(L.FontName,size),Margin=new Padding(0,0,0,10),MaximumSize=new Size(650,0) }; }
    public static FlowLayoutPanel Column() { return new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Margin=Padding.Empty,Padding=new Padding(24),BackColor=Color.White }; }
    public static void Open(string target) { Process.Start(new ProcessStartInfo(target) { UseShellExecute=true }); }
}
sealed class Provider {
    public string Name { get; set; }
    public string OriginalDescription { get; private set; }
    public string Description { get { return ConfigText.Service(Name,OriginalDescription); } set { OriginalDescription=value; } }
    public override string ToString() { return Description; }
}
sealed class Wizard : Form {
    readonly Engine engine; readonly string remote; readonly string type; readonly bool allQuestions; readonly string displayName;
    FlowLayoutPanel panel=UI.Column(); Button next; Control input; Dictionary<string,object> current; Label title,help; bool busy,complete,cancelRequested;
    public bool CleanupSafe { get; private set; }
    public Wizard(Engine e,string name,string backend,string label,bool advanced) {
        CleanupSafe=true; engine=e; remote=name; type=backend; displayName=label; allQuestions=advanced||(backend!="drive"&&backend!="onedrive"); Text=L.T("로그인 / 인증 · 2단계")+" · "+label; Size=new Size(740,650); MinimumSize=Size; StartPosition=FormStartPosition.CenterParent; Font=new Font(L.FontName,10); Controls.Add(panel);
        Shown+=async (s,a)=> { if(engine!=null) await Advance(true); else Render(); };
        FormClosing+=(s,a)=> { if(busy) { cancelRequested=true; a.Cancel=true; return; } if(!complete) DialogResult=DialogResult.Cancel; };
    }
    internal static Wizard Preview(bool drives=false,bool shared=false) {
        var preview=new Wizard(null,"preview",shared?"drive":"onedrive",L.T("내 원드라이브"),false);
        preview.current=J.Obj("State","preview","Option",J.Obj("Name","config_is_local","Help","Use web browser to automatically authenticate rclone with remote?\n * Say Y if the machine running rclone has a web browser you can use\n * Say N if running rclone on a (remote) machine without web browser access\nIf not sure try Y. If Y failed, try N.\n","Type","bool","Default",true,"Exclusive",true,"Examples",new object[]{J.Obj("Value","true","Help","Yes"),J.Obj("Value","false","Help","No")}));
        if(drives) preview.current=J.Obj("State","preview","Option",J.Obj("Name","config_driveid","Help","Select drive you want to use","Type","string","Required",true,"Exclusive",true,"Default","sample-personal-01","Examples",new object[]{J.Obj("Value","sample-personal-01","Help","OneDrive (personal)"),J.Obj("Value","sample-work-02","Help","Work files (business)"),J.Obj("Value","sample-library-03","Help","Team Documents (documentLibrary)"),J.Obj("Value","sample-unknown-04","Help","")}));
        if(shared) preview.current=J.Obj("State","preview","Option",J.Obj("Name","config_shared_client_id","Help","rclone's shared Google Drive client_id is being retired and will stop working during 2026.\nCreate your own to avoid interruption: https://rclone.org/drive/#making-your-own-client-id\n\nContinue using the shared client_id anyway?","Type","bool","Default",false,"Exclusive",true,"Examples",new object[]{J.Obj("Value","true","Help","Yes"),J.Obj("Value","false","Help","No")}));
        preview.Text=L.Pair("화면 점검용 예시 · ","UI preview · ")+preview.Text;
        return preview;
    }
    async Task Advance(bool first) {
        string answer="";
        if(!first) {
            if(input is ComboBox) { var combo=(ComboBox)input; var item=combo.SelectedItem as Choice; answer=item!=null?item.Value:combo.Text; }
            else answer=input.Text;
            var option=J.Map(J.Get(current,"Option")); if(answer.Length==0) answer=J.Str(option,"Default");
            if(J.Bool(option,"Required")&&answer.Length==0) { MessageBox.Show(this,L.T("값을 입력하거나 목록에서 선택하세요.")); return; }
        }
        busy=true; if(next!=null) next.Enabled=false; UseWaitCursor=true;
        try {
            var opt=J.Obj("nonInteractive",true,"all",allQuestions,"obscure",true);
            if(!first) { opt["continue"]=true; opt["state"]=J.Str(current,"State"); opt["result"]=answer; }
            panel.Controls.Clear(); StepHeader(); panel.Controls.Add(UI.Label(displayName+L.T("에 로그인해 주세요"),18)); panel.Controls.Add(UI.Label(L.T("브라우저에 열린 서비스의 로그인 화면에서 인증을 완료해 주세요.\n로그인이 끝나면 다음 단계가 자동으로 표시됩니다.")));
            panel.Controls.Add(UI.Button(L.Pair("취소","Cancel"),(s,a)=> { cancelRequested=true; }));
            CleanupSafe=false;
            current=await engine.Job(first?"config/create":"config/update",first?J.Obj("name",remote,"type",type,"parameters",J.Obj(),"opt",opt):J.Obj("name",remote,"parameters",J.Obj(),"opt",opt),()=>cancelRequested);
            CleanupSafe=true;
            if(String.IsNullOrEmpty(J.Str(current,"State"))) { complete=true; busy=false; DialogResult=DialogResult.OK; Close(); return; }
            Render();
        } catch(ConfirmedJobCancellation) { CleanupSafe=true; busy=false; DialogResult=DialogResult.Cancel; Close(); }
        catch(Exception e) { ErrorDialog.ShowError(this,e.Message); panel.Controls.Clear(); panel.Controls.Add(UI.Label(L.T("연결 설정을 완료하지 못했습니다"),18)); panel.Controls.Add(UI.Label(L.T("인증 또는 입력값을 확인한 뒤 계정 추가를 다시 시도하세요. 비밀값 보호를 위해 서버 응답 원문은 표시하지 않습니다."))); var close=UI.Button(L.T("닫기"),(s,a)=>Close()); panel.Controls.Add(close); }
        finally { busy=false; UseWaitCursor=false; if(next!=null) next.Enabled=true; }
    }
    void Render() {
        panel.Controls.Clear(); StepHeader(); var o=J.Map(J.Get(current,"Option")); string key=J.Str(o,"Name");
        title=UI.Label(ConfigText.Title(o,type),18); panel.Controls.Add(title);
        if((key=="config_shared_client_id"||key=="client_id"||key=="client_secret")&&(type=="drive"||type=="google photos"||type=="googlephotos")) {
            var easyGuide=new LinkLabel { Text=L.Pair("쉽게 따라해 보세요! — Google 앱 ID 만들기","Follow along! — Create your Google app ID"),AutoSize=true,Margin=new Padding(0,0,0,10) };
            easyGuide.LinkClicked+=(s,a)=>UI.Open("https://rclone-google-guide-261117.heejung1989.chatgpt.site"); panel.Controls.Add(easyGuide);
        }
        string error=J.Str(current,"Error"); if(error!="") { var l=UI.Label(L.T("입력값을 확인하세요. 이 단계에 맞는 값을 다시 선택하거나 입력해 주세요.")); l.ForeColor=Color.Firebrick; panel.Controls.Add(l); }
        string translatedHelp=ConfigText.Help(o,type);
        if(translatedHelp.Length>850 || translatedHelp.Split('\n').Length>10) {
            panel.Controls.Add(new TextBox { Text=translatedHelp.Replace("\r\n","\n").Replace("\n",Environment.NewLine),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Width=635,Height=190,BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle });
        } else { help=UI.Label(translatedHelp); help.MaximumSize=new Size(640,0); panel.Controls.Add(help); }
        if(L.Korean || key=="config_driveid") {
            var raw=new TextBox { Text=ConfigText.Raw(o),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Width=635,Height=180,Visible=false,BackColor=UI.Bg };
            var toggle=new LinkLabel { Text=L.Pair("원문 도움말과 선택지 보기 (영어)","Show original help and choices"),AutoSize=true,Margin=new Padding(0,3,0,12) };
            toggle.LinkClicked+=(s,a)=> { raw.Visible=!raw.Visible; toggle.Text=raw.Visible?L.Pair("원문 도움말 접기","Hide original help"):L.Pair("원문 도움말과 선택지 보기 (영어)","Show original help and choices"); }; panel.Controls.Add(toggle); panel.Controls.Add(raw);
        }
        if((key=="config_shared_client_id"||key=="client_id"||key=="client_secret")&&(type=="drive"||type=="google photos"||type=="googlephotos")) {
            var docs=new LinkLabel { Text=L.Pair("앱 ID 발급 방법 보기 (공식 안내)","How to create an app ID (official guide)"),AutoSize=true,Margin=new Padding(0,6,0,10) };
            docs.LinkClicked+=(s,a)=>UI.Open(type=="drive"?"https://rclone.org/drive/#making-your-own-client-id":"https://rclone.org/googlephotos/#making-your-own-client-id"); panel.Controls.Add(docs);
        }
        string guidance=ConfigText.Guidance(o,type); if(guidance.Length>0) { panel.Controls.Add(UI.Label(L.Pair("설정 안내","Setup guidance"),11)); var note=UI.Label(guidance,9); note.MinimumSize=new Size(635,0); note.MaximumSize=new Size(635,0); note.BackColor=UI.Bg; note.Padding=new Padding(10); panel.Controls.Add(note); }
        bool password=WizardGuidance.IsSecret(o); var examples=J.Items(J.Get(o,"Examples")).Select(J.Map).ToList();
        if(!password && (examples.Count>0 || J.Str(o,"Type")=="bool")) {
            var combo=new ComboBox { Width=635,DropDownStyle=J.Bool(o,"Exclusive")?ComboBoxStyle.DropDownList:ComboBoxStyle.DropDown,DropDownWidth=850 };
            foreach(var ex in examples) combo.Items.Add(new Choice { Value=J.Str(ex,"Value"),Help=ConfigText.Choice(o,J.Str(ex,"Value"),J.Str(ex,"Help"),type),OriginalHelp=J.Str(ex,"Help") });
            if(combo.Items.Count==0) { combo.Items.Add(new Choice { Value="true",Help=ConfigText.Choice(o,"true","Yes",type) }); combo.Items.Add(new Choice { Value="false",Help=ConfigText.Choice(o,"false","No",type) }); }
            string def=J.Str(o,"Default"); foreach(Choice item in combo.Items) if(item.Value.Equals(def,StringComparison.OrdinalIgnoreCase)) combo.SelectedItem=item;
            if(combo.SelectedIndex<0 && combo.DropDownStyle!=ComboBoxStyle.DropDownList) combo.Text=def;
            input=combo;
        } else input=new TextBox { Width=635,UseSystemPasswordChar=password,Text=J.Str(o,"Default") };
        panel.Controls.Add(input);
        var choices=input as ComboBox;
        if(choices!=null) { string defaultValue=J.Str(o,"Default"); for(int index=0;index<choices.Items.Count;index++) if(((Choice)choices.Items[index]).Value.Equals(defaultValue,StringComparison.OrdinalIgnoreCase)) { choices.SelectedIndex=index; break; } }
        if(key=="config_driveid") {
            var details=UI.Label("",10); details.MaximumSize=new Size(635,0); details.Padding=new Padding(12); details.BackColor=UI.Bg;
            var combo=input as ComboBox;
            Action update=()=> { var selected=combo==null?null:combo.SelectedItem as Choice; details.Text=selected==null?L.Pair("연결할 저장소를 목록에서 선택하세요.","Select a storage location from the list."):ConfigText.DriveGuide(selected.OriginalHelp); };
            if(combo!=null) combo.SelectedIndexChanged+=(s,a)=>update(); update(); panel.Controls.Add(details);
            panel.Controls.Add(UI.Label(L.Pair("저장소를 고르고 ‘다음’을 누른 뒤 확인 화면에서 이름과 종류가 맞는지 확인하세요. 목록만으로는 어느 저장소인지 확정할 수 없습니다.","Choose a location, click Next, then verify its name and type on the confirmation screen. The list alone may not identify the storage you want."),9));
        } else {
            if(input is ComboBox && examples.Any(ex=>!String.IsNullOrWhiteSpace(J.Str(ex,"Help")) && J.Str(ex,"Help")!="Yes" && J.Str(ex,"Help")!="No")) {
                var combo=(ComboBox)input;
                panel.Controls.Add(UI.Label(L.Pair("선택지 설명","About this choice"),10));
                var selectedHelp=new TextBox { Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Width=635,Height=100,BackColor=UI.Bg,BorderStyle=BorderStyle.None };
                Action update=()=> { var choice=combo.SelectedItem as Choice; selectedHelp.Text=choice==null?"":HelpTranslations.Text(choice.OriginalHelp).Replace("\r\n","\n").Replace("\n",Environment.NewLine); };
                combo.SelectedIndexChanged+=(s,a)=>update(); update(); panel.Controls.Add(selectedHelp);
            }
            panel.Controls.Add(UI.Label(WizardGuidance.InputHint(o,input is ComboBox),9));
        }
        next=UI.Button(L.T("다음 →"),async(s,a)=>await Advance(false),true); next.Enabled=engine!=null; panel.Controls.Add(next); input.Focus();
    }
    void StepHeader() { var steps=UI.Label(L.T("✓ 서비스 선택    →    ② 로그인 / 인증    →    ③ 드라이브 연결"),11); steps.ForeColor=UI.Blue; panel.Controls.Add(steps); panel.Controls.Add(UI.Label(displayName,10)); }
    sealed class Choice { public string Value,Help,OriginalHelp; public override string ToString() { return Help; } }
}
sealed class MainWindow : Form {
    Engine engine; bool ready,busy,exit,refreshing; readonly string data;
    readonly LocalSettingsStore localSettings=new LocalSettingsStore();
    List<Provider> providers=new List<Provider>(); Dictionary<string,string> mounted=new Dictionary<string,string>();
    ListView list; Label status,summary,dependencyHint; Button mountButton; ComboBox letter; TextBox path; NumericUpDown cache; CheckBox readOnly; FlowLayoutPanel actions; NotifyIcon tray; System.Windows.Forms.Timer timer;
    public MainWindow() {
        data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CloudDock");
        Text=L.T("구름드라이브 · 클라우드를 내 드라이브로"); Size=new Size(1120,760); MinimumSize=new Size(950,650); StartPosition=FormStartPosition.CenterScreen; Font=new Font(L.FontName,10); BackColor=UI.Bg;
        var shell=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2,RowCount=1 }; shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,205)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); Controls.Add(shell);
        var sidebar=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty,BackColor=Color.FromArgb(23,35,55) };
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); sidebar.RowStyles.Add(new RowStyle(SizeType.Percent,100)); sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize)); shell.Controls.Add(sidebar,0,0);
        var nav=UI.Column(); nav.BackColor=sidebar.BackColor; nav.Margin=Padding.Empty; nav.Padding=new Padding(22,30,12,0); sidebar.Controls.Add(nav,0,0);
        var brand=UI.Label(L.Pair("구름드라이브","Gureum Drive"),17); brand.ForeColor=Color.White; nav.Controls.Add(brand); var sub=UI.Label(L.T("나의 클라우드 작업 공간"),9); sub.ForeColor=Color.LightSteelBlue; nav.Controls.Add(sub);
        nav.Controls.Add(UI.Button(L.T("내 드라이브"),(s,a)=>{})); nav.Controls.Add(UI.Button(L.T("MYBOX 연결 안내"),(s,a)=>Mybox())); nav.Controls.Add(UI.Button(L.T("데이터 폴더"),(s,a)=>UI.Open(data))); nav.Controls.Add(UI.Button(L.Pair("WinFsp 설치하기","Install WinFsp"),(s,a)=>ShowWinFspSetup())); nav.Controls.Add(UI.Button(L.T("rclone 정보"),(s,a)=>UI.Open("https://rclone.org/")));
        foreach(Control control in nav.Controls) { var button=control as Button; if(button!=null) { button.AutoSize=false; button.Size=new Size(160,40); button.Margin=new Padding(0,0,0,8); button.BackColor=Color.FromArgb(35,51,75); button.ForeColor=Color.White; button.FlatAppearance.BorderSize=0; } } var credits=UI.Label("rclone · MIT\n\nWinFsp - Windows File System Proxy, Copyright (C) Bill Zissimopoulos",8); credits.ForeColor=Color.LightSteelBlue; credits.MaximumSize=new Size(163,0); credits.Margin=new Padding(22,16,12,20); sidebar.Controls.Add(credits,0,1);
        var main=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(28),ColumnCount=1,RowCount=6 }; foreach(int h in new[]{90,52,0,190,52,36}) main.RowStyles.Add(h==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,h)); shell.Controls.Add(main,1,0);
        var header=UI.Column(); header.Padding=Padding.Empty; header.BackColor=UI.Bg; header.AutoScroll=false; header.Controls.Add(UI.Label(L.T("내 드라이브"),23)); summary=UI.Label(L.T("연결 준비 중…")); header.Controls.Add(summary); main.Controls.Add(header,0,0);
        actions=new FlowLayoutPanel { Dock=DockStyle.Fill }; actions.Controls.Add(UI.Button(L.T("＋ 클라우드 추가"),async(s,a)=>await Run(Add),true)); actions.Controls.Add(UI.Button(L.T("새로고침"),async(s,a)=>await Run(RefreshList))); main.Controls.Add(actions,0,1);
        list=new ListView { Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,BorderStyle=BorderStyle.None,BackColor=Color.White };
        list.Columns.Add(L.T("연결 이름"),230); list.Columns.Add(L.T("서비스"),150); list.Columns.Add(L.T("상태"),130); list.Columns.Add(L.T("드라이브"),95); list.SelectedIndexChanged+=(s,a)=>LoadMountSettings(); list.DoubleClick+=(s,a)=>OpenDrive(); main.Controls.Add(list,0,2);
        var settings=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(0,14,0,0),ColumnCount=4,RowCount=5 };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100)); settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100)); settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,135));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute,25)); settings.RowStyles.Add(new RowStyle(SizeType.Absolute,32)); settings.RowStyles.Add(new RowStyle(SizeType.AutoSize)); settings.RowStyles.Add(new RowStyle(SizeType.AutoSize)); settings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settings.Controls.Add(UI.Label(L.T("드라이브")),0,0); letter=new ComboBox { Width=88,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,0,12,0) }; for(char c='D';c<='Z';c++) letter.Items.Add(c+":"); letter.SelectedItem="Z:"; settings.Controls.Add(letter,0,1);
        settings.Controls.Add(UI.Label(L.T("원격 폴더")),1,0); path=new TextBox { Dock=DockStyle.Top,Margin=new Padding(0,0,16,0) }; settings.Controls.Add(path,1,1);
        settings.Controls.Add(UI.Label(L.T("캐시 GB")),2,0); cache=new NumericUpDown { Width=84,Minimum=1,Maximum=2048,Value=10,Margin=new Padding(0,0,16,0) }; settings.Controls.Add(cache,2,1);
        readOnly=new CheckBox { Text=L.T("읽기 전용"),AutoSize=true,Margin=new Padding(0,2,0,0) }; settings.Controls.Add(readOnly,3,1);
        var folderHint=UI.Label(L.Pair("원격 폴더는 드라이브의 시작 위치로 사용할 클라우드 안의 폴더입니다. 비워 두면 전체를 연결합니다.\n예: Documents/Work → 해당 폴더부터 표시합니다. 내 PC 경로가 아닙니다.","Remote folder is the folder inside your cloud to use as the drive root. Leave blank to connect the whole cloud.\nExample: Documents/Work shows that folder as the root. This is not a local PC path."),9); folderHint.MaximumSize=new Size(760,0); folderHint.ForeColor=UI.Muted; folderHint.Margin=new Padding(0,4,0,0); settings.Controls.Add(folderHint,0,2); settings.SetColumnSpan(folderHint,4);
        var hint=UI.Label(L.T("파일은 탐색기에서 열고 저장하세요. 캐시 한도는 목표값이며 열린 파일과 업로드 대기 파일로 초과할 수 있습니다.\n닫기 버튼은 트레이로 숨깁니다. 종료하려면 트레이 메뉴를 사용하세요."),9); hint.MaximumSize=new Size(760,0); hint.Margin=new Padding(0,8,0,0); settings.Controls.Add(hint,0,3); settings.SetColumnSpan(hint,4); dependencyHint=UI.Label("",9); dependencyHint.ForeColor=Color.FromArgb(151,85,0); settings.Controls.Add(dependencyHint,0,4); settings.SetColumnSpan(dependencyHint,4); main.Controls.Add(settings,0,3);
        var bottom=new FlowLayoutPanel { Dock=DockStyle.Fill }; mountButton=UI.Button(L.T("드라이브 연결"),async(s,a)=> { if(!WinFspSupport.CanMount) ShowWinFspSetup(); else await Run(Mount); },true); bottom.Controls.Add(mountButton); bottom.Controls.Add(UI.Button(L.T("연결 해제"),async(s,a)=>await Run(Unmount))); bottom.Controls.Add(UI.Button(L.T("탐색기 열기"),(s,a)=>OpenDrive())); bottom.Controls.Add(UI.Button(L.T("계정 제거"),async(s,a)=>await Run(Remove))); main.Controls.Add(bottom,0,4);
        status=UI.Label(L.T("시작하는 중…"),9); status.Dock=DockStyle.Fill; main.Controls.Add(status,0,5);
        var menu=new ContextMenuStrip(); menu.Items.Add(L.T("구름드라이브 열기"),null,(s,a)=>Restore()); menu.Items.Add(L.T("종료"),null,async(s,a)=>await Run(Quit));
        tray=new NotifyIcon { Icon=SystemIcons.Application,Text=L.Pair("구름드라이브","Gureum Drive"),Visible=true,ContextMenuStrip=menu }; tray.DoubleClick+=(s,a)=>Restore();
        FormClosing+=(s,a)=> {
            if(exit) return;
            if(a.CloseReason==CloseReason.WindowsShutDown) {
                // The bounded worker is read-only: a timeout cannot unmount drives later.
                if(!busy && ready && !engine.ConfigJobUnconfirmed) {
                    try {
                        var check=Task.Run(async()=> J.Items(J.Get(await engine.Call("mount/listmounts",J.Obj()),"mountPoints")).Any());
                        check.ContinueWith(t=> { var observed=t.Exception; },TaskContinuationOptions.OnlyOnFaulted);
                        if(check.Wait(1500) && !check.Result) { exit=true; return; }
                    } catch { }
                }
                a.Cancel=true; Restore();
                status.Text=L.Pair("Windows 종료를 보류했습니다. 파일을 닫고 트레이 메뉴에서 구름드라이브를 종료한 뒤 다시 시도하세요.","Windows shutdown deferred. Close your files, exit Gureum Drive from the tray, then try again.");
                return;
            }
            a.Cancel=true; Hide();
        }; FormClosed+=(s,a)=> { timer.Stop(); tray.Dispose(); if(engine!=null) engine.Dispose(); };
        timer=new System.Windows.Forms.Timer { Interval=8000 }; timer.Tick+=async(s,a)=> { if(ready&&!busy) await Run(RefreshList); };
        Shown+=async(s,a)=> { await Run(async()=> { engine=new Engine(data); await engine.Start(); providers=J.Items(J.Get(await engine.Call("config/providers",J.Obj()),"providers")).Select(J.Map).Select(p=>new Provider { Name=J.Str(p,"Name"),Description=J.Str(p,"Description") }).ToList(); ready=true; await RefreshList(); timer.Start(); if(!WinFspSupport.CanMount) ShowWinFspSetup(); }); };
        Activated+=(s,a)=> { if(mountButton!=null) UpdateDependencyState(); };
        UpdateDependencyState();
    }
    void ShowWinFspSetup() { using(var dialog=new WinFspSetup()) dialog.ShowDialog(this); UpdateDependencyState(); }
    void UpdateDependencyState() {
        if(mountButton==null) return;
        mountButton.Enabled=ready;
        mountButton.Text=WinFspSupport.CanMount?L.T("드라이브 연결"):L.Pair("WinFsp 설치하기","Install WinFsp");
        dependencyHint.Text=WinFspSupport.CanMount?"":WinFspSupport.Status+L.Pair(" · WinFsp 설치하기 버튼에서 안내를 확인하세요."," · Click Install WinFsp for instructions.");
    }
    void Restore() { Show(); WindowState=FormWindowState.Normal; Activate(); }
    async Task Run(Func<Task> operation) { if(busy) return; busy=true; UseWaitCursor=true; try { await operation(); } catch(Exception e) { status.Text=L.T("작업 실패"); ErrorDialog.ShowError(this,e.Message); } finally { busy=false; UseWaitCursor=false; } }
    void Require() { if(!ready) throw new Exception(L.T("rclone 엔진이 준비되지 않았습니다. rclone.exe를 확인한 뒤 앱을 다시 실행하세요.")); }
    string Selected() { if(list.SelectedItems.Count==0) throw new Exception(L.T("먼저 계정을 선택하세요.")); return list.SelectedItems[0].Name; }
    async Task RefreshList() {
        Require(); string selected=list.SelectedItems.Count>0?list.SelectedItems[0].Name:"";
        var remotes=J.Items(J.Get(await engine.Call("config/listremotes",J.Obj()),"remotes")).Select(Convert.ToString).ToList();
        var mounts=J.Items(J.Get(await engine.Call("mount/listmounts",J.Obj()),"mountPoints")); mounted.Clear();
        foreach(var m in mounts) { var d=J.Map(m); string mp=J.Str(d,"MountPoint"),fs=J.Str(d,"Fs"); if(mp!="") mounted[mp]=fs; }
        refreshing=true; list.BeginUpdate(); try { list.Items.Clear(); foreach(string r in remotes) { string n=r.TrimEnd(':'); var conf=await engine.Call("config/get",J.Obj("name",n)); string type=J.Str(conf,"type"); var p=providers.FirstOrDefault(x=>x.Name==type); string mp=mounted.FirstOrDefault(x=>x.Value.StartsWith(n+":",StringComparison.Ordinal)).Key;
            var item=new ListViewItem(new[]{Display(n),p==null?type:p.Description,mp==null?L.T("연결 안 됨"):L.T("● 연결됨"),mp??"—"}); item.Name=n; item.Tag=type; list.Items.Add(item); if(n==selected) item.Selected=true;
        } } finally { list.EndUpdate(); refreshing=false; } summary.Text=remotes.Count+L.T("개 계정  ·  ")+mounted.Count+L.T("개 드라이브 연결됨"); status.Text=L.T("준비됨 · ")+providers.Count+L.T("개 rclone 서비스 · ")+DateTime.Now.ToString("HH:mm")+" · "+WinFspSupport.Status; UpdateDependencyState();
    }
    Dictionary<string,object> ReadLocalSettings(string file,bool forWrite=false) {
        var result=localSettings.Read(file,forWrite); if(localSettings.Warning!="") status.Text=localSettings.Warning; return result;
    }
    string NamesFile { get { return Path.Combine(data,"names.json"); } }
    Dictionary<string,object> ReadNames() { return ReadLocalSettings(NamesFile); }
    string Display(string remote) { var names=ReadNames(); string value=J.Str(names,remote); return value.Length==0?remote:value; }
    void WriteName(string remote,string display) { var names=ReadLocalSettings(NamesFile,true); names[remote]=display; var temp=NamesFile+".tmp"; File.WriteAllText(temp,J.Serializer().Serialize(names)); if(File.Exists(NamesFile)) File.Replace(temp,NamesFile,null); else File.Move(temp,NamesFile); }
    async Task Add() {
        Require(); await engine.EnsureConfigIdle(); using(var dialog=new ConnectionSetup(providers)) {
            dialog.MyboxRequested+=(s,a)=>Mybox();
            if(dialog.ShowDialog(this)!=DialogResult.OK) return;
            string key=dialog.SelectedProvider.Name; string remote=key; int suffix=1;
            var existing=J.Items(J.Get(await engine.Call("config/listremotes",J.Obj()),"remotes")).Select(x=>Convert.ToString(x).TrimEnd(':')).ToList();
            while(existing.Any(x=>x.Equals(remote,StringComparison.OrdinalIgnoreCase))) remote=key+"-"+(++suffix);
            bool success,cleanupSafe;
            using(var wizard=new Wizard(engine,remote,key,dialog.DisplayName,dialog.Advanced)) { success=wizard.ShowDialog(this)==DialogResult.OK; cleanupSafe=wizard.CleanupSafe; }
            if(!success && !cleanupSafe) { status.Text=L.Pair("인증 완료 여부를 확인하지 못해 계정 설정을 보존했습니다.","Authentication completion is unknown. Account settings were preserved."); return; }
            if(!success) { var saved=J.Items(J.Get(await engine.Call("config/listremotes",J.Obj()),"remotes")).Select(Convert.ToString); if(saved.Any(x=>x.TrimEnd(':')==remote)) await engine.Call("config/delete",J.Obj("name",remote)); await RefreshList(); return; }
            WriteName(remote,dialog.DisplayName); await RefreshList();
            foreach(ListViewItem item in list.Items) if(item.Name==remote) { item.Selected=true; item.EnsureVisible(); }
            if(!WinFspSupport.CanMount) { ShowWinFspSetup(); status.Text=L.Pair("계정을 저장했습니다. WinFsp 설치 후 다시 실행하면 연결할 수 있습니다.","Connection saved. Install WinFsp and restart Gureum Drive to connect the drive."); return; }
            using(var setup=new DriveSetup(dialog.DisplayName)) {
                if(setup.ShowDialog(this)!=DialogResult.OK) { status.Text=L.T("계정을 추가했습니다. 나중에 목록에서 선택한 뒤 ‘드라이브 연결’을 누르세요."); return; }
                letter.SelectedItem=setup.Letter.SelectedItem; path.Text=setup.Folder.Text; cache.Value=setup.Cache.Value; readOnly.Checked=setup.ReadOnly.Checked;
                await Mount();
            }
        }
    }
    string SettingsFile { get { return Path.Combine(data,"mounts.json"); } }
    Dictionary<string,object> ReadSettings() { return ReadLocalSettings(SettingsFile); }
    void LoadMountSettings() { if(refreshing || list.SelectedItems.Count==0) return; try { var all=ReadSettings(); var d=J.Map(J.Get(all,list.SelectedItems[0].Name)); if(d.Count==0) return; letter.SelectedItem=J.Str(d,"letter"); path.Text=J.Str(d,"path"); cache.Value=Convert.ToDecimal(J.Get(d,"cache")??10); readOnly.Checked=J.Bool(d,"readOnly"); } catch { status.Text=L.T("저장된 연결 설정을 읽지 못했습니다."); } }
    async Task Mount() {
        Require(); if(!WinFspSupport.CanMount) { ShowWinFspSetup(); return; } string n=Selected(); if(mounted.Any(x=>x.Value.StartsWith(n+":",StringComparison.Ordinal))) throw new Exception(L.T("이 계정은 이미 연결되어 있습니다.")); string mp=Convert.ToString(letter.SelectedItem); if(mp=="") throw new Exception(L.T("드라이브 문자를 선택하세요.")); if(Directory.Exists(mp+"\\")||mounted.ContainsKey(mp)) throw new Exception(L.T("이미 사용 중인 드라이브 문자입니다."));
        var settings=ReadLocalSettings(SettingsFile,true);
        string fs=n+":"+path.Text.Trim(); status.Text=L.T("드라이브 연결 중…");
        Exception failure=null;
        try {
        await engine.Job("mount/mount",J.Obj("fs",fs,"mountPoint",mp,"mountOpt",J.Obj("NetworkMode",true,"VolName","CloudDock-"+n+"-"+mp.TrimEnd(':')),"vfsOpt",J.Obj("CacheMode",3,"CacheMaxSize",(long)cache.Value*1024*1024*1024,"ReadOnly",readOnly.Checked)));
        settings=ReadLocalSettings(SettingsFile,true); settings[n]=J.Obj("letter",mp,"path",path.Text.Trim(),"cache",cache.Value,"readOnly",readOnly.Checked); string temp=SettingsFile+".tmp"; File.WriteAllText(temp,J.Serializer().Serialize(settings)); if(File.Exists(SettingsFile)) File.Replace(temp,SettingsFile,null); else File.Move(temp,SettingsFile);
        } catch(Exception e) { failure=e; }
        await RefreshList(); if(failure!=null) throw failure;
    }
    bool ConfirmPending() {
        return MessageBox.Show(this,L.Pair("미전송 또는 실패한 변경 사항이 있습니다. 원본 파일을 닫고 백업했는지 확인하세요. 계속하면 업로드가 중단됩니다. 캐시는 보존되지만 클라우드 반영은 보장되지 않습니다. 재시도하려면 같은 계정·원격 폴더·캐시 설정으로 다시 연결해야 합니다. 계속할까요?","Unsent or failed changes remain. Close and back up your original files first. Continuing interrupts uploads. Cache is preserved but cloud changes are not guaranteed. Retrying requires reconnecting with the same account, remote folder and cache settings. Continue?"),L.Pair("미전송 변경 사항 확인","Confirm unsent changes"),MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes;
    }
    async Task Unmount() {
        Require(); string n=Selected(); var entries=mounted.Where(x=>x.Value.StartsWith(n+":",StringComparison.Ordinal)).ToList();
        foreach(var p in entries) if(await engine.Pending(p.Value)) { if(!ConfirmPending()) return; break; }
        Exception failure=null; try { foreach(var p in entries) await engine.Job("mount/unmount",J.Obj("mountPoint",p.Key)); }
        catch(Exception e) { failure=e; }
        await RefreshList(); if(failure!=null) throw failure;
    }
    void OpenDrive() { try { string n=Selected(); string mp=mounted.FirstOrDefault(x=>x.Value.StartsWith(n+":",StringComparison.Ordinal)).Key; if(mp==null) throw new Exception(L.T("계정을 먼저 연결하세요.")); UI.Open(mp+"\\"); } catch(Exception e) { ErrorDialog.ShowError(this,e.Message); } }
    async Task Remove() {
        Require(); await engine.EnsureConfigIdle(); await RefreshList(); string n=Selected();
        if(mounted.Any(x=>x.Value.StartsWith(n+":",StringComparison.Ordinal))) throw new Exception(L.T("드라이브 연결을 먼저 해제하세요."));
        bool pending=PendingState.RemoteCacheBusy(Path.Combine(data,"cache","vfsMeta"),n);
        string warning=pending?L.Pair("미전송 또는 확인할 수 없는 캐시가 남아 있습니다. 설정을 제거하면 자동 재전송이 불가능해질 수 있습니다. 원본과 캐시를 백업했는지 확인하세요. 캐시 파일은 지우지 않습니다. 그래도 계정 설정을 제거할까요?","Unsent or unverified cache remains. Removing settings may prevent automatic retries. Back up originals and cache first. Cache files will not be deleted. Remove account settings anyway?"):"'"+n+L.T("' 계정 연결 설정을 제거할까요? 클라우드 파일은 삭제되지 않습니다.");
        if(MessageBox.Show(this,warning,L.T("계정 제거"),MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes) return;
        await engine.Call("config/delete",J.Obj("name",n)); await RefreshList();
    }
    async Task Quit() { if(ready) { await RefreshList(); foreach(var p in mounted) if(await engine.Pending(p.Value)) { if(!ConfirmPending()) return; break; } await engine.Job("mount/unmountall",J.Obj()); } exit=true; Close(); }
    void Mybox() { using(var f=new Form { Text=L.T("MYBOX 연결"),Size=new Size(720,520),StartPosition=FormStartPosition.CenterParent,Font=Font }) { var col=UI.Column(); f.Controls.Add(col); col.Controls.Add(UI.Label(L.T("네이버 MYBOX · 중계 연결"),18)); col.Controls.Add(UI.Label(L.T("MYBOX는 rclone 기본 지원 서비스가 아닙니다. 현재 버전은 외부 S3 중계기를 실행한 후 S3 서비스로 연결하는 방식을 제공합니다.\n\n1. MYBOX에서 개인 액세스 토큰 발급\n2. mybox-s3 중계기 설치 및 실행\n3. 계정 추가 → s3 → Other 선택\n4. 중계기의 endpoint, access key, secret key 입력\n5. 연결할 원격 폴더에 중계기의 bucket 이름 입력\n\n중계기 자동 설치와 직접 API 연동은 아직 구현되지 않았습니다. 실제 MYBOX 계정의 파일 편집 호환성도 검증 전입니다. 토큰은 만료 시 갱신해야 합니다."))); col.Controls.Add(UI.Button(L.T("MYBOX API 안내"),(s,a)=>UI.Open("https://developers.mybox.naver.com/getting-started"))); col.Controls.Add(UI.Button(L.T("mybox-s3 소스"),(s,a)=>UI.Open("https://github.com/chiwanpark/mybox-s3"))); f.ShowDialog(this); } }
}
static class Program {
    [STAThread] static void Main(string[] args) { L.Initialize(args.FirstOrDefault(x=>x.StartsWith("--lang="))==null?null:args.First(x=>x.StartsWith("--lang=")).Substring(7));
        if(args.Any(x=>x.StartsWith("--render-wizard="))) {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            using(var form=Wizard.Preview(false,true)) { form.Show(); Application.DoEvents(); var combo=form.Controls.OfType<FlowLayoutPanel>().SelectMany(p=>p.Controls.OfType<ComboBox>()).First(); if(combo.SelectedIndex<0) throw new Exception("Preview default selection missing"); using(var bitmap=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height)); bitmap.Save(args.First(x=>x.StartsWith("--render-wizard=")).Substring(16)); } form.Close(); } return;
        }
        if(args.Contains("--preview=pending")) { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new ErrorDialog(L.T("파일이 열려 있거나 업로드가 대기·진행·실패 상태입니다. 파일을 닫고 업로드 완료 후 다시 시도하세요."))); return; }
        if(args.Any(x=>x.StartsWith("--check-localization="))) { try { LocalizationChecks.Run(args.First(x=>x.StartsWith("--check-localization=")).Substring(21)); } catch(Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"localization-test.txt"),"FAIL: "+ex); Environment.ExitCode=1; } return; }
        if(args.Any(x=>x.StartsWith("--preview="))) { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); string screen=args.First(x=>x.StartsWith("--preview=")).Substring(10); if(screen=="dependency") { Application.Run(new WinFspSetup()); return; } if(screen=="error") { Application.Run(new ErrorDialog("failed to mount FUSE fs: mount stopped before calling Init: mount failed: cgofuse: cannot find winfsp\nHint: Install WinFsp from https://winfsp.dev/rel/")); return; } if(screen=="wizard"||screen=="drives") Application.Run(Wizard.Preview(screen=="drives")); else if(screen=="drive") Application.Run(new DriveSetup(L.T("내 원드라이브"))); else Application.Run(new ConnectionSetup(new System.Collections.Generic.List<Provider>{new Provider { Name="drive",Description="Google Drive" },new Provider { Name="onedrive",Description="Microsoft OneDrive" },new Provider { Name="webdav",Description="WebDAV" }})); return; }
        if(args.Contains("--self-test")) { try { Smoke().GetAwaiter().GetResult(); Environment.ExitCode=0; } catch(Exception e) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),"FAIL: "+e); Environment.ExitCode=1; } return; }
        bool created; using(var mutex=new System.Threading.Mutex(true,"Local\\CloudDock-"+WindowsIdentity.GetCurrent().User.Value,out created)) { if(!created) { MessageBox.Show(L.T("구름드라이브가 이미 실행 중입니다. 트레이 아이콘을 열어 주세요.")); return; } Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); try { Application.Run(new MainWindow()); } catch(Exception e) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-error.txt"),e.ToString()); ErrorDialog.ShowError(null,e.Message); } }
    }
    static async Task Smoke() {
        string folder=Path.Combine(Path.GetTempPath(),"CloudDock-smoke-"+Guid.NewGuid().ToString("N"));
        using(var e=new Engine(folder)) { await e.Start(); var providers=await e.Call("config/providers",J.Obj()); int count=J.Items(J.Get(providers,"providers")).Count(); if(count<30) throw new Exception("Provider discovery failed");
            var q=await e.Job("config/create",J.Obj("name","smoke","type","sftp","parameters",J.Obj(),"opt",J.Obj("all",true,"nonInteractive",true,"obscure",true)));
            if(J.Str(q,"State")==""||J.Map(J.Get(q,"Option")).Count==0) throw new Exception("Wizard state missing");
            var next=await e.Job("config/update",J.Obj("name","smoke","parameters",J.Obj(),"opt",J.Obj("all",true,"nonInteractive",true,"continue",true,"state",J.Str(q,"State"),"result","localhost")));
            if(J.Str(next,"State")=="") throw new Exception("Wizard continuation failed");
            await e.Call("config/delete",J.Obj("name","smoke"));
            await e.Job("config/create",J.Obj("name","localtest","type","local","parameters",J.Obj(),"opt",J.Obj("nonInteractive",true)));
            await e.Call("operations/list",J.Obj("fs","localtest:"+folder.Replace('\\','/'),"remote",""));
            var list=await e.Call("config/listremotes",J.Obj()); if(!J.Items(J.Get(list,"remotes")).Any(x=>Convert.ToString(x).TrimEnd(':')=="localtest")) throw new Exception("Remote missing");
            await e.Call("config/delete",J.Obj("name","localtest")); await e.Call("mount/listmounts",J.Obj());
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"self-test.txt"),"PASS: engine startup, authenticated RC, "+count+" providers, configuration wizard + continuation, local listing, remote create/delete, mount inventory.\r\nNo cloud credentials or cloud files used. Test data: "+folder);
        }
    }
}
}

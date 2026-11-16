using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace CloudDock {
static class L {
    public static bool Korean { get; private set; }
    static Dictionary<string,object> english;
    public static void Initialize(string locale) {
        var culture=String.IsNullOrEmpty(locale)?CultureInfo.CurrentUICulture:CultureInfo.GetCultureInfo(locale);
        Korean=culture.TwoLetterISOLanguageName=="ko";
        Thread.CurrentThread.CurrentUICulture=CultureInfo.GetCultureInfo(Korean?"ko-KR":"en-US");
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CloudDock.Strings.en.json"))
        using(var reader=new StreamReader(stream)) english=J.Map(J.Serializer().DeserializeObject(reader.ReadToEnd()));
    }
    public static string T(string source) { if(Korean) return source; object value; return english!=null&&english.TryGetValue(source,out value)?Convert.ToString(value):source; }
    public static string Pair(string ko,string en) { return Korean?ko:en; }
    public static string Format(string ko,string en,params object[] values) { return String.Format(CultureInfo.CurrentCulture,Pair(ko,en),values); }
    public static string FontName { get { return Korean?"맑은 고딕":"Segoe UI"; } }
}
}

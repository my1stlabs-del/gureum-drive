using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudDock {
sealed class ConfirmedJobCancellation : OperationCanceledException {}
static class RcJob {
    public static async Task<Dictionary<string,object>> Wait(Func<string,Dictionary<string,object>,Task<Dictionary<string,object>>> call,object id,Func<bool> cancelled,int delay=250) {
        bool stopSent=false;
        for(;;) {
            await Task.Delay(delay);
            var job=await call("job/status",J.Obj("jobid",id));
            bool cancel=cancelled!=null && cancelled();
            if(J.Bool(job,"finished")) {
                if(stopSent || cancel) throw new ConfirmedJobCancellation();
                if(!J.Bool(job,"success")) throw new Exception(J.Str(job,"error"));
                return J.Map(J.Get(job,"output"));
            }
            if(cancel && !stopSent) {
                bool stopFailed=false;
                try { await call("job/stop",J.Obj("jobid",id)); stopSent=true; }
                catch(Exception) { stopFailed=true; }
                if(stopFailed) {
                    // A failed stop request is not proof the job stopped. Verify before cleanup.
                    var after=await call("job/status",J.Obj("jobid",id));
                    if(J.Bool(after,"finished")) throw new ConfirmedJobCancellation();
                    throw new InvalidOperationException(L.Pair("인증 작업 중단을 확인하지 못했습니다. 설정을 보존하며 작업 완료 전 새 인증을 차단합니다.","Authentication stop could not be confirmed. Settings are preserved and new authentication is blocked until the job finishes."));
                }
            }
        }
    }
}
}

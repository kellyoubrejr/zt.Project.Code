using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;

namespace Kingdee.Zitn.Project.Code.Interface.ToOther
{
    /// <summary>
    /// 其他系统服务接口
    /// </summary>
    public class OtherService : AbstractWebApiBusinessService
    {
        public OtherService(KDServiceContext context)
            : base(context)
        {
        }

    }
}

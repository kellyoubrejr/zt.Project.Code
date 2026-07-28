using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;

namespace Kingdee.Zitn.Project.Code.Interface.ToBPM
{
    /// <summary>
    /// BPM服务接口
    /// </summary>
    public class BpmService : AbstractWebApiBusinessService
    {
        public BpmService(KDServiceContext context)
            : base(context)
        {
        }

    }
}

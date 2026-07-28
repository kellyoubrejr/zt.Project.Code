using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;

namespace Kingdee.Zitn.Project.Code.Interface.ToPLM
{
    /// <summary>
    /// PLM服务接口
    /// </summary>
    public class PlmService : AbstractWebApiBusinessService
    {
        public PlmService(KDServiceContext context)
            : base(context)
        {
        }

    }
}

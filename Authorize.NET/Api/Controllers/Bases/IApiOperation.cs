namespace AuthorizeNet.Api.Controllers.Bases
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /**
     * @author ramittal
     *
     */
#pragma warning disable 1591
    public interface IApiOperation<TQ, TS>  
        where TQ : Contracts.V1.ANetApiRequest 
        where TS : Contracts.V1.ANetApiResponse
	{
        TS GetApiResponse();
        Contracts.V1.ANetApiResponse GetErrorResponse();
        Task<TS> ExecuteWithApiResponseAsync(Environment environment = null);
        Task ExecuteAsync(Environment environment = null);
        Contracts.V1.messageTypeEnum GetResultCode();
        List<string> GetResults();
    }
#pragma warning restore 1591
}
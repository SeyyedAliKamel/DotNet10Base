using Application.Interfaces.Contexts;
using Common.Dto;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Infrastructure.ExternalApi.PaymentGatewaye
{
    public interface IPaymentGatewayeService
    {
        ResultDto SendViaToken(SepTxn request);
    }
    public class PaymentGatewayeService: IPaymentGatewayeService
    {
        private readonly IDataBaseContext _context;
        public ResultDto SendViaToken (SepTxn txn)
        {
            string urlAddress = "https://sep.shaparak.ir/onlinepg/onlinepg";
            var restCliet = new RestClient(urlAddress);
            var request = new RestRequest("payment", Method.Post)
            {
                RequestFormat = DataFormat.Json,
                OnBeforeDeserialization = resp =>
                {
                    resp.ContentType = "application/json";
                }
            };
            txn.Action = "token";
            request.AddBody(txn);
            
            var sepResult = restCliet.Execute(request);
            var sepPgToken = (IDictionary<string,object>)JsonConvert.DeserializeObject(sepResult.Content);
            return new ResultDto
            {
                Result = sepPgToken,
                IsSuccess = true,
                Status = 200,
                Message = "اطلاعات ارسال شد"
            };

        }
    }
    public class SepTxn
    {
        public string Action { get; set; }
        public string TerminalId { get; set; }
        public string RedirectUrl { get; set; }
        public string ResNum { get; set; }
        public long Amount { get; set; }
        public long? CellNumber { get; set; }
        public List<IBanInfo>? SettleMentIBANInfo { get; set; }
    }

    public class IBanInfo
    {
        public string IBAN { get; set; }
        public long Amount { get; set; }
        public string PurchaseID { get; set; }
    }

}

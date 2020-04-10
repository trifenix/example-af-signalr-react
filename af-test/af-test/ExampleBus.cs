using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using System.Text;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace af_test {
    public static class ExampleBus {

        private const string AUTH_HEADER_NAME = "Authorization";
        private const string BEARER_PREFIX = "Bearer ";

        [FunctionName("Test-bus")]
        public static async Task RunBus(
            [ServiceBusTrigger("agroqueue", Connection = "ServiceBusConnectionString", IsSessionsEnabled = true)]Message message,
            [SignalR(HubName = "chat")]IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log) {
            Thread.Sleep(1000);
            log.LogInformation($"----------------------------------------------------------------------------------------");
            log.LogInformation($"Se finite {Encoding.UTF8.GetString(message.Body)} and session : {message.SessionId}");
            log.LogInformation($"----------------------------------------------------------------------------------------");
            await signalRMessages.AddAsync(
                new SignalRMessage {
                    Target = "send",
                    Arguments = new[] { $"Termino {message.SessionId} ejemplo para el cristian" }
                });
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequest req,
            [SignalRConnectionInfo(HubName = "chat")]SignalRConnectionInfo connectionInfo) {
            return connectionInfo;
        }

        //Test with imperative binding
        [FunctionName("MessagesNegotiateBinding")]
        public static IActionResult NegotiatBinding(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1.0/messages/binding/negotiate")] HttpRequest req,
            IBinder binder,
            ILogger log) {
            if (req.Headers.ContainsKey(AUTH_HEADER_NAME) && req.Headers[AUTH_HEADER_NAME].ToString().StartsWith(BEARER_PREFIX)) {
                var token = req.Headers["Authorization"].ToString().Substring(BEARER_PREFIX.Length);
                log.LogInformation("with binding " + token);
                // extract userId from token
                var userId = "userIdExctractedFromToken"; // needs real impl...
                var connectionInfo = binder.Bind<SignalRConnectionInfo>(new SignalRConnectionInfoAttribute { HubName = "chat", UserId = userId });
                log.LogInformation("negotiated " + connectionInfo);
                //https://gist.github.com/ErikAndreas/72c94a0c8a9e6e632f44522c41be8ee7
                // connectionInfo contains an access key token with a name identifier claim set to the authenticated user
                return new OkObjectResult(connectionInfo);                
            }
            else
                return new BadRequestObjectResult("No access token submitted.");
        }

        [FunctionName("SendMessage")]
        public static Task SendMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]object message,
        [SignalR(HubName = "chat")]IAsyncCollector<SignalRMessage> signalRMessages) {
            return signalRMessages.AddAsync(
                new SignalRMessage {
                    Target = "send",
                    Arguments = new[] { "mensaje desde http" }
                });
        }

    }

}
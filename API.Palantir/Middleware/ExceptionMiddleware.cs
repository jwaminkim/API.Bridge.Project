
using Serilog;
using API.Palantir.Models;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace API.Palantir.Middleware
{
    public class ExceptionMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                string requestBody = string.Empty;
                if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
                {
                    requestBody = await ReadRequestBodyAsync(context);
                }

                string errorId = Guid.NewGuid().ToString();
                var errorModel = new ErrorModel
                {
                    Timestamp = DateTime.UtcNow,
                    ErrorId = errorId,
                    RequestPath = context.Request.Path,
                    Method = context.Request.Method,
                    QueryString = context.Request.QueryString.ToString(),
                    RequestBody = requestBody,
                    Source = exception.TargetSite?.DeclaringType?.FullName,
                    StackTrace = exception.StackTrace,
                    Messages = new List<string> { exception.Message }
                };

                if (exception is not CustomException && exception.InnerException != null)
                {
                    while (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }
                }

                if (exception is FluentValidation.ValidationException fluentException)
                {
                    foreach (var error in fluentException.Errors)
                    {
                        errorModel.Messages.Add(error.ErrorMessage);
                    }
                }

                switch (exception)
                {
                    case CustomException e:
                        errorModel.StatusCode = (int)e.StatusCode;
                        if (e.ErrorMessages is not null)
                        {
                            errorModel.Messages = e.ErrorMessages;
                        }

                        break;

                    case KeyNotFoundException:
                        errorModel.StatusCode = (int)HttpStatusCode.NotFound;
                        break;

                    case FluentValidation.ValidationException:
                        errorModel.StatusCode = (int)HttpStatusCode.BadRequest;
                        break;

                    default:
                        errorModel.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                //Log.Error($"{errorResult.Exception} Request failed with Status Code {errorResult.StatusCode} and Error Id {errorId}.");
                /*
                Log.Error("\nStatusCode : " + errorModel.StatusCode.ToString());
                Log.Error("ErrorsId : " + errorModel.ErrorId);
                Log.Error("Message : " + errorModel.Messages);
                Log.Error("StackTrack : " + exception.StackTrace);
                */
                Log.Error(JsonConvert.SerializeObject(errorModel, Formatting.Indented));
                var response = context.Response;
                if (!response.HasStarted)
                {
                    ResponseModel<object> responseModel = new ResponseModel<object>();
                    responseModel.IsSuccess = false;
                    responseModel.ErrorId = errorModel.ErrorId;
                    responseModel.Message = errorModel.Messages;

                    response.ContentType = "application/json";
                    response.StatusCode = errorModel.StatusCode;

                    await response.WriteAsync(JsonConvert.SerializeObject(responseModel, Formatting.Indented));
                }
                else
                {
                    Log.Warning("Can't write error response. Response has already started.");
                }
            }
        }
        private async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                 context.Request.Body,
                 Encoding.UTF8,
                 detectEncodingFromByteOrderMarks: false,
                 bufferSize: 1024,
                 leaveOpen: true);

            string body = await reader.ReadToEndAsync();

            context.Request.Body.Position = 0;

            return body;
        }
    }
}

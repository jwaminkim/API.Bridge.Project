using API.Palantir.Enums;
using API.Palantir.Models.Compare;
using API.Palantir.Models.Discipline;
using API.Palantir.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace API.Palantir.Controllers
{    
    [ApiController]
    [Route("api/palantir")]
    public class PalantirController : ControllerBase
    {
        private readonly PalantirAuthService _auth;
        private readonly PalantirService _service;

        public PalantirController(PalantirAuthService auth, PalantirService service)
        {
            _auth = auth;
            _service = service;
        }
        
        [HttpPost("exceptiontest")]
        public IActionResult Test([FromBody] string dto)
        {
            throw new Exception("ExceptionMiddleware 테스트용");
        }

        [HttpPost("compare")]
        public async Task<IActionResult> Compare([FromBody] List<NoteCompareRequest> request)
        {
            #region test code
            //try
            //{
            //    var result = request;

            //    return Ok(new
            //    {
            //        isSuccess = true,
            //        resultData = result
            //    });
            //}
            //catch (Exception ex)
            //{
            //    Log.Error(ex, "Palantir API Error");
            //    return StatusCode(Convert.ToInt32(HttpStatusCode.InternalServerError), new
            //    {
            //        isSuccess = false,
            //        resultData = "[Palantir Compare API] 처리 중 오류 발생 : " + ex.Message
            //    });
            //}
            #endregion
            #region real code            
            //log주석//Log.Information($"GetTokenAsync start");
            
            var token = await _auth.GetTokenAsync();

            //log주석//Log.Information($"GetTokenAsync end");

            var result = await _service
                .PalantirProcessAndReturnAsync<
                    NoteCompareRequest,
                    NoteCompareResponse>(
                        token,
                        request,
                        PalantirNoteType.Compare);

            return Ok(result);
            #endregion
        }

        [HttpPost("discipline")]
        public async Task<IActionResult> Discipline([FromBody] List<NoteDisciplineRequest> request)
        {
            //log주석//Log.Information($"GetTokenAsync start");

            var token = await _auth.GetTokenAsync();

            //log주석//Log.Information($"GetTokenAsync end");

            var result = await _service
                .PalantirProcessAndReturnAsync<
                    NoteDisciplineRequest,
                    NoteDisciplineResponse>(
                        token,
                        request,
                        PalantirNoteType.Discipline);

            return Ok(result);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
namespace PawPuffPractice.Api.Controllers;

// api/Practice/
[ApiController]
[Route("api/[controller]")]
public class PracticeController : ControllerBase
{
    //ControllerBase 提供 API 常用功能，例如：
    //Ok()
    // BadRequest()
    // Unauthorized()
    // NotFound()
    // Conflict()




    //取得、讀取
    // api/Practice/hello
    [HttpGet("hello")]
    public IActionResult Hello()
    {
        //HTTP 200 OK
        return Ok(new
        {
            mesage = "PawPuff練習API 連線成功"
        });
    }





}
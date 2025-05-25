using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using FaceONNX;
using FaceRecognition.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognition.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaceRecogController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly FaceDetector _detector;
    private readonly FaceEmbedder _embedder;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public FaceRecogController(IWebHostEnvironment env, UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _env = env;
        _userManager = userManager;
        _signInManager = signInManager;
        _detector = new FaceDetector(); // mtcnn.onnx
        _embedder = new FaceEmbedder(); // arcface.onnx
    }

    [HttpPost("verify-image")]
    public async Task<IActionResult> VerifyImage([FromForm] IFormFile uploadedImage, [FromForm] string userId)
    {
        if (uploadedImage == null || string.IsNullOrEmpty(userId))
            return BadRequest("اطلاعات ناقص است.");

        var refImagePath = Path.Combine(_env.ContentRootPath, "UserImages", $"{userId}.jpg");
        if (!System.IO.File.Exists(refImagePath))
            return NotFound("تصویر مرجع یافت نشد.");

        // ذخیره تصویر دریافتی موقتاً
        var uploadedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
        using (var fs = new FileStream(uploadedPath, FileMode.Create))
        {
            await uploadedImage.CopyToAsync(fs);
        }

        // بارگذاری تصاویر به‌صورت Bitmap
        using var refBitmap = new Bitmap(refImagePath);
        using var inputBitmap = new Bitmap(uploadedPath);

        // تشخیص چهره در تصویر مرجع
        var refFaces = _detector.Forward(refBitmap);
        if (refFaces == null || refFaces.Length == 0)
            return BadRequest("چهره‌ای در تصویر مرجع یافت نشد.");

        // تشخیص چهره در تصویر آپلودشده
        var inputFaces = _detector.Forward(inputBitmap);
        if (inputFaces == null || inputFaces.Length == 0)
            return BadRequest("چهره‌ای در تصویر آپلودی یافت نشد.");

        // استخراج چهره‌ها به‌صورت Bitmap
        var refFaceBitmap = CropFace(refBitmap, refFaces[0]);
        var inputFaceBitmap = CropFace(inputBitmap, inputFaces[0]);

        // دریافت بردارهای ویژگی
        var refVector = _embedder.Forward(refFaceBitmap);
        var inputVector = _embedder.Forward(inputFaceBitmap);

        // محاسبه شباهت
        var similarity = CosineSimilarity(refVector, inputVector);

        if (similarity > 0.5f)
        {
            // TODO: صدور توکن اینجا
            return Ok(new { success = true, message = "چهره تایید شد ✅", similarity });
        }

        return Unauthorized(new { success = false, message = "چهره تطبیق ندارد ❌", similarity });
    }

    // ✂️ بریدن چهره از تصویر اصلی
    private Bitmap CropFace(Bitmap original, FaceDetectionResult face)
    {
        var rect = new Rectangle(
            Math.Max(face.Rectangle.X, 0),
            Math.Max(face.Rectangle.Y, 0),
            Math.Min(face.Rectangle.Width, original.Width - face.Rectangle.X),
            Math.Min(face.Rectangle.Height, original.Height - face.Rectangle.Y)
        );

        return original.Clone(rect, original.PixelFormat);
    }

    // 🧠 شباهت کسینوسی
    private static float CosineSimilarity(float[] vec1, float[] vec2)
    {
        float dot = 0f, mag1 = 0f, mag2 = 0f;
        for (int i = 0; i < vec1.Length; i++)
        {
            dot += vec1[i] * vec2[i];
            mag1 += vec1[i] * vec1[i];
            mag2 += vec2[i] * vec2[i];
        }

        return dot / (float)(Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }

    [HttpPost("compare-images")]
    public async Task<IActionResult> CompareImages([FromForm] IFormFile image1, [FromForm] IFormFile image2)
    {
        if (image1 == null || image2 == null)
            return BadRequest("هر دو تصویر باید ارسال شوند.");

        // ذخیره موقت دو تصویر
        var tempPath1 = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
        var tempPath2 = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

        await using (var fs1 = new FileStream(tempPath1, FileMode.Create))
            await image1.CopyToAsync(fs1);

        await using (var fs2 = new FileStream(tempPath2, FileMode.Create))
            await image2.CopyToAsync(fs2);

        using var bmp1 = new Bitmap(tempPath1);
        using var bmp2 = new Bitmap(tempPath2);

        // تشخیص چهره‌ها
        var faces1 = _detector.Forward(bmp1);
        var faces2 = _detector.Forward(bmp2);

        if (faces1 == null || faces1.Length == 0 || faces2 == null || faces2.Length == 0)
            return BadRequest("در یکی از تصاویر چهره‌ای یافت نشد.");

        // بریدن چهره
        var faceBmp1 = CropFace(bmp1, faces1[0]);
        var faceBmp2 = CropFace(bmp2, faces2[0]);

        // بردار ویژگی
        var vector1 = _embedder.Forward(faceBmp1);
        var vector2 = _embedder.Forward(faceBmp2);

        // محاسبه شباهت
        var similarity = CosineSimilarity(vector1, vector2);

        return Ok(new
        {
            similarity,
            match = similarity > 0.5f,
            message = similarity > 0.5f ? "چهره‌ها مشابه‌اند ✅" : "چهره‌ها متفاوت‌اند ❌"
        });
    }


   [HttpPost("FaceCompare")]
public async Task<IActionResult> FaceCompare([FromForm] IFormFile FaceImage, [FromForm] string phoneNumber)
{
    if (string.IsNullOrEmpty(phoneNumber))
        return BadRequest("شماره همراه الزامی است");

    if (FaceImage == null || FaceImage.Length == 0)
        return BadRequest("تصویر چهره الزامی است");

    var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    if (user == null || string.IsNullOrEmpty(user.FaceImagePath))
        return NotFound("کاربر یافت نشد یا عکس ندارد");

    var refImagePath = Path.Combine(_env.WebRootPath, "Images", "UserImages", user.FaceImagePath);
    if (!System.IO.File.Exists(refImagePath))
        return NotFound("تصویر مرجع کاربر یافت نشد");
    if (FaceImage != null && FaceImage.Length > 0)
    {
        if (!Directory.Exists(_env.WebRootPath + @"\Images\" + "TestImages"))
        {
            Directory.CreateDirectory(_env.WebRootPath + @"\Images\" + "TestImages");
        }

        var path = _env.WebRootPath + @"\Images\" + "TestImages" + "\\" + FaceImage.FileName;
        using var f = System.IO.File.Create(path);
        FaceImage.CopyTo(f);
    }
    // ذخیره موقت عکس ورودی
    var uploadedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
    try
    {
        using (var fs = new FileStream(uploadedPath, FileMode.Create))
        {
            await FaceImage.CopyToAsync(fs);
        }

        var detector = new FaceDetector();
        var embedder = new FaceEmbedder();

        using var refBitmap = new Bitmap(refImagePath);
        using var inputBitmap = new Bitmap(uploadedPath);

        var refFaces = detector.Forward(refBitmap);
        var inputFaces = detector.Forward(inputBitmap);

        if (refFaces.Length == 0 || inputFaces.Length == 0)
            return BadRequest("چهره‌ای در تصویر یافت نشد");

        var refVector = embedder.Forward(CropFace(refBitmap, refFaces[0]));
        var inputVector = embedder.Forward(CropFace(inputBitmap, inputFaces[0]));

        var similarity = CosineSimilarity(refVector, inputVector);
        if (similarity >= 0.5f)
        {
            await _signInManager.SignInAsync(user, true);
            return Ok("ورود موفق");
        }

        return Unauthorized("چهره تأیید نشد");
    }
    finally
    {
        // حذف فایل موقت در پایان کار
        if (System.IO.File.Exists(uploadedPath))
        {
            System.IO.File.Delete(uploadedPath);
        }
    }
}


}
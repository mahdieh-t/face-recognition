using FaceRecognition.Entity;
using FaceRecognition.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognition.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _env;

    public AuthController(SignInManager<User> signInManager, UserManager<User> userManager, IWebHostEnvironment env)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _env = env;
    }

    public async Task<IActionResult> Register()
    {
        return View();
    }

    public async Task<IActionResult> Login()
    {
        return View();
    }

    public async Task<IActionResult> RegisterUser(string phoneNumber, string name, IFormFile FaceImage)
    {
        if (await _userManager.Users.AnyAsync(x => x.PhoneNumber == phoneNumber))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = new User
        {
            UserName = phoneNumber,
            PhoneNumber = phoneNumber,
            FullName = name,
            FaceImagePath = FaceImage?.FileName,
        };

        // ذخیره عکس چهره
        if (FaceImage != null && FaceImage.Length > 0)
        {
            if (!Directory.Exists(_env.WebRootPath + @"\Images\" + "UserImages"))
            {
                Directory.CreateDirectory(_env.WebRootPath + @"\Images\" + "UserImages");
            }

            var path = _env.WebRootPath + @"\Images\" + "UserImages" + "\\" + FaceImage.FileName;
            using var f = System.IO.File.Create(path);
            FaceImage.CopyTo(f);
        }

        var result = await _userManager.CreateAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction("Login", "Auth");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return RedirectToAction("Login");
    }


    public async Task<IActionResult> Step1_Login(string phoneNumber)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
            return NotFound("کاربر یافت نشد");

        // ذخیره شماره در Session یا TempData
        TempData["PhoneNumber"] = phoneNumber;
        return RedirectToAction("Step2_FaceVerification", "Auth", new { phoneNumber = phoneNumber });
    }

    public async Task<IActionResult> Step2_FaceVerification(string phoneNumber)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
            return NotFound("کاربر یافت نشد");
        return View();
    }
}
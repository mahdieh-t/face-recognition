using System.Diagnostics;
using FaceRecognition.Entity;
using Microsoft.AspNetCore.Mvc;
using FaceRecognition.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognition.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<User> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<User> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            ViewBag.user = await _userManager.GetUserAsync(User);
            return View();
        }
        else
        {
            return RedirectToAction("Login", "Auth");
        }
    }

    public async Task<IActionResult> Users()
    {
        if (User.Identity.IsAuthenticated)
        {
            ViewBag.users = await _userManager.Users.ToListAsync();
            return View();
        }
        else
        {
            return RedirectToAction("Login", "Auth");
        }
    }
}
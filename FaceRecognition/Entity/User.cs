using Microsoft.AspNetCore.Identity;

namespace FaceRecognition.Entity;

public class User:IdentityUser<int>
{
    public string FullName { get; set; }=string.Empty;
    public string FaceImagePath { get; set; }=string.Empty; // مسیر عکس چهره
}
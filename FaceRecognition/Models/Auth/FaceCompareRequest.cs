using System.ComponentModel.DataAnnotations;

namespace FaceRecognition.Models.Auth;

public class z  
{
    public IFormFile FaceImage { get; set; } 
    [Required]
    public string PhoneNumber { get; set; }= string.Empty;
}
using FaceRecognition.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognition.Database;

public class FaceRecognitionDbContext:IdentityDbContext<User,Role,int>
{
    public FaceRecognitionDbContext(DbContextOptions<FaceRecognitionDbContext> options) : base(options)
    {
    }

    public FaceRecognitionDbContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Data Source=185.165.118.72;Initial Catalog=ParsTest11233;User ID=dev;Password=4$433qfJv;Trust Server Certificate=True"
        );
        base.OnConfiguring(optionsBuilder);
    }

}
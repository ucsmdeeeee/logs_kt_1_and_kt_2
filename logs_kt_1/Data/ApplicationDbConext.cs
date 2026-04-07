using Microsoft.EntityFrameworkCore;
using logs_kt_1.Models;

namespace logs_kt_1.Data
{
    public class ApplicationDbConext : DbContext
    {
        public DbSet<Book> Books { get; set; }

        public ApplicationDbConext(DbContextOptions<ApplicationDbConext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>().HasKey(b => b.Id);
        } 
    }
}

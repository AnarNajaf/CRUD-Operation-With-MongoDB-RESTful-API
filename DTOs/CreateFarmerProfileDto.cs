using iTarlaMapBackend.Models;

namespace iTarlaMapBackend.DTOs
{
    
public class CreateFarmerProfileDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; }= null!;
    public string PhoneNumber { get; set; }=null!;
    public string Address { get; set; }
}

}
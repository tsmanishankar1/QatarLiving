using System.ComponentModel.DataAnnotations;

public class CreateDeviceDto
{
    [Required]
    [StringLength(int.MaxValue, MinimumLength = 1)]
    public string FcmToken { get; set; }

    [Required]
    [StringLength(int.MaxValue, MinimumLength = 1)]
    public string DeviceType { get; set; } // 'android' | 'ios' | 'web'

    [Required]
    [StringLength(int.MaxValue, MinimumLength = 1)]
    public string DeviceId { get; set; }

    [StringLength(int.MaxValue, MinimumLength = 1)]
    public string? DeviceName { get; set; }

    [StringLength(int.MaxValue, MinimumLength = 1)]
    public string? DeviceModel { get; set; }
}
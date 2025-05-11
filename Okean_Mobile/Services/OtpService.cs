using System.Security.Cryptography;

namespace Okean_Mobile.Services
{
    public interface IOtpService
    {
        string GenerateOtp();
        bool ValidateOtp(string storedOtp, string inputOtp);
    }

    public class OtpService : IOtpService
    {
        public string GenerateOtp()
        {
            // Tạo OTP 6 chữ số
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        public bool ValidateOtp(string storedOtp, string inputOtp)
        {
            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(inputOtp))
                return false;

            return storedOtp.Equals(inputOtp);
        }
    }
} 
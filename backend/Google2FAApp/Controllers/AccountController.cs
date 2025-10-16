using Microsoft.AspNetCore.Mvc;
using OtpNet;
using QRCoder;
using System.IO;
using Google2FAApp.Data;
using Google2FAApp.Models;

namespace Google2FAApp.Controllers
{
    public class AccountController : Controller
    {
        // -----------------------------
        // STEP 1: Show Login Page
        // -----------------------------
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = FakeDatabase.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user == null)
            {
                ViewBag.Error = "Invalid credentials";
                return View();
            }

            // Store email in TempData for session tracking
            TempData["UserEmail"] = user.Email;

            if (user.Is2FAEnabled)
            {
                // Redirect to OTP verification page if 2FA is enabled
                return RedirectToAction("VerifyLoginOtp");
            }

            return RedirectToAction("Home");
        }

        // -----------------------------
        // STEP 2: Show Register Page
        // -----------------------------
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string email, string password)
        {
            // Simple in-memory user registration
            FakeDatabase.Users.Add(new User { Email = email, Password = password });
            return RedirectToAction("Login");
        }

        // -----------------------------
        // STEP 3: Home Page
        // -----------------------------
        [HttpGet]
        public IActionResult Home()
        {
            var email = TempData["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            // Keep TempData alive for next requests
            TempData.Keep("UserEmail");

            ViewBag.Email = email;
            return View();
        }

        // -----------------------------
        // STEP 4: Enable 2FA (show QR and secret key)
        // -----------------------------
        [HttpGet]
        public IActionResult Enable2FA()
        {
            var email = TempData["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            TempData.Keep("UserEmail");

            var user = FakeDatabase.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login");

            // Generate secret key for Google Authenticator
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            var secretKeyString = Base32Encoding.ToString(secretKey);
            user.TwoFASecret = secretKeyString;

            // Generate QR code
            string otpAuthUrl = $"otpauth://totp/{email}?secret={secretKeyString}&issuer=Google2FAApp";
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var qrBitmap = qrCode.GetGraphic(20);

            // Convert QR bitmap to Base64 string for HTML
            using var ms = new MemoryStream();
            qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            string qrImage = Convert.ToBase64String(ms.ToArray());

            ViewBag.QrCodeImage = qrImage;
            ViewBag.SecretKey = secretKeyString;

            return View();
        }

        // -----------------------------
        // STEP 5: Verify OTP during 2FA setup
        // -----------------------------
        [HttpPost]
        public IActionResult Verify2FA(string otp)
        {
            TempData.Keep("UserEmail");
            var email = TempData["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var user = FakeDatabase.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login");

            var totp = new Totp(Base32Encoding.ToBytes(user.TwoFASecret!));
            bool isValid = totp.VerifyTotp(otp, out _, new VerificationWindow(1, 1));

            if (isValid)
            {
                user.Is2FAEnabled = true;
                return RedirectToAction("Home");
            }

            ViewBag.Error = "Invalid OTP code.";
            ViewBag.QrCodeImage = null;
            ViewBag.SecretKey = user.TwoFASecret;
            return View("Enable2FA");
        }

        // -----------------------------
        // STEP 6: Show login OTP verification page
        // -----------------------------
        [HttpGet]
        public IActionResult VerifyLoginOtp()
        {
            TempData.Keep("UserEmail");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyLoginOtp(string otp)
        {
            TempData.Keep("UserEmail");
            var email = TempData["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var user = FakeDatabase.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login");

            var totp = new Totp(Base32Encoding.ToBytes(user.TwoFASecret!));
            bool isValid = totp.VerifyTotp(otp, out _, new VerificationWindow(1, 1));

            if (isValid) return RedirectToAction("Home");

            ViewBag.Error = "Invalid OTP code.";
            return View();
        }

        // -----------------------------
        // STEP 7: Disable 2FA
        // -----------------------------
        [HttpPost]
        public IActionResult Disable2FA()
        {
            TempData.Keep("UserEmail");
            var email = TempData["UserEmail"]?.ToString();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var user = FakeDatabase.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                user.Is2FAEnabled = false;
                user.TwoFASecret = null;
            }

            return RedirectToAction("Home");
        }
    }
}

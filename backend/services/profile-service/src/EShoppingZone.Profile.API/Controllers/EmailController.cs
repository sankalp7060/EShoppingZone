using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace EShoppingZone.Profile.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly ILogger<EmailController> _logger;
        private readonly IConfiguration _configuration;

        public EmailController(
            ILogger<EmailController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOTP([FromBody] SendOTPRequest request)
        {
            try
            {
                // Get SMTP settings from configuration
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var appPassword = _configuration["EmailSettings:AppPassword"];

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
                {
                    _logger.LogError("Email settings are not properly configured");
                    return StatusCode(500, new { success = false, message = "Email service not configured" });
                }

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Password Reset OTP - EShoppingZone",
                    Body = GenerateOTPEmailTemplate(request.Otp),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(request.Email);

                // Configure SMTP client
                using var smtpClient = new SmtpClient(smtpServer, port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // 30 seconds timeout
                };

                // Send email
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("OTP sent successfully to {Email}", request.Email);
                return Ok(new { success = true, message = "OTP sent successfully" });
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending OTP to {Email}: {StatusCode}", request.Email, smtpEx.StatusCode);
                
                // Provide more specific error messages
                string errorMessage = smtpEx.StatusCode switch
                {
                    SmtpStatusCode.MailboxBusy => "Mailbox is busy, please try again",
                    SmtpStatusCode.MailboxUnavailable => "Mailbox unavailable",
                    SmtpStatusCode.ExceededStorageAllocation => "Mailbox full",
                    _ => "Failed to send email. Please check SMTP configuration"
                };
                
                return StatusCode(500, new { success = false, message = errorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP email to {Email}", request.Email);
                return StatusCode(500, new { success = false, message = $"Failed to send email: {ex.Message}" });
            }
        }

        private string GenerateOTPEmailTemplate(string otp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset OTP</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}
        .container {{
            max-width: 500px;
            margin: 20px auto;
            background: white;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 30px 20px;
            text-align: center;
        }}
        .logo {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .title {{
            color: white;
            font-size: 28px;
            font-weight: bold;
            margin: 0;
        }}
        .subtitle {{
            color: rgba(255,255,255,0.9);
            font-size: 14px;
            margin-top: 8px;
        }}
        .content {{
            padding: 40px 30px;
            text-align: center;
        }}
        .greeting {{
            font-size: 18px;
            color: #333;
            margin-bottom: 20px;
        }}
        .message {{
            color: #666;
            font-size: 16px;
            margin-bottom: 25px;
        }}
        .otp-code {{
            font-size: 42px;
            font-weight: bold;
            color: #667eea;
            letter-spacing: 12px;
            padding: 20px;
            background: #f8f9ff;
            border-radius: 12px;
            margin: 25px 0;
            font-family: 'Courier New', monospace;
            border: 2px dashed #e0e0e0;
        }}
        .warning {{
            background: #fff3e0;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            font-size: 13px;
            color: #856404;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #999;
            border-top: 1px solid #eee;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background: #667eea;
            color: white;
            text-decoration: none;
            border-radius: 6px;
            margin-top: 20px;
            font-weight: 500;
        }}
        @media only screen and (max-width: 600px) {{
            .container {{
                margin: 10px;
                border-radius: 12px;
            }}
            .content {{
                padding: 25px 20px;
            }}
            .otp-code {{
                font-size: 32px;
                letter-spacing: 8px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🛍️</div>
            <h1 class='title'>EShoppingZone</h1>
            <div class='subtitle'>Your Trusted Shopping Partner</div>
        </div>
        <div class='content'>
            <div class='greeting'>Hello,</div>
            <div class='message'>
                We received a request to reset your password. Use the following OTP code to proceed:
            </div>
            <div class='otp-code'>
                {otp}
            </div>
            <div class='message'>
                This OTP is valid for <strong>10 minutes</strong>.
            </div>
            <div class='warning'>
                ⚠️ <strong>Security Alert:</strong> Never share this OTP with anyone. Our support team will never ask for your OTP.
            </div>
            <div class='message'>
                If you didn't request this, please ignore this email. Your password will remain unchanged.
            </div>
        </div>
        <div class='footer'>
            <p>&copy; 2025 EShoppingZone. All rights reserved.</p>
            <p style='margin-top: 10px;'>
                <small>This is an automated message, please do not reply to this email.</small>
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }

    public class SendOTPRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
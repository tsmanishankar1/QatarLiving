using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomException
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException(string message = "Invalid credentials") : base(message) { }
    }

    public class TwoFactorRequiredException : Exception
    {
        public ApplicationUser User { get; }

        public TwoFactorRequiredException(ApplicationUser user) : base("Two-Factor Authentication is required.")
        {
            User = user;
        }
    }

    public class EmailNotVerifiedException : Exception
    {
        public EmailNotVerifiedException(string message = "Email not verified.") : base(message) { }
    }

    public class PhoneNotVerifiedException : Exception
    {
        public PhoneNotVerifiedException(string message = "Phone number not verified.") : base(message) { }
    }

    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message = "User not found.") : base(message) { }
    }

    public class RegistrationConflictException : Exception
    {
        public RegistrationConflictException(string message) : base(message) { }
    }

    public class VerificationRequiredException : Exception
    {
        public VerificationRequiredException()
            : base("Please verify your Email and Phone Number before registering.") { }
    }

    public class EmailAlreadyRegisteredException : Exception
    {
        public EmailAlreadyRegisteredException()
            : base("This email is already registered.") { }
    }
    public class UsernameTakenException : Exception
    {
        public UsernameTakenException(string username)
            : base($"Username '{username}' is already taken.") { }
    }

    public class InvalidMobileFormatException : Exception
    {
        public InvalidMobileFormatException()
            : base("Invalid mobile number format. Please enter a valid 10 to 15 digits.") { }
    }
    public class InvalidEmailFormatException : Exception
    {
        public InvalidEmailFormatException()
            : base("Invalid email format.") { }
    }
    public class RegistrationValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public RegistrationValidationException(IDictionary<string, string[]> errors)
            : base("Validation failed during registration.")
        {
            Errors = errors;
        }
    }

    public class OtpNotRequestedException : Exception
    {
        public OtpNotRequestedException() : base("OTP not requested or expired.") { }
    }

    public class InvalidOtpException : Exception
    {
        public InvalidOtpException() : base("Invalid or expired OTP.") { }
    }

    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message = "Invalid or expired token.") : base(message) { }
    }

    public class UserEmailNotFoundException : Exception
    {
        public UserEmailNotFoundException(string message = "User with this email is not registered or email not confirmed.") : base(message) { }
    }

    public class PasswordResetValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public PasswordResetValidationException(IDictionary<string, string[]> errors)
            : base("Password reset validation failed.")
        {
            Errors = errors;
        }
    }


    public class PhoneAlreadyRegisteredException : Exception
    {
        public PhoneAlreadyRegisteredException(string message = "Phone number already registered.") : base(message) { }
    }

    public class SmsSendingFailedException : Exception
    {
        public SmsSendingFailedException(string message = "Failed to send OTP via SMS. Please try again later.") : base(message) { }
    }
    public class PhoneOtpMissingException : Exception
    {
        public PhoneOtpMissingException(string message = "OTP not requested or expired.") : base(message) { }
    }

    public class InvalidPhoneOtpException : Exception
    {
        public InvalidPhoneOtpException(string message = "Invalid or expired OTP.") : base(message) { }
    }

    public class ForgotPasswordUserNotFoundException : Exception
    {
        public ForgotPasswordUserNotFoundException(string message = "User is not registered with this email or email is not confirmed.")
            : base(message) { }
    }
    public class ResetPasswordUserNotFoundException : Exception
    {
        public ResetPasswordUserNotFoundException(string message = "User not found or email not confirmed.") : base(message) { }
    }

    public class ResetPasswordInvalidTokenException : Exception
    {
        public ResetPasswordInvalidTokenException(string message = "Invalid or expired token.") : base(message) { }
    }

    public class SearchValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public SearchValidationException(IDictionary<string, string[]> errors)
        {
            Errors = errors;
        }
    }
    public class SaveSearchException : Exception
    {
        public SaveSearchException(string message) : base(message) { }
    }

    public class GetSearchesException : Exception
    {
        public GetSearchesException(string message) : base(message) { }
    }

}

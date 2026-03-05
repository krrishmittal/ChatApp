using ChatApp.Application.DTOs.Request;
using FluentValidation;

namespace ChatApp.Application.Validators;
public class RegisterRequestValidator: AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full Name is required")
            .MaximumLength(100).WithMessage("Full Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid Email Id");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be atleast 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain atleast one uppercase character")
            .Matches("[a-z]").WithMessage("Password must contain atleast one lowercase character")
            .Matches("[0-9]").WithMessage("Password must contain atleast one digit")
            .Matches(@"[\W]").WithMessage("Password must contain at least one special character");


        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm Password is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x=>x.ProfilePicture)
            .Must(file=>file==null || file.Length <= 5 * 1024 * 1024).WithMessage("Profile picture must be less than 5MB")
            .Must(file=>file==null || new[] {"image/jpeg","image/png", "image/webp"}.Contains(file.ContentType)).WithMessage("Only JPEG, PNG and WEBP formats are allowed");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");

        //RuleFor(x => x.CaptchaToken)
        //    .NotEmpty().WithMessage("Captcha token is required.");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(6).WithMessage("OTP must be 6 digits.")
            .Matches("^[0-9]+$").WithMessage("OTP must be numeric.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID token is required.");
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full Name is required.")
            .MaximumLength(100).WithMessage("Full Name must not exceed 100 characters.");
    }
}

public class UpdateProfilePictureRequestValidator : AbstractValidator<UpdateProfilePictureRequest>
{
    public UpdateProfilePictureRequestValidator()
    {
        RuleFor(x => x.ProfilePicture)
            .NotNull().WithMessage("Profile picture is required.")
            .Must(file => file != null && file.Length <= 5 * 1024 * 1024)
                .WithMessage("Profile picture must be less than 5MB.")
            .Must(file => file != null && new[] { "image/jpeg", "image/png", "image/webp" }.Contains(file.ContentType))
                .WithMessage("Only JPEG, PNG and WEBP formats are allowed.");
    }
}